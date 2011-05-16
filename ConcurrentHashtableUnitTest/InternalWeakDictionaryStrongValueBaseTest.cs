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
    public class InternalWeakDictionaryStrongValueBaseTest
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

        class IWDT : InternalWeakDictionaryStrongValueBase<Trashable, Tuple<int, int>, int, KeyValuePair<int, int>>
        {
            public IWDT()
                : base(new TrashableComparer())
            { }

            public IDictionary<Tuple<int, int>, Trashable> Keys = new Dictionary<Tuple<int, int>, Trashable>();

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

            protected override Trashable FromStackKeyToSearchKey(KeyValuePair<int, int> externalKey)
            {
                return FromExternalKeyToSearchKey(Tuple.Create(externalKey.Key, externalKey.Value));
            }

            protected override Trashable FromStackKeyToStorageKey(KeyValuePair<int, int> externalKey)
            {
                return FromExternalKeyToStorageKey(Tuple.Create(externalKey.Key, externalKey.Value));
            }

            protected override bool FromInternalKeyToExternalKey(Trashable internalKey, out Tuple<int, int> externalKey)
            {
                externalKey = System.Linq.Enumerable.First(Keys, kvp => object.ReferenceEquals(kvp.Value, internalKey)).Key;
                return !internalKey.IsGarbage; 
            }

            protected override bool FromInternalKeyToStackKey(Trashable internalKey, out KeyValuePair<int, int> externalKey)
            {
                Tuple<int, int> itm;
                bool res = FromInternalKeyToExternalKey(internalKey, out itm);
                externalKey = new KeyValuePair<int, int>(itm.Item1, itm.Item2);
                return res;
            }
        }


        [TestMethod()]
        public void ContainsKeyTest()
        {
            IWDT iwdt = new IWDT();

            Assert.IsFalse(iwdt.ContainsKey(HT(1,2)));
            iwdt.AddOrUpdate(HT(1,1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            Assert.IsTrue(iwdt.ContainsKey(HT(1, 2)));
            iwdt.Keys[Tuple.Create(1,1)].IsGarbage = true;
            Assert.IsFalse(iwdt.ContainsKey(HT(1, 2)));
            iwdt.AddOrUpdate(HT(2, 1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            Assert.IsTrue(iwdt.ContainsKey(HT(2, 2)));
            Assert.IsTrue(iwdt.ContainsKey(HT(2, 2)));
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

            iwdt.AddOrUpdate(HT(1, 1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            contents = iwdt.GetContents();

            Assert.AreEqual(3, contents.Count);
            Assert.IsTrue(contents.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(1, 1), 2)));
            Assert.IsTrue(contents.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(2, 1), 3)));
            Assert.IsTrue(contents.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(3, 1), 4)));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            contents = iwdt.GetContents();

            Assert.AreEqual(2, contents.Count);
            Assert.IsTrue(contents.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(2, 1), 3)));
            Assert.IsTrue(contents.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(3, 1), 4)));
        }


        [TestMethod()]
        public void GetEnumeratorTest()
        {
            IWDT iwdt = new IWDT();
            var asEnumerable = (IEnumerable<KeyValuePair<Tuple<int,int>, int>>)iwdt;

            using (var it = asEnumerable.GetEnumerator())
                Assert.IsFalse(it.MoveNext());

            iwdt.AddOrUpdate(HT(1, 1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            using (var it = asEnumerable.GetEnumerator())
            {
                Assert.IsTrue(it.MoveNext());
                Assert.IsTrue(it.MoveNext());
                Assert.IsTrue(it.MoveNext());
                Assert.IsFalse(it.MoveNext());
            }

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            using (var it = asEnumerable.GetEnumerator())
            {
                Assert.IsTrue(it.MoveNext());
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

            iwdt.AddOrUpdate(HT(1, 1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            Assert.AreEqual(2, iwdt.GetItem(HT(1, 2)));
            Assert.AreEqual(3, iwdt.GetItem(HT(2, 2)));
            Assert.AreEqual(4, iwdt.GetItem(HT(3, 2)));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            try
            {
                var itm = iwdt.GetItem(HT(1, 2));
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

            var ret = iwdt.GetOrAdd(HT(1, 1), 2);

            Assert.AreEqual(2, ret);
            Assert.AreEqual(2, iwdt.GetItem(HT(1, 1)));

            ret = iwdt.GetOrAdd(HT(1, 1), 3);

            Assert.AreEqual(2, ret);
            Assert.AreEqual(2, iwdt.GetItem(HT(1, 2)));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            ret = iwdt.GetOrAdd(HT(1, 2), 3);

            Assert.AreEqual(3, ret);
            Assert.AreEqual(3, iwdt.GetItem(HT(1, 2)));
        }

        /// <summary>
        ///A test for InsertContents
        ///</summary>
        [TestMethod()]
        public void InsertContentsTest()
        {
            IWDT iwdt = new IWDT();

            IEnumerable<KeyValuePair<Tuple<int, int>, int>> contents =
                new KeyValuePair<Tuple<int, int>, int>[] {
                    new KeyValuePair<Tuple<int, int>, int> ( Tuple.Create(1,1), 2 ),
                    new KeyValuePair<Tuple<int, int>, int> ( Tuple.Create(3,1), 7 ),
                    new KeyValuePair<Tuple<int, int>, int> ( Tuple.Create(5,1), 13 )
                }
            ;

            iwdt.InsertContents(contents);

            Assert.AreEqual(2, iwdt.GetItem(HT(1, 2)));
            Assert.AreEqual(7, iwdt.GetItem(HT(3, 2)));
            Assert.AreEqual(13, iwdt.GetItem(HT(5, 2)));
        }

        /// <summary>
        ///A test for SetItem
        ///</summary>
        [TestMethod()]
        public void SetItemTest()
        {
            IWDT iwdt = new IWDT();

            iwdt.SetItem(HT(1, 1), 3);

            Assert.AreEqual(3, iwdt.GetItem(HT(1, 2)));

            iwdt.SetItem(HT(1, 1), 7);

            Assert.AreEqual(7, iwdt.GetItem(HT(1, 2)));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            iwdt.SetItem(HT(1, 3), 13);

            Assert.AreEqual(13, iwdt.GetItem(HT(1, 2)));
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.Add
        ///</summary>
        [TestMethod()]
        public void AddTest()
        {
            IWDT iwdt = new IWDT();
            var asCollection = (ICollection<KeyValuePair<Tuple<int,int>, int>>)iwdt;

            asCollection.Add(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(1, 1), 4));

            Assert.AreEqual(4, iwdt.GetItem(HT(1, 2)));

            try
            {
                asCollection.Add(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(1, 3), 8));
                Assert.Fail();
            }
            catch (ArgumentException)
            { }

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            asCollection.Add(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(1, 3), 8));

            Assert.AreEqual(8, iwdt.GetItem(HT(1, 2)));
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.Clear
        ///</summary>
        [TestMethod()]
        public void ClearTest()
        {
            IWDT iwdt = new IWDT();
            var asCollection = (ICollection<KeyValuePair<Tuple<int,int>, int>>)iwdt;

            IEnumerable<KeyValuePair<Tuple<int, int>, int>> contents =
                new KeyValuePair<Tuple<int, int>, int>[] {
                    new KeyValuePair<Tuple<int, int>, int> ( Tuple.Create(1,1), 2 ),
                    new KeyValuePair<Tuple<int, int>, int> ( Tuple.Create(3,1), 7 ),
                    new KeyValuePair<Tuple<int, int>, int> ( Tuple.Create(5,1), 13 )
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
            var asCollection = (ICollection<KeyValuePair<Tuple<int, int>, int>>)iwdt;

            Assert.IsFalse(asCollection.Contains( new KeyValuePair<Tuple<int,int>,int>( Tuple.Create(1,2), 2 ) ) );

            iwdt.AddOrUpdate(HT(1, 1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            Assert.IsTrue(asCollection.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(1, 2), 2)));
            Assert.IsTrue(asCollection.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(2, 2), 3)));
            Assert.IsTrue(asCollection.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(3, 2), 4)));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            Assert.IsFalse(asCollection.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(1, 2), 2)));
            Assert.IsTrue(asCollection.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(2, 2), 3)));
            Assert.IsTrue(asCollection.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(3, 2), 4)));
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.CopyTo
        ///</summary>
        [TestMethod()]
        public void CopyToTest()
        {
            IWDT iwdt = new IWDT();
            var asCollection = (ICollection<KeyValuePair<Tuple<int, int>, int>>)iwdt;

            iwdt.AddOrUpdate(HT(1, 1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            var array = new KeyValuePair<Tuple<int, int>, int>[3];

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

            array = new KeyValuePair<Tuple<int, int>, int>[3];

            asCollection.CopyTo(array, 1);

            Assert.AreEqual(default(KeyValuePair<Tuple<int, int>, int>), array[0]);
            Assert.AreEqual(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(2, 1), 3), array[1]);
            Assert.AreEqual(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(3, 1), 4), array[2]);
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.Remove
        ///</summary>
        [TestMethod()]
        public void RemoveTest()
        {
            IWDT iwdt = new IWDT();
            var asCollection = (ICollection<KeyValuePair<Tuple<int, int>, int>>)iwdt;

            Assert.IsFalse(asCollection.Remove(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(1, 2), 2)));

            iwdt.AddOrUpdate(HT(1, 1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            Assert.IsTrue(asCollection.Remove(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(1, 2), 2)));
            Assert.IsFalse(asCollection.Remove(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(1, 2), 2)));

            Assert.IsFalse(asCollection.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(1, 2), 2)));

            iwdt.Keys[Tuple.Create(2, 1)].IsGarbage = true;

            Assert.IsFalse(asCollection.Remove(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(2, 2), 3)));
            Assert.IsTrue(asCollection.Remove(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(3, 2), 4)));
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.Add
        ///</summary>
        [TestMethod()]
        public void AddTest1()
        {
            IWDT iwdt = new IWDT();
            var asDictionary = (IDictionary<Tuple<int, int>, int>)iwdt;

            asDictionary.Add(Tuple.Create(1, 1), 4);

            Assert.AreEqual(4, iwdt.GetItem(HT(1, 2)));

            try
            {
                asDictionary.Add(Tuple.Create(1, 3), 8);
                Assert.Fail();
            }
            catch (ArgumentException)
            { }

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            asDictionary.Add(Tuple.Create(1, 3), 8);

            Assert.AreEqual(8, iwdt.GetItem(HT(1, 2)));
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.ContainsKey
        ///</summary>
        [TestMethod()]
        public void ContainsKeyTest1()
        {
            IWDT iwdt = new IWDT();
            var asDictionary = (IDictionary<Tuple<int, int>, int>)iwdt;

            Assert.IsFalse(asDictionary.ContainsKey(Tuple.Create(1, 2)));

            iwdt.AddOrUpdate(HT(1, 1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            Assert.IsTrue(asDictionary.ContainsKey(Tuple.Create(1, 2)));
            Assert.IsTrue(asDictionary.ContainsKey(Tuple.Create(2, 2)));
            Assert.IsTrue(asDictionary.ContainsKey(Tuple.Create(3, 2)));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            Assert.IsFalse(asDictionary.ContainsKey(Tuple.Create(1, 2)));
            Assert.IsTrue(asDictionary.ContainsKey(Tuple.Create(2, 2)));
            Assert.IsTrue(asDictionary.ContainsKey(Tuple.Create(3, 2)));
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.Remove
        ///</summary>
        [TestMethod()]
        public void RemoveTest1()
        {
            IWDT iwdt = new IWDT();
            var asDictionary = (IDictionary<Tuple<int, int>, int>)iwdt;

            Assert.IsFalse(asDictionary.Remove(Tuple.Create(1, 2)));

            iwdt.AddOrUpdate(HT(1, 1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            Assert.IsTrue(asDictionary.Remove(Tuple.Create(1, 2)));

            Assert.IsFalse(asDictionary.ContainsKey(Tuple.Create(1, 2)));

            iwdt.Keys[Tuple.Create(2, 1)].IsGarbage = true;

            Assert.IsFalse(asDictionary.Remove(Tuple.Create(2, 2)));
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.TryGetValue
        ///</summary>
        [TestMethod()]
        public void TryGetValueTest()
        {
            IWDT iwdt = new IWDT();

            int value;

            Assert.IsFalse(iwdt.TryGetValue(HT(1, 2), out value));

            iwdt.AddOrUpdate(HT(1, 1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            Assert.IsTrue(iwdt.TryGetValue(HT(1, 2), out value));
            Assert.AreEqual(2, value);

            Assert.IsTrue(iwdt.TryGetValue(HT(2, 2), out value));
            Assert.AreEqual(3, value);

            Assert.IsTrue(iwdt.TryGetValue(HT(3, 2), out value));
            Assert.AreEqual(4, value);

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            Assert.IsFalse(iwdt.TryGetValue(HT(1, 2), out value));
        }

        /// <summary>
        ///A test for ToArray
        ///</summary>
        [TestMethod()]
        public void ToArrayTest()
        {
            IWDT iwdt = new IWDT();

            Assert.AreEqual(0, iwdt.ToArray().Length);

            iwdt.AddOrUpdate(HT(1, 1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            var array = iwdt.ToArray();

            Assert.AreEqual(3, array.Length);

            var list = System.Linq.Enumerable.ToList(array);

            Assert.IsTrue(list.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(1, 1), 2)));
            Assert.IsTrue(list.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(2, 1), 3)));
            Assert.IsTrue(list.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(3, 1), 4)));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            array = iwdt.ToArray();

            Assert.AreEqual(2, array.Length);

            list = System.Linq.Enumerable.ToList(array);

            Assert.IsTrue(list.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(2, 1), 3)));
            Assert.IsTrue(list.Contains(new KeyValuePair<Tuple<int, int>, int>(Tuple.Create(3, 1), 4)));
        }

        /// <summary>
        ///A test for TryAdd
        ///</summary>
        [TestMethod()]
        public void TryAddTest()
        {
            IWDT iwdt = new IWDT();

            Assert.IsTrue(iwdt.TryAdd(HT(1, 1), 4));

            Assert.AreEqual(4, iwdt.GetItem(HT(1, 2)));

            Assert.IsFalse(iwdt.TryAdd(HT(1, 3), 8));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            Assert.IsTrue(iwdt.TryAdd(HT(1, 3), 8));

            Assert.AreEqual(8, iwdt.GetItem(HT(1, 2)));
        }


        /// <summary>
        ///A test for TryRemove
        ///</summary>
        [TestMethod()]
        public void TryRemoveTest()
        {
            IWDT iwdt = new IWDT();
            var asDictionary = (IDictionary<Tuple<int, int>, int>)iwdt;

            int value;

            Assert.IsFalse(iwdt.TryRemove(HT(1, 2), out value));

            iwdt.AddOrUpdate(HT(1, 1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            Assert.IsTrue(iwdt.TryRemove(HT(1, 2), out value));
            Assert.AreEqual(2, value);

            Assert.IsFalse(asDictionary.ContainsKey(Tuple.Create(1, 2)));

            iwdt.Keys[Tuple.Create(2, 1)].IsGarbage = true;

            Assert.IsFalse(iwdt.TryRemove(HT(2, 2), out value));
        }

        /// <summary>
        ///A test for TryUpdate
        ///</summary>
        [TestMethod()]
        public void TryUpdateTest()
        {
            var iwdt = new IWDT();

            Assert.IsFalse(iwdt.TryUpdate(HT(1, 2), 12, 2));

            iwdt.AddOrUpdate(HT(1, 1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            Assert.IsTrue(iwdt.TryUpdate(HT(1, 2), 12, 2));

            Assert.AreEqual(12, iwdt.GetItem(HT(1, 2)));

            iwdt.Keys[Tuple.Create(2, 1)].IsGarbage = true;

            Assert.IsFalse(iwdt.TryUpdate(HT(2, 2), 13, 3));
        }

        /// <summary>
        ///A test for TvdP.Collections.IMaintainable.DoMaintenance
        ///</summary>
        [TestMethod()]
        public void DoMaintenanceTest()
        {
            var iwdt = new IWDT();

            iwdt.AddOrUpdate(HT(1, 1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            iwdt.Keys[Tuple.Create(2, 1)].IsGarbage = true;

            var asBaseDictionary = (IDictionary<Trashable, int>)iwdt;

            Assert.AreEqual(3, asBaseDictionary.Count);

            ((IMaintainable)iwdt).DoMaintenance();

            Assert.AreEqual(2, asBaseDictionary.Count);

            Assert.IsTrue(asBaseDictionary.Contains(new KeyValuePair<Trashable, int>(iwdt.Keys[Tuple.Create(1, 1)], 2)));
            Assert.IsTrue(asBaseDictionary.Contains(new KeyValuePair<Trashable, int>(iwdt.Keys[Tuple.Create(3, 1)], 4)));
        }

        /// <summary>
        ///A test for IsEmpty
        ///</summary>
        [TestMethod()]
        public void IsEmptyTest()
        {
            var iwdt = new IWDT();

            Assert.IsTrue(iwdt.IsEmpty);

            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });

            Assert.IsFalse(iwdt.IsEmpty);

            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            Assert.IsFalse(iwdt.IsEmpty);

            iwdt.Keys[Tuple.Create(2, 1)].IsGarbage = true;

            Assert.IsFalse(iwdt.IsEmpty);

            iwdt.Keys[Tuple.Create(3, 1)].IsGarbage = true;
            
            Assert.IsTrue(iwdt.IsEmpty);        
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.Count
        ///</summary>
        [TestMethod()]
        public void CountTest()
        {
            var iwdt = new IWDT();
            var asCollection = (ICollection<KeyValuePair<Tuple<int, int>, int>>)iwdt;

            Assert.AreEqual(0, asCollection.Count);

            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });

            Assert.AreEqual(1, asCollection.Count);

            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            Assert.AreEqual(2, asCollection.Count);

            iwdt.Keys[Tuple.Create(2, 1)].IsGarbage = true;

            Assert.AreEqual(1, asCollection.Count);

            iwdt.Keys[Tuple.Create(3, 1)].IsGarbage = true;

            Assert.AreEqual(0, asCollection.Count);
        }

        /// <summary>
        ///A test for System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<EK,EV>>.IsReadOnly
        ///</summary>
        [TestMethod()]
        public void IsReadOnlyTest()
        {
            var iwdt = new IWDT();
            var asCollection = (ICollection<KeyValuePair<Tuple<int, int>, int>>)iwdt;
            Assert.IsFalse(asCollection.IsReadOnly);
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.Item
        ///</summary>
        [TestMethod()]
        public void ItemTest()
        {
            var iwdt = new IWDT();
            var asDictionary = (IDictionary<Tuple<int, int>, int>)iwdt;
            int value;

            try
            {
                value = asDictionary[Tuple.Create(1, 2)] ;
                Assert.Fail();
            }
            catch (KeyNotFoundException)
            {}

            asDictionary[Tuple.Create(1, 1)] = 2;
            asDictionary[Tuple.Create(2, 1)] = 3;

            Assert.AreEqual(2, asDictionary[Tuple.Create(1, 2)]);
            Assert.AreEqual(3, asDictionary[Tuple.Create(2, 2)]);

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            try
            {
                value = asDictionary[Tuple.Create(1, 2)];
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
            var asDictionary = (IDictionary<Tuple<int, int>, int>)iwdt;

            Assert.AreEqual(0, asDictionary.Keys.Count);

            iwdt.AddOrUpdate(HT(1, 1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            var keys = asDictionary.Keys;

            Assert.AreEqual(3, keys.Count);
            Assert.IsTrue(keys.Contains(Tuple.Create(1, 1)));
            Assert.IsTrue(keys.Contains(Tuple.Create(2, 1)));
            Assert.IsTrue(keys.Contains(Tuple.Create(3, 1)));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            keys = asDictionary.Keys;

            Assert.AreEqual(2, keys.Count);
            Assert.IsTrue(keys.Contains(Tuple.Create(2, 1)));
            Assert.IsTrue(keys.Contains(Tuple.Create(3, 1)));
        }

        /// <summary>
        ///A test for System.Collections.Generic.IDictionary<EK,EV>.Values
        ///</summary>
        [TestMethod()]
        public void ValuesTest()
        {
            IWDT iwdt = new IWDT();
            var asDictionary = (IDictionary<Tuple<int, int>, int>)iwdt;

            var values = asDictionary.Values;

            Assert.AreEqual(0, values.Count);

            iwdt.AddOrUpdate(HT(1, 1), 2, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(2, 1), 3, (k, v) => { throw new AssertFailedException(); return default(int); });
            iwdt.AddOrUpdate(HT(3, 1), 4, (k, v) => { throw new AssertFailedException(); return default(int); });

            Assert.AreEqual(3, values.Count);
            Assert.IsTrue(values.Contains(2));
            Assert.IsTrue(values.Contains(3));
            Assert.IsTrue(values.Contains(4));

            iwdt.Keys[Tuple.Create(1, 1)].IsGarbage = true;

            Assert.AreEqual(2, values.Count);
            Assert.IsTrue(values.Contains(3));
            Assert.IsTrue(values.Contains(4));
        }
    }
}
