using System;
using System.ComponentModel.DataAnnotations;

namespace ElectronicPratient.Models
{
    public class ShowInfo
    {
        //HISTORIA LECZENIA
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime date;

        [Display(Name = "Reason")]
        public string reason;

        [Display(Name = "Amount")]
        public string amount;

        public string originalModel;
        public string elemID;
    }
}