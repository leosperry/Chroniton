using NUnit.Framework;
using System.Linq;

namespace TheCollective.Tests
{
    [TestFixture]
    public class ConcurrentHashSetTests
    {
        public class CtorTests : ConcurrentHashSetTests
        {
            [Test]
            public void EmptyCtorShouldReturnHashSet()
            {
                ConcurrentHashSet<int> set = new ConcurrentHashSet<int>();
                Assert.NotNull(set);
            }

            [Test]
            public void CollectionCtorShouldReturnHashSet()
            {
                ConcurrentHashSet<int> set = new ConcurrentHashSet<int>(Enumerable.Range(0, 5));
                Assert.NotNull(set);
                Assert.AreEqual(5, set.Count);
            }
        }

        public class MethodTests : ConcurrentHashSetTests
        {
            ConcurrentHashSet<int> _setUnderTest = 
                new ConcurrentHashSet<int>(Enumerable.Range(0, 5));

            public class AddTests : MethodTests
            {
                [Test]
                public void ShouldAdd()
                {
                    _setUnderTest.Add(1701);
                    Assert.AreEqual(6, _setUnderTest.Count);
                    Assert.True(_setUnderTest.Contains(1701));
                }
            }

            public class RemoveTests : MethodTests
            {
                [Test]
                public void ShouldRemove()
                {
                    _setUnderTest.Remove(3);
                    Assert.AreEqual(4, _setUnderTest.Count);
                    Assert.False(_setUnderTest.Contains(3));
                }
            }

            public class ClearTests : MethodTests
            {
                [Test]
                public void ShouldClear()
                {
                    _setUnderTest.Clear();
                    Assert.AreEqual(0, _setUnderTest.Count);
                    Assert.False(_setUnderTest.Any());
                }
            }

            public class CopyToTests : MethodTests
            {
                int[] ints = new int[5];
                
                [Test]
                public void ShouldCopy()
                {
                    _setUnderTest.CopyTo(ints, 0);
                    Assert.AreEqual(4, ints[4]);
                }
            }

            public class GetEnumeratorTests: MethodTests
            {
                [Test]
                public void ShouldEnumerate()
                {
                    Assert.AreEqual(4, _setUnderTest.Max());
                }
            }
        }

    }
}
