using Chroniton.Jobs;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chroniton.Tests
{
	[TestFixture]
	public class SingularityTests
	{
		public class InstanceTests : SingularityTests
		{
			[Test]
			public void ShouldReturnInstance()
			{
				var instance1 = Singularity.Instance;
				var instance2 = Singularity.Instance;
				Assert.NotNull(instance1);
				Assert.AreEqual(instance1, instance2);
			}
		}

		public class FunctionalTests : SingularityTests
		{
			Singularity UnderTest;
			Mock<ISchedule> MockSchedule;
			Queue<DateTime> scheduledTimes;
			ManualResetEvent successManualReset;
			ManualResetEvent errorManualReset;
			ManualResetEvent scheduleErrorManualReset;
			Exception reportedException;
			IJobScheduler scheduler = new ChronitonFactory().GetJobScheduler<InMemoryContinuum>();
			InMemoryContinuum _continuum = new ContinuumFactory().GetContinuum<InMemoryContinuum>();

			[SetUp]
			public void Setup()
			{
				UnderTest = Singularity.Instance;
				UnderTest.MaximumThreads = 5;

				scheduledTimes = new Queue<DateTime>();

				MockSchedule = new Mock<ISchedule>();
				MockSchedule.Setup(s => s.NextScheduledTime(It.IsAny<ScheduledJobBase>()))
					.Returns(() => scheduledTimes.Count > 0 ? scheduledTimes.Dequeue() : DateTime.MaxValue);

				successManualReset = new ManualResetEvent(false);
				UnderTest.OnSuccess += UnderTest_OnSuccess;

				errorManualReset = new ManualResetEvent(false);
				UnderTest.OnJobError += UnderTest_OnJobError;

				scheduleErrorManualReset = new ManualResetEvent(false);
				UnderTest.OnScheduleError += UnderTest_OnScheduleError;
			}

			private void UnderTest_OnScheduleError(ScheduledJobEventArgs job, Exception e)
			{
				scheduleErrorManualReset.Set();
				reportedException = e;
			}

			private void UnderTest_OnJobError(ScheduledJobEventArgs job, Exception e)
			{
				errorManualReset.Set();
				reportedException = e;
			}

			private void UnderTest_OnSuccess(ScheduledJobEventArgs job)
			{
				successManualReset.Set();
			}

			[TearDown]
			public void Stop()
			{
				UnderTest.Stop();
				UnderTest.OnSuccess -= UnderTest_OnSuccess;
				UnderTest.OnJobError -= UnderTest_OnJobError;
				UnderTest.OnScheduleError -= UnderTest_OnScheduleError;
				successManualReset.Reset();
				errorManualReset.Reset();
				_continuum.ClearAll();
			}

			public class ScheduleTests : FunctionalTests
			{
				Mock<IJob> MockJob;
				Mock<IParameterizedJob<string>> MockParamJob;

				[SetUp]
				public void SetUpScheduleAndJob()
				{
					MockJob = new Mock<IJob>();

					MockParamJob = new Mock<IParameterizedJob<string>>();
				}

				public class WhenScheduledNow : ScheduleTests
				{
					[Test]
					public void ShouldRun()
					{
						scheduler.ScheduleJob(MockSchedule.Object, MockJob.Object, true);
						UnderTest.Start();
						Assert.True(successManualReset.WaitOne(1000));
					}

					[Test]
					public void ParameterizedShouldRun()
					{
						scheduler.ScheduleParameterizedJob(MockSchedule.Object, MockParamJob.Object, "hello", true);
						UnderTest.Start();
						Assert.True(successManualReset.WaitOne(5000));
					}
				}

				public class WhenNotScheduledNow : ScheduleTests
				{
					[SetUp]
					public void SetSchedule()
					{
						scheduledTimes.Enqueue(DateTime.UtcNow.AddSeconds(4));
					}

					[Test]
					public void ShouldRunOnSchedule()
					{
						scheduledTimes.Enqueue(DateTime.UtcNow.AddSeconds(4));

						scheduler.ScheduleJob(MockSchedule.Object, MockJob.Object, false);
						UnderTest.Start();
						Assert.False(successManualReset.WaitOne(200));
						Assert.True(successManualReset.WaitOne(5000));
					}

					[Test]
					public void ParameterizedShouldRunOnSchedule()
					{
						scheduledTimes.Enqueue(DateTime.UtcNow.AddSeconds(4));

						scheduler.ScheduleParameterizedJob(MockSchedule.Object, MockParamJob.Object, "", false);
						UnderTest.Start();
						Assert.False(successManualReset.WaitOne(2000));
						Assert.True(successManualReset.WaitOne(5000));
					}
				}

				public class WhenScheduledLater : ScheduleTests
				{
					[Test]
					public void ShouldRunLater()
					{
						var startTime = DateTime.UtcNow.Add(TimeSpan.FromSeconds(4));
						scheduler.ScheduleJob(MockSchedule.Object, MockJob.Object, startTime);
						UnderTest.Start();
						Assert.False(successManualReset.WaitOne(200));
						Assert.True(successManualReset.WaitOne(5000));
					}

					[Test]
					public void ParameterizedShouldRun()
					{
						var startTime = DateTime.UtcNow.Add(TimeSpan.FromSeconds(4));

						scheduler.ScheduleParameterizedJob(MockSchedule.Object, MockParamJob.Object, "hello", startTime);
						UnderTest.Start();
						Assert.False(successManualReset.WaitOne(200));
						Assert.True(successManualReset.WaitOne(25000));
					}
				}
			}

			public class WhenThreadsExhausted : FunctionalTests
			{
				SimpleJob longRunningJob;
				SimpleJob waitingJob;
				ManualResetEvent delayReset;

				[SetUp]
				public void SetItUp()
				{
					delayReset = new ManualResetEvent(false);
					UnderTest.MaximumThreads = 1;
					longRunningJob = new SimpleJob(dt => Task.Delay(2000).Wait());
					waitingJob = new SimpleJob((dt) => Task.Run(() => delayReset.Set()));
				}

				[Test]
				public void ShouldWaitAndExecute()
				{
					scheduler.ScheduleJob(MockSchedule.Object, longRunningJob, true);
					scheduler.ScheduleJob(MockSchedule.Object, waitingJob, true);

					UnderTest.Start();

					Assert.False(delayReset.WaitOne(200));
					Assert.True(delayReset.WaitOne(10000));
				}
			}

			public class WhenJobThrowsException : FunctionalTests
			{
				Mock<IJob> exeptionJob;

				[Test]
				public void ShouldReportError()
				{
					exeptionJob = new Mock<IJob>();
					exeptionJob.Setup(j => j.Start(It.IsAny<DateTime>())).Throws(new DivideByZeroException());

					scheduler.ScheduleJob(MockSchedule.Object, exeptionJob.Object, true);
					UnderTest.Start();
					Assert.True(errorManualReset.WaitOne());
					Assert.IsInstanceOf<DivideByZeroException>(reportedException);
				}

				[Test]
				public void ShouldReportErrorInAsyncTask()
				{
					SimpleJob testJob = new SimpleJob(async (dt) => await Task.Run(() => { throw new DivideByZeroException(); }));
					scheduler.ScheduleJob(MockSchedule.Object, exeptionJob.Object, true);
					UnderTest.Start();
					Assert.True(errorManualReset.WaitOne());
					Assert.IsInstanceOf<DivideByZeroException>(reportedException);
				}
			}

			public class SetNextScheduleTests : FunctionalTests
			{
				TestSchedule TestSchedule;

				public class WhenNever : SetNextScheduleTests
				{
					[SetUp]
					public void InitSchedule()
					{
						TestSchedule = new TestSchedule()
						{
							NextSchedule = NextScheduleType.Never
						};
					}

					[Test]
					public void ShouldNotRun()
					{
						var job = new SimpleJob((dt) => { });
						scheduler.ScheduleJob(TestSchedule, job, true);
						UnderTest.Start();
						Assert.True(successManualReset.WaitOne(205000));
						successManualReset.Reset();
						Assert.False(successManualReset.WaitOne(500));
					}
				}

				public class WhenNextAfterPrevious : SetNextScheduleTests
				{
					[SetUp]
					public void InitSchedule()
					{
						TestSchedule = new TestSchedule();
					}

					[Test]
					public void ShouldExecute()
					{
						var job = new SimpleJob((dt) => { });
						scheduler.ScheduleJob(TestSchedule, job, true);
						UnderTest.Start();
						Assert.True(successManualReset.WaitOne());
						successManualReset.Reset();
						Assert.True(successManualReset.WaitOne());
					}
				}

				public class WhenNextBeforePrevious : SetNextScheduleTests
				{
					[SetUp]
					public void InitSchedule()
					{
						TestSchedule = new TestSchedule() { NextSchedule = NextScheduleType.Earlier };
					}

					[Test]
					public void ShouldExecuteWhenRunAgain()
					{
						var job = new SimpleJob((dt) => { })
						{ ScheduleMissedBehavior = ScheduleMissedBehavior.RunAgain };
						scheduler.ScheduleJob(TestSchedule, job, true);
						UnderTest.Start();
						Assert.True(successManualReset.WaitOne());
						successManualReset.Reset();
						Assert.True(successManualReset.WaitOne());
					}

					[Test]
					public void ShouldExecuteOnNextWhenSkip()
					{
						var job = new SimpleJob((dt) => TestSchedule.NextSchedule = NextScheduleType.Skip)
						{ ScheduleMissedBehavior = ScheduleMissedBehavior.SkipExecution };
						scheduler.ScheduleJob(TestSchedule, job, true);
						UnderTest.Start();
						Assert.True(successManualReset.WaitOne());
						successManualReset.Reset();
						Assert.False(successManualReset.WaitOne(1000));
						Assert.False(successManualReset.WaitOne(2000));
						Assert.True(successManualReset.WaitOne(3000));
					}

					[Test]
					public void ShouldReportExceptionWhenSkipAndStillEarlier()
					{
						var job = new SimpleJob((dt) => { })
						{ ScheduleMissedBehavior = ScheduleMissedBehavior.SkipExecution };
						scheduler.ScheduleJob(TestSchedule, job, true);
						UnderTest.Start();
						Assert.True(successManualReset.WaitOne());
						successManualReset.Reset();
						Assert.True(scheduleErrorManualReset.WaitOne());
					}

					[Test]
					public void ShouldReportExceptionWhenExceptionSet()
					{
						var job = new SimpleJob((dt) => { })
						{ ScheduleMissedBehavior = ScheduleMissedBehavior.ThrowException };
						scheduler.ScheduleJob(TestSchedule, job, true);
						UnderTest.Start();
						Assert.True(successManualReset.WaitOne());
						successManualReset.Reset();
						Assert.True(scheduleErrorManualReset.WaitOne());
					}
				}

				public class WhenScheduleThrows : SetNextScheduleTests
				{
					Mock<IJob> MockJob;

					[SetUp]
					public void SetException()
					{
						MockSchedule = new Mock<ISchedule>();
						MockSchedule.Setup(s => s.NextScheduledTime(It.IsAny<ScheduledJob>()))
							.Throws(new DivideByZeroException());

						MockJob = new Mock<IJob>();
					}

					ManualResetEvent reset;

					[Test]
					public void ShouldReportException()
					{
						UnderTest.OnScheduleError += UnderTest_OnScheduleError1;
						reset = new ManualResetEvent(false);
						scheduler.ScheduleJob(MockSchedule.Object, MockJob.Object, true);
						UnderTest.Start();
						Assert.True(reset.WaitOne());
						UnderTest.OnScheduleError -= UnderTest_OnScheduleError1;
					}

					private void UnderTest_OnScheduleError1(ScheduledJobEventArgs job, Exception e)
					{
						Assert.IsInstanceOf<DivideByZeroException>(e);
						reset.Set();
					}
				}
			}

			public class StopScheduledJob : FunctionalTests
			{
				SimpleJob TestJob;
				TestSchedule TestSchedule;

				[SetUp]
				public void SetUpSchedule()
				{
					TestJob = new SimpleJob((dt) => { });
					TestSchedule = new TestSchedule() { NextSchedule = NextScheduleType.Later };
				}

				[Test]
				public void ShouldStop()
				{
					UnderTest.MaximumThreads = 1;
					var scheduledJob = scheduler.ScheduleJob(TestSchedule, TestJob, true);
					UnderTest.Start();
					scheduler.StopScheduledJob(scheduledJob);
					successManualReset.WaitOne(200);
					successManualReset.Reset();
					Assert.False(successManualReset.WaitOne(2000));
				}

				[Test]
				public void ShouldNotReschedule()
				{
					UnderTest.MaximumThreads = 1;

					ManualResetEvent removedJobManual = new ManualResetEvent(false);
					var longRunningJob = new SimpleJob(async (dt) => await Task.Delay(5000))
					{
						Name = "long running job"
					};
					scheduler.ScheduleJob(TestSchedule, longRunningJob, true);
					string state = "never run";
					var jobToBeRemoved = new SimpleJob((dt) => Task.Run(() => state = "I ran"))
					{
						Name = "job to be removed"
					};

					var scheduledJob = scheduler.ScheduleJob(TestSchedule, jobToBeRemoved, true);
					UnderTest.Start();

					scheduler.StopScheduledJob(scheduledJob);

					Assert.AreEqual("never run", state);
				}
			}
		}
	}

	public enum NextScheduleType
	{
		Now, Earlier, Later, Exception, Skip, Never
	}

	public class TestSchedule : ISchedule
	{
		public string Name { get; set; }

		public NextScheduleType NextSchedule { get; set; } = NextScheduleType.Now;

		public DateTime NextScheduledTime(ScheduledJobBase scheduledJob)
		{
			switch (NextSchedule)
			{
				case NextScheduleType.Never:
					return Constants.Never;
				case NextScheduleType.Now:
					return DateTime.UtcNow;
				case NextScheduleType.Earlier:
					return DateTime.UtcNow.AddDays(-1);
				case NextScheduleType.Later:
					return DateTime.UtcNow.AddSeconds(2);
				case NextScheduleType.Skip:
					return DateTime.UtcNow.AddSeconds(4);
				case NextScheduleType.Exception:
				default:
					throw new DivideByZeroException();
			}
		}
	}
}
