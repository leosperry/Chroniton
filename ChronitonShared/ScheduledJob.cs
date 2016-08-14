using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Chroniton
{
    public abstract class ScheduledJobBase : IComparable<ScheduledJobBase>
    {
        public Guid ID { get; set; }

        public ISchedule Schedule { get; set; }

        public DateTime RunTime { get; set; }

        public int RunCount { get; set; }

        public int CompareTo(ScheduledJobBase other)
        {
            return this.RunTime.CompareTo(other.RunTime);
        }

        ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        bool _preventReschedule = false;
        internal bool PreventReschedule
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _preventReschedule;
                }
                finally
                {
                    if (_lock.IsReadLockHeld) _lock.ExitReadLock();
                }
            }
            set
            {
                _lock.EnterWriteLock();
                try
                {
                    _preventReschedule = value;
                }
                finally
                {
                    if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
                }
            }
        }

        public abstract Task Execute(DateTime scheduledTime);

        public IJobBase GetJob()
        {
            var propInfo = this.GetType().GetTypeInfo()
                .GetProperty("Job");
            return (IJobBase)(propInfo?.GetValue(this));
        }
    }

    public class ScheduledJob: ScheduledJobBase
    {
        public IJob Job { get; set; }

        public override async Task Execute(DateTime scheduledTime)
        {
            await Job.Start(scheduledTime);
        }
    }

    public class ParameterizedScheduledJob<T> : ScheduledJobBase
    {
        public T Parameter { get; set; }

        public IParameterizedJob<T> Job { get; set; }

        public override async Task Execute(DateTime scheduledTime)
        {
            await Job.Start(Parameter, scheduledTime);
        }
    }
}
