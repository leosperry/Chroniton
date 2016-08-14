using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Chroniton
{
    public interface IChronitonFactory
    {
        ISingularity GetSingularity();
        IJobScheduler GetJobScheduler<T>() where T : IContinuum, new();
    }

    public interface IJobScheduler
    {
        /// <summary>
        /// Schedules a job
        /// </summary>
        /// <param name="schedule">The schedule object which determines when the job runs</param>
        /// <param name="job">The job to execute</param>
        /// <param name="runImmediately">When true, schedules the job immediately, otherwise runs when based on the schedule</param>
        /// <returns>A ScheduledJob object representing a job, schedule and when it runs</returns>
        ScheduledJob ScheduleJob(ISchedule schedule, IJob job, bool runImmediately);

        /// <summary>
        /// Schedules a job to start at a specified time
        /// </summary>
        /// <param name="schedule">The schedule object which determines when the job runs</param>
        /// <param name="job">The job to execute</param>
        /// <param name="firstRun">The time the job should first run</param>
        /// <returns>A ScheduledJob object representing a job, schedule and when it runs</returns>
        ScheduledJob ScheduleJob(ISchedule schedule, IJob job, DateTime firstRun);

        /// <summary>
        /// Schedules a job
        /// </summary>
        /// <param name="schedule">The schedule object which determines when the job runs</param>
        /// <param name="job">The job to execute</param>
        /// <param name="parameter">The parameter to pass to the job</param>
        /// <param name="runImmediately">When true, schedules the job immediately, otherwise runs when based on the schedule</param>
        /// <returns>A ScheduledJob object representing a job, schedule and when it runs</returns>
        ParameterizedScheduledJob<T> ScheduleParameterizedJob<T>(ISchedule schedule, IParameterizedJob<T> job, T parameter, bool runImmediately);

        /// <summary>
        /// Schedules a parameterized job to start at a specified time
        /// </summary>
        /// <param name="schedule">The schedule object which determines when the job runs</param>
        /// <param name="job">The job to execute</param>
        /// <param name="parameter"></param>
        /// <param name="firstRun">The time the job should first run</param>
        /// <returns>A ScheduledJob object representing a job, schedule and when it runs</returns>
        ParameterizedScheduledJob<T> ScheduleParameterizedJob<T>(ISchedule schedule, IParameterizedJob<T> job, T parameter, DateTime firstRun);

        /// <summary>
        /// Stops the job from executing again.
        /// </summary>
        /// <param name="scheduledJob">The job to stop</param>
        /// <returns>true if found and removed. false if currently executing or not found.
        /// Either way, the job will not run again on the associated schedule</returns>
        bool StopScheduledJob(ScheduledJobBase scheduledJob);
    }

    public class ChronitonFactory : IChronitonFactory
    {
        public IJobScheduler GetJobScheduler<T>() where T : IContinuum, new()
        {
            ContinuumFactory fact = new ContinuumFactory();
            var continuum = fact.GetContinuum<T>();
            return new JobScheduler<T>(continuum);
        }

        public ISingularity GetSingularity()
        {
            return Singularity.Instance;
        }
    }

    public class JobScheduler<T> : IJobScheduler where T : IContinuum
    {
        readonly T _continuum;

        public JobScheduler(T continuum)
        {
            _continuum = continuum;
        }

        public ScheduledJob ScheduleJob(ISchedule schedule, IJob job, bool runImmediately)
        {
            var scheduledJob = new ScheduledJob()
            {
                Job = job,
                Schedule = schedule
            };
            DateTime firstRun = runImmediately ? DateTime.UtcNow : schedule.NextScheduledTime(scheduledJob);

            queueJob(firstRun, scheduledJob);
            return scheduledJob;
        }

        public ScheduledJob ScheduleJob(ISchedule schedule, IJob job, DateTime firstRun)
        {
            var scheduledJob = new ScheduledJob()
            {
                Job = job,
                Schedule = schedule
            };

            queueJob(firstRun, scheduledJob);
            return scheduledJob;
        }

        public ParameterizedScheduledJob<Tparam> ScheduleParameterizedJob<Tparam>(ISchedule schedule, IParameterizedJob<Tparam> job, Tparam parameter, bool runImmediately)
        {
            var scheduledJob = new ParameterizedScheduledJob<Tparam>()
            {
                Job = job,
                Schedule = schedule,
                Parameter = parameter
            };
            DateTime firstRun = runImmediately ? DateTime.UtcNow : schedule.NextScheduledTime(scheduledJob);

            queueJob(firstRun, scheduledJob);

            return scheduledJob;
        }

        public ParameterizedScheduledJob<Tparam> ScheduleParameterizedJob<Tparam>(ISchedule schedule, IParameterizedJob<Tparam> job, Tparam parameter, DateTime firstRun)
        {
            var scheduledJob = new ParameterizedScheduledJob<Tparam>()
            {
                Job = job,
                Schedule = schedule,
                Parameter = parameter
            };

            queueJob(firstRun, scheduledJob);

            return scheduledJob;
        }

        private void queueJob(DateTime nextRun, ScheduledJobBase scheduledJob)
        {
            scheduledJob.RunTime = nextRun;
            _continuum.Add(scheduledJob);
            //_onScheduled?.Invoke(new ScheduledJobEventArgs(scheduledJob));
        }

        public bool StopScheduledJob(ScheduledJobBase scheduledJob)
        {
            scheduledJob.PreventReschedule = true;
            return _continuum.Remove(scheduledJob.ID);
        }
    }

}
