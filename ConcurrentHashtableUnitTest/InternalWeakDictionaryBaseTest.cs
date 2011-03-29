using TvdP.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace ConcurrentHashtableUnitTest
{
    
    
    /// <summary>
    ///This is a test class for InternalWeakDictionaryBaseTest and is intended
    ///to contain all InternalWeakDictionaryBaseTest Unit Tests
    ///</summary>
    [TestClass()]
    public class InternalWeakDictionaryBaseTest
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

        class Trashable : ITrashable
        {
            public int Value;
 
            #region ITrashable Members

            public bool IsGarbage
            { get; set; }

            #endregion
        }

        class TrashableComparer : IEqualityComparer<Trashable>
        {
            #region IEqualityComparer<Trashable> Members

            public bool Equals(Trashable x, Trashable y)
            { return x.IsGarbage || y.IsGarbage ? object.ReferenceEquals(x,y) : x.Value == y.Value; }

            public int GetHashCode(Trashable obj)
            { return obj.Value.GetHashCode(); }

            #endregion
        }

        static KeyValuePair<int, int> HT(int f, int s)
        { return new KeyValuePair<int, int>(f, s); }

        class IWDT : InternalWeakDictionaryBase<Trashable, Trashable, Tuple<int, int>, Tuple<int, int>, KeyValuePair<int, int>>
        {
            public IWDT()
                : base(new TrashableComparer())
            { }

            public IDictionary<Tuple<int, int>, Trashable> Keys = new Dictionary<Tuple<int, int>, Trashable>();
            public IDictionary<Tuple<int, int>, Trashable> Values = new Dictionary<Tuple<int, int>, Trashable>();


            protected override Trashable FromExternalKeyToSearchKey(Tuple<int, int> externalKey)
            {
                Trashable res;

                if (!Keys.TryGetValue(externalKey, out res))
                    Keys[externalKey] = res = new Trashable { Value = externalKey.Item1 };

                return res;
            }

            protected override Trashable FromExternalKeyToStorageKey(Tuple<int, int> externalKey)
            {
                Trashable res;

                if (!Keys.TryGetValue(externalKey, out res))
                    Keys[externalKey] = res = new Trashable { Value = externalKey.Item1 };

                return res;
            }

            protected override Trashable FromHeapKeyToSearchKey(KeyValuePair<int, int> externalKey)
            {
                return FromExternalKeyToSearchKey(Tuple.Create(externalKey.Key, externalKey.Value));
            }

            protected override Trashable FromHeapKeyToStorageKey(KeyValuePair<int, int> externalKey)
            {
                return FromExternalKeyToStorageKey(Tuple.Create(externalKey.Key, externalKey.Value));
            }

            protected override bool FromInternalKeyToExternalKey(Trashable internalKey, out Tuple<int, int> externalKey)
            {
                externalKey = System.Linq.Enumerable.First(Keys, kvp => object.ReferenceEquals(kvp.Value, internalKey)).Key;
                return !internalKey.IsGarbage; 
            }

            protected override bool FromInternalKeyToHeapKey(Trashable internalKey, out KeyValuePair<int, int> externalKey)
            {
                Tuple<int, int> itm;
                bool res = FromInternalKeyToExternalKey(internalKey, out itm);
                externalKey = new KeyValuePair<int, int>(itm.Item1, itm.Item2);
                return res;
            }

            protected override Trashable FromExternalValueToInternalValue(Tuple<int, int> externalValue)
            {
                Trashable res;

                if (!Values.TryGetValue(externalValue, out res))
                    Values[externalValue] = res = new Trashable { Value = externalValue.Item1 };

                return res;
            }

            protected override bool FromInternalValueToExternalValue(Trashable internalValue, out Tuple<int, int> externalValue)
            {
                externalValue = System.Linq.Enumerable.First(Values, kvp => object.ReferenceEquals(kvp.Value, internalValue)).Key;
                return !internalValue.IsGarbage;
            }
        }



        internal virtual InternalWeakDictionaryBase<IK, IV, EK, EV, HK> CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            // TODO: Instantiate an appropriate concrete class.
            InternalWeakDictionaryBase<IK, IV, EK, EV, HK> target = null;
            return target;
        }

        [TestMethod()]
        public void ContainsKeyTest()
        {
            IWDT iwdt = new IWDT();

            Assert.IsFalse(iwdt.ContainsKey(HT(1,2)));
            iwdt.AddOrUpdate(HT(1,1), Tuple.Create(2,1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int,int>); });
            Assert.IsTrue(iwdt.ContainsKey(HT(1, 2)));
            iwdt.Keys[Tuple.Create(1,1)].IsGarbage = true;
            Assert.IsFalse(iwdt.ContainsKey(HT(1, 2)));
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            Assert.IsTrue(iwdt.ContainsKey(HT(2, 2)));
            iwdt.Values[Tuple.Create(2, 1)].IsGarbage = true;
            Assert.IsFalse(iwdt.ContainsKey(HT(2, 2)));
        }


        /// <summary>
        /// A test for GetContents
        /// </summary>
        [TestMethod()]
        public void GetContentsTest()
        {
            IWDT iwdt = new IWDT();

            var contents = iwdt.GetContents();

            Assert.AreEqual(0, contents.Count);

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            contents = iwdt.GetContents();

            Assert.AreEqual(3, contents.Count);
            Assert.IsTrue(contents.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 1), Tuple.Create(2, 1))));
            Assert.IsTrue(contents.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(2, 1), Tuple.Create(3, 1))));
            Assert.IsTrue(contents.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(3, 1), Tuple.Create(4, 1))));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            contents = iwdt.GetContents();

            Assert.AreEqual(1, contents.Count);
            Assert.IsTrue(contents.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(2, 1), Tuple.Create(3, 1))));
        }


        [TestMethod()]
        public void GetEnumeratorTest()
        {
            IWDT iwdt = new IWDT();
            var asEnumerable = (IEnumerable<KeyValuePair<Tuple<int,int>, Tuple<int,int>>>)iwdt;

            using (var it = asEnumerable.GetEnumerator())
                Assert.IsFalse(it.MoveNext());

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            using (var it = asEnumerable.GetEnumerator())
            {
                Assert.IsTrue(it.MoveNext());
                Assert.IsTrue(it.MoveNext());
                Assert.IsTrue(it.MoveNext());
                Assert.IsFalse(it.MoveNext());
            }

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            using (var it = asEnumerable.GetEnumerator())
            {
                Assert.IsTrue(it.MoveNext());
                Assert.IsFalse(it.MoveNext());
            }        
        }

        [TestMethod()]
        public void GetItemTest()
        {
            IWDT iwdt = new IWDT();

            try
            {
                var itm = iwdt.GetItem(HT(1, 1));
                Assert.Fail();
            }
            catch (KeyNotFoundException)
            { }

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            Assert.AreEqual(Tuple.Create(2, 1), iwdt.GetItem(HT(1, 2)));
            Assert.AreEqual(Tuple.Create(3, 1), iwdt.GetItem(HT(2, 2)));
            Assert.AreEqual(Tuple.Create(4, 1), iwdt.GetItem(HT(3, 2)));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            try
            {
                var itm = iwdt.GetItem(HT(1, 2));
                Assert.Fail();
            }
            catch (KeyNotFoundException)
            { }

            try
            {
                var itm = iwdt.GetItem(HT(3, 2));
                Assert.Fail();
            }
            catch (KeyNotFoundException)
            { }
        }

        /// <summary>
        ///A test for GetOrAdd
        ///</summary>
        [TestMethod()]
        public void GetOrAddTest()
        {
            IWDT iwdt = new IWDT();

            var ret = iwdt.GetOrAdd(HT(1, 1), Tuple.Create(2, 1));

            Assert.AreEqual(Tuple.Create(2, 1), ret);
            Assert.AreEqual(Tuple.Create(2, 1), iwdt.GetItem(HT(1, 1)));

            ret = iwdt.GetOrAdd(HT(1, 1), Tuple.Create(3, 1));

            Assert.AreEqual(Tuple.Create(2, 1), ret);
            Assert.AreEqual(Tuple.Create(2, 1), iwdt.GetItem(HT(1, 2)));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            ret = iwdt.GetOrAdd(HT(1, 2), Tuple.Create(3, 1));

            Assert.AreEqual(Tuple.Create(3, 1), ret);
            Assert.AreEqual(Tuple.Create(3, 1), iwdt.GetItem(HT(1, 2)));
        }

        /// <summary>
        ///A test for InsertContents
        ///</summary>
        [TestMethod()]
        public void InsertContentsTest()
        {
            IWDT iwdt = new IWDT();

            IEnumerable<KeyValuePair<Tuple<int, int>, Tuple<int, int>>> contents =
                new KeyValuePair<Tuple<int, int>, Tuple<int, int>>[] {
                    new KeyValuePair<Tuple<int, int>, Tuple<int, int>> ( Tuple.Create(1,1), Tuple.Create(2,1) ),
                    new KeyValuePair<Tuple<int, int>, Tuple<int, int>> ( Tuple.Create(3,1), Tuple.Create(7,1) ),
                    new KeyValuePair<Tuple<int, int>, Tuple<int, int>> ( Tuple.Create(5,1), Tuple.Create(13,1) )
                }
            ;

            iwdt.InsertContents(contents);

            Assert.AreEqual(Tuple.Create(2, 1), iwdt.GetItem(HT(1, 2)));
            Assert.AreEqual(Tuple.Create(7, 1), iwdt.GetItem(HT(3, 2)));
            Assert.AreEqual(Tuple.Create(13, 1), iwdt.GetItem(HT(5, 2)));
        }

        /// <summary>
        ///A test for SetItem
        ///</summary>
        [TestMethod()]
        public void SetItemTest()
        {
            IWDT iwdt = new IWDT();

            iwdt.SetItem(HT(1, 1), Tuple.Create(3, 1));

            Assert.AreEqual(Tuple.Create(3, 1), iwdt.GetItem(HT(1, 2)));

            iwdt.SetItem(HT(1, 1), Tuple.Create(7, 1));

            Assert.AreEqual(Tuple.Create(7, 1), iwdt.GetItem(HT(1, 2)));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            iwdt.SetItem(HT(1, 3), Tuple.Create(13, 1));

            Assert.AreEqual(Tuple.Create(13, 1), iwdt.GetItem(HT(1, 2)));

            iwdt.Values[Tuple.Create(13, 1)].IsGarbage = true;

            iwdt.SetItem(HT(1, 3), Tuple.Create(13, 3));

            Assert.AreEqual(Tuple.Create(13, 3), iwdt.GetItem(HT(1, 2)));
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.Add
        ///</summary>
        [TestMethod()]
        public void AddTest()
        {
            IWDT iwdt = new IWDT();
            var asCollecion = (ICollection<KeyValuePair<Tuple<int,int>, Tuple<int,int>>>)iwdt;

            asCollecion.Add(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 1), Tuple.Create(4, 1)));

            Assert.AreEqual(Tuple.Create(4, 1), iwdt.GetItem(HT(1, 2)));

            try
            {
                asCollecion.Add(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 3), Tuple.Create(8, 1)));
                Assert.Fail();
            }
            catch (ArgumentException)
            { }

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            asCollecion.Add(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 3), Tuple.Create(8, 1)));

            Assert.AreEqual(Tuple.Create(8, 1), iwdt.GetItem(HT(1, 2)));

            iwdt.Values[Tuple.Create(8, 1)].IsGarbage = true;

            asCollecion.Add(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 3), Tuple.Create(8, 3)));

            Assert.AreEqual(Tuple.Create(8, 3), iwdt.GetItem(HT(1, 2)));
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.Clear
        ///</summary>
        [TestMethod()]
        public void ClearTest()
        {
            IWDT iwdt = new IWDT();
            var asCollecion = (ICollection<KeyValuePair<Tuple<int,int>, Tuple<int,int>>>)iwdt;

            IEnumerable<KeyValuePair<Tuple<int, int>, Tuple<int, int>>> contents =
                new KeyValuePair<Tuple<int, int>, Tuple<int, int>>[] {
                    new KeyValuePair<Tuple<int, int>, Tuple<int, int>> ( Tuple.Create(1,1), Tuple.Create(2,1) ),
                    new KeyValuePair<Tuple<int, int>, Tuple<int, int>> ( Tuple.Create(3,1), Tuple.Create(7,1) ),
                    new KeyValuePair<Tuple<int, int>, Tuple<int, int>> ( Tuple.Create(5,1), Tuple.Create(13,1) )
                }
            ;

            iwdt.InsertContents(contents);

            asCollecion.Clear();

            Assert.AreEqual(0, iwdt.Count);
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.Contains
        ///</summary>
        [TestMethod()]
        public void ContainsTest()
        {
            IWDT iwdt = new IWDT();
            var asCollecion = (ICollection<KeyValuePair<Tuple<int, int>, Tuple<int, int>>>)iwdt;

            Assert.IsFalse(asCollecion.Contains( new KeyValuePair<Tuple<int,int>,Tuple<int,int>>( Tuple.Create(1,2), Tuple.Create(2,1) ) ) );

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            Assert.IsTrue(asCollecion.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 2), Tuple.Create(2, 1))));
            Assert.IsTrue(asCollecion.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(2, 2), Tuple.Create(3, 1))));
            Assert.IsTrue(asCollecion.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(3, 2), Tuple.Create(4, 1))));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            Assert.IsFalse(asCollecion.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 2), Tuple.Create(2, 1))));
            Assert.IsTrue(asCollecion.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(2, 2), Tuple.Create(3, 1))));
            Assert.IsFalse(asCollecion.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(3, 2), Tuple.Create(4, 1))));
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.CopyTo
        ///</summary>
        [TestMethod()]
        public void CopyToTest()
        {
            IWDT iwdt = new IWDT();
            var asCollecion = (ICollection<KeyValuePair<Tuple<int, int>, Tuple<int, int>>>)iwdt;

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            var array = new KeyValuePair<Tuple<int, int>, Tuple<int, int>>[3];

            try
            {
                asCollecion.CopyTo(array, 4);
                Assert.Fail();
            }
            catch (IndexOutOfRangeException)
            { }

            try
            {
                asCollecion.CopyTo(array, -1);
                Assert.Fail();
            }
            catch (IndexOutOfRangeException)
            { }

            try
            {
                asCollecion.CopyTo(null, 0);
                Assert.Fail();
            }
            catch (ArgumentNullException)
            { }

            try
            {
                asCollecion.CopyTo(array, 1);
                Assert.Fail();
            }
            catch (ArgumentException)
            { }

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            array = new KeyValuePair<Tuple<int, int>, Tuple<int, int>>[3];

            asCollecion.CopyTo(array, 1);

            Assert.AreEqual(default(KeyValuePair<Tuple<int, int>, Tuple<int, int>>), array[0]);
            Assert.AreEqual(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(2, 1), Tuple.Create(3, 1)), array[1]);
            Assert.AreEqual(default(KeyValuePair<Tuple<int, int>, Tuple<int, int>>), array[2]);        
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.Remove
        ///</summary>
        void RemoveTestHelper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            ICollection<KeyValuePair<EK, EV>> target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            KeyValuePair<EK, EV> item = new KeyValuePair<EK, EV>(); // TODO: Initialize to an appropriate value
            bool expected = false; // TODO: Initialize to an appropriate value
            bool actual;
            actual = target.Remove(item);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        [DeploymentItem("ConcurrentHashtable.dll")]
        public void RemoveTest()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call RemoveTestHelper<IK, IV, EK, EV, HK>() with appropriate type parame" +
                    "ters.");
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.Add
        ///</summary>
        void AddTest1Helper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            IDictionary<EK, EV> target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            EK key = default(EK); // TODO: Initialize to an appropriate value
            EV value = default(EV); // TODO: Initialize to an appropriate value
            target.Add(key, value);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        [TestMethod()]
        [DeploymentItem("ConcurrentHashtable.dll")]
        public void AddTest1()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call AddTest1Helper<IK, IV, EK, EV, HK>() with appropriate type paramete" +
                    "rs.");
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.ContainsKey
        ///</summary>
        void ContainsKeyTest1Helper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            IDictionary<EK, EV> target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            EK key = default(EK); // TODO: Initialize to an appropriate value
            bool expected = false; // TODO: Initialize to an appropriate value
            bool actual;
            actual = target.ContainsKey(key);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        [DeploymentItem("ConcurrentHashtable.dll")]
        public void ContainsKeyTest1()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call ContainsKeyTest1Helper<IK, IV, EK, EV, HK>() with appropriate type " +
                    "parameters.");
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.Remove
        ///</summary>
        void RemoveTest1Helper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            IDictionary<EK, EV> target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            EK key = default(EK); // TODO: Initialize to an appropriate value
            bool expected = false; // TODO: Initialize to an appropriate value
            bool actual;
            actual = target.Remove(key);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        [DeploymentItem("ConcurrentHashtable.dll")]
        public void RemoveTest1()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call RemoveTest1Helper<IK, IV, EK, EV, HK>() with appropriate type param" +
                    "eters.");
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.TryGetValue
        ///</summary>
        void TryGetValueTestHelper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            IDictionary<EK, EV> target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            EK key = default(EK); // TODO: Initialize to an appropriate value
            EV value = default(EV); // TODO: Initialize to an appropriate value
            EV valueExpected = default(EV); // TODO: Initialize to an appropriate value
            bool expected = false; // TODO: Initialize to an appropriate value
            bool actual;
            actual = target.TryGetValue(key, out value);
            Assert.AreEqual(valueExpected, value);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        [DeploymentItem("ConcurrentHashtable.dll")]
        public void TryGetValueTest()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call TryGetValueTestHelper<IK, IV, EK, EV, HK>() with appropriate type p" +
                    "arameters.");
        }

        /// <summary>
        ///A test for ToArray
        ///</summary>
        void ToArrayTestHelper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            InternalWeakDictionaryBase<IK, IV, EK, EV, HK> target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            KeyValuePair<EK, EV>[] expected = null; // TODO: Initialize to an appropriate value
            KeyValuePair<EK, EV>[] actual;
            actual = target.ToArray();
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        public void ToArrayTest()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call ToArrayTestHelper<IK, IV, EK, EV, HK>() with appropriate type param" +
                    "eters.");
        }

        /// <summary>
        ///A test for TryAdd
        ///</summary>
        void TryAddTestHelper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            InternalWeakDictionaryBase<IK, IV, EK, EV, HK> target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            HK key = default(HK); // TODO: Initialize to an appropriate value
            EV value = default(EV); // TODO: Initialize to an appropriate value
            bool expected = false; // TODO: Initialize to an appropriate value
            bool actual;
            actual = target.TryAdd(key, value);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        public void TryAddTest()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call TryAddTestHelper<IK, IV, EK, EV, HK>() with appropriate type parame" +
                    "ters.");
        }

        /// <summary>
        ///A test for TryGetValue
        ///</summary>
        void TryGetValueTest1Helper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            InternalWeakDictionaryBase<IK, IV, EK, EV, HK> target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            HK key = default(HK); // TODO: Initialize to an appropriate value
            EV value = default(EV); // TODO: Initialize to an appropriate value
            EV valueExpected = default(EV); // TODO: Initialize to an appropriate value
            bool expected = false; // TODO: Initialize to an appropriate value
            bool actual;
            actual = target.TryGetValue(key, out value);
            Assert.AreEqual(valueExpected, value);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        public void TryGetValueTest1()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call TryGetValueTest1Helper<IK, IV, EK, EV, HK>() with appropriate type " +
                    "parameters.");
        }

        /// <summary>
        ///A test for TryRemove
        ///</summary>
        void TryRemoveTestHelper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            InternalWeakDictionaryBase<IK, IV, EK, EV, HK> target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            HK key = default(HK); // TODO: Initialize to an appropriate value
            EV value = default(EV); // TODO: Initialize to an appropriate value
            EV valueExpected = default(EV); // TODO: Initialize to an appropriate value
            bool expected = false; // TODO: Initialize to an appropriate value
            bool actual;
            actual = target.TryRemove(key, out value);
            Assert.AreEqual(valueExpected, value);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        public void TryRemoveTest()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call TryRemoveTestHelper<IK, IV, EK, EV, HK>() with appropriate type par" +
                    "ameters.");
        }

        /// <summary>
        ///A test for TryUpdate
        ///</summary>
        void TryUpdateTestHelper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            InternalWeakDictionaryBase<IK, IV, EK, EV, HK> target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            HK key = default(HK); // TODO: Initialize to an appropriate value
            EV newValue = default(EV); // TODO: Initialize to an appropriate value
            EV comparisonValue = default(EV); // TODO: Initialize to an appropriate value
            bool expected = false; // TODO: Initialize to an appropriate value
            bool actual;
            actual = target.TryUpdate(key, newValue, comparisonValue);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        public void TryUpdateTest()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call TryUpdateTestHelper<IK, IV, EK, EV, HK>() with appropriate type par" +
                    "ameters.");
        }

        /// <summary>
        ///A test for TvdP.Collections.IMaintainable.DoMaintenance
        ///</summary>
        void DoMaintenanceTestHelper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            IMaintainable target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            target.DoMaintenance();
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        [TestMethod()]
        [DeploymentItem("ConcurrentHashtable.dll")]
        public void DoMaintenanceTest()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call DoMaintenanceTestHelper<IK, IV, EK, EV, HK>() with appropriate type" +
                    " parameters.");
        }

        /// <summary>
        ///A test for IsEmpty
        ///</summary>
        void IsEmptyTestHelper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            InternalWeakDictionaryBase<IK, IV, EK, EV, HK> target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            bool actual;
            actual = target.IsEmpty;
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        public void IsEmptyTest()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call IsEmptyTestHelper<IK, IV, EK, EV, HK>() with appropriate type param" +
                    "eters.");
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.Count
        ///</summary>
        void CountTestHelper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            ICollection<KeyValuePair<EK, EV>> target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            int actual;
            actual = target.Count;
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        [DeploymentItem("ConcurrentHashtable.dll")]
        public void CountTest()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call CountTestHelper<IK, IV, EK, EV, HK>() with appropriate type paramet" +
                    "ers.");
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.IsReadOnly
        ///</summary>
        void IsReadOnlyTestHelper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            ICollection<KeyValuePair<EK, EV>> target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            bool actual;
            actual = target.IsReadOnly;
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        [DeploymentItem("ConcurrentHashtable.dll")]
        public void IsReadOnlyTest()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call IsReadOnlyTestHelper<IK, IV, EK, EV, HK>() with appropriate type pa" +
                    "rameters.");
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.Item
        ///</summary>
        void ItemTestHelper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            IDictionary<EK, EV> target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            EK key = default(EK); // TODO: Initialize to an appropriate value
            EV expected = default(EV); // TODO: Initialize to an appropriate value
            EV actual;
            target[key] = expected;
            actual = target[key];
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        [DeploymentItem("ConcurrentHashtable.dll")]
        public void ItemTest()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call ItemTestHelper<IK, IV, EK, EV, HK>() with appropriate type paramete" +
                    "rs.");
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.Keys
        ///</summary>
        void KeysTestHelper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            IDictionary<EK, EV> target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            ICollection<EK> actual;
            actual = target.Keys;
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        [DeploymentItem("ConcurrentHashtable.dll")]
        public void KeysTest()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call KeysTestHelper<IK, IV, EK, EV, HK>() with appropriate type paramete" +
                    "rs.");
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.Values
        ///</summary>
        void ValuesTestHelper<IK, IV, EK, EV, HK>()
            where IK : ITrashable
            where IV : ITrashable
            where HK : struct
        {
            IDictionary<EK, EV> target = CreateInternalWeakDictionaryBase<IK, IV, EK, EV, HK>(); // TODO: Initialize to an appropriate value
            ICollection<EV> actual;
            actual = target.Values;
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        [TestMethod()]
        [DeploymentItem("ConcurrentHashtable.dll")]
        public void ValuesTest()
        {
            Assert.Inconclusive("No appropriate type parameter is found to satisfies the type constraint(s) of IK." +
                    " Please call ValuesTestHelper<IK, IV, EK, EV, HK>() with appropriate type parame" +
                    "ters.");
        }
    }
}
