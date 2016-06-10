using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chroniton
{
    public enum JobStatus
    {
        None = 0,
        Scheduled = 1,
        Started = 3,
        InProgress = 7,
        Stopped = 8
    }

    public enum ScheduleMissedBehavior
    {
        RunAgain = 0,
        SkipExecution = 1,
        ThrowException = 2
    }

    public interface IJobBase
    {
        ScheduleMissedBehavior ScheduleMissedBehavior { get; }
        void Abort();
    }

    public interface IJob : IJobBase
    {
        Task Start();
    }

    public interface IParameterizedJob<T> : IJobBase
    {
        Task Start(T parameter);
    }

    public class SimpleJob : IJob
    {
        public virtual ScheduleMissedBehavior ScheduleMissedBehavior
        {
            get
            {
                return ScheduleMissedBehavior.RunAgain;
            }
        }

        public Task Start()
        {
            throw new NotImplementedException();
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }
    }
}
