using System;
using System.Threading;
using System.Threading.Tasks;
using TheCollective;

namespace Chroniton
{
    public class Singularity : ISingularity
    {
        MinHeap<ScheduledJob> _scheduledQueue = new MinHeap<ScheduledJob>();
        ConcurrentHashSet<Task> _tasks = new ConcurrentHashSet<Task>();

        Thread _schedulingThread = null;
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

        internal Singularity()
        {
        }

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

                        _schedulingThread = new Thread(this.run);
                        _schedulingThread.Start();
                    }
                }
            }
            else
            {
                // log or whatever that we're already started
            }
        }

        public void Stop()
        {
            if (_started)
            {
                _started = false;
                _schedulingThread.Join();
                while (_scheduledQueue.Count > 0) { _scheduledQueue.Extract(); }
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
                var scheduledJob = _scheduledQueue.Peek();
                if (scheduledJob != null && scheduledJob.NextRun < DateTime.UtcNow)
                {
                    SpinWait.SpinUntil(() => _tasks.Count < this.MaximumThreads);
                    //we know we need to fire the next job
                    //another higher priority scheduled job could've been added while waiting
                    //So, we need to extract (which will be thread safe) the 
                    //next item from the queue

                    runJob(_scheduledQueue.Extract());
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

        private void runJob(ScheduledJob job)
        {
            if (_started)
            {
                var task = Task.Run(job.JobTask);
                _tasks.Add(task);
                task.ContinueWith(t => jobEnd(task, job));
            }
        }

        private void jobEnd(Task t, ScheduledJob job)
        {
            if (t.Exception == null)
            {
                Task.Run(() => _onSuccess?.Invoke(new ScheduledJobEventArgs(job)));
            }
            else
            {
                Task.Run(() => _onJobError?.Invoke(new ScheduledJobEventArgs(job),
                    t.Exception is AggregateException? t.Exception.InnerException : t.Exception));
            }
            _tasks.Remove(t);

            setNextExecution(job);
        }

        private void setNextExecution(ScheduledJob job)
        {
            if (job.PreventReschedule)
            {
                return;
            }
            DateTime next, now = DateTime.UtcNow;
            try
            {
                next = job.Schedule.NextScheduledTime(job.NextRun);
            }
            catch (Exception ex)
            {
                _onScheduleError?.Invoke(new ScheduledJobEventArgs(job), ex);
                return;
            }
            if (next < now)
            {
                switch (job.Job.ScheduleMissedBehavior)
                {
                    case ScheduleMissedBehavior.RunAgain:
                        next = now;
                        break;
                    case ScheduleMissedBehavior.SkipExecution:
                        next = job.Schedule.NextScheduledTime(next);
                        if (next < now)
                        {
                            _onScheduleError?.Invoke(new ScheduledJobEventArgs(job), 
                                new Exception("dude quit and figure your schedule out"));
                            return;
                        }
                        break;
                    case ScheduleMissedBehavior.ThrowException:
                        _onScheduleError?.Invoke(new ScheduledJobEventArgs(job),
                            new Exception("dude quit and figure your schedule out"));
                        return;
                }
            }
            queueJob(next, job);
        }

        public IScheduledJob ScheduleJob(ISchedule schedule, IJob job, bool runImmediately)
        {
            DateTime firstRun = runImmediately ? DateTime.UtcNow : schedule.NextScheduledTime(DateTime.Now);
            return ScheduleJob(schedule, job, firstRun);
        }

        public IScheduledJob ScheduleJob(ISchedule schedule, IJob job, DateTime firstRun)
        {
            var scheduledJob = new ScheduledJob()
            {
                Job = job,
                Schedule = schedule
            };
            scheduledJob.JobTask = new Func<Task>(()=> job.Start(scheduledJob.NextRun));

            queueJob(firstRun, scheduledJob);
            return scheduledJob;
        }

        public IScheduledJob ScheduleParameterizedJob<T>(ISchedule schedule, IParameterizedJob<T> job, T parameter, bool runImmediately)
        {
            DateTime firstRun = runImmediately ? DateTime.UtcNow : schedule.NextScheduledTime(DateTime.Now);
            return ScheduleParameterizedJob(schedule, job, parameter, firstRun);
        }

        public IScheduledJob ScheduleParameterizedJob<T>(ISchedule schedule, IParameterizedJob<T> job, T parameter, DateTime firstRun)
        {
            var scheduledJob = new ScheduledJob()
            {
                Job = job,
                Schedule = schedule,
            };
            scheduledJob.JobTask = new Func<Task>(() => job.Start(parameter, scheduledJob.NextRun));

            queueJob(firstRun, scheduledJob);

            return scheduledJob;
        }

        private void queueJob(DateTime nextRun, ScheduledJob scheduledJob)
        {
            scheduledJob.NextRun = nextRun;
            _scheduledQueue.Add(scheduledJob);
            _onScheduled?.Invoke(new ScheduledJobEventArgs(scheduledJob));
        }

        public bool StopScheduledJob(IScheduledJob scheduledJob)
        {
            ScheduledJob job = (ScheduledJob)scheduledJob;
            job.PreventReschedule = true;
            return _scheduledQueue.FindExtract(job);
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
