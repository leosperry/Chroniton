using System;

namespace Chroniton
{
    public class ScheduledJobEventArgs
    {
        /// <summary>
        /// the job
        /// </summary>
        public IJobBase Job { get; internal set; }

        /// <summary>
        /// the schedule on which the job runs
        /// </summary>
        public ISchedule Schedule { get; internal set; }

        /// <summary>
        /// When the job was scheduled. For some events, this is the last time the job started.
        /// </summary>
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
        /// <summary>
        /// The maximum number of Tasks to run simultaneously
        /// </summary>
        int MaximumThreads { get; set; }

        /// <summary>
        /// Starts the singularity and begins processing jobs
        /// </summary>
        void Start();

        /// <summary>
        /// Stops scheduling new jobs and waits for current jobs to finish.
        /// </summary>
        void Stop();

        /// <summary>
        /// Schedules a job
        /// </summary>
        /// <param name="schedule">The schedule object which determines when the job runs</param>
        /// <param name="job">The job to execute</param>
        /// <param name="runImmediately">When true, schedules the job immediately, otherwise runs when based on the schedule</param>
        /// <returns>A ScheduledJob object representing a job, schedule and when it runs</returns>
        IScheduledJob ScheduleJob(ISchedule schedule, IJob job, bool runImmediately);

        /// <summary>
        /// Schedules a job to start at a specified time
        /// </summary>
        /// <param name="schedule">The schedule object which determines when the job runs</param>
        /// <param name="job">The job to execute</param>
        /// <param name="firstRun">The time the job should first run</param>
        /// <returns>A ScheduledJob object representing a job, schedule and when it runs</returns>
        IScheduledJob ScheduleJob(ISchedule schedule, IJob job, DateTime firstRun);

        /// <summary>
        /// Schedules a job
        /// </summary>
        /// <param name="schedule">The schedule object which determines when the job runs</param>
        /// <param name="job">The job to execute</param>
        /// <param name="parameter">The parameter to pass to the job</param>
        /// <param name="runImmediately">When true, schedules the job immediately, otherwise runs when based on the schedule</param>
        /// <returns>A ScheduledJob object representing a job, schedule and when it runs</returns>
        IScheduledJob ScheduleParameterizedJob<T>(ISchedule schedule, IParameterizedJob<T> job, T parameter, bool runImmediately);

        /// <summary>
        /// Schedules a parameterized job to start at a specified time
        /// </summary>
        /// <param name="schedule">The schedule object which determines when the job runs</param>
        /// <param name="job">The job to execute</param>
        /// <param name="parameter"></param>
        /// <param name="firstRun">The time the job should first run</param>
        /// <returns>A ScheduledJob object representing a job, schedule and when it runs</returns>
        IScheduledJob ScheduleParameterizedJob<T>(ISchedule schedule, IParameterizedJob<T> job, T parameter, DateTime firstRun);

        /// <summary>
        /// Stops the job from executing again
        /// </summary>
        /// <param name="scheduledJob">The job to stop</param>
        /// <returns>true if found and removed</returns>
        bool StopScheduledJob(IScheduledJob scheduledJob);

        /// <summary>
        /// Fires when a job is rescheduled - will not fire on calls to ScheduleJob()
        /// </summary>
        event JobEventHandler OnScheduled;

        /// <summary>
        /// Fires when a job is about to be scheduled and call to NextScheduledTime fails.
        /// In this case, the next run cannot be determined and is not scheduled
        /// </summary>
        event JobExceptionHandler OnScheduleError;

        /// <summary>
        /// Fires when a job successfully executes
        /// </summary>
        event JobEventHandler OnSuccess;

        /// <summary>
        /// Fires when a job throws an exeption
        /// </summary>
        event JobExceptionHandler OnJobError;
    }
}
