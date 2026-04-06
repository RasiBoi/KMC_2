using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KMCEvent.Api.Model
{
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
    }
}
