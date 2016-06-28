namespace Chroniton
{
    /// <summary>
    /// represents the behavior to employ when a job completes after it's next scheduled time
    /// </summary>
    public enum ScheduleMissedBehavior
    {
        /// <summary>
        /// run the job again immediately
        /// </summary>
        RunAgain = 0,
        /// <summary>
        /// skips the missed execution and calls NextScheduledTime() to determine the next run
        /// if the next run is still before the previously scheduled time an exeption is thrown
        /// and the job is not rescheduled
        /// </summary>
        SkipExecution = 1,
        /// <summary>
        /// Throws an exception and does not reschedule the job
        /// </summary>
        ThrowException = 2
    }
}
