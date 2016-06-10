using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chroniton;
using Microsoft.Practices.Unity;
using Chroniton.Schedules;

namespace ChronitonExample
{
    class Program
    {
        static void Main(string[] args)
        {
            UnityContainer container = new UnityContainer();

            container.RegisterType<ISingularityFactory, SingularityFactory>();

            var factory = container.Resolve<ISingularityFactory>();
            var singularity = factory.GetSingularity();

            ISchedule schedule = new ConstantSchedule();
            IJob job = new ConsoleWritingJob();
            singularity.ScheduleJob(schedule, job, false);

            var schedule2 = new EveryXTimeSchedule(TimeSpan.FromSeconds(1));
            IJob job2 = new ConsoleWritingJob();
            singularity.ScheduleJob(schedule2, job2, true);

            singularity.ScheduleParameterizedJob(schedule, new NamedConsoleWritingJob(), "schedule1", true);
            singularity.ScheduleParameterizedJob(schedule2, new NamedConsoleWritingJob(), "schedule2", true);

            singularity.ScheduleJob(new EveryXTimeSchedule(TimeSpan.FromSeconds(7)), job, DateTime.UtcNow.AddSeconds(20));

            singularity.Start();

        }
    }
}
