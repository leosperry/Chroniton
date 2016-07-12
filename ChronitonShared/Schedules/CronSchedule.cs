using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Chroniton.Schedules
{


    public class CronSchedule
    {
        public CronSchedule(string cron)
        {
        }

        class Cron
        {
            public string Minute { get; set; }
            public string Hour { get; set; }
            public string Date { get; set; }
            public string Month { get; set; }
            public string DayOfWeek { get; set; }
            public string Year { get; set; }

        }
    }
}
