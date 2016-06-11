using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chroniton
{
    public interface IScheduledJob : IComparable<IScheduledJob>
    {
        IJobBase Job { get; }
        ISchedule Schedule { get; }
        DateTime NextRun { get; }
    }

    internal class ScheduledJob : IScheduledJob, IComparable<IScheduledJob>
    {
        public IJobBase Job { get; internal set; }

        public ISchedule Schedule { get; internal set; }


        public DateTime NextRun { get; internal set; }

        public int CompareTo(IScheduledJob other)
        {
            return this.NextRun.CompareTo(other.NextRun);
        }

        internal Func<Task> JobTask { get; set; }
    }
}
