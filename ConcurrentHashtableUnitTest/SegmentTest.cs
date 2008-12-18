/*  
 Copyright 2008 The 'A Concurrent Hashtable' development team  
 (http://www.codeplex.com/CH/People/ProjectPeople.aspx)

 This library is licensed under the GNU Library General Public License (LGPL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://www.codeplex.com/CH/license.
*/

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TvdP.Collections
{
    /// <summary>
    /// Summary description for SegmentTest
    /// </summary>
    [TestClass]
    public class SegmentTest
    {
        public SegmentTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

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
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion


        class SegmentTraits : ConcurrentWeakHashtable<long?, int>
        {
            internal SegmentTraits()
                : base()
            {
                Initialize();
            }

            #region ISegmentTraits<long?,int> Members

            internal protected override UInt32 GetItemHashCode(ref long? storedItem)
            {
                return (UInt32)storedItem.GetHashCode();
            }

            internal protected override UInt32 GetKeyHashCode(ref int searchKey)
            {
                return (UInt32)((long?)searchKey).GetHashCode();
            }

            internal protected override bool ItemEqualsKey(ref long? storedItem, ref int searchKey)
            {
                return storedItem == (long?)searchKey;
            }

            internal protected override bool ItemEqualsItem(ref long? storedItem1, ref long? storedItem2)
            {
                return storedItem1 == storedItem2;
            }

            internal protected override bool IsEmpty(ref long? storedItem)
            {
                return !storedItem.HasValue;
            }

            protected internal override bool IsGarbage(ref long? item)
            {
                return false;
            }

            #endregion
        }

        [TestMethod]
        public void SegmentInserts()
        {
            var segment = Segment<long?, int>.Create(4);
            var traits = new SegmentTraits();

            long?[] items = new long?[] { 34L, 2L, 94L, 236L, 41L, 34928L, 222L, 345L, 123L, 1092L, 347L };

            for (int i = 0, end = items.Length; i != end; ++i)
            {
                long? oldItem;
                bool res = segment.InsertItem(ref items[i], out oldItem, traits);
                Assert.IsFalse(res, "Unique inserts in empty table should return false.");                
            }

            Assert.AreEqual(
                items.Length,
                segment._Count,                
                "Number of items in segment doesn't match number of inserts"
            );

            var it = -1;
            long? item;

            while((it = segment.GetNextItem(it, out item, traits)) >= 0)
                Assert.IsTrue(
                    items.Contains(item),
                    "Item not present in inserted items collection."
                );
        }

        [TestMethod]
        public void SegmentDuplicateInserts()
        {
            var segment = Segment<long?, int>.Create(4);
            var traits = new SegmentTraits();

            long?[] items = new long?[] { 34L, 2L, 94L, 236L, 41L, 34928L, 222L, 345L, 123L, 1092L, 347L };

            for (int i = 0, end = items.Length; i != end; ++i)
            {
                long? oldItem;
                Assert.IsFalse(segment.InsertItem(ref items[i], out oldItem, traits), "Unique keys, should not expect a replace.");
            }

            for (int i = 0, end = items.Length; i != end; ++i)
            {
                long? oldItem;
                Assert.IsTrue(segment.InsertItem(ref items[i], out oldItem, traits), "Duplicate keys, expect replacements.");
            }

            Assert.AreEqual(
                segment._Count,
                items.Length,
                "Number of items in segment doesn't match number of unique inserts"
            );
        }

        [TestMethod]
        public void SegmentFindItem()
        {
            var segment = Segment<long?, int>.Create(4);
            var traits = new SegmentTraits();

            long?[] items = new long?[] { 34L, 2L, 94L, 236L, 41L, 34928L, 222L, 345L, 123L, 1092L, 347L };

            for (int i = 0, end = items.Length; i != end; ++i)
            {
                long? oldItem;
                Assert.IsFalse(segment.InsertItem(ref items[i], out oldItem, traits), "Unique keys, should not expect a replace.");
            }

            int[] searchKeys = new int[] { 2, 34928, 1092, 222, 94, 34, 347, 123, 41, 345 };

            for (int i = 0, end = searchKeys.Length; i != end; ++i)
            {
                long? foundItem;
                Assert.IsTrue(segment.FindItem(ref searchKeys[i], out foundItem, traits), "Expected to find an item.");
                Assert.AreEqual(foundItem, (long?)searchKeys[i], "Found item not expected value.");
            }

            int[] searchKeys2 = new int[] { 12, 0, 13, 55, 1000, 10, 20, 124, 42, 345345 };

            for (int i = 0, end = searchKeys2.Length; i != end; ++i)
            {
                long? foundItem;
                Assert.IsFalse(segment.FindItem(ref searchKeys2[i], out foundItem, traits), "Expected not to find an item.");
            }
        }

        [TestMethod]
        public void SegmentClear()
        {
            var segment = Segment<long?, int>.Create(4);
            var traits = new SegmentTraits();

            long?[] items = new long?[] { 34L, 2L, 94L, 236L, 41L, 34928L, 222L, 345L, 123L, 1092L, 347L };

            for (int i = 0, end = items.Length; i != end; ++i)
            {
                long? oldItem;
                segment.InsertItem(ref items[i], out oldItem, traits);
            }

            segment.Clear(traits);

            Assert.AreEqual(segment._Count, 0, "Expected segment to be empty after Clear().");

            int[] searchKeys = new int[] { 2, 34928, 1092, 222, 94, 34, 347, 123, 41, 345 };

            for (int i = 0, end = searchKeys.Length; i != end; ++i)
            {
                long? foundItem;
                Assert.IsFalse(segment.FindItem(ref searchKeys[i], out foundItem, traits), "Expected not to find an item after Clear().");
            }
        }

        [TestMethod]
        public void SegmentRemoveItem()
        {
            var segment = Segment<long?, int>.Create(4);
            var traits = new SegmentTraits();

            long?[] items = new long?[] { 34L, 2L, 94L, 236L, 41L, 34928L, 222L, 345L, 123L, 1092L, 347L };

            for (int i = 0, end = items.Length; i != end; ++i)
            {
                long? oldItem;
                Assert.IsFalse(segment.InsertItem(ref items[i], out oldItem, traits), "Unique keys, should not expect a replace.");
            }

            int[] searchKeys = new int[] { 2, 34928, 1092, 222, 94 };

            for (int i = 0, end = searchKeys.Length; i != end; ++i)
            {
                long? foundItem;
                Assert.IsTrue(segment.RemoveItem(ref searchKeys[i], out foundItem, traits), "Expected to find and remove an item.");
                Assert.AreEqual((long?)searchKeys[i], foundItem, "Found item not expected value.");
            }

            Assert.AreEqual(6, segment._Count, "Expected 6 items left.");

            for (int i = 0, end = searchKeys.Length; i != end; ++i)
            {
                long? foundItem;
                Assert.IsFalse(segment.RemoveItem(ref searchKeys[i], out foundItem, traits), "Expected not to find removed item.");
            }

            int[] searchKeys2 = new int[] { 12, 0, 13, 55, 1000, 10, 20, 124, 42, 345345 };

            for (int i = 0, end = searchKeys2.Length; i != end; ++i)
            {
                long? foundItem;
                Assert.IsFalse(segment.RemoveItem(ref searchKeys2[i], out foundItem, traits), "Expected not to be able to remove item not in segment.");
            }

            int[] searchKeys3 = new int[] { 34, 347, 123, 41, 345, 236 };

            for (int i = 0, end = searchKeys3.Length; i != end; ++i)
            {
                long? foundItem;
                Assert.IsTrue(segment.RemoveItem(ref searchKeys3[i], out foundItem, traits), "Expected to find and remove an item.");
                Assert.AreEqual((long?)searchKeys3[i], foundItem, "Found item not expected value.");
            }

            Assert.AreEqual(0, segment._Count, "Expected 0 items left.");
        }

        [TestMethod]
        public void SegmentGetOldest()
        {
            var segment = Segment<long?, int>.Create(4);
            var traits = new SegmentTraits();

            long?[] items = new long?[] { 34L, 2L, 94L, 236L, 41L, 34928L, 222L, 345L, 123L, 1092L, 347L };

            for (int i = 0, end = items.Length; i != end; ++i)
            {
                long? oldItem;
                Assert.IsFalse(segment.GetOldestItem(ref items[i], out oldItem, traits), "Unique keys, should not expect an older item.");
                Assert.AreEqual(items[i], oldItem, "Should receive newly inserted item.");
            }

            for (int i = 0, end = items.Length; i != end; ++i)
            {
                long? oldItem;
                Assert.IsTrue(segment.InsertItem(ref items[i], out oldItem, traits), "Duplicate keys, expect older items");
                Assert.AreEqual(items[i], oldItem, "Should receive correct older item.");
            }

            Assert.AreEqual(
                segment._Count,
                items.Length,
                "Number of items in segment doesn't match number of unique inserts"
            );
        }

        [TestMethod]
        public void SegmentGetNextItem()
        {
            var segment = Segment<long?, int>.Create(4);
            var traits = new SegmentTraits();

            long?[] items = new long?[] { 34L, 2L, 94L, 236L, 41L, 34928L, 222L, 345L, 123L, 1092L, 347L };

            for (int i = 0, end = items.Length; i != end; ++i)
            {
                long? oldItem;
                segment.InsertItem(ref items[i], out oldItem, traits);
            }

            int iterator = -1;
            int count = 0;
            long? storedItem ;
            List<long?> itemsList = new List<long?>(items);

            while( (iterator = segment.GetNextItem(iterator, out storedItem, traits)) >= 0 )
            {
                ++count;
                Assert.IsTrue(itemsList.Contains(storedItem), "Received unexpected item from GetNextItem.");
                itemsList.Remove(storedItem);
            }

            Assert.AreEqual(items.Length, count, "Unexpected number of items returned.");
        }
    }
}
