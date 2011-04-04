using TvdP.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ConcurrentHashtableUnitTest
{
    
    
    /// <summary>
    ///This is a test class for WeakKeyTest and is intended
    ///to contain all WeakKeyTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WeakKeyTest
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
        ///A test for GetValue
        ///</summary>
        public void GetValueTestHelper<E>(E e)
            where E : class
        {
            WeakKey<E> target = new WeakKey<E>() { _elementReference = new WeakReference(e) }; 
            E value = null; 
            E valueExpected = e; 
            bool actual;
            actual = target.GetValue(out value, true);
            Assert.AreEqual(valueExpected, value);
            Assert.IsTrue(actual);

            ((WeakReference)target._elementReference).Target = null;
            actual = target.GetValue(out value, true);
            Assert.IsFalse(actual);

            ((WeakReference)target._elementReference).Target = WeakKey<E>.NullValue;
            actual = target.GetValue(out value, true);
            Assert.IsTrue(actual);
            Assert.AreEqual(null, value);

            target._elementReference = e;
            actual = target.GetValue(out value, false);
            Assert.AreEqual(valueExpected, value);
            Assert.IsTrue(actual);
        }

        [TestMethod()]
        public void GetValueTest()
        {
            GetValueTestHelper<GenericParameterHelper>(new GenericParameterHelper());
        }

        /// <summary>
        ///A test for SetValue
        ///</summary>
        public void SetValueTestHelper<E>(E e)
            where E : class
        {
            WeakKey<E> target = new WeakKey<E>(); 
            E value = e; 
            target.SetValue(value, false);
            Assert.AreEqual(e, target._elementReference);
            target.SetValue(value, true);
            Assert.AreEqual(e, ((WeakReference)target._elementReference).Target);
        }

        [TestMethod()]
        public void SetValueTest()
        {
            SetValueTestHelper<GenericParameterHelper>(new GenericParameterHelper());
        }

        /// <summary>
        ///A test for IsGarbage
        ///</summary>
        public void IsGarbageTestHelper<E>(E e)
            where E : class
        {
            WeakKey<E> target = new WeakKey<E>() { _elementReference = new WeakReference(e) }; // TODO: Initialize to an appropriate value
            Assert.IsFalse(target.IsGarbage);

            ((WeakReference)target._elementReference).Target = null;
            Assert.IsTrue(target.IsGarbage);
        }

        [TestMethod()]
        public void IsGarbageTest()
        {
            IsGarbageTestHelper<GenericParameterHelper>(new GenericParameterHelper());
        }
    }
}
