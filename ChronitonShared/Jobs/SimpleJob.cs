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

        Action<DateTime> _task;
        
        /// <summary>
        /// Initializes a new instance of a simple job
        /// </summary>
        /// <param name="action">An action to wrap in a task which will be run asynchronously</param>
        public SimpleJob(Action<DateTime> action)
        {
            _task = action;
        }

        /// <summary>
        /// Initializes a new instance of a simple job
        /// </summary>
        /// <param name="action">An action to wrap in a task which will be run asynchronously</param>
        /// <param name="name">A name for the job</param>
        public SimpleJob(Action<DateTime> action, string name)
        {
            Name = name;
            _task = action;
        }

        public async Task Start(DateTime scheduledTime)
        {
            await Task.Run(() => _task(scheduledTime));
        }
    }

    /// <summary>
    /// A simple parameterized job
    /// </summary>
    /// <typeparam name="T">The type for the parameter which 
    /// will be passed when the job starts</typeparam>
    public class SimpleParameterizedJob<T> : IParameterizedJob<T>
    {
        public string Name { get; set; }

        public T Parameter { get; set; }

        public virtual ScheduleMissedBehavior ScheduleMissedBehavior { get; set; }
            = ScheduleMissedBehavior.RunAgain;

        Action<T, DateTime> _task;

        /// <summary>
        /// Initializes a new instance of a simple job which takes a parameter
        /// </summary>
        /// <param name="task">An action which will be wrapped in a tast to run asynchronously 
        /// when the job is started</param>
        public SimpleParameterizedJob(Action<T, DateTime> task)
        {
            _task = task;
        }

        /// <summary>
        /// Initializes a new instance of a simple job which takes a parameter
        /// </summary>
        /// <param name="task">A function which returns the task to run when
        /// the job is started</param>
        /// <param name="name">A name for the job</param>
        public SimpleParameterizedJob(Action<T, DateTime> task, string name)
        {
            _task = task;
            this.Name = name;
        }

        public async Task Start(T parameter, DateTime scheduledTime)
        {
            await Task.Run(() => _task(parameter, scheduledTime));
        }
    }
}
