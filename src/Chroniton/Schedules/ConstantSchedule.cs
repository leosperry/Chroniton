using System;

namespace Chroniton.Schedules
{
	public class ConstantSchedule : ISchedule
    {
        public string Name { get; set; }

        public DateTime NextScheduledTime(ScheduledJobBase scheduledJob)
        {
            return DateTime.UtcNow;
        }
    }
}
