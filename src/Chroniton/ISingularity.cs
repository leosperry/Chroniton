namespace Chroniton
{
	public interface ISingularity
    {
        /// <summary>
        /// The maximum number of Tasks to run simultaneously
        /// </summary>
        int MaximumThreads { get; set; }

        /// <summary>
        /// The amount of time to wait each time through the main loop. Default is 5.
        /// If set to zero, each loop will wait 1/10th of a second.
        /// </summary>
        int MillisecondWait { get; set; }

        /// <summary>
        /// returns true if the Singularity is currently scheduling/executing jobs
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// Starts the singularity and begins processing jobs
        /// </summary>
        void Start();

        /// <summary>
        /// Stops scheduling new jobs and waits for current jobs to finish.
        /// </summary>
        void Stop();

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
