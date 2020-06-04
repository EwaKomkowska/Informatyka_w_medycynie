using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ElectronicPratient.Models
{
    public class EditMedicationRequest
    {
        public string ID { get; set; }

        [Display(Name = "Reason")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Reason is required")]
        public string Reason { get; set; }

        [Display(Name = "Dosage Instruction")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Instruction is required")]
        public string Instruction { get; set; }
    }
}