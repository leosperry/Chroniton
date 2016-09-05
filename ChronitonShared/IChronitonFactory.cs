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
}
