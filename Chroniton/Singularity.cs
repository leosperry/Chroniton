using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheCollective;

namespace Chroniton
{
    public class Singularity : ISingularity
    {
        int _spinWait = 5;
        MinHeap<ScheduledJob> _scheduledQueue = new MinHeap<ScheduledJob>();
        ConcurrentQueue<ScheduledJob> _jobQueue = new ConcurrentQueue<ScheduledJob>();
        ConcurrentQueue<Task> _doneTasks = new ConcurrentQueue<Task>();
        ConcurrentHashSet<Task> _tasks = new ConcurrentHashSet<Task>();

        Thread _schedulingThread = null;
        Thread _jobWatcherThread = null;
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

                        _jobWatcherThread = new Thread(this.jobWatcher);
                        _jobWatcherThread.Start();
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
            _started = false;
            _schedulingThread.Join();
            _jobWatcherThread.Join();

            ScheduledJob queuedJob;
            Task donejob;

            while (_scheduledQueue.Count > 0) { _scheduledQueue.Extract(); }
            while (_jobQueue.TryDequeue(out queuedJob)) { }
            while (_doneTasks.TryDequeue(out donejob)) { }
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
                    //we need to fire the next job
                    //another scheduled job could've been added in the mirosecond to reach the 
                    //next line. So, we need to extract (which will be thread safe) the 
                    //next item from the queue
                    _jobQueue.Enqueue(_scheduledQueue.Extract());
                }
                Thread.SpinWait(_spinWait);
            }//something said stop
        }

        /// <summary>
        /// thread for executing jobs
        /// </summary>
        private void jobWatcher()
        {
            Task done;
            while (_started)
            {
                while (_doneTasks.TryDequeue(out done))
                {
                    _tasks.Remove(done);
                }

                ScheduledJob job;
                if (_jobQueue.TryDequeue(out job) && !job.PreventReschedule)
                {
                    SpinWait.SpinUntil(() => _tasks.Count - _doneTasks.Count < this.MaximumThreads);
                    if (!job.PreventReschedule)
                    {
                        runJob(job);
                    }
                }
                Thread.SpinWait(_spinWait);
            }
            SpinWait.SpinUntil(() => _tasks.Count - _doneTasks.Count < this.MaximumThreads);
            Task.WaitAll(_tasks.ToArray());
            while (_doneTasks.TryDequeue(out done))
            {
                _tasks.Remove(done);
            }
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
            _doneTasks.Enqueue(t);
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
                Schedule = schedule,
                JobTask = new Func<Task>(() => job.Start())
            };

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
                JobTask = new Func<Task>(() => job.Start(parameter))
            };

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
