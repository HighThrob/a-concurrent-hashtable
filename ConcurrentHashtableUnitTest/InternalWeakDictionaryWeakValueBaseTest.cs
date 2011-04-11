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
    public class InternalWeakDictionaryWeakValueBaseTest
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

        class ValueTrashable : IWeakValueRef<Tuple<int,int>>, IEquatable<ValueTrashable>
        {
            public Tuple<int, int> Value;

            public bool IsGarbage
            { get; set; }

            public override bool Equals(object obj)
            { return Equals(obj as ValueTrashable); }

            public override int GetHashCode()
            {
                return Value.Item1.GetHashCode();
            }

            #region IWeakValueRef<Tuple<int,int>> Members

            public object Reference
            { get { return this; } }

            public bool GetValue(out Tuple<int, int> value)
            {
                if (this.IsGarbage)
                {
                    value = default(Tuple<int, int>);
                    return false;
                }

                value = Value;
                return true;
            }

            #endregion

            #region IEquatable<ValueTrashable> Members

            public bool Equals(ValueTrashable other)
            {
                return other != null && (IsGarbage || other.IsGarbage ? object.ReferenceEquals(this, other) : Value.Item1 == other.Value.Item1);
            }

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

        class IWDT : InternalWeakDictionaryWeakValueBase<Trashable, ValueTrashable, Tuple<int, int>, Tuple<int, int>, KeyValuePair<int, int>>
        {
            public IWDT()
                : base(new TrashableComparer())
            { }

            public IDictionary<Tuple<int, int>, Trashable> Keys = new Dictionary<Tuple<int, int>, Trashable>();
            public IDictionary<Tuple<int, int>, ValueTrashable> Values = new Dictionary<Tuple<int, int>, ValueTrashable>();


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

            protected override ValueTrashable FromExternalValueToInternalValue(Tuple<int, int> externalValue)
            {
                ValueTrashable res;

                if (!Values.TryGetValue(externalValue, out res))
                    Values[externalValue] = res = new ValueTrashable { Value = externalValue };

                return res;
            }
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
            var asCollection = (ICollection<KeyValuePair<Tuple<int,int>, Tuple<int,int>>>)iwdt;

            asCollection.Add(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 1), Tuple.Create(4, 1)));

            Assert.AreEqual(Tuple.Create(4, 1), iwdt.GetItem(HT(1, 2)));

            try
            {
                asCollection.Add(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 3), Tuple.Create(8, 1)));
                Assert.Fail();
            }
            catch (ArgumentException)
            { }

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            asCollection.Add(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 3), Tuple.Create(8, 1)));

            Assert.AreEqual(Tuple.Create(8, 1), iwdt.GetItem(HT(1, 2)));

            iwdt.Values[Tuple.Create(8, 1)].IsGarbage = true;

            asCollection.Add(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 3), Tuple.Create(8, 3)));

            Assert.AreEqual(Tuple.Create(8, 3), iwdt.GetItem(HT(1, 2)));
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.Clear
        ///</summary>
        [TestMethod()]
        public void ClearTest()
        {
            IWDT iwdt = new IWDT();
            var asCollection = (ICollection<KeyValuePair<Tuple<int,int>, Tuple<int,int>>>)iwdt;

            IEnumerable<KeyValuePair<Tuple<int, int>, Tuple<int, int>>> contents =
                new KeyValuePair<Tuple<int, int>, Tuple<int, int>>[] {
                    new KeyValuePair<Tuple<int, int>, Tuple<int, int>> ( Tuple.Create(1,1), Tuple.Create(2,1) ),
                    new KeyValuePair<Tuple<int, int>, Tuple<int, int>> ( Tuple.Create(3,1), Tuple.Create(7,1) ),
                    new KeyValuePair<Tuple<int, int>, Tuple<int, int>> ( Tuple.Create(5,1), Tuple.Create(13,1) )
                }
            ;

            iwdt.InsertContents(contents);

            asCollection.Clear();

            Assert.AreEqual(0, iwdt.Count);
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.Contains
        ///</summary>
        [TestMethod()]
        public void ContainsTest()
        {
            IWDT iwdt = new IWDT();
            var asCollection = (ICollection<KeyValuePair<Tuple<int, int>, Tuple<int, int>>>)iwdt;

            Assert.IsFalse(asCollection.Contains( new KeyValuePair<Tuple<int,int>,Tuple<int,int>>( Tuple.Create(1,2), Tuple.Create(2,1) ) ) );

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            Assert.IsTrue(asCollection.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 2), Tuple.Create(2, 1))));
            Assert.IsTrue(asCollection.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(2, 2), Tuple.Create(3, 1))));
            Assert.IsTrue(asCollection.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(3, 2), Tuple.Create(4, 1))));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            Assert.IsFalse(asCollection.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 2), Tuple.Create(2, 1))));
            Assert.IsTrue(asCollection.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(2, 2), Tuple.Create(3, 1))));
            Assert.IsFalse(asCollection.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(3, 2), Tuple.Create(4, 1))));
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.CopyTo
        ///</summary>
        [TestMethod()]
        public void CopyToTest()
        {
            IWDT iwdt = new IWDT();
            var asCollection = (ICollection<KeyValuePair<Tuple<int, int>, Tuple<int, int>>>)iwdt;

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            var array = new KeyValuePair<Tuple<int, int>, Tuple<int, int>>[3];

            try
            {
                asCollection.CopyTo(array, 4);
                Assert.Fail();
            }
            catch (IndexOutOfRangeException)
            { }

            try
            {
                asCollection.CopyTo(array, -1);
                Assert.Fail();
            }
            catch (IndexOutOfRangeException)
            { }

            try
            {
                asCollection.CopyTo(null, 0);
                Assert.Fail();
            }
            catch (ArgumentNullException)
            { }

            try
            {
                asCollection.CopyTo(array, 1);
                Assert.Fail();
            }
            catch (ArgumentException)
            { }

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            array = new KeyValuePair<Tuple<int, int>, Tuple<int, int>>[3];

            asCollection.CopyTo(array, 1);

            Assert.AreEqual(default(KeyValuePair<Tuple<int, int>, Tuple<int, int>>), array[0]);
            Assert.AreEqual(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(2, 1), Tuple.Create(3, 1)), array[1]);
            Assert.AreEqual(default(KeyValuePair<Tuple<int, int>, Tuple<int, int>>), array[2]);        
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.Remove
        ///</summary>
        [TestMethod()]
        public void RemoveTest()
        {
            IWDT iwdt = new IWDT();
            var asCollection = (ICollection<KeyValuePair<Tuple<int, int>, Tuple<int, int>>>)iwdt;

            Assert.IsFalse(asCollection.Remove(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 2), Tuple.Create(2, 1))));

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            Assert.IsTrue(asCollection.Remove(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 2), Tuple.Create(2, 2))));
            Assert.IsFalse(asCollection.Remove(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 2), Tuple.Create(2, 2))));

            Assert.IsFalse(asCollection.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 2), Tuple.Create(2, 2))));

            iwdt.Keys[Tuple.Create(2, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            Assert.IsFalse(asCollection.Remove(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(2, 2), Tuple.Create(3, 2))));
            Assert.IsFalse(asCollection.Remove(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(3, 2), Tuple.Create(4, 2))));
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.Add
        ///</summary>
        [TestMethod()]
        public void AddTest1()
        {
            IWDT iwdt = new IWDT();
            var asDictionary = (IDictionary<Tuple<int, int>, Tuple<int, int>>)iwdt;

            asDictionary.Add(Tuple.Create(1, 1), Tuple.Create(4, 1));

            Assert.AreEqual(Tuple.Create(4, 1), iwdt.GetItem(HT(1, 2)));

            try
            {
                asDictionary.Add(Tuple.Create(1, 3), Tuple.Create(8, 1));
                Assert.Fail();
            }
            catch (ArgumentException)
            { }

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            asDictionary.Add(Tuple.Create(1, 3), Tuple.Create(8, 1));

            Assert.AreEqual(Tuple.Create(8, 1), iwdt.GetItem(HT(1, 2)));

            iwdt.Values[Tuple.Create(8, 1)].IsGarbage = true;

            asDictionary.Add(Tuple.Create(1, 3), Tuple.Create(8, 3));

            Assert.AreEqual(Tuple.Create(8, 3), iwdt.GetItem(HT(1, 2)));
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.ContainsKey
        ///</summary>
        [TestMethod()]
        public void ContainsKeyTest1()
        {
            IWDT iwdt = new IWDT();
            var asDictionary = (IDictionary<Tuple<int, int>, Tuple<int, int>>)iwdt;

            Assert.IsFalse(asDictionary.ContainsKey(Tuple.Create(1, 2)));

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            Assert.IsTrue(asDictionary.ContainsKey(Tuple.Create(1, 2)));
            Assert.IsTrue(asDictionary.ContainsKey(Tuple.Create(2, 2)));
            Assert.IsTrue(asDictionary.ContainsKey(Tuple.Create(3, 2)));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            Assert.IsFalse(asDictionary.ContainsKey(Tuple.Create(1, 2)));
            Assert.IsTrue(asDictionary.ContainsKey(Tuple.Create(2, 2)));
            Assert.IsFalse(asDictionary.ContainsKey(Tuple.Create(3, 2)));
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.Remove
        ///</summary>
        [TestMethod()]
        public void RemoveTest1()
        {
            IWDT iwdt = new IWDT();
            var asDictionary = (IDictionary<Tuple<int, int>, Tuple<int, int>>)iwdt;

            Assert.IsFalse(asDictionary.Remove(Tuple.Create(1, 2)));

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            Assert.IsTrue(asDictionary.Remove(Tuple.Create(1, 2)));

            Assert.IsFalse(asDictionary.ContainsKey(Tuple.Create(1, 2)));

            iwdt.Keys[Tuple.Create(2, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            Assert.IsFalse(asDictionary.Remove(Tuple.Create(2, 2)));
            Assert.IsFalse(asDictionary.Remove(Tuple.Create(3, 2)));
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.TryGetValue
        ///</summary>
        [TestMethod()]
        public void TryGetValueTest()
        {
            IWDT iwdt = new IWDT();

            Tuple<int,int> value;

            Assert.IsFalse(iwdt.TryGetValue(HT(1, 2), out value));

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            Assert.IsTrue(iwdt.TryGetValue(HT(1, 2), out value));
            Assert.AreEqual(Tuple.Create(2, 1), value);

            Assert.IsTrue(iwdt.TryGetValue(HT(2, 2), out value));
            Assert.AreEqual(Tuple.Create(3, 1), value);

            Assert.IsTrue(iwdt.TryGetValue(HT(3, 2), out value));
            Assert.AreEqual(Tuple.Create(4, 1), value);

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            Assert.IsFalse(iwdt.TryGetValue(HT(1, 2), out value));
            Assert.IsFalse(iwdt.TryGetValue(HT(3, 2), out value));
        }

        /// <summary>
        ///A test for ToArray
        ///</summary>
        [TestMethod()]
        public void ToArrayTest()
        {
            IWDT iwdt = new IWDT();

            Assert.AreEqual(0, iwdt.ToArray().Length);

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            var array = iwdt.ToArray();

            Assert.AreEqual(3, array.Length);

            var list = System.Linq.Enumerable.ToList(array);

            Assert.IsTrue(list.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(1, 1), Tuple.Create(2, 1))));
            Assert.IsTrue(list.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(2, 1), Tuple.Create(3, 1))));
            Assert.IsTrue(list.Contains(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(3, 1), Tuple.Create(4, 1))));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            array = iwdt.ToArray();

            Assert.AreEqual(1, array.Length);

            Assert.AreEqual(new KeyValuePair<Tuple<int, int>, Tuple<int, int>>(Tuple.Create(2, 1), Tuple.Create(3, 1)), array[0]);
        }

        /// <summary>
        ///A test for TryAdd
        ///</summary>
        [TestMethod()]
        public void TryAddTest()
        {
            IWDT iwdt = new IWDT();

            Assert.IsTrue(iwdt.TryAdd(HT(1, 1), Tuple.Create(4, 1)));

            Assert.AreEqual(Tuple.Create(4, 1), iwdt.GetItem(HT(1, 2)));

            Assert.IsFalse(iwdt.TryAdd(HT(1, 3), Tuple.Create(8, 1)));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            Assert.IsTrue(iwdt.TryAdd(HT(1, 3), Tuple.Create(8, 1)));

            Assert.AreEqual(Tuple.Create(8, 1), iwdt.GetItem(HT(1, 2)));

            iwdt.Values[Tuple.Create(8, 1)].IsGarbage = true;

            Assert.IsTrue(iwdt.TryAdd(HT(1, 3), Tuple.Create(8, 3)));

            Assert.AreEqual(Tuple.Create(8, 3), iwdt.GetItem(HT(1, 2)));
        }


        /// <summary>
        ///A test for TryRemove
        ///</summary>
        [TestMethod()]
        public void TryRemoveTest()
        {
            IWDT iwdt = new IWDT();
            var asDictionary = (IDictionary<Tuple<int, int>, Tuple<int, int>>)iwdt;

            Tuple<int, int> value;

            Assert.IsFalse(iwdt.TryRemove(HT(1, 2), out value));

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            Assert.IsTrue(iwdt.TryRemove(HT(1, 2), out value));
            Assert.AreEqual(Tuple.Create(2, 1), value);

            Assert.IsFalse(asDictionary.ContainsKey(Tuple.Create(1, 2)));

            iwdt.Keys[Tuple.Create(2, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            Assert.IsFalse(iwdt.TryRemove(HT(2, 2), out value));
            Assert.IsFalse(iwdt.TryRemove(HT(3, 2), out value));
        }

        /// <summary>
        ///A test for TryUpdate
        ///</summary>
        [TestMethod()]
        public void TryUpdateTest()
        {
            var iwdt = new IWDT();

            Assert.IsFalse(iwdt.TryUpdate(HT(1, 2), Tuple.Create(12, 1), Tuple.Create(2, 2)));

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            Assert.IsTrue(iwdt.TryUpdate(HT(1, 2), Tuple.Create(12, 1), Tuple.Create(2, 2)));

            Assert.AreEqual(Tuple.Create(12, 1), iwdt.GetItem(HT(1, 2)));

            iwdt.Keys[Tuple.Create(2, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            Assert.IsFalse(iwdt.TryUpdate(HT(2, 2), Tuple.Create(13, 1), Tuple.Create(3, 2)));
            Assert.IsFalse(iwdt.TryUpdate(HT(3, 2), Tuple.Create(14, 1), Tuple.Create(4, 2)));
        }

        /// <summary>
        ///A test for TvdP.Collections.IMaintainable.DoMaintenance
        ///</summary>
        [TestMethod()]
        public void DoMaintenanceTest()
        {
            var iwdt = new IWDT();

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            iwdt.Keys[Tuple.Create(2, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            var asBaseDictionary = (IDictionary<Trashable, ValueTrashable>)iwdt;

            Assert.AreEqual(3, asBaseDictionary.Count);

            ((IMaintainable)iwdt).DoMaintenance();

            Assert.AreEqual(1, asBaseDictionary.Count);

            Assert.IsTrue(asBaseDictionary.Contains( new KeyValuePair<Trashable,ValueTrashable>(iwdt.Keys[Tuple.Create(1,1)], iwdt.Values[Tuple.Create(2,1)])));
        }

        /// <summary>
        ///A test for IsEmpty
        ///</summary>
        [TestMethod()]
        public void IsEmptyTest()
        {
            var iwdt = new IWDT();

            Assert.IsTrue(iwdt.IsEmpty);

            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            Assert.IsFalse(iwdt.IsEmpty);

            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            Assert.IsFalse(iwdt.IsEmpty);

            iwdt.Keys[Tuple.Create(2, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            Assert.IsTrue(iwdt.IsEmpty);        
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.Count
        ///</summary>
        [TestMethod()]
        public void CountTest()
        {
            var iwdt = new IWDT();
            var asCollection = (ICollection<KeyValuePair<Tuple<int, int>, Tuple<int, int>>>)iwdt;

            Assert.AreEqual(0, asCollection.Count);

            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            Assert.AreEqual(1, asCollection.Count);

            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            Assert.AreEqual(2, asCollection.Count);

            iwdt.Keys[Tuple.Create(2, 1)].IsGarbage = true;

            Assert.AreEqual(1, asCollection.Count);

            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            Assert.AreEqual(0, asCollection.Count);
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.IsReadOnly
        ///</summary>
        [TestMethod()]
        public void IsReadOnlyTest()
        {
            var iwdt = new IWDT();
            var asCollection = (ICollection<KeyValuePair<Tuple<int, int>, Tuple<int, int>>>)iwdt;
            Assert.IsFalse(asCollection.IsReadOnly);
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.Item
        ///</summary>
        [TestMethod()]
        public void ItemTest()
        {
            var iwdt = new IWDT();
            var asDictionary = (IDictionary<Tuple<int, int>, Tuple<int, int>>)iwdt;
            Tuple<int, int> value;

            try
            {
                value = asDictionary[Tuple.Create(1, 2)] ;
                Assert.Fail();
            }
            catch (KeyNotFoundException)
            {}

            asDictionary[Tuple.Create(1, 1)] = Tuple.Create(2, 1);
            asDictionary[Tuple.Create(2, 1)] = Tuple.Create(3, 1);

            Assert.AreEqual(Tuple.Create(2, 1), asDictionary[Tuple.Create(1, 2)]);
            Assert.AreEqual(Tuple.Create(3, 1), asDictionary[Tuple.Create(2, 2)]);

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(3, 1)].IsGarbage = true;

            try
            {
                value = asDictionary[Tuple.Create(1, 2)];
                Assert.Fail();
            }
            catch (KeyNotFoundException)
            { }

            try
            {
                value = asDictionary[Tuple.Create(2, 2)];
                Assert.Fail();
            }
            catch (KeyNotFoundException)
            { }
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.Keys
        ///</summary>
        [TestMethod()]
        public void KeysTest()
        {
            IWDT iwdt = new IWDT();
            var asDictionary = (IDictionary<Tuple<int, int>, Tuple<int, int>>)iwdt;

            Assert.AreEqual(0, asDictionary.Keys.Count);

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            var keys = asDictionary.Keys;

            Assert.AreEqual(3, keys.Count);
            Assert.IsTrue(keys.Contains(Tuple.Create(1, 1)));
            Assert.IsTrue(keys.Contains(Tuple.Create(2, 1)));
            Assert.IsTrue(keys.Contains(Tuple.Create(3, 1)));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            keys = asDictionary.Keys;

            Assert.AreEqual(1, keys.Count);
            Assert.IsTrue(keys.Contains(Tuple.Create(2, 1)));
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.Values
        ///</summary>
        [TestMethod()]
        public void ValuesTest()
        {
            IWDT iwdt = new IWDT();
            var asDictionary = (IDictionary<Tuple<int, int>, Tuple<int, int>>)iwdt;

            var values = asDictionary.Values;

            Assert.AreEqual(0, values.Count);

            iwdt.AddOrUpdate(HT(1, 1), Tuple.Create(2, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(2, 1), Tuple.Create(3, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });
            iwdt.AddOrUpdate(HT(3, 1), Tuple.Create(4, 1), (k, v) => { throw new AssertFailedException(); return default(Tuple<int, int>); });

            Assert.AreEqual(3, values.Count);
            Assert.IsTrue(values.Contains(Tuple.Create(2, 1)));
            Assert.IsTrue(values.Contains(Tuple.Create(3, 1)));
            Assert.IsTrue(values.Contains(Tuple.Create(4, 1)));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;
            iwdt.Values[Tuple.Create(4, 1)].IsGarbage = true;

            Assert.AreEqual(1, values.Count);
            Assert.IsTrue(values.Contains(Tuple.Create(3, 1)));
        }
    }
}
