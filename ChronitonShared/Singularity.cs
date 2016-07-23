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
                Task.WaitAll(_schedulingThread);
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
                if (scheduledJob != null && scheduledJob.RunTime < DateTime.UtcNow)
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
            if (_started && !job.PreventReschedule)
            {
                job.RunCount++;
                var task = Task.Run(job.JobTask);
                _tasks.Add(task);
                task.ContinueWith(t => jobEnd(task, job));
            }
        }

        private void jobEnd(Task jobTask, ScheduledJob job)
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
                next = job.Schedule.NextScheduledTime(job);
            }
            catch (Exception ex)
            {
                _onScheduleError?.Invoke(new ScheduledJobEventArgs(job), ex);
                return;
            }
            if (next == Chroniton.Constants.Never)
            {
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
                        next = job.Schedule.NextScheduledTime(job);
                        if (next < now)
                        {
                            _onScheduleError?.Invoke(new ScheduledJobEventArgs(job), 
                                new Exception("The schedule twice returned a time before the end of the previous execution. "));
                            return;
                        }
                        break;
                    case ScheduleMissedBehavior.ThrowException:
                        _onScheduleError?.Invoke(new ScheduledJobEventArgs(job),
                            new Exception("The schedule returned a time before the end of the previous execution"));
                        return;
                }
            }
            queueJob(next, job);
        }

        public IScheduledJob ScheduleJob(ISchedule schedule, IJob job, bool runImmediately)
        {
            var scheduledJob = new ScheduledJob()
            {
                Job = job,
                Schedule = schedule
            };
            scheduledJob.JobTask = new Func<Task>(() => job.Start(scheduledJob.RunTime));
            DateTime firstRun = runImmediately ? DateTime.UtcNow : schedule.NextScheduledTime(scheduledJob);

            queueJob(firstRun, scheduledJob);
            return scheduledJob;
        }

        public IScheduledJob ScheduleJob(ISchedule schedule, IJob job, DateTime firstRun)
        {
            var scheduledJob = new ScheduledJob()
            {
                Job = job,
                Schedule = schedule
            };
            scheduledJob.JobTask = new Func<Task>(()=> job.Start(scheduledJob.RunTime));

            queueJob(firstRun, scheduledJob);
            return scheduledJob;
        }

        public IScheduledJob ScheduleParameterizedJob<T>(ISchedule schedule, IParameterizedJob<T> job, T parameter, bool runImmediately)
        {
            var scheduledJob = new ScheduledJob()
            {
                Job = job,
                Schedule = schedule,
            };
            scheduledJob.JobTask = new Func<Task>(() => job.Start(parameter, scheduledJob.RunTime));
            DateTime firstRun = runImmediately ? DateTime.UtcNow : schedule.NextScheduledTime(scheduledJob);

            queueJob(firstRun, scheduledJob);

            return scheduledJob;
        }

        public IScheduledJob ScheduleParameterizedJob<T>(ISchedule schedule, IParameterizedJob<T> job, T parameter, DateTime firstRun)
        {
            var scheduledJob = new ScheduledJob()
            {
                Job = job,
                Schedule = schedule,
            };
            scheduledJob.JobTask = new Func<Task>(() => job.Start(parameter, scheduledJob.RunTime));

            queueJob(firstRun, scheduledJob);

            return scheduledJob;
        }

        private void queueJob(DateTime nextRun, ScheduledJob scheduledJob)
        {
            scheduledJob.RunTime = nextRun;
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
