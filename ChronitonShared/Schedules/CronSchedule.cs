using Chroniton.Schedules.Cron;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Chroniton.Schedules
{


    public class CronSchedule : ISchedule
    {
        CronDateFinder _finder;

        public CronSchedule(string cron)
        {
            _finder = new CronParser().Parse(cron);
        }

        public string Name { get; set; }

        public DateTime NextScheduledTime(DateTime afterThisTime)
        {
            throw new NotImplementedException();
        }
    }
}
