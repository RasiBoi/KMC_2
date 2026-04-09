using KMCEvent.Api.Controller;
using KMCEvent.Api.Data;
using KMCEvent.Api.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Services;
using EventRecord = KMCEvent.Api.Model.Blanket;
using RegistrationRecord = KMCEvent.Api.Model.Inventory;

namespace KMCEvent.Api
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    public class Api : System.Web.Services.WebService
    {
        [WebMethod]
        public User Login(string user, string pass)
        {
            var db = new DatabaseManager();
            var cmd = new SqlCommand("SELECT UserID, Username, Role, DisplayName, Email FROM Users WHERE Username = @u AND PasswordHash = @p");
            cmd.Parameters.AddWithValue("@u", user);
            cmd.Parameters.AddWithValue("@p", pass);
            var dt = db.Execute(cmd);
            if (dt.Rows.Count > 0)
            {
                return new User
                {
                    UserID = (int)dt.Rows[0]["UserID"],
                    Username = dt.Rows[0]["Username"].ToString(),
                    Role = dt.Rows[0]["Role"].ToString(),
                    DisplayName = dt.Rows[0]["DisplayName"] == DBNull.Value ? string.Empty : dt.Rows[0]["DisplayName"].ToString(),
                    Email = dt.Rows[0]["Email"] == DBNull.Value ? string.Empty : dt.Rows[0]["Email"].ToString()
                };
            }
            return null;
        }

        [WebMethod]
        public List<User> GetUsersByRole(string role)
        {
            var db = new DatabaseManager();
            var cmd = new SqlCommand("SELECT UserID, Username, Role, DisplayName, Email FROM Users WHERE Role = @role");
            cmd.Parameters.AddWithValue("@role", role);
            var dt = db.Execute(cmd);
            var userList = new List<User>();
            foreach (DataRow row in dt.Rows)
            {
                userList.Add(new User
                {
                    UserID = (int)row["UserID"],
                    Username = row["Username"].ToString(),
                    Role = row["Role"].ToString(),
                    DisplayName = row["DisplayName"] == DBNull.Value ? string.Empty : row["DisplayName"].ToString(),
                    Email = row["Email"] == DBNull.Value ? string.Empty : row["Email"].ToString()
                });
            }
            return userList;
        }

        // Compatibility SOAP operations retained for the existing generated client proxy.
        [WebMethod(MessageName = "Manufacturer_GetBlankets")]
        public List<EventRecord> Legacy_ListEventsForCompatibilityClient()
        {
            return new OrganizerActions().GetAllEvents();
        }

        [WebMethod(MessageName = "Manufacturer_AddBlanket")]
        public void Legacy_CreateEventForCompatibilityClient(EventRecord b)
        {
            new OrganizerActions().CreateEvent(b);
        }

        [WebMethod(MessageName = "Manufacturer_UpdateBlanket")]
        public void Legacy_UpdateEventForCompatibilityClient(EventRecord b)
        {
            new OrganizerActions().UpdateEvent(b);
        }

        [WebMethod(MessageName = "Manufacturer_DeleteBlanket")]
        public void Legacy_ArchiveEventForCompatibilityClient(int eventId)
        {
            new OrganizerActions().ArchiveEvent(eventId);
        }

        [WebMethod(MessageName = "Distributor_GetInventory")]
        public List<RegistrationRecord> Legacy_GetParticipantRegistrationsForCompatibilityClient(int distId)
        {
            return new ParticipantActions().GetParticipantRegistrations(distId);
        }

        [WebMethod(MessageName = "Distributor_UpdateStock")]
        public void Legacy_UpdateRegistrationSeatsForCompatibilityClient(int invId, int qty)
        {
            new ParticipantActions().UpdateRegistrationSeatCount(invId, qty);
        }

        [WebMethod(MessageName = "Distributor_GiveToSeller")]
        public void Legacy_RegisterParticipantForCompatibilityClient(int distId, int sellerId, int blanketId, int qty)
        {
            new ParticipantActions().RegisterForEvent(distId, blanketId, qty);
        }

        [WebMethod(MessageName = "SearchUserStock")]
        public List<RegistrationRecord> Legacy_SearchEventsForParticipantForCompatibilityClient(int userId, string query)
        {
            return new ParticipantActions().SearchEventsForParticipant(userId, query);
        }

        [WebMethod(MessageName = "Seller_SearchBlankets")]
        public List<EventRecord> Legacy_SearchPublicEventsForCompatibilityClient(string query)
        {
            return new EventCatalogActions().SearchPublicEventsByQuery(query);
        }

        // Scenario-first API endpoints for external organizers and integrations.
        [WebMethod]
        public List<EventRecord> Organizer_GetMyEvents(int organizerId)
        {
            return new OrganizerActions().GetEventsByOrganizer(organizerId);
        }

        [WebMethod]
        public int Organizer_CreateEvent(EventRecord eventPayload)
        {
            return new OrganizerActions().CreateEvent(eventPayload);
        }

        [WebMethod]
        public bool Organizer_UpdateEvent(EventRecord eventPayload)
        {
            new OrganizerActions().UpdateEvent(eventPayload);
            return true;
        }

        [WebMethod]
        public bool Organizer_DeleteEvent(int eventId, int organizerId)
        {
            return new OrganizerActions().DeleteEvent(eventId, organizerId);
        }

        [WebMethod]
        public List<EventRecord> Public_SearchEvents(string keyword, string eventType, string eventDate)
        {
            return new EventCatalogActions().SearchEvents(keyword, eventType, eventDate);
        }

        [WebMethod]
        public bool Participant_Register(int participantId, int eventId, int seatCount)
        {
            new ParticipantActions().RegisterForEvent(participantId, eventId, seatCount);
            return true;
        }

        [WebMethod]
        public List<RegistrationRecord> Participant_GetMyRegistrations(int participantId)
        {
            return new ParticipantActions().GetParticipantRegistrations(participantId);
        }
    }
}

