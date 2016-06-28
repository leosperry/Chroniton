using System;
using System.Threading;
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
    }
}
