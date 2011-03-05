using TvdP.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace ConcurrentHashtableUnitTest
{
    
    
    /// <summary>
    ///This is a test class for WeakKeyComparerTest and is intended
    ///to contain all WeakKeyComparerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WeakKeyComparerTest
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
        void EqualsTestHelper<E>( E e1, E e2 )
            where E : class
        {
            WeakKeyComparer<E> target = new WeakKeyComparer<E>() { _equalityComparer = EqualityComparer<E>.Default } ; 
            var key1 = new WeakKey<E>() { _elementReference = new WeakReference(e1) };
            var key2 = new WeakKey<E>() { _elementReference = new WeakReference(e2) };

            Assert.IsFalse(target.Equals(ref key1, true, ref key2, true));
            Assert.IsFalse(target.Equals(ref key2, true, ref key1, true));

            Assert.IsTrue(target.Equals(ref key1, true, ref key1, true));
            Assert.IsTrue(target.Equals(ref key2, true, ref key2, true));

            key2.SetValue(e1, true);

            Assert.IsTrue(target.Equals(ref key1, true, ref key2, true));
            Assert.IsTrue(target.Equals(ref key2, true, ref key1, true));

            ((WeakReference)key1._elementReference).Target = null;

            Assert.IsFalse(target.Equals(ref key1, true, ref key2, true));
            Assert.IsFalse(target.Equals(ref key2, true, ref key1, true));

            ((WeakReference)key2._elementReference).Target = null;

            Assert.IsFalse(target.Equals(ref key1, true, ref key2, true));
            Assert.IsFalse(target.Equals(ref key2, true, ref key1, true));

            Assert.IsTrue(target.Equals(ref key1, true, ref key1, true));
            Assert.IsTrue(target.Equals(ref key2, true, ref key2, true));

            key1.SetValue(e1, true);
            key2.SetValue(e2, false);

            Assert.IsFalse(target.Equals(ref key1, true, ref key2, false));
            Assert.IsFalse(target.Equals(ref key2, false, ref key1, true));

            Assert.IsTrue(target.Equals(ref key1, true, ref key1, true));
            Assert.IsTrue(target.Equals(ref key2, false, ref key2, false));

            key2.SetValue(e1, false);

            Assert.IsTrue(target.Equals(ref key1, true, ref key2, false));
            Assert.IsTrue(target.Equals(ref key2, false, ref key1, true));

            key2.SetValue(null, false);

            Assert.IsFalse(target.Equals(ref key1, true, ref key2, false));
            Assert.IsFalse(target.Equals(ref key2, false, ref key1, true));

            ((WeakReference)key1._elementReference).Target = null;

            Assert.IsFalse(target.Equals(ref key1, true, ref key2, false));
            Assert.IsFalse(target.Equals(ref key2, false, ref key1, true));

            key1.SetValue(e1, false);
            key2.SetValue(e2, false);

            Assert.IsFalse(target.Equals(ref key1, false, ref key2, false));
            Assert.IsFalse(target.Equals(ref key2, false, ref key1, false));

            key2.SetValue(e1, false);

            Assert.IsTrue(target.Equals(ref key1, false, ref key2, false));
            Assert.IsTrue(target.Equals(ref key2, false, ref key1, false));        
        }

        [TestMethod()]
        public void EqualsTest()
        {
            EqualsTestHelper(new Object(), new Object());
        }

        /// <summary>
        ///A test for GetHashCode
        ///</summary>
        void GetHashCodeTestHelper<E>(E e)
            where E : class
        {
            WeakKeyComparer<E> target = new WeakKeyComparer<E>() { _equalityComparer = EqualityComparer<E>.Default };
            var key = new WeakKey<E>() { _elementReference = new WeakReference(e) };

            int hash1 = target.GetHashCode(ref key, true);

            key.SetValue(e, false);

            int hash2 = target.GetHashCode(ref key, false);

            Assert.AreEqual(hash1, hash2);
        }

        [TestMethod()]
        public void GetHashCodeTest()
        {
            GetHashCodeTestHelper(new GenericParameterHelper());
        }
    }
}
