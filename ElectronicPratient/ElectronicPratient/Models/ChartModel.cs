using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ElectronicPratient.Models
{
    public class ChartModel
    {
        [Display(Name = "Date")]
        public string label;

        [Display(Name = "Weight")]
        public double y;

        public ChartModel(double y, string label)           
        {
            this.y = y;
            this.label = label;
        }
    }
}