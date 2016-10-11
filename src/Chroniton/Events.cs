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

        public ScheduledJobEventArgs(ScheduledJobBase scheduledJob)
        {
            Job = scheduledJob.GetJob();
            Schedule = scheduledJob.Schedule;
            ScheduledTime = scheduledJob.RunTime;
        }
    }

    public delegate void JobEventHandler(ScheduledJobEventArgs job);
    public delegate void JobExceptionHandler(ScheduledJobEventArgs job, Exception e);
}
