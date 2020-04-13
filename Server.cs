using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace HydraMVC.Models
{
    public class Server
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]//this attribute lets you enter the primary key for the server rather than having the database generate it.

        public string ServerName { get; set; }
        public string IpAddress { get; set; }
        public string BOI { get; set; }
        public string GameServer { get; set; }
        public string HotFixes { get; set; }
        public string PlayerVersions { get; set; }
        public string ReportServer { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }
        public string LoggedInUsers { get; set; }
        public string ActiveUsers { get; set; }
        public string WAP { get; set; }
        public string Rack { get; set; }
        [Key]
        public int Id { get; set; }
    }
}