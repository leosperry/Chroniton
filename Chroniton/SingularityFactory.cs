using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chroniton
{
    public interface ISingularityFactory
    {
        ISingularity GetSingularity();
    }

    public class SingularityFactory : ISingularityFactory
    {
        public ISingularity GetSingularity()
        {
            return Singularity.Instance;
        }
    }
}
