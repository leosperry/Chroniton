using Chroniton;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chroniton.Schedules
{
    public class RunOnceSchedule : ISchedule
    {
        DateTime? _runAt = null;

        public string Name { get; set; }

        public virtual DateTime NextScheduledTime(IScheduledJob scheduledJob)
        {
            if (scheduledJob.RunCount > 0)
            {
                return Chroniton.Constants.Never;
            }
            else
            {
                return _runAt.HasValue ? _runAt.Value : DateTime.UtcNow;
            }
        }

        /// <summary>
        /// when NextScheduledTime is called, will return DateTime.UtcNow
        /// </summary>
        public RunOnceSchedule()
        {

        }

        /// <summary>
        /// when NextScheduledTime is called, will return runAt
        /// </summary>
        public RunOnceSchedule(DateTime runAt)
        {
            _runAt = runAt;
        }

        /// <summary>
        /// when NextScheduledTime is called, will return DateTime.UtcNow + runIn
        /// </summary>
        public RunOnceSchedule(TimeSpan runIn)
        {
            _runAt = DateTime.UtcNow.Add(runIn);
        }
    }
}
