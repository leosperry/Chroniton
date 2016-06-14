using System;
using System.Threading.Tasks;

namespace Chroniton.Jobs
{
    /// <summary>
    /// A simple job
    /// </summary>
    public class SimpleJob : IJob
    {
        public string Name { get; set; }

        public virtual ScheduleMissedBehavior ScheduleMissedBehavior { get; set; }
            = ScheduleMissedBehavior.RunAgain;

        Func<DateTime, Task> _task;

        /// <summary>
        /// Initializes a new instance of a simple job
        /// </summary>
        /// <param name="task">A function which returns the task to run when
        /// the job is started</param>
        public SimpleJob(Func<DateTime, Task> task)
        {
            _task = task;
        }

        /// <summary>
        /// Initializes a new instance of a simple job
        /// </summary>
        /// <param name="task">A function which returns the task to run when
        /// the job is started</param>
        /// <param name="name">A name for the job</param>
        public SimpleJob(Func<DateTime, Task> task, string name)
        {
            _task = task;
            this.Name = name;
        }

        public async Task Start(DateTime scheduledTime)
        {
            await _task(scheduledTime);
        }
    }

    /// <summary>
    /// A simple parameterized job
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimpleParameterizedJob<T> : IParameterizedJob<T>
    {
        public string Name { get; set; }

        public virtual ScheduleMissedBehavior ScheduleMissedBehavior { get; set; }
            = ScheduleMissedBehavior.RunAgain;

        Func<T, DateTime, Task> _task;

        /// <summary>
        /// Initializes a new instance of a simple job which takes a parameter
        /// </summary>
        /// <param name="task">A function which returns the task to run when
        /// the job is started</param>
        public SimpleParameterizedJob(Func<T, DateTime, Task> task)
        {
            _task = task;
        }

        /// <summary>
        /// Initializes a new instance of a simple job which takes a parameter
        /// </summary>
        /// <param name="task">A function which returns the task to run when
        /// the job is started</param>
        /// <param name="name">A name for the job</param>

        public SimpleParameterizedJob(Func<T, DateTime, Task> task, string name)
        {
            _task = task;
            this.Name = name;
        }

        public async Task Start(T parameter, DateTime scheduledTime)
        {
            await _task(parameter, scheduledTime);
        }
    }
}
