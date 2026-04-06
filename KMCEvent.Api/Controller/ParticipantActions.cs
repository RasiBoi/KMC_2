using KMCEvent.Api.Data;
using KMCEvent.Api.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using RegistrationRecord = KMCEvent.Api.Model.Inventory;

namespace KMCEvent.Api.Controller
{
    public class ParticipantActions
    {
        private readonly DatabaseManager _db = new DatabaseManager();

        public List<RegistrationRecord> GetParticipantRegistrations(int participantId)
        {
            var cmd = new SqlCommand(@"
                SELECT
                    r.RegistrationID,
                    e.EventID,
                    e.Title AS EventTitle,
                    r.ParticipantUserID,
                    r.SeatCount,
                    e.EventType,
                    e.EventDate,
                    e.Venue,
                    r.RegisteredAt
                FROM EventRegistrations r
                INNER JOIN Events e ON e.EventID = r.EventID
                WHERE r.ParticipantUserID = @participantId
                  AND e.IsActive = 1
                ORDER BY e.EventDate, e.Title;");
            cmd.Parameters.AddWithValue("@participantId", participantId);
            return MapDataTableToRegistrationList(_db.Execute(cmd));
        }

        public List<RegistrationRecord> SearchEventsForParticipant(int participantId, string query)
        {
            string keyword;
            string eventType;
            DateTime? eventDate;
            ParseSearchQuery(query, out keyword, out eventType, out eventDate);

            var cmd = new SqlCommand(@"
                SELECT
                    e.EventID AS RegistrationID,
                    e.EventID,
                    e.Title AS EventTitle,
                    @participantId AS ParticipantUserID,
                    (e.Capacity - ISNULL(r.TotalSeats, 0)) AS SeatCount,
                    e.EventType,
                    e.EventDate,
                    e.Venue,
                    CAST('1900-01-01' AS DATETIME) AS RegisteredAt
                FROM Events e
                LEFT JOIN (
                    SELECT EventID, SUM(SeatCount) AS TotalSeats
                    FROM EventRegistrations
                    GROUP BY EventID
                ) r ON r.EventID = e.EventID
                WHERE e.IsActive = 1
                  AND (e.Capacity - ISNULL(r.TotalSeats, 0)) > 0
                  AND (@keyword = ''
                       OR e.Title LIKE @keywordLike
                       OR e.EventType LIKE @keywordLike
                       OR e.Description LIKE @keywordLike
                       OR e.Venue LIKE @keywordLike
                       OR CONVERT(VARCHAR(10), e.EventDate, 120) LIKE @keywordLike)
                  AND (@eventType = '' OR e.EventType = @eventType)
                  AND (@eventDate IS NULL OR CAST(e.EventDate AS DATE) = @eventDate)
                ORDER BY e.EventDate, e.Title;");

            cmd.Parameters.AddWithValue("@participantId", participantId);
            cmd.Parameters.AddWithValue("@keyword", keyword);
            cmd.Parameters.AddWithValue("@keywordLike", "%" + keyword + "%");
            cmd.Parameters.AddWithValue("@eventType", eventType);
            var dateParameter = cmd.Parameters.Add("@eventDate", SqlDbType.Date);
            dateParameter.Value = eventDate.HasValue ? (object)eventDate.Value.Date : DBNull.Value;

            return MapDataTableToRegistrationList(_db.Execute(cmd));
        }

        public void UpdateRegistrationSeatCount(int registrationId, int seatCount)
        {
            if (seatCount <= 0)
            {
                throw new ArgumentException("Seat count must be greater than zero.");
            }

            var cmd = new SqlCommand(@"
                UPDATE EventRegistrations
                SET SeatCount = @seatCount,
                    RegisteredAt = SYSUTCDATETIME()
                WHERE RegistrationID = @registrationId;");
            cmd.Parameters.AddWithValue("@registrationId", registrationId);
            cmd.Parameters.AddWithValue("@seatCount", seatCount);
            _db.ExecuteNonQuery(cmd);
        }

        public void RegisterForEvent(int participantId, int eventId, int seatCount)
        {
            if (participantId <= 0)
            {
                throw new ArgumentException("A valid participant ID is required.");
            }

            if (eventId <= 0)
            {
                throw new ArgumentException("A valid event ID is required.");
            }

            if (seatCount <= 0)
            {
                throw new ArgumentException("Seat count must be greater than zero.");
            }

            // Transaction ensures seat validation and registration update happen atomically.
            using (var con = new SqlConnection(_db.GetConnectionString()))
            {
                con.Open();
                var transaction = con.BeginTransaction();
                try
                {
                    var availabilityCommand = new SqlCommand(@"
                        SELECT e.Capacity - ISNULL(SUM(r.SeatCount), 0) AS SeatsRemaining
                        FROM Events e
                        LEFT JOIN EventRegistrations r ON r.EventID = e.EventID
                        WHERE e.EventID = @eventId
                          AND e.IsActive = 1
                        GROUP BY e.Capacity;", con, transaction);
                    availabilityCommand.Parameters.AddWithValue("@eventId", eventId);

                    var availabilityResult = availabilityCommand.ExecuteScalar();
                    if (availabilityResult == null)
                    {
                        throw new InvalidOperationException("The selected event was not found or is inactive.");
                    }

                    var seatsRemaining = Convert.ToInt32(availabilityResult);
                    if (seatsRemaining < seatCount)
                    {
                        throw new InvalidOperationException("Not enough seats are available for this event.");
                    }

                    var upsertCommand = new SqlCommand(@"
                        IF EXISTS (SELECT 1 FROM EventRegistrations WHERE EventID = @eventId AND ParticipantUserID = @participantId)
                            UPDATE EventRegistrations
                            SET SeatCount = SeatCount + @seatCount,
                                RegisteredAt = SYSUTCDATETIME()
                            WHERE EventID = @eventId
                              AND ParticipantUserID = @participantId;
                        ELSE
                            INSERT INTO EventRegistrations (EventID, ParticipantUserID, SeatCount, RegisteredAt)
                            VALUES (@eventId, @participantId, @seatCount, SYSUTCDATETIME());", con, transaction);
                    upsertCommand.Parameters.AddWithValue("@eventId", eventId);
                    upsertCommand.Parameters.AddWithValue("@participantId", participantId);
                    upsertCommand.Parameters.AddWithValue("@seatCount", seatCount);
                    upsertCommand.ExecuteNonQuery();

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private static void ParseSearchQuery(string query, out string keyword, out string eventType, out DateTime? eventDate)
        {
            keyword = string.Empty;
            eventType = string.Empty;
            eventDate = null;

            if (string.IsNullOrWhiteSpace(query))
            {
                return;
            }

            var keywordParts = new List<string>();
            var tokens = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                if (token.StartsWith("type:", StringComparison.OrdinalIgnoreCase))
                {
                    eventType = token.Substring(5).Trim().Replace("_", " ");
                    continue;
                }

                if (token.StartsWith("date:", StringComparison.OrdinalIgnoreCase))
                {
                    DateTime parsedDate;
                    if (DateTime.TryParse(token.Substring(5).Trim(), out parsedDate))
                    {
                        eventDate = parsedDate.Date;
                    }
                    continue;
                }

                DateTime directDate;
                if (DateTime.TryParse(token, out directDate))
                {
                    eventDate = directDate.Date;
                }
                else
                {
                    keywordParts.Add(token);
                }
            }

            keyword = string.Join(" ", keywordParts).Trim();
        }

        private List<RegistrationRecord> MapDataTableToRegistrationList(DataTable dt)
        {
            var list = new List<RegistrationRecord>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new RegistrationRecord
                {
                    RegistrationID = row["RegistrationID"] == DBNull.Value ? 0 : Convert.ToInt32(row["RegistrationID"]),
                    EventID = row["EventID"] == DBNull.Value ? 0 : Convert.ToInt32(row["EventID"]),
                    EventTitle = row["EventTitle"] == DBNull.Value ? string.Empty : row["EventTitle"].ToString(),
                    ParticipantUserID = row["ParticipantUserID"] == DBNull.Value ? 0 : Convert.ToInt32(row["ParticipantUserID"]),
                    SeatCount = row["SeatCount"] == DBNull.Value ? 0 : Convert.ToInt32(row["SeatCount"]),
                    EventType = row.Table.Columns.Contains("EventType") && row["EventType"] != DBNull.Value ? row["EventType"].ToString() : string.Empty,
                    EventDate = row.Table.Columns.Contains("EventDate") && row["EventDate"] != DBNull.Value ? Convert.ToDateTime(row["EventDate"]) : DateTime.MinValue,
                    Venue = row.Table.Columns.Contains("Venue") && row["Venue"] != DBNull.Value ? row["Venue"].ToString() : string.Empty,
                    RegisteredOn = row.Table.Columns.Contains("RegisteredOn") && row["RegisteredOn"] != DBNull.Value
                        ? Convert.ToDateTime(row["RegisteredOn"])
                        : row.Table.Columns.Contains("RegisteredAt") && row["RegisteredAt"] != DBNull.Value
                            ? Convert.ToDateTime(row["RegisteredAt"])
                            : DateTime.MinValue
                });
            }
            return list;
        }
    }
}
