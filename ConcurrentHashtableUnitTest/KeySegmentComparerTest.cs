using TvdP.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Collections.Generic;

namespace ConcurrentHashtableUnitTest
{
    
    
    /// <summary>
    ///This is a test class for KeySegmentComparerTest and is intended
    ///to contain all KeySegmentComparerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class KeySegmentComparerTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion



        /// <summary>
        ///A test for Equals
        ///</summary>
        void EqualsTestHelper<E, T>(IEqualityComparer<E> elementComparer, E element1, E element2, IEqualityComparer<T> tailComparer, T tail1, T tail2)
            where E : class
            where T : class
        {
            KeySegmentComparer<E, WeakKey<T>> target =
                new KeySegmentComparer<E, WeakKey<T>>()
                {
                    _elementComparer = new WeakKeyComparer<E> { _equalityComparer = elementComparer },
                    _tail = new WeakKeyComparer<T> { _equalityComparer = tailComparer }
                }
            ;

            var x = new KeySegment<E, WeakKey<T>>() { _elementReference = new WeakKey<E>(), _tail = new WeakKey<T>() }; 
            x.SetValue(element1, true);
            x._tail.SetValue(tail1, true);

            var xExpected = x;

            var y = new KeySegment<E, WeakKey<T>>() { _elementReference = new WeakKey<E>(), _tail = new WeakKey<T>() };
            y.SetValue(element2, true);
            y._tail.SetValue(tail2, true);

            var yExpected = y;

            Assert.IsFalse( target.Equals(ref x, true, ref y, true) );
            Assert.AreEqual(xExpected, x);
            Assert.AreEqual(yExpected, y);

            x._tail.SetValue(tail2, true);
            y.SetValue(element1, true);

            Assert.IsTrue(target.Equals(ref x, true, ref y, true));

            x.SetValue(element1, false);
            x._tail.SetValue(tail1, false);

            y.SetValue(element2, true);
            y._tail.SetValue(tail2, true);

            Assert.IsFalse(target.Equals(ref x, false, ref y, true));
            Assert.IsFalse(target.Equals(ref y, true, ref x, false));

            x._tail.SetValue(tail2, false);
            y.SetValue(element1, true);

            Assert.IsTrue(target.Equals(ref x, false, ref y, true));
            Assert.IsTrue(target.Equals(ref y, true, ref x, false));

            x.SetValue(element1, false);
            x._tail.SetValue(tail1, false);

            y.SetValue(element2, false);
            y._tail.SetValue(tail2, false);

            Assert.IsFalse(target.Equals(ref x, false, ref y, false));

            x._tail.SetValue(tail2, false);
            y.SetValue(element1, false);

            Assert.IsTrue(target.Equals(ref x, false, ref y, false));
        }

        [TestMethod()]
        public void EqualsTest()
        {
            EqualsTestHelper(EqualityComparer<object>.Default, new Object(), new Object(), EqualityComparer<string>.Default, "A", "B");
        }

        /// <summary>
        ///A test for GetHashCode
        ///</summary>
        void GetHashCodeTestHelper<E, T>(IEqualityComparer<E> elementComparer, E[] elements, IEqualityComparer<T> tailComparer, T[] tails)
            where E : class
            where T : class
        {
            KeySegmentComparer<E, WeakKey<T>> target =
                new KeySegmentComparer<E, WeakKey<T>>()
                {
                    _elementComparer = new WeakKeyComparer<E> { _equalityComparer = elementComparer },
                    _tail = new WeakKeyComparer<T> { _equalityComparer = tailComparer }
                }
            ;

            var x = new KeySegment<E, WeakKey<T>>() { _elementReference = new WeakKey<E>(), _tail = new WeakKey<T>() };

            List<int> codes = new List<int>();

            foreach(var element in elements)
                foreach (var tail in tails)
                {
                    x.SetValue(element, true);
                    x._tail.SetValue(tail, true);

                    var xCopy = x;

                    int code = target.GetHashCode(ref x, true);

                    Assert.AreEqual(x, xCopy);

                    codes.Add(code);

                    x.SetValue(element, false);
                    x._tail.SetValue(tail, false);

                    int code2 = target.GetHashCode(ref x, false);

                    Assert.AreEqual(code, code2);
                }

            Assert.IsTrue(codes.Distinct().Count() - codes.Count < codes.Count / 10);
        }

        [TestMethod()]
        public void GetHashCodeTest()
        {
            GetHashCodeTestHelper(
                EqualityComparer<object>.Default, 
                new Object[] { new Object(), new Object(), new Object(), new Object(), new Object(), }, 
                EqualityComparer<string>.Default, 
                new string[] { "A", "B", "C", "D", "E", "F" }
            );
        }
    }
}
