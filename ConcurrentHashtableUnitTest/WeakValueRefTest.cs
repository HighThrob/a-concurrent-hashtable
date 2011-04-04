using TvdP.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ConcurrentHashtableUnitTest
{
    
    
    /// <summary>
    ///This is a test class for WeakValueRefTest and is intended
    ///to contain all WeakValueRefTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WeakValueRefTest
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
        ///A test for Create
        ///</summary>
        public void CreateTestHelper<V>(V v)
            where V : class
        {
            var target = WeakValueRef<V>.Create(null);

            Assert.IsFalse(target.IsGarbage);

            V stored;

            Assert.IsTrue(target.GetValue(out stored));
            Assert.IsTrue(null == stored);

            target = WeakValueRef<V>.Create(v);

            Assert.IsTrue(target.GetValue(out stored));
            Assert.AreEqual(v, stored);
        }

        [TestMethod()]
        public void CreateTest()
        {
            CreateTestHelper(new Object());
        }

        /// <summary>
        ///A test for Equals
        ///</summary>
        public void EqualsTestHelper<E>(E a, E b)
            where E : class
        {
            WeakValueRef<E> target = WeakValueRef<E>.Create(a);
            WeakValueRef<E> other = WeakValueRef<E>.Create(b);
            Assert.IsFalse(target.Equals(other));

            other = WeakValueRef<E>.Create(a);
            Assert.IsTrue(target.Equals(other));

            ((WeakReference)target._valueReference).Target = null;

            Assert.IsTrue(target.Equals(target));

            ((WeakReference)other._valueReference).Target = null;

            Assert.IsFalse(target.Equals(other));
        }

        [TestMethod()]
        public void EqualsTest()
        {
            EqualsTestHelper<Object>(new Object(), new Object());
        }

        /// <summary>
        ///A test for Equals
        ///</summary>
        public void EqualsTest1Helper<E>(E a)
            where E : class
        {
            WeakValueRef<E> target = WeakValueRef<E>.Create(a) ;
            object obj = null; // TODO: Initialize to an appropriate value
            Assert.IsFalse(target.Equals(obj));

            obj = new object();
            Assert.IsFalse(target.Equals(obj));

            obj = WeakValueRef<E>.Create(a);
            Assert.IsTrue(target.Equals(obj));
        }

        [TestMethod()]
        public void EqualsTest1()
        {
            EqualsTest1Helper<System.Collections.Generic.List<int>>(new System.Collections.Generic.List<int>());
        }


        /// <summary>
        ///A test for GetValue
        ///</summary>
        public void GetValueTestHelper<V>(V v)
            where V : class
        {
            var target = WeakValueRef<V>.Create(null);

            V stored;

            Assert.IsTrue(target.GetValue(out stored));
            Assert.IsTrue(null == stored);

            target = WeakValueRef<V>.Create(v);

            Assert.IsTrue(target.GetValue(out stored));
            Assert.AreEqual(v, stored);
        }

        [TestMethod()]
        public void GetValueTest()
        {
            GetValueTestHelper<GenericParameterHelper>(new GenericParameterHelper());
        }

        /// <summary>
        ///A test for IsGarbage
        ///</summary>
        public void IsGarbageTestHelper<V>(V v)
            where V : class
        {
            WeakValueRef<V> target = WeakValueRef<V>.Create(v);

            Assert.IsFalse(target.IsGarbage);

            target._valueReference.Target = null;

            Assert.IsTrue(target.IsGarbage);
        }

        [TestMethod()]
        public void IsGarbageTest()
        {
            IsGarbageTestHelper<GenericParameterHelper>( new GenericParameterHelper() );
        }

        /// <summary>
        ///A test for Reference
        ///</summary>
        public void ReferenceTestHelper<V>(V v)
            where V : class
        {
            WeakValueRef<V> target1 = WeakValueRef<V>.Create(v); 
            WeakValueRef<V> target2 = WeakValueRef<V>.Create(v);
            Assert.IsTrue(object.ReferenceEquals(target1.Reference, target1.Reference));
            Assert.IsFalse(object.ReferenceEquals(target1.Reference, target2.Reference));
        }

        [TestMethod()]
        public void ReferenceTest()
        {
            ReferenceTestHelper<GenericParameterHelper>( new GenericParameterHelper() );
        }
    }
}
