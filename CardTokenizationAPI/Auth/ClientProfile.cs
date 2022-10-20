using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CardTokenizationAPI.Auth
{
    public class ClientProfile
    {
        public int ID { get; set; }
        public string clientName { get; set; }
        public string clientId { get; set; }
        public Guid clientSecret { get; set; }
        public string clientDescription { get; set; }
        public bool isDeleted { get; set; }
        public DateTime deletedDate { get; set; }
        public string deletedBy { get; set; }
        public DateTime updatedDate { get; set; }
        public string updatedBy { get; set; }
        public DateTime createdDate { get; set; }
        public string createdBy { get; set; }
        public string clientIPAddress { get; set; }
        public string status { get; set; }
        public bool unRestricted { get; set; }
    }
}