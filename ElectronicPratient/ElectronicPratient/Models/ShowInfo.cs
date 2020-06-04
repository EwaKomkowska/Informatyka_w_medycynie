using System;
using System.ComponentModel.DataAnnotations;

namespace ElectronicPratient.Models
{
    public class ShowInfo
    {
        //HISTORIA LECZENIA
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime date { get; set; }

        [Display(Name = "Reason")]
        public string reason { get; set; }

        [Display(Name = "Amount")]
        public string amount { get; set; }

        public string originalModel { get; set; }
        public string elemID { get; set; }
    }
}