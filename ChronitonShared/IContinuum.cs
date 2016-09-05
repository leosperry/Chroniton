using System;
using System.Threading.Tasks;

namespace Chroniton
{
    public interface IContinuum
    {
        Guid Add(ScheduledJobBase scheduledJob);

        bool Remove(Guid jobId);

        Task CleanUp();

        ScheduledJobBase ExtactNextReady();

        ScheduledJobBase GetJob(Guid id);
        void Initialize();
    }
}
