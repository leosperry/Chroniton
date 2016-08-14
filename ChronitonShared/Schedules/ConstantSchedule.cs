using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
