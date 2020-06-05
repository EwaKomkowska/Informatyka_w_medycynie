using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ElectronicPratient.Models
{
    public class HistoryMedicationRequest
    {
        [Display(Name = "Version")]
        public int VersionId { get; set; }

        [Display(Name = "Last Update")]
        public DateTimeOffset? LastUpdate { get; set; }

        [Display(Name = "Reason")]
        public string Reason { get; set; }

        [Display(Name = "Dosage Instruction")]
        public string Instruction { get; set; }

        public List<string> color { get; set; }
        public HistoryMedicationRequest()
        {
            color = new List<string>() { "", ""};
        }
    }
}