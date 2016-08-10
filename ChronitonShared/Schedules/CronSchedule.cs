using Chroniton.Schedules.Cron;
using System;

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

        public virtual DateTime NextScheduledTime(IScheduledJob scheduledJob)
        {
            var date = _finder.GetNext(scheduledJob.RunTime);
            if (date.HasValue)
            {
                return date.Value;
            }
            else
            {
                return Constants.Never;
            }
        }
    }
}
