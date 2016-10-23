using Chroniton.Jobs;
using Chroniton.Schedules;
using System;
using System.Threading.Tasks;

namespace Chroniton.NetCore.Example
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var factory = new ChronitonFactory();
			var singularity = factory.GetSingularity();
			var scheduler = factory.GetJobScheduler<InMemoryContinuum>();

			var job = new SimpleParameterizedJob<string>((parameter, scheduledTime) =>
				Console.WriteLine($"{parameter}\tscheduled: {scheduledTime.ToString("o")}"));

			var schedule = new EveryXTimeSchedule(TimeSpan.FromSeconds(1));

			var scheduledJob = scheduler.ScheduleParameterizedJob(
				schedule, job, "Hello World", true); //starts immediately

			var startTime = DateTime.UtcNow.Add(TimeSpan.FromSeconds(5));
			var schedule2 = new CronSchedule("* * * * * * *");

			var scheduledJob2 = scheduler.ScheduleParameterizedJob(
				schedule2, job, "Hello World 2", startTime);

			var scheduledJob3 = scheduler.ScheduleParameterizedJob(
				new RunOnceSchedule(TimeSpan.FromSeconds(3)), job, "Hello World 3", false);

			singularity.Start();

			Task.Delay(10 * 1000).Wait();

			scheduler.StopScheduledJob(scheduledJob);

			Task.Delay(5 * 1000).Wait();

			singularity.Stop();

			Console.ReadKey();
		}
	}
}