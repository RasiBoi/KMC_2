using KMCEvent.Api.Data;
using KMCEvent.Api.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using EventRecord = KMCEvent.Api.Model.Blanket;

namespace KMCEvent.Api.Controller
{
    public class EventCatalogActions
    {
        private readonly DatabaseManager _db = new DatabaseManager();

        public List<EventRecord> SearchPublicEventsByQuery(string query)
        {
            string keyword;
            string eventType;
            DateTime? eventDate;
            ParseSearchQuery(query, out keyword, out eventType, out eventDate);

            return SearchEventsInternal(keyword, eventType, eventDate);
        }

        public List<EventRecord> SearchEvents(string keyword, string eventType, string eventDateText)
        {
            DateTime parsedDate;
            DateTime? eventDate = DateTime.TryParse(eventDateText, out parsedDate) ? (DateTime?)parsedDate.Date : null;
            return SearchEventsInternal(keyword, eventType, eventDate);
        }

        private List<EventRecord> SearchEventsInternal(string keyword, string eventType, DateTime? eventDate)
        {
            keyword = string.IsNullOrWhiteSpace(keyword) ? string.Empty : keyword.Trim();
            eventType = string.IsNullOrWhiteSpace(eventType) ? string.Empty : eventType.Trim();

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
                  AND (@keyword = ''
                       OR e.Title LIKE @keywordLike
                       OR e.EventType LIKE @keywordLike
                       OR e.Description LIKE @keywordLike
                       OR e.Venue LIKE @keywordLike
                       OR CONVERT(VARCHAR(10), e.EventDate, 120) LIKE @keywordLike)
                  AND (@eventType = '' OR e.EventType = @eventType)
                  AND (@eventDate IS NULL OR CAST(e.EventDate AS DATE) = @eventDate)
                ORDER BY e.EventDate, e.Title;");

            cmd.Parameters.AddWithValue("@keyword", keyword);
            cmd.Parameters.AddWithValue("@keywordLike", "%" + keyword + "%");
            cmd.Parameters.AddWithValue("@eventType", eventType);
            var dateParameter = cmd.Parameters.Add("@eventDate", SqlDbType.Date);
            dateParameter.Value = eventDate.HasValue ? (object)eventDate.Value.Date : DBNull.Value;

            return MapDataTableToEvents(_db.Execute(cmd));
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

        private static List<EventRecord> MapDataTableToEvents(DataTable dt)
        {
            var events = new List<EventRecord>();
            foreach (DataRow row in dt.Rows)
            {
                events.Add(new EventRecord
                {
                    EventID = row["EventID"] == DBNull.Value ? 0 : Convert.ToInt32(row["EventID"]),
                    Title = row["Title"] == DBNull.Value ? string.Empty : row["Title"].ToString(),
                    EventType = row["EventType"] == DBNull.Value ? string.Empty : row["EventType"].ToString(),
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
