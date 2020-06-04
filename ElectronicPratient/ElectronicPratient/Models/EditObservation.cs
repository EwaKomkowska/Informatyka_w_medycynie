using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ElectronicPratient.Models
{
    public partial class EditObservation
    {
        public string ID { get; set; }

        [Display(Name = "Status")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Status is required")]
        public ObservationStatus? Status { get; set; }

        [Display(Name = "Reason")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Reason is required")]
        public string Reason { get; set; }

        public SelectList value = new SelectList(
            new List<SelectListItem>
            {
                new SelectListItem { Selected = true, Text = ObservationStatus.Amended.ToString(), Value = ObservationStatus.Amended.ToString() },
                new SelectListItem { Selected = false, Text = ObservationStatus.Cancelled.ToString(), Value = ObservationStatus.Cancelled.ToString()},
                new SelectListItem { Selected = false, Text = ObservationStatus.Corrected.ToString(), Value = ObservationStatus.Corrected.ToString()},
                new SelectListItem { Selected = false, Text = ObservationStatus.EnteredInError.ToString(), Value = ObservationStatus.EnteredInError.ToString()},
                new SelectListItem { Selected = false, Text = ObservationStatus.Final.ToString(), Value = ObservationStatus.Final.ToString()},
            }, "Value" , "Text", 1);
    }

}