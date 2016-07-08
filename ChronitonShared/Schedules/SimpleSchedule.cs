using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public DateTime NextScheduledTime(IScheduledJob scheduledJob)
        {
            return _getNextSchedule();
        }
    }
}
