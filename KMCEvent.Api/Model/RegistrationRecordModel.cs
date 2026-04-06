using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KMCEvent.Api.Model
{
    public class Inventory
    {
        // Legacy members retained for service compatibility.
        public int InventoryID { get; set; }
        public int ProductID { get; set; }
        public string BlanketName { get; set; }
        public int OwnerUserID { get; set; }
        public int UnitsInStock { get; set; }

        // Preferred event-platform aliases.
        public int RegistrationID
        {
            get { return InventoryID; }
            set { InventoryID = value; }
        }

        public int EventID
        {
            get { return ProductID; }
            set { ProductID = value; }
        }

        public string EventTitle
        {
            get { return BlanketName; }
            set { BlanketName = value; }
        }

        public int ParticipantUserID
        {
            get { return OwnerUserID; }
            set { OwnerUserID = value; }
        }

        public int SeatCount
        {
            get { return UnitsInStock; }
            set { UnitsInStock = value; }
        }

        // Event-platform fields.
        public string EventType { get; set; }
        public DateTime EventDate { get; set; }
        public string Venue { get; set; }
        public DateTime RegisteredOn { get; set; }
    }
}
