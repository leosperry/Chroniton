## Synopsis

A library for running tasks(jobs) on schedules. It supports:

* Strongly typed jobs
* Custom schedules
* Running jobs on multiple schedules
* Multiple jobs on a single schedule.
* Limiting the number of threads on which work is done
* Managing behaviors of jobs which run beyond their next scheduled time
* Dependency Injection initialization

## Code Example

	static void Main(string[] args)
	{
		ISingularityFactory factory = new SingularityFactory();
		ISingularity singularity = factory.GetSingularity();

		var job = new SimpleParameterizedJob<string>(
			anything => Task.Run(()=> Console.WriteLine(anything)));

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
	
In the above example, here's what happens:
The first job starts immediately and print's "Hello World" once every second.
Five seconds later the second job starts and prints "Hello World2" every second.
Five seconds later the first job stops and only the second job is running.
Five seconds later, Stop() is called and the second job also stops.

## Motivation

This project was inspired for the need to have a strongly typed .NET solution for running tasks on schedules. 

## Installation

nuget instructions will go here

## Contributors

Created by : Leonard Sperry
leosperry@outlook.com

## License

Licensed under the MIT License

## Future Features

* Serialization
* More Built in Schedule types
* 

