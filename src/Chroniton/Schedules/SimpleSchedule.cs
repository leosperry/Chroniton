using System;

namespace Chroniton.Schedules
{
	public class SimpleSchedule : ISchedule
    {
        Func<DateTime> _getNextSchedule;

        public string Name { get; set; }

        /// <summary>
        /// A user defined schedule
        /// </summary>
        /// <param name="getNextSchedule">a function to return then next scheduled time</param>
        public SimpleSchedule(Func<DateTime> getNextSchedule)
        {
            _getNextSchedule = getNextSchedule;
        }

        public DateTime NextScheduledTime(ScheduledJobBase scheduledJob)
        {
            return _getNextSchedule();
        }
    }
}
