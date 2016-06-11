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

        public class MethodTests: SingularityTests
        {
            Singularity UnderTest;
            Mock<ISchedule> MockSchedule;
            Mock<IJob> MockJob;
            Mock<IParameterizedJob<string>> MockParamJob;
            int callCount;
            Queue<DateTime> scheduledTimes;
            AutoResetEvent _TestTrigger;

            [SetUp]
            public void SetUpScheduleAndJob()
            {
                UnderTest = Singularity.Instance;
                MockSchedule = new Mock<ISchedule>();
                MockJob = new Mock<IJob>();
                MockParamJob = new Mock<IParameterizedJob<string>>();
                _TestTrigger = new AutoResetEvent(false);
                _TestTrigger.Reset();

                UnderTest.OnSuccess += UnderTest_OnSuccess;

                callCount = 0;

                MockJob.Setup(j => j.Start()).Callback(() => callCount++);
                MockParamJob.Setup(j => j.Start(It.IsAny<string>())).Callback(() => callCount++);


                scheduledTimes = new Queue<DateTime>();
                MockSchedule.Setup(s => s.NextScheduledTime(It.IsAny<DateTime>()))
                    .Returns(() => scheduledTimes.Count > 0 ? scheduledTimes.Dequeue() : DateTime.MaxValue);
            }

            private void UnderTest_OnSuccess(ScheduledJobEventArgs job)
            {
                _TestTrigger.Set();
            }

            [TearDown]
            public void Stop()
            {
                UnderTest.Stop();
                UnderTest.OnSuccess -= UnderTest_OnSuccess;
            }

            public class WhenScheduledNow : MethodTests
            {
                [Test]
                public void ShouldRun()
                {
                    UnderTest.ScheduleJob(MockSchedule.Object, MockJob.Object, true);
                    UnderTest.Start();
                    _TestTrigger.WaitOne();
                    Assert.AreEqual(1, callCount);
                }

                [Test]
                public void ParameterizedShouldRun()
                {
                    UnderTest.ScheduleParameterizedJob(MockSchedule.Object, MockParamJob.Object, "hello", true);
                    UnderTest.Start();
                    _TestTrigger.WaitOne();
                    Assert.AreEqual(1, callCount);
                }

            }
        }
    }

    
}
