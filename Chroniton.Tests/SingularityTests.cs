using Chroniton.Jobs;
using Chroniton.Schedules;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public class FunctionalTests: SingularityTests
        {
            Singularity UnderTest;
            Mock<ISchedule> MockSchedule;
            Queue<DateTime> scheduledTimes;
            ManualResetEvent successManualReset;
            ManualResetEvent errorManualReset;
            ManualResetEvent scheduleErrorManualReset;
            Exception reportedException;

            [SetUp]
            public void Setup()
            {
                UnderTest = Singularity.Instance;
                UnderTest.MaximumThreads = 5;

                scheduledTimes = new Queue<DateTime>();

                MockSchedule = new Mock<ISchedule>();
                MockSchedule.Setup(s => s.NextScheduledTime(It.IsAny<DateTime>()))
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
                        UnderTest.ScheduleJob(MockSchedule.Object, MockJob.Object, true);
                        UnderTest.Start();
                        Assert.True(successManualReset.WaitOne(1000));
                        successManualReset.Reset();
                    }

                    [Test]
                    public void ParameterizedShouldRun()
                    {
                        UnderTest.ScheduleParameterizedJob(MockSchedule.Object, MockParamJob.Object, "hello", true);
                        UnderTest.Start();
                        Assert.True(successManualReset.WaitOne(5000));
                        successManualReset.Reset();
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
                        UnderTest.ScheduleJob(MockSchedule.Object, MockJob.Object, false);
                        UnderTest.Start();
                        Assert.False(successManualReset.WaitOne(200));
                        Assert.True(successManualReset.WaitOne(5000));

                        successManualReset.Reset();
                    }

                    [Test]
                    public void ParameterizedShouldRunOnSchedule()
                    {
                        UnderTest.ScheduleParameterizedJob(MockSchedule.Object, MockParamJob.Object, "", false);
                        UnderTest.Start();
                        Assert.False(successManualReset.WaitOne(200));
                        Assert.True(successManualReset.WaitOne(5000));

                        successManualReset.Reset();
                    }
                }

                public class WhenScheduledLater : ScheduleTests
                {
                    [Test]
                    public void ShouldRunLater()
                    {
                        var startTime = DateTime.UtcNow.Add(TimeSpan.FromSeconds(4));
                        UnderTest.ScheduleJob(MockSchedule.Object, MockJob.Object, startTime);
                        UnderTest.Start();
                        Assert.False(successManualReset.WaitOne(200));
                        Assert.True(successManualReset.WaitOne(5000));

                        successManualReset.Reset();
                    }

                    [Test]
                    public void ParameterizedShouldRun()
                    {
                        var startTime = DateTime.UtcNow.Add(TimeSpan.FromSeconds(4));

                        UnderTest.ScheduleParameterizedJob(MockSchedule.Object, MockParamJob.Object, "hello", startTime);
                        UnderTest.Start();
                        Assert.False(successManualReset.WaitOne(200));
                        Assert.True(successManualReset.WaitOne(5000));

                        successManualReset.Reset();
                    }
                }
            }

            public class WhenThreadsExhausted: FunctionalTests
            {
                SimpleJob longRunningJob;
                SimpleJob waitingJob;
                ManualResetEvent delayReset;

                [SetUp]
                public void SetItUp()
                {
                    delayReset = new ManualResetEvent(false);
                    UnderTest.MaximumThreads = 1;
                    longRunningJob = new SimpleJob(async (dt) => await Task.Delay(2000));
                    waitingJob = new SimpleJob((dt) => Task.Run(() => delayReset.Set()));
                }

                [Test]
                public void ShouldWaitAndExecute()
                {
                    UnderTest.ScheduleJob(MockSchedule.Object, longRunningJob, true);
                    UnderTest.ScheduleJob(MockSchedule.Object, waitingJob, true);

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

                    UnderTest.ScheduleJob(MockSchedule.Object, exeptionJob.Object, true);
                    UnderTest.Start();
                    Assert.True(errorManualReset.WaitOne());
                    Assert.IsInstanceOf<DivideByZeroException>(reportedException);
                }

                [Test]
                public void ShouldReportErrorInAsyncTask()
                {
                    SimpleJob testJob = new SimpleJob(async (dt) => await Task.Run(() => { throw new DivideByZeroException(); }));
                    UnderTest.ScheduleJob(MockSchedule.Object, exeptionJob.Object, true);
                    UnderTest.Start();
                    Assert.True(errorManualReset.WaitOne());
                    Assert.IsInstanceOf<DivideByZeroException>(reportedException);
                }
            }

            public class SetNextScheduleTests: FunctionalTests
            {                     
                TestSchedule TestSchedule;

                public class WhenNextAfterPrevious: SetNextScheduleTests
                {
                    [SetUp]
                    public void InitSchedule()
                    {
                        TestSchedule = new TestSchedule();
                    }

                    [Test]
                    public void ShouldExecute()
                    {
                        var job = new SimpleJob((dt)=>Task.CompletedTask);
                        UnderTest.ScheduleJob(TestSchedule, job, true);
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
                        var job = new SimpleJob((dt) => Task.CompletedTask)
                        { ScheduleMissedBehavior = ScheduleMissedBehavior.RunAgain };
                        UnderTest.ScheduleJob(TestSchedule, job, true);
                        UnderTest.Start();
                        Assert.True(successManualReset.WaitOne());
                        successManualReset.Reset();
                        Assert.True(successManualReset.WaitOne());
                    }

                    [Test]
                    public void ShouldExecuteOnNextWhenSkip()
                    {
                        var job = new SimpleJob((dt) => Task.Run(() => TestSchedule.NextSchedule = NextScheduleType.Skip))
                        { ScheduleMissedBehavior = ScheduleMissedBehavior.SkipExecution };
                        UnderTest.ScheduleJob(TestSchedule, job, true);
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
                        var job = new SimpleJob((dt) => Task.CompletedTask)
                        { ScheduleMissedBehavior = ScheduleMissedBehavior.SkipExecution };
                        UnderTest.ScheduleJob(TestSchedule, job, true);
                        UnderTest.Start();
                        Assert.True(successManualReset.WaitOne());
                        successManualReset.Reset();
                        Assert.True(scheduleErrorManualReset.WaitOne());
                    }

                    [Test]
                    public void ShouldReportExceptionWhenExceptionSet()
                    {
                        var job = new SimpleJob((dt) => Task.CompletedTask)
                        { ScheduleMissedBehavior = ScheduleMissedBehavior.ThrowException };
                        UnderTest.ScheduleJob(TestSchedule, job, true);
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
                        MockSchedule.Setup(s => s.NextScheduledTime(It.IsAny<DateTime>()))
                            .Throws(new DivideByZeroException());

                        MockJob = new Mock<IJob>();
                    }

                    ManualResetEvent reset;

                    [Test]
                    public void ShouldReportException()
                    {
                        UnderTest.OnScheduleError += UnderTest_OnScheduleError1;
                        reset = new ManualResetEvent(false);
                        UnderTest.ScheduleJob(MockSchedule.Object, MockJob.Object, true);
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
                    TestJob = new SimpleJob((dt)=> Task.CompletedTask);
                    TestSchedule = new TestSchedule() { NextSchedule = NextScheduleType.Later };
                }

                [Test]
                public void ShouldStop()
                {
                    UnderTest.MaximumThreads = 1;
                    var scheduledJob = UnderTest.ScheduleJob(TestSchedule, TestJob, true);
                    UnderTest.Start();
                    UnderTest.StopScheduledJob(scheduledJob);
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
                        Name= "long running job"
                    };
                    UnderTest.ScheduleJob(TestSchedule, longRunningJob, true);
                    string state = "never run";
                    var jobToBeRemoved = new SimpleJob((dt) => Task.Run(() => state = "I ran"))
                    {
                        Name = "job to be removed"
                    };

                    var scheduledJob = UnderTest.ScheduleJob(TestSchedule, jobToBeRemoved, true);
                    UnderTest.Start();

                    UnderTest.StopScheduledJob(scheduledJob);

                    Assert.AreEqual("never run", state);
                }
            }
        }
    }

    public enum NextScheduleType
    {
        Now, Earlier, Later, Exception,
        Skip
    }

    public class TestSchedule : ISchedule
    {

        public string Name { get; set; }

        public NextScheduleType NextSchedule { get; set; } = NextScheduleType.Now;

        public DateTime NextScheduledTime(DateTime afterThisTime)
        {
            switch (NextSchedule)
            {
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
