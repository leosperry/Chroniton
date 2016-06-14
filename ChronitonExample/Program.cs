using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chroniton;
using Microsoft.Practices.Unity;
using Chroniton.Schedules;
using System.Threading;
using Chroniton.Jobs;

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

            var job = new SimpleParameterizedJob<string>(
                (parameter, scheduledTime) => Task.Run(() => 
                Console.WriteLine($"{parameter}\tscheduled: {scheduledTime.ToString("o")}")));

            var schedule = new EveryXTimeSchedule(TimeSpan.FromSeconds(1));

            var scheduledJob = singularity.ScheduleParameterizedJob(
                schedule, job, "Hello World", true); //starts immediately

            var startTime = DateTime.UtcNow.Add(TimeSpan.FromSeconds(5));

            var scheduledJob2 = singularity.ScheduleParameterizedJob(
                schedule, job, "Hello World 2", startTime);

            singularity.Start();

            Thread.Sleep(10 * 1000);

            singularity.StopScheduledJob(scheduledJob);

            Thread.Sleep(5 * 1000);

            singularity.Stop();

            Console.ReadKey();
        }
    }
}
