using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheCollective;

namespace Chroniton
{
    public class Singularity : ISingularity
    {
        ConcurrentHashSet<Task> _tasks = new ConcurrentHashSet<Task>();
        IContinuumFactory _multiverse = new ContinuumFactory();

        Task _schedulingThread = null;
        object _startStopLoc = new { };
        bool _started = false;

        static readonly object _instanceLoc = new { };
        static Singularity _instance;
        public static Singularity Instance
        {
            get
            {
                lock (_instanceLoc)
                {
                    if (_instance == null)
                    {
                        _instance = new Singularity();
                    }
                }
                return _instance;
            }
        }

        internal Singularity() { }

        ~Singularity()
        {
            Stop();
        }

        ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        int _maxThreads = 5;
        public int MaximumThreads
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _maxThreads;
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
                    _maxThreads = value;
                }
                finally
                {
                    if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
                }
            }
        }

        int _tickWait = 5 * 10000;
        public int MillisecondWait
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _tickWait / 10000;
                }
                finally
                {
                    if (_lock.IsReadLockHeld) _lock.ExitReadLock();
                }
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("value must be greater than zero");
                }
                _lock.EnterWriteLock();
                try
                {
                    _tickWait = value == 0 ? 1000 : value * 10000;
                }
                finally
                {
                    if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
                }
            }
        }

        public bool IsStarted
        {
            get
            {
                return _started;
            }
        }

        public void Start()
        {
            if (!_started)
            {
                lock (_startStopLoc)
                {
                    if (!_started)
                    {
                        _started = true;

                        _schedulingThread = Task.Run(new Action(this.run));
                    }
                }
            }
        }

        public void Stop()
        {
            if (_started)
            {
                _started = false;
                _schedulingThread.Wait();
                Task.WaitAll(_multiverse.Select(c => c.CleanUp()).ToArray());
            }
        }

        /// <summary>
        /// main thread to watch for new jobs which need to be done
        /// and add them to the job queue
        /// </summary>
        private void run()
        {
            while (_started)
            {
                SpinWait.SpinUntil(() => _tasks.Count < this.MaximumThreads);
                foreach (var continuum in _multiverse)
                {
                    var scheduledJob = continuum.GetNext();
                    //if (scheduledJob != null && scheduledJob.RunTime < DateTime.UtcNow)
                    if (scheduledJob != null)
                    {
                        runJob(scheduledJob, continuum);
                    }
                }
                wait();
            }//something said stop
        }

        private void wait()
        {
            long ticks = 5000;
            _lock.EnterReadLock();
            try
            {
                ticks = _tickWait;
            }
            finally
            {
                if (_lock.IsReadLockHeld) _lock.ExitReadLock();
            }

            DateTime waitUntil = DateTime.UtcNow.AddTicks(ticks);
            SpinWait.SpinUntil(() => DateTime.UtcNow > waitUntil);
        }

        private void runJob(ScheduledJobBase scheduledJob, IContinuum continuum)
        {
            if (_started)
            {
                scheduledJob.RunCount++;
                var task = scheduledJob.Execute(scheduledJob.RunTime);
                _tasks.Add(task);
                task.ContinueWith(t => jobEnd(task, scheduledJob, continuum));
            }
        }

        private void jobEnd(Task jobTask, ScheduledJobBase job, IContinuum continuum)
        {
            if (jobTask.Exception == null)
            {
                Task.Run(() => _onSuccess?.Invoke(new ScheduledJobEventArgs(job)));
            }
            else
            {
                Task.Run(() => _onJobError?.Invoke(new ScheduledJobEventArgs(job),
                    jobTask.Exception is AggregateException? jobTask.Exception.InnerException : jobTask.Exception));
            }
            _tasks.Remove(jobTask);

            setNextExecution(job, continuum);
        }

        private void setNextExecution(ScheduledJobBase scheduledJob, IContinuum continuum)
        {
            if (scheduledJob.PreventReschedule)
            {
                return;
            }
            DateTime next, now = DateTime.UtcNow;
            try
            {
                next = scheduledJob.Schedule.NextScheduledTime(scheduledJob);
            }
            catch (Exception ex)
            {
                _onScheduleError?.Invoke(new ScheduledJobEventArgs(scheduledJob), ex);
                return;
            }
            if (next == Chroniton.Constants.Never)
            {
                return;
            }
            if (next < now)
            {
                switch (scheduledJob.GetJob().ScheduleMissedBehavior)
                {
                    case ScheduleMissedBehavior.RunAgain:
                        next = now;
                        break;
                    case ScheduleMissedBehavior.SkipExecution:
                        next = scheduledJob.Schedule.NextScheduledTime(scheduledJob);
                        if (next < now)
                        {
                            _onScheduleError?.Invoke(new ScheduledJobEventArgs(scheduledJob), 
                                new Exception("The schedule twice returned a time before the end of the previous execution. "));
                            return;
                        }
                        break;
                    case ScheduleMissedBehavior.ThrowException:
                        _onScheduleError?.Invoke(new ScheduledJobEventArgs(scheduledJob),
                            new Exception("The schedule returned a time before the end of the previous execution"));
                        return;
                }
            }
            rescheduleJob(next, scheduledJob, continuum);
        }

        private void rescheduleJob(DateTime next, ScheduledJobBase job, IContinuum continuum)
        {
            if (next != Constants.Never)
            {
                job.RunTime = next;
                continuum.Add(job);
                _onScheduled?.Invoke(new ScheduledJobEventArgs(job)); 
            }
        }

        #region Events

        private JobEventHandler _onScheduled;
        public event JobEventHandler OnScheduled
        {
            add
            {
                _onScheduled += value;
            }
            remove
            {
                _onScheduled -= value;
            }
        }

        private JobEventHandler _onSuccess;
        public event JobEventHandler OnSuccess
        {
            add
            {
                _onSuccess += value;
            }
            remove
            {
                _onSuccess -= value;
            }
        }

        private event JobExceptionHandler _onJobError;
        public event JobExceptionHandler OnJobError
        {
            add
            {
                _onJobError += value;
            }
            remove
            {
                _onJobError -= value;
            }
        }

        private event JobExceptionHandler _onScheduleError;
        public event JobExceptionHandler OnScheduleError
        {
            add
            {
                _onScheduleError += value;
            }
            remove
            {
                _onScheduleError -= value;
            }
        }

        #endregion
    }
}
