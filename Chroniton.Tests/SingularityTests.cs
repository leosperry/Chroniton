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
            Exception reportedException;

            [SetUp]
            public void Setup()
            {
                UnderTest = Singularity.Instance;

                scheduledTimes = new Queue<DateTime>();

                MockSchedule = new Mock<ISchedule>();
                MockSchedule.Setup(s => s.NextScheduledTime(It.IsAny<DateTime>()))
                    .Returns(() => scheduledTimes.Count > 0 ? scheduledTimes.Dequeue() : DateTime.MaxValue);

                successManualReset = new ManualResetEvent(false);
                UnderTest.OnSuccess += UnderTest_OnSuccess;

                errorManualReset = new ManualResetEvent(false);
                UnderTest.OnJobError += UnderTest_OnJobError;
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
            }

            public class ScheduleTests : FunctionalTests
            {
                int callCount;
                Mock<IJob> MockJob;
                Mock<IParameterizedJob<string>> MockParamJob;

                [SetUp]
                public void SetUpScheduleAndJob()
                {
                    callCount = 0;

                    MockJob = new Mock<IJob>();
                    MockJob.Setup(j => j.Start()).Callback(() => callCount++);

                    MockParamJob = new Mock<IParameterizedJob<string>>();
                    MockParamJob.Setup(j => j.Start(It.IsAny<string>())).Callback(() => callCount++);
                }

                
                public class WhenScheduledNow : ScheduleTests
                {
                    [Test]
                    public void ShouldRun()
                    {
                        UnderTest.ScheduleJob(MockSchedule.Object, MockJob.Object, true);
                        UnderTest.Start();
                        successManualReset.WaitOne(1000);
                        Assert.AreEqual(1, callCount);
                        successManualReset.Reset();
                    }

                    [Test]
                    public void ParameterizedShouldRun()
                    {
                        UnderTest.ScheduleParameterizedJob(MockSchedule.Object, MockParamJob.Object, "hello", true);
                        UnderTest.Start();
                        successManualReset.WaitOne();
                        Assert.AreEqual(1, callCount);
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
                        successManualReset.WaitOne(200);
                        Assert.AreEqual(0, callCount);
                        successManualReset.WaitOne(5000);
                        Assert.AreEqual(1, callCount);

                        successManualReset.Reset();
                    }

                    [Test]
                    public void ParameterizedShouldRunOnSchedule()
                    {
                        UnderTest.ScheduleParameterizedJob(MockSchedule.Object, MockParamJob.Object, "", false);
                        UnderTest.Start();
                        successManualReset.WaitOne(200);
                        Assert.AreEqual(0, callCount);
                        successManualReset.WaitOne(5000);
                        Assert.AreEqual(1, callCount);

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
                        successManualReset.WaitOne(200);
                        Assert.AreEqual(0, callCount);
                        successManualReset.WaitOne(5000);
                        Assert.AreEqual(1, callCount);

                        successManualReset.Reset();
                    }

                    [Test]
                    public void ParameterizedShouldRun()
                    {
                        var startTime = DateTime.UtcNow.Add(TimeSpan.FromSeconds(4));

                        UnderTest.ScheduleParameterizedJob(MockSchedule.Object, MockParamJob.Object, "hello", startTime);
                        UnderTest.Start();
                        successManualReset.WaitOne(200);
                        Assert.AreEqual(0, callCount);
                        successManualReset.WaitOne(5000);
                        Assert.AreEqual(1, callCount);

                        successManualReset.Reset();
                    }
                }
            }

            public class WhenThreadsExhausted: FunctionalTests
            {
                SimpleJob simpJob1;
                SimpleJob simpJob2;
                ManualResetEvent delayReset;

                [SetUp]
                public void SetItUp()
                {
                    delayReset = new ManualResetEvent(false);
                    UnderTest.MaximumThreads = 1;
                    simpJob1 = new SimpleJob(() => Task.Delay(2000));
                    simpJob2 = new SimpleJob(() => Task.Run(() => delayReset.Set()));
                }

                [Test]
                public void ShouldWaitAndExecute()
                {
                    UnderTest.ScheduleJob(MockSchedule.Object, simpJob1, true);
                    UnderTest.ScheduleJob(MockSchedule.Object, simpJob2, true);
                    UnderTest.Start();
                    Assert.False(delayReset.WaitOne(1000));
                    Assert.True(delayReset.WaitOne(3000));
                }
            }

            public class WhenJobThrowsException : FunctionalTests
            {
                Mock<IJob> exeptionJob;

                [SetUp]
                public void SetUpException()
                {
                    exeptionJob = new Mock<IJob>();
                    exeptionJob.Setup(j => j.Start()).Throws(new DivideByZeroException());
                }

                [Test]
                public void ShouldReportError()
                {
                    UnderTest.ScheduleJob(MockSchedule.Object, exeptionJob.Object, true);
                    UnderTest.Start();
                    Assert.True(errorManualReset.WaitOne());
                    Assert.IsInstanceOf<DivideByZeroException>(reportedException);
                }

                [Test]
                public void ShouldReportErrorInAsyncTask()
                {
                    SimpleJob testJob = new SimpleJob(() => Task.Run(() => { throw new DivideByZeroException(); }));
                    UnderTest.ScheduleJob(MockSchedule.Object, exeptionJob.Object, true);
                    UnderTest.Start();
                    Assert.True(errorManualReset.WaitOne());
                    Assert.IsInstanceOf<DivideByZeroException>(reportedException);
                }
            }
        }

    }
}
