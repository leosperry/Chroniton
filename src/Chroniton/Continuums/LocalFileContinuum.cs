using Chroniton.Jobs;
using Chroniton.Schedules;
using System;
using System.Threading.Tasks;

namespace Chroniton
{
	public class LocalFileContinuum : InMemoryContinuum
	{
		static readonly object _diskLock = new { };

		public override async Task CleanUp()
		{
			await saveToDisk();
		}

		public override void Initialize()
		{
			readAllFromDisk();

			var chronFactory = new ChronitonFactory();
			var inMem = chronFactory.GetJobScheduler<InMemoryContinuum>();

			inMem.ScheduleJob(new EveryXTimeSchedule(TimeSpan.FromMinutes(5)),
				new SimpleJob(dt => saveToDisk().Wait()), false);
		}

		private void readAllFromDisk()
		{

		}

		private async Task saveToDisk()
		{
			lock (_diskLock)
			{

			}
		}
	}

}
