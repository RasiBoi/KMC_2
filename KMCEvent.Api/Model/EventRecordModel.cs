using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KMCEvent.Api.Model
{
    public class Blanket
    {
        // Preferred event-platform aliases.
        public int EventID
        {
            get { return BlanketID; }
            set { BlanketID = value; }
        }

        public string Title
        {
            get { return BlanketName; }
            set { BlanketName = value; }
        }

        public string EventType
        {
            get { return Fabric; }
            set { Fabric = value; }
        }

        public string Description
        {
            get { return DescriptionNotes; }
            set { DescriptionNotes = value; }
        }

        public decimal TicketPrice
        {
            get { return BasePrice; }
            set { BasePrice = value; }
        }

        // Event-platform fields.
        public DateTime EventDate { get; set; }
        public string Venue { get; set; }
        public int Capacity { get; set; }
        public int CreatedByUserID { get; set; }
        public string OrganizerName { get; set; }
        public int SeatsRemaining { get; set; }
    }
}
