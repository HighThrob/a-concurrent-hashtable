using TvdP.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ConcurrentHashtableUnitTest
{
    
    
    /// <summary>
    ///This is a test class for KeySegmentTest and is intended
    ///to contain all KeySegmentTest Unit Tests
    ///</summary>
    [TestClass()]
    public class KeySegmentTest
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


        class TrashableStub : ITrashable
        {
            public bool _isGarbage;

            #region ITrashable Members

            public bool IsGarbage
            {
                get { return _isGarbage; }
            }

            #endregion
        }

        /// <summary>
        ///A test for GetValue
        ///</summary>
        void GetValueTestHelper<E, T>( E e, T tail )
            where E : class
            where T : ITrashable
        {
            KeySegment<E, T> target = 
                new KeySegment<E, T>() { 
                    _elementReference = new WeakKey<E> { 
                        _elementReference = new WeakReference(e) 
                    }, 
                    _tail = tail 
                }
            ; 
            E value = null; // TODO: Initialize to an appropriate value
            Assert.IsTrue(target.GetValue(out value, true));
            Assert.AreEqual(e, value);

            ((WeakReference)target._elementReference._elementReference).Target = null;

            Assert.IsFalse(target.GetValue(out value, true));

            ((WeakReference)target._elementReference._elementReference).Target = WeakKey<E>.NullValue;
            Assert.IsTrue(target.GetValue(out value, true));
            Assert.AreEqual(null, value);

            target._elementReference._elementReference = e;
            Assert.IsTrue(target.GetValue(out value, false));
            Assert.AreEqual(e, value);
        }

        [TestMethod()]
        public void GetValueTest()
        {
            GetValueTestHelper(new GenericParameterHelper(), new TrashableStub());
        }

        /// <summary>
        ///A test for SetValue
        ///</summary>
        void SetValueTestHelper<E, T>(E e, T tail)
            where E : class
            where T : ITrashable
        {
            KeySegment<E, T> target =
                new KeySegment<E, T>()
                {
                    _elementReference = new WeakKey<E>
                    {
                        _elementReference = new WeakReference(null)
                    },
                    _tail = tail
                }
            ;

            target.SetValue(e, true);
           
            E value = null;

            Assert.IsTrue(target.GetValue(out value, true));
            Assert.AreEqual(e, value);

            target.SetValue(null, true);

            Assert.IsTrue(target.GetValue(out value, true));
            Assert.AreEqual(null, value);

            target.SetValue(e, false);

            Assert.IsTrue(target.GetValue(out value, false));
            Assert.AreEqual(e, value);
        }

        [TestMethod()]
        public void SetValueTest()
        {
            SetValueTestHelper(new GenericParameterHelper(), new TrashableStub());
        }

        /// <summary>
        ///A test for IsGarbage
        ///</summary>
        void IsGarbageTestHelper<E>(E e)
            where E : class
        {
            var target =
                new KeySegment<E, TrashableStub>()
                {
                    _elementReference = new WeakKey<E>
                    {
                        _elementReference = new WeakReference(e)
                    },
                    _tail = new TrashableStub()
                }
            ;

            Assert.IsFalse(target.IsGarbage);

            target._tail._isGarbage = true;

            Assert.IsTrue(target.IsGarbage);

            target._tail._isGarbage = false;
            ((WeakReference)target._elementReference._elementReference).Target = null;

            Assert.IsTrue(target.IsGarbage);

            target.SetValue(null, true);

            Assert.IsFalse(target.IsGarbage);
        }

        [TestMethod()]
        public void IsGarbageTest()
        {
            IsGarbageTestHelper(new GenericParameterHelper());
        }
    }
}
