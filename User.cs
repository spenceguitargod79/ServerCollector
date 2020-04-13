using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace HydraMVC.Models
{
    public class User
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]//this attribute lets you enter the primary key for the server rather than having the database generate it.

       
        public string UserName { get; set; }
        /* Code change by vrushali
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime LogoutTime { get; set; }
         */
        public string Permission { get; set; }
        [Key]
        public int UserId { get; set; }
    }
}