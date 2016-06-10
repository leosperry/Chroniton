using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using Collective;

namespace Chroniton
{
    public class ScheduledJobEventArgs
    {
        public IJobBase Job { get; internal set; }
        public ISchedule Schedule { get; internal set; }
        public DateTime ScheduledTime { get; internal set; }

        public ScheduledJobEventArgs(IScheduledJob scheduledJob)
        {
            Job = scheduledJob.Job;
            Schedule = scheduledJob.Schedule;
            ScheduledTime = scheduledJob.NextRun;
        }
    }

    public delegate void JobEventHandler(ScheduledJobEventArgs job);
    public delegate void JobExceptionHandler(ScheduledJobEventArgs job, Exception e);
    
    public interface ISingularity : IDisposable
    {
        int MaximumThreads { get; set; }
        void Start();
        void Stop();
        IScheduledJob ScheduleJob(ISchedule schedule, IJob job, bool runImmediately);
        IScheduledJob ScheduleJob(ISchedule schedule, IJob job, DateTime firstRun);
        IScheduledJob ScheduleParameterizedJob<T>(ISchedule schedule, IParameterizedJob<T> job, T parameter, bool runImmediately);
        IScheduledJob ScheduleParameterizedJob<T>(ISchedule schedule, IParameterizedJob<T> job, T parameter, DateTime firstRun);
        bool StopScheduledJob(IScheduledJob scheduledJob);

        event JobEventHandler OnScheduled;
        event JobExceptionHandler OnScheduleError;
        event JobEventHandler OnSuccess;
        event JobExceptionHandler OnJobError;
    }

    public class Singularity : ISingularity
    {
        int _spinWait = 2;
        MinHeap<ScheduledJob> _scheduledQueue = new MinHeap<ScheduledJob>();
        ConcurrentQueue<ScheduledJob> _jobQueue = new ConcurrentQueue<ScheduledJob>();
        ConcurrentQueue<Task> _doneTasks = new ConcurrentQueue<Task>();
        ConcurrentHashSet<Task> _tasks = new ConcurrentHashSet<Task>();

        Thread _schedulingThread = null;
        Thread _jobRunningThread = null;
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

        public int MaximumThreads { get; set; } = 5;

        public void Start()
        {
            if (!_started)
            {
                lock (_startStopLoc)
                {
                    if (!_started)
                    {
                        _schedulingThread = new Thread(this.run);
                        _schedulingThread.Start();

                        _jobRunningThread = new Thread(this.jobWatcher);
                        _jobRunningThread.Start();
                        _started = true;
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
            _jobRunningThread.Join();
            Task.WaitAll(_tasks.ToArray());
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

        private void jobWatcher()
        {
            while (_started)
            {
                //clean up done tasks
                Task done;
                while (_doneTasks.TryDequeue(out done))
                {
                    _tasks.Remove(done);
                }

                ScheduledJob job;
                if (_jobQueue.TryDequeue(out job))
                {
                    if (_tasks.Count > MaximumThreads)
                    {
                        Task.WaitAny(_tasks.ToArray());
                    }
                    runJob(job);
                }
                Thread.SpinWait(_spinWait);
            }
        }

        private void runJob(ScheduledJob job)
        {
            if (_started)
            {
                var task = Task.Run(() => job.JobTask());
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
                Task.Run(() => _onJobError?.Invoke(new ScheduledJobEventArgs(job), t.Exception.InnerException));
            }
            _doneTasks.Enqueue(t);
            setNextExecution(job);
        }

        private void setNextExecution(ScheduledJob job)
        {
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
                JobTask = job.Start
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
            var scheduledJob = new ScheduledParameterizedJob<T>()
            {
                Job = job,
                Schedule = schedule,
                Parameter = parameter,
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
            ScheduledJob job = scheduledJob as ScheduledJob;
            if (job == null)
            {
                return false;
            }
            return _scheduledQueue.FindExtract(job);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
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
