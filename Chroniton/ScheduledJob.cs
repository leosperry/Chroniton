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
        JobStatus Status { get; }
        DateTime NextRun { get; }
    }

    public interface IScheduledParameterizedJob<T> : IScheduledJob
    {
        T Parameter { get; set; }
    }

    public class ScheduledJob : IScheduledJob, IComparable<IScheduledJob>
    {
        public IJobBase Job
        {
            get; internal set;
        }

        public ISchedule Schedule
        {
            get; internal set;
        }

        public JobStatus Status { get; internal set; }

        public DateTime NextRun
        {
            get; internal set;
        }

        public int CompareTo(IScheduledJob other)
        {
            return this.NextRun.CompareTo(other.NextRun);
        }

        internal Func<Task> JobTask { get; set; }
    }

    public class ScheduledParameterizedJob<T> : ScheduledJob, IScheduledParameterizedJob<T>
    {
        public T Parameter { get; set; }
    }
}
