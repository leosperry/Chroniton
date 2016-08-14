using System;
using System.Threading.Tasks;

namespace Chroniton
{
    public interface IJobBase
    {
        /// <summary>
        /// A name for the job
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The behavior to employ when job completes after the next scheduled time
        /// </summary>
        ScheduleMissedBehavior ScheduleMissedBehavior { get; }
    }

    public interface IJob : IJobBase
    {
        /// <summary>
        /// is called when the job is scheduled to run
        /// </summary>
        /// <returns>A task to run asynchronously</returns>
        Task Start(DateTime scheduledTime);
    }

    public interface IParameterizedJob<T> : IJobBase
    {
        T Parameter { get; set; }

        /// <summary>
        /// is called when the job is scheduled to run
        /// </summary>
        /// <returns>A task to run asynchronously</returns>
        Task Start(T parameter, DateTime scheduledTime);
    }
}
