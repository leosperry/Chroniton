﻿using Chroniton;
using System;
using System.Threading.Tasks;

namespace ChronitonExample
{
	class ConsoleWritingJob : IJob
	{
		public string Name { get; set; }

		int _runNumber = 0;

		public ScheduleMissedBehavior ScheduleMissedBehavior
		{
			get
			{
				return ScheduleMissedBehavior.RunAgain;
			}
		}

		public void Abort()
		{
			throw new NotImplementedException();
		}

		public async Task Start(DateTime scheduledTime)
		{
			await Task.Delay(500);
			Console.WriteLine($"on success {++_runNumber}");
		}
	}

	class NamedConsoleWritingJob : IParameterizedJob<string>
	{
		int _runNumber = 0;

		public string Name { get; set; }

		public ScheduleMissedBehavior ScheduleMissedBehavior
		{
			get
			{
				return ScheduleMissedBehavior.RunAgain;
			}
		}

		public void Abort()
		{
			throw new NotImplementedException();
		}

		public async Task Start(string parameter, DateTime scheduledTime)
		{
			await Task.Delay(500);
			Console.WriteLine($"success {++_runNumber} {parameter}");
		}
	}
}