using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ElectronicPratient.Models
{
    public class HistoryPatient
    {
        [Display(Name = "Version")]
        public int VersionId { get; set; }

        [Display(Name = "Last Update")]
        public DateTimeOffset? LastUpdate { get; set; }

        [Display(Name = "Name")]
        public string FirstName { get; set; }

        [Display(Name = "Surname")]
        public string Surname { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; }

        public List<string> color { get; set; }

        public HistoryPatient()
        {
            color = new List<string>() { "", "", "" } ;
        }
    }
}