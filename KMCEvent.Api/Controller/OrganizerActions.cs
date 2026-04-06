using KMCEvent.Api.Data;
using KMCEvent.Api.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using EventRecord = KMCEvent.Api.Model.Blanket;

namespace KMCEvent.Api.Controller
{
    public class OrganizerActions
    {
        private readonly DatabaseManager _db = new DatabaseManager();

        public List<EventRecord> GetAllEvents()
        {
            var cmd = new SqlCommand(@"
                SELECT
                    e.EventID,
                    e.Title,
                    e.EventType,
                    e.Description,
                    e.TicketPrice,
                    e.EventDate,
                    e.Venue,
                    e.Capacity,
                    e.CreatedByUserID,
                    u.Username AS OrganizerName,
                    (e.Capacity - ISNULL(r.TotalSeats, 0)) AS SeatsRemaining
                FROM Events e
                INNER JOIN Users u ON u.UserID = e.CreatedByUserID
                LEFT JOIN (
                    SELECT EventID, SUM(SeatCount) AS TotalSeats
                    FROM EventRegistrations
                    GROUP BY EventID
                ) r ON r.EventID = e.EventID
                WHERE e.IsActive = 1
                ORDER BY e.EventDate, e.Title;");

            return MapDataTableToEvents(_db.Execute(cmd));
        }

        public List<EventRecord> GetEventsByOrganizer(int organizerId)
        {
            var cmd = new SqlCommand(@"
                SELECT
                    e.EventID,
                    e.Title,
                    e.EventType,
                    e.Description,
                    e.TicketPrice,
                    e.EventDate,
                    e.Venue,
                    e.Capacity,
                    e.CreatedByUserID,
                    u.Username AS OrganizerName,
                    (e.Capacity - ISNULL(r.TotalSeats, 0)) AS SeatsRemaining
                FROM Events e
                INNER JOIN Users u ON u.UserID = e.CreatedByUserID
                LEFT JOIN (
                    SELECT EventID, SUM(SeatCount) AS TotalSeats
                    FROM EventRegistrations
                    GROUP BY EventID
                ) r ON r.EventID = e.EventID
                WHERE e.IsActive = 1 AND e.CreatedByUserID = @organizerId
                ORDER BY e.EventDate, e.Title;");
            cmd.Parameters.AddWithValue("@organizerId", organizerId);

            return MapDataTableToEvents(_db.Execute(cmd));
        }

        public int CreateEvent(EventRecord eventRecord)
        {
            ValidateEventPayload(eventRecord, false);

            var cmd = new SqlCommand(@"
                INSERT INTO Events
                    (Title, EventType, Description, Venue, EventDate, TicketPrice, Capacity, CreatedByUserID, IsActive, CreatedAt)
                VALUES
                    (@title, @eventType, @description, @venue, @eventDate, @ticketPrice, @capacity, @createdByUserId, 1, SYSUTCDATETIME());

                SELECT CAST(SCOPE_IDENTITY() AS INT);");

            cmd.Parameters.AddWithValue("@title", eventRecord.Title.Trim());
            cmd.Parameters.AddWithValue("@eventType", string.IsNullOrWhiteSpace(eventRecord.EventType) ? "General" : eventRecord.EventType.Trim());
            cmd.Parameters.AddWithValue("@description", string.IsNullOrWhiteSpace(eventRecord.Description) ? string.Empty : eventRecord.Description.Trim());
            cmd.Parameters.AddWithValue("@venue", string.IsNullOrWhiteSpace(eventRecord.Venue) ? "TBD" : eventRecord.Venue.Trim());
            cmd.Parameters.AddWithValue("@eventDate", NormalizeEventDate(eventRecord.EventDate));
            cmd.Parameters.AddWithValue("@ticketPrice", eventRecord.TicketPrice < 0 ? 0 : eventRecord.TicketPrice);
            cmd.Parameters.AddWithValue("@capacity", eventRecord.Capacity <= 0 ? 100 : eventRecord.Capacity);
            cmd.Parameters.AddWithValue("@createdByUserId", eventRecord.CreatedByUserID);

            var createdId = Convert.ToInt32(_db.ExecuteScalar(cmd));
            eventRecord.EventID = createdId;
            return createdId;
        }

        public void UpdateEvent(EventRecord eventRecord)
        {
            ValidateEventPayload(eventRecord, true);

            var cmd = new SqlCommand(@"
                UPDATE Events
                SET
                    Title = @title,
                    EventType = @eventType,
                    Description = @description,
                    Venue = @venue,
                    EventDate = @eventDate,
                    TicketPrice = @ticketPrice,
                    Capacity = @capacity,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE EventID = @eventId
                  AND CreatedByUserID = @createdByUserId
                  AND IsActive = 1;");

            cmd.Parameters.AddWithValue("@eventId", eventRecord.EventID);
            cmd.Parameters.AddWithValue("@createdByUserId", eventRecord.CreatedByUserID);
            cmd.Parameters.AddWithValue("@title", eventRecord.Title.Trim());
            cmd.Parameters.AddWithValue("@eventType", string.IsNullOrWhiteSpace(eventRecord.EventType) ? "General" : eventRecord.EventType.Trim());
            cmd.Parameters.AddWithValue("@description", string.IsNullOrWhiteSpace(eventRecord.Description) ? string.Empty : eventRecord.Description.Trim());
            cmd.Parameters.AddWithValue("@venue", string.IsNullOrWhiteSpace(eventRecord.Venue) ? "TBD" : eventRecord.Venue.Trim());
            cmd.Parameters.AddWithValue("@eventDate", NormalizeEventDate(eventRecord.EventDate));
            cmd.Parameters.AddWithValue("@ticketPrice", eventRecord.TicketPrice < 0 ? 0 : eventRecord.TicketPrice);
            cmd.Parameters.AddWithValue("@capacity", eventRecord.Capacity <= 0 ? 100 : eventRecord.Capacity);

            var affectedRows = _db.ExecuteNonQuery(cmd);
            if (affectedRows == 0)
            {
                throw new UnauthorizedAccessException("Only the event creator can update this event.");
            }
        }

        public void ArchiveEvent(int eventId)
        {
            var cmd = new SqlCommand(@"
                UPDATE Events
                SET IsActive = 0, UpdatedAt = SYSUTCDATETIME()
                WHERE EventID = @eventId;");
            cmd.Parameters.AddWithValue("@eventId", eventId);
            _db.ExecuteNonQuery(cmd);
        }

        public bool DeleteEvent(int eventId, int organizerId)
        {
            var cmd = new SqlCommand(@"
                UPDATE Events
                SET IsActive = 0, UpdatedAt = SYSUTCDATETIME()
                WHERE EventID = @eventId
                  AND CreatedByUserID = @organizerId;");
            cmd.Parameters.AddWithValue("@eventId", eventId);
            cmd.Parameters.AddWithValue("@organizerId", organizerId);
            return _db.ExecuteNonQuery(cmd) > 0;
        }

        private static DateTime NormalizeEventDate(DateTime eventDate)
        {
            return eventDate == DateTime.MinValue ? DateTime.Today : eventDate;
        }

        private static void ValidateEventPayload(EventRecord eventRecord, bool requireId)
        {
            if (eventRecord == null)
            {
                throw new ArgumentException("Event payload is required.");
            }

            if (requireId && eventRecord.EventID <= 0)
            {
                throw new ArgumentException("A valid event ID is required.");
            }

            if (string.IsNullOrWhiteSpace(eventRecord.Title))
            {
                throw new ArgumentException("Event title is required.");
            }

            if (eventRecord.CreatedByUserID <= 0)
            {
                throw new ArgumentException("CreatedByUserID is required.");
            }
        }

        private List<EventRecord> MapDataTableToEvents(DataTable dt)
        {
            var events = new List<EventRecord>();
            foreach (DataRow row in dt.Rows)
            {
                events.Add(new EventRecord
                {
                    EventID = Convert.ToInt32(row["EventID"]),
                    Title = row["Title"].ToString(),
                    EventType = row["EventType"].ToString(),
                    Description = row["Description"] == DBNull.Value ? string.Empty : row["Description"].ToString(),
                    TicketPrice = row["TicketPrice"] == DBNull.Value ? 0 : Convert.ToDecimal(row["TicketPrice"]),
                    EventDate = row["EventDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["EventDate"]),
                    Venue = row["Venue"] == DBNull.Value ? string.Empty : row["Venue"].ToString(),
                    Capacity = row["Capacity"] == DBNull.Value ? 0 : Convert.ToInt32(row["Capacity"]),
                    CreatedByUserID = row["CreatedByUserID"] == DBNull.Value ? 0 : Convert.ToInt32(row["CreatedByUserID"]),
                    OrganizerName = row["OrganizerName"] == DBNull.Value ? string.Empty : row["OrganizerName"].ToString(),
                    SeatsRemaining = row["SeatsRemaining"] == DBNull.Value ? 0 : Convert.ToInt32(row["SeatsRemaining"])
                });
            }

            return events;
        }
    }
}
