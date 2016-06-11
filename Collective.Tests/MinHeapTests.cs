using NUnit.Framework;
using System;
using System.Linq;
using TheCollective;

namespace Collective.Tests
{
    [TestFixture]
    public class MinHeapTests
    {
        public class CtorTests: MinHeapTests
        {
            [Test]
            public void EmptyCtorShouldHave16Capacity()
            {
                MinHeap<int> heap = new MinHeap<int>();
                Assert.AreEqual(16, heap.Capacity); 
            }

            [Test]
            public void ShouldSetCapacity()
            {
                MinHeap<string> heap = new MinHeap<string>(42);
                Assert.AreEqual(42, heap.Capacity);
            }

            [Test]
            public void ShouldThrowExceptionWhenLessThan1()
            {
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                    new MinHeap<int>(0));
                Assert.True(ex.Message.StartsWith("capacity must be greater than 0"));
            }

            [Test]
            public void WhenPassedCollection_ShouldInitializeCorrectly()
            {
                MinHeap<int> heap = new MinHeap<int>(Enumerable.Range(0, 10).ToArray());
                Assert.AreEqual(15, heap.Capacity);
            }
        }

        public class MethodTests : MinHeapTests
        {
            protected MinHeap<int> heapUnderTest;

            [SetUp]
            public void initializeHeap()
            {
                heapUnderTest = new MinHeap<int>();
            }

            public class AddTests : MethodTests
            {
                [Test]
                public void ShouldThrowArgumentNull()
                {
                    var heap = new MinHeap<string>();
                    Assert.Throws<ArgumentNullException>(() => heap.Add(null));
                }

                [Test]
                public void CountShouldIncrease()
                {
                    Assert.AreEqual(0, heapUnderTest.Count);
                    heapUnderTest.Add(0);
                    Assert.AreEqual(1, heapUnderTest.Count);
                }

                [Test]
                public void ShouldNotChangeCapacityWhenNotReached()
                {
                    Assert.AreEqual(16, heapUnderTest.Capacity);
                    heapUnderTest.Add(0);
                    Assert.AreEqual(16, heapUnderTest.Capacity);
                }

                [Test]
                public void CapacityShouldIncreaseWhenReached()
                {
                    for (int i = 0; i < 17; i++)
                    {
                        heapUnderTest.Add(i);
                    }
                    Assert.AreEqual(17, heapUnderTest.Count);
                    Assert.AreEqual(32, heapUnderTest.Capacity);
                }
            }

            public class AddRangeTests : MethodTests
            {
                [Test]
                public void ShouldAddAll()
                {
                    heapUnderTest.AddRange(Enumerable.Range(0, 17));
                    Assert.AreEqual(17, heapUnderTest.Count);
                    Assert.AreEqual(32, heapUnderTest.Capacity);
                }
            }

            public class PeekTests : MethodTests
            {
                [Test]
                public void ShouldReturnItem()
                {
                    heapUnderTest.Add(42);
                    Assert.AreEqual(42, heapUnderTest.Peek());
                }
            }

            public class ExtractTests : MethodTests
            {
                [Test]
                public void ShouldReturnSmallestAndCountShouldDecrease()
                {
                    heapUnderTest.Add(1701);
                    heapUnderTest.Add(42);
                    Assert.AreEqual(42, heapUnderTest.Extract());
                    Assert.AreEqual(1, heapUnderTest.Count);
                }

                [Test]
                public void ShouldThrowWhenNothing()
                {
                    Assert.Throws<Exception>(() => heapUnderTest.Extract());
                }
            }

            public class TryExtractTests : MethodTests
            {
                [Test]
                public void WhenEmptyShouldReturnFalse()
                {
                    int item;
                    Assert.False(heapUnderTest.TryExtract(out item));
                    Assert.AreEqual(default(int), item);
                }

                [Test]
                public void ShouldReturnItemAndReturnTrue()
                {
                    heapUnderTest.Add(42);
                    int item;
                    Assert.True(heapUnderTest.TryExtract(out item));
                    Assert.AreEqual(42, item);
                }
            }

            public class FunctionalTest: MethodTests
            {
                [Test]
                public void ShouldDoTheThings()
                {
                    int someNumber = 9;
                    Assert.AreEqual(0, heapUnderTest.Count);
                    for (int i = 0; i < someNumber; i++)
                    {
                        heapUnderTest.Add(1701);
                        heapUnderTest.Add(-2);
                        heapUnderTest.Add(42);
                        heapUnderTest.Add(i);
                    }

                    Assert.AreEqual(64, heapUnderTest.Capacity);

                    for (int i = 0; i < someNumber; i++)
                    {
                        var item = heapUnderTest.Extract();
                        Assert.AreEqual(-2, item);
                        Assert.AreEqual(someNumber * 4 - i - 1, heapUnderTest.Count);
                    }

                    for (int i = 0; i < someNumber; i++)
                    {
                        var item = heapUnderTest.Extract();
                        Assert.AreEqual(i, item);
                    }
                }
            }
        }
    }
}
