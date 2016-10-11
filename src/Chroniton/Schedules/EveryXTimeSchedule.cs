using System;

namespace Chroniton.Schedules
{
	public class EveryXTimeSchedule : ISchedule
    {
        TimeSpan _interval;

        public EveryXTimeSchedule(TimeSpan interval)
        {
            if (interval < TimeSpan.Zero)
            {
                throw new ArgumentException("interval cannot be less than zero", nameof(interval));
            }
            _interval = interval;
        }

        public string Name
        {
            get; set;
        }

        public DateTime NextScheduledTime(ScheduledJobBase scheduledJob)
        {
            return scheduledJob.RunTime + _interval;
        }
    }
}
