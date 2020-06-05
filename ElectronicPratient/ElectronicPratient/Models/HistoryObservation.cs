using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ElectronicPratient.Models
{
    public class HistoryObservation
    {
        [Display(Name = "Version")]
        public int VersionId { get; set; }

        [Display(Name = "Last Update")]
        public DateTimeOffset? LastUpdate { get; set; }

        [Display(Name = "Status")]
        [DataType(DataType.Text)]
        public ObservationStatus? Status { get; set; }

        [Display(Name = "Reason")]
        public string Reason { get; set; }

        public List<string> color { get; set; }

        public HistoryObservation()
        {
            color = new List<string>() { "", "" };
        }
    }
}