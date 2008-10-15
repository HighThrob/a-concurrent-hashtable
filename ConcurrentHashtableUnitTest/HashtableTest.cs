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
using SCG = System.Collections.Generic;
using System.Threading;

namespace TvdP.Collections
{
    [TestClass]
    public class HashtableTest
    {
        SCG.Dictionary<int, string> GetFiller(string prefix)
        {
            var dict = new SCG.Dictionary<int, string>();

            for (int i = 0; i < 1000; ++i)
            {
                //var h = Hasher.Rehash(EqualityComparer<int>.Default.GetHashCode(Hasher.Rehash(i)));
                var h = Hasher.Rehash(i);

                unchecked
                {
                    dict.Add((int)h, prefix + (h & 31).ToString());
                }
            }

            return dict;
        }

        SCG.Dictionary<int, string> GetFiller()
        { return GetFiller("STR:"); }

        SCG.Dictionary<int, string> GetTiller()
        {
            var dict = new SCG.Dictionary<int, string>();

            for (int i = 1000; i < 2000; ++i)
            {
                //var h = Hasher.Rehash(EqualityComparer<int>.Default.GetHashCode(Hasher.Rehash(i)));
                var h = Hasher.Rehash(i);

                unchecked
                {
                    dict.Add((int)h, "STR:" + (h & 31).ToString());
                }
            }

            return dict;
        }

        class HashtableStub : ConcurrentWeakHashtable<KeyValuePair<int, string>?, int>
        {
            public HashtableStub()
                : base()
            {
                _Comparer = EqualityComparer<int>.Default;

                Initialize();
            }

            readonly IEqualityComparer<int> _Comparer;

            internal protected override UInt32 GetHashCode(ref KeyValuePair<int, string>? item)
            {
                return item.HasValue ? Hasher.Rehash(_Comparer.GetHashCode(item.Value.Key)) : 0;
                //return item.HasValue ? item.Value.Key : 0;
            }

            internal protected override UInt32 GetHashCode(ref int key)
            {
                return Hasher.Rehash(_Comparer.GetHashCode(key));
                //return key;
            }

            internal protected override bool Equals(ref KeyValuePair<int, string>? item, ref int key)
            { return item.HasValue && _Comparer.Equals(item.Value.Key, key); }

            internal protected override bool Equals(ref KeyValuePair<int, string>? item1, ref KeyValuePair<int, string>? item2)
            { return item1.HasValue && item2.HasValue && _Comparer.Equals(item1.Value.Key, item2.Value.Key); }

            internal protected override bool IsEmpty(ref KeyValuePair<int, string>? item)
            { return !item.HasValue; }

            internal protected override bool IsGarbage(ref KeyValuePair<int, string>? item)
            { return item.HasValue && item.Value.Value == "garbage"; }

            public new bool FindItem(ref int searchKey, out KeyValuePair<int, string>? item)
            { return base.FindItem(ref searchKey, out item); }

            public new bool InsertItem(ref KeyValuePair<int, string>? searchKey, out KeyValuePair<int, string>? replacedItem)
            { return base.InsertItem(ref searchKey, out replacedItem); }

            public new bool GetOldestItem(ref KeyValuePair<int, string>? searchKey, out KeyValuePair<int, string>? item)
            { return base.GetOldestItem(ref searchKey, out item); }

            public new bool RemoveItem(ref int searchKey, out KeyValuePair<int, string>? removedItem)
            { return base.RemoveItem(ref searchKey, out removedItem); }

            public new void Clear()
            { base.Clear(); }

            public new IEnumerable<KeyValuePair<int, string>?> Items
            { get{ return base.Items; } }

            public new int Count
            {
                get { return base.Count; }
            }

            public new object SyncRoot
            {
                get { return base.SyncRoot; }
            }
        }

        [TestMethod]
        public void HashtableInsertItem()
        {
            var filler = GetFiller();
            var stub = new HashtableStub();

            foreach (var kvp in filler)
            {
                KeyValuePair<int, string>? newItem = kvp;
                KeyValuePair<int, string>? replacedItem;

                Assert.IsFalse(stub.InsertItem(ref newItem, out replacedItem), "All unique item inserts, expected false returned by InsertItem");
            }

            Assert.AreEqual(filler.Count, stub.Count, "Expected _Count to be same as number of unique inserts.");

            GC.Collect();

            Thread.Sleep(1000); // 1 sec

            Assert.AreEqual(filler.Count, stub.Count, "Expected _Count to be same as number of unique inserts, even after GC.");

            foreach (var kvp in filler)
            {
                KeyValuePair<int, string>? newItem = kvp;
                KeyValuePair<int, string>? replacedItem;

                Assert.IsTrue(stub.InsertItem(ref newItem, out replacedItem), "All double inserts, expected true returned by InsertItem");

                Assert.IsTrue(replacedItem.HasValue, "Expected valid item as replaced item.");
                Assert.AreEqual(kvp, replacedItem.Value, "Expected replaced item to be equal to inserted item.");
            }

            Assert.AreEqual(filler.Count, stub.Count, "Expected _Count unchanged after only duplicate inserts.");

            lock (stub.SyncRoot)
            {
                foreach (var item in stub.Items)
                {
                    Assert.IsTrue(item.HasValue, "Expected valid items return from iterator.");

                    Assert.IsTrue(filler.Remove(item.Value.Key), "Expected item to be present ONCE amongst filler items.");
                }
            }

            Assert.AreEqual(0, filler.Count, "Expected all items to be returned by iterator.");

            //inserting from multiple threads.
            filler = GetFiller();
            stub = new HashtableStub();
            int runningThreads = 10;

            for (int i = 0; i < 10; ++i)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(
                    new WaitCallback(
                        delegate(object tgt)
                        {
                            int j = 0;

                            foreach (var kvp in filler)
                            {
                                KeyValuePair<int, string>? newItem = kvp;
                                KeyValuePair<int, string>? replacedItem;

                                stub.InsertItem(ref newItem, out replacedItem);

                                if (j == i)
                                {
                                    j = 0;
                                    Thread.Sleep(0);
                                }
                                else
                                    ++j;
                            }

                            Interlocked.Decrement(ref runningThreads);
                        }
                    )
                );
            }

            int wait = 200;

            do
            { Thread.Sleep(100); }
            while (Interlocked.Decrement(ref wait) > 0 && runningThreads != 0); 

            Assert.AreEqual(0, runningThreads, "Expected all threads to be finished by now.");
            Assert.AreEqual(filler.Count, stub.Count, "Expected _Count to be equal to number of unique inserted items.");
        }

        [TestMethod]
        public void HashtableItems()
        {
            var filler = GetFiller();
            var stub = new HashtableStub();

            foreach (var kvp in filler)
            {
                KeyValuePair<int, string>? newItem = kvp;
                KeyValuePair<int, string>? replacedItem;

                Assert.IsFalse(stub.InsertItem(ref newItem, out replacedItem), "All unique item inserts, expected false returned by InsertItem");
            }

            lock (stub.SyncRoot)
            {
                foreach (var item in stub.Items)
                {
                    Assert.IsTrue(item.HasValue, "Expected valid items return from iterator.");

                    Assert.IsTrue(filler.Remove(item.Value.Key), "Expected item to be present ONCE amongst filler items.");
                }
            }

            filler = GetFiller();

            Assert.AreEqual(filler.Count, stub.Count, "Expected _Count to be same as number of unique inserts.");

            GC.Collect();

            Thread.Sleep(1000); // 1 sec

            Assert.AreEqual(filler.Count, stub.Count, "Expected _Count to be same as number of unique inserts, even after GC.");
           
            lock (stub.SyncRoot)
            {
                foreach (var item in stub.Items)
                {
                    Assert.IsTrue(item.HasValue, "Expected valid items return from iterator, even after GC.");

                    Assert.IsTrue(filler.Remove(item.Value.Key), "Expected item to be present ONCE amongst filler items, even after GC.");
                }
            }

            Assert.AreEqual(0, filler.Count, "Expected all items to be returned by iterator, even after GC.");
        }

        [TestMethod]
        public void HashtableFindItem()
        {
            var filler = GetFiller();
            var tiller = GetTiller();
            var stub = new HashtableStub();

            foreach (var kvp in filler)
            {
                KeyValuePair<int, string>? newItem = kvp;
                KeyValuePair<int, string>? replacedItem;

                Assert.IsFalse(stub.InsertItem(ref newItem, out replacedItem), "All unique item inserts, expected false returned by InsertItem");
            }

            Assert.AreEqual(filler.Count, stub.Count, "Expected _Count to be same as number of unique inserts.");

            foreach (var kvp in filler)
            {
                int key = kvp.Key;
                KeyValuePair<int, string>? foundItem;

                Assert.IsTrue(stub.FindItem(ref key, out foundItem), "Expected return true on find on inserted key");
                Assert.AreEqual(kvp, foundItem.Value, "Expected found item to be same as inserted item.");
            }

            foreach (var kvp in tiller)
            {
                if (!filler.ContainsKey(kvp.Key))
                {
                    int key = kvp.Key;
                    KeyValuePair<int, string>? foundItem;

                    Assert.IsFalse(stub.FindItem(ref key, out foundItem), "Expected return false on find on NOT inserted key");
                }
            }

            GC.Collect();

            Thread.Sleep(1000); // 1 sec

            Assert.AreEqual(filler.Count, stub.Count, "Expected _Count to be same as number of unique inserts, even after GC.");

            foreach (var kvp in filler)
            {
                int key = kvp.Key;
                KeyValuePair<int, string>? foundItem;

                Assert.IsTrue(stub.FindItem(ref key, out foundItem), "Expected return true on find on inserted key, even after GC.");
                Assert.AreEqual(kvp, foundItem.Value, "Expected found item to be same as inserted item, even after GC.");
            }

            foreach (var kvp in tiller)
            {
                if (!filler.ContainsKey(kvp.Key))
                {
                    int key = kvp.Key;
                    KeyValuePair<int, string>? foundItem;

                    Assert.IsFalse(stub.FindItem(ref key, out foundItem), "Expected return false on find on NOT inserted key, even after GC.");
                }
            }

            //finding int multiple threads.
            filler = GetFiller();
            int runningThreads = 10;

            for (int i = 0; i < 10; ++i)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(
                    new WaitCallback(
                        delegate(object tgt)
                        {
                            int j = 0;
                            var set = i % 2 == 0 ? filler : tiller;

                            foreach (var kvp in set)
                            {
                                int key = kvp.Key;
                                KeyValuePair<int, string>? foundItem;

                                Assert.IsTrue((set == filler) == stub.FindItem(ref key, out foundItem), "Expected return true on find on inserted key, even after GC and on multiple threads.");

                                if (j == i)
                                {
                                    j = 0;
                                    Thread.Sleep(0);
                                }
                                else
                                    ++j;
                            }

                            Interlocked.Decrement(ref runningThreads);
                        }
                    )
                );
            }

            {
                int wait = 200;

                do
                { Thread.Sleep(100); }
                while (Interlocked.Decrement(ref wait) > 0 && runningThreads != 0);
            }

            Assert.AreEqual(0, runningThreads, "Expected all threads to be finished by now.");
        }

        [TestMethod]
        public void HashtableGetOldestItem()
        {
            var filler = GetFiller();
            var tiller = GetFiller("alt:");
            var stub = new HashtableStub();

            foreach (var kvp in filler)
            {
                KeyValuePair<int, string>? newItem = kvp;
                KeyValuePair<int, string>? replacedItem;

                Assert.IsFalse(stub.GetOldestItem(ref newItem, out replacedItem), "All unique item inserts, expected false returned by GetOldestItem");
                Assert.AreEqual(kvp, replacedItem.Value, "Expected oldest item to be equal to inserted item.");
            }

            foreach (var kvp in tiller)
            {
                KeyValuePair<int, string>? newItem = kvp;
                KeyValuePair<int, string>? replacedItem;

                Assert.IsTrue(stub.GetOldestItem(ref newItem, out replacedItem), "All double inserts, expected true returned by GetOldestItem");

                Assert.IsTrue(replacedItem.HasValue, "Expected valid item as original item.");
                Assert.AreNotEqual(kvp, replacedItem.Value, "Expected replaced item to be original item.");
            }

            Assert.AreEqual(filler.Count, stub.Count, "Expected _Count to be same as number of unique inserts.");

            GC.Collect();

            Thread.Sleep(1000); // 1 sec

            Assert.AreEqual(filler.Count, stub.Count, "Expected _Count to be same as number of unique inserts, even after GC.");

            foreach (var kvp in tiller)
            {
                KeyValuePair<int, string>? newItem = kvp;
                KeyValuePair<int, string>? replacedItem;

                Assert.IsTrue(stub.GetOldestItem(ref newItem, out replacedItem), "All double inserts, expected true returned by GetOldestItem, even after GC.");

                Assert.IsTrue(replacedItem.HasValue, "Expected valid item as original item, even after GC.");
                Assert.AreNotEqual(kvp, replacedItem.Value, "Expected replaced item to be original item, even after GC.");
            }

            //finding int multiple threads.
            stub = new HashtableStub();
            int runningThreads = 10;

            for (int i = 0; i < 10; ++i)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(
                    new WaitCallback(
                        delegate(object tgt)
                        {
                            int j = 0;
                            var set = i % 2 == 0 ? filler : tiller;

                            foreach (var kvp in set)
                            {
                                KeyValuePair<int, string>? newItem = kvp;
                                KeyValuePair<int, string>? replacedItem;

                                stub.GetOldestItem(ref newItem, out replacedItem);

                                if (j == i)
                                {
                                    j = 0;
                                    Thread.Sleep(0);
                                }
                                else
                                    ++j;
                            }

                            Interlocked.Decrement(ref runningThreads);
                        }
                    )
                );
            }

            {
                int wait = 200;

                do
                { Thread.Sleep(100); }
                while (Interlocked.Decrement(ref wait) > 0 && runningThreads != 0);
            }

            Assert.AreEqual(0, runningThreads, "Expected all threads to be finished by now.");

            //when he hashtable contains an item that is marked as garbage; it should not get returned by GetOldestItem.

            stub = new HashtableStub();

            KeyValuePair<int, string>? garbageItem = new KeyValuePair<int, string>(10, "garbage"); //specially marked as garbage.
            KeyValuePair<int, string>? replacedItem2;

            stub.InsertItem(ref garbageItem, out replacedItem2);

            KeyValuePair<int, string>? newItem2 = new KeyValuePair<int, string>(10, "not garbage");

            Assert.AreEqual(false, stub.GetOldestItem(ref newItem2, out replacedItem2), "Expected not to find a garbage item.");
            Assert.AreNotEqual(garbageItem, replacedItem2, "Expected returned item not to be a stored garbage item.");

        }

        [TestMethod]
        public void HashtableRemoveItem()
        {
            var filler = GetFiller();
            var tiller = GetTiller();
            var stub = new HashtableStub();

            foreach (var kvp in filler)
            {
                KeyValuePair<int, string>? newItem = kvp;
                KeyValuePair<int, string>? replacedItem;

                Assert.IsFalse(stub.InsertItem(ref newItem, out replacedItem), "All unique item inserts, expected false returned by GetOldestItem");
                Assert.AreEqual(kvp, replacedItem.Value, "Expected oldest item to be equal to inserted item.");
            }

            foreach (var kvp in tiller)
            {
                if (!filler.ContainsKey(kvp.Key))
                {
                    int key = kvp.Key;
                    KeyValuePair<int, string>? removedItem;

                    Assert.IsFalse(stub.RemoveItem(ref key, out removedItem), "Removing not inserted item, expected false returned by RemoveItem");
                }
            }

            Assert.AreEqual(filler.Count, stub.Count, "Expected _Count to be same as number of unique inserts.");

            GC.Collect();

            Thread.Sleep(1000); // 1 sec

            Assert.AreEqual(filler.Count, stub.Count, "Expected _Count to be same as number of unique inserts, even after GC.");

            foreach (var kvp in tiller)
            {
                if (!filler.ContainsKey(kvp.Key))
                {
                    int key = kvp.Key;
                    KeyValuePair<int, string>? removedItem;

                    Assert.IsFalse(stub.RemoveItem(ref key, out removedItem), "Removing not inserted item, expected false returned by RemoveItem, even after GC.");
                }
            }

            foreach (var kvp in filler)
            {
                int key = kvp.Key;
                KeyValuePair<int, string>? removedItem;

                Assert.IsTrue(stub.RemoveItem(ref key, out removedItem), "Inserted items, expected true returned by RemoveItem");
                Assert.AreEqual(kvp, removedItem.Value, "Expected removed item to be equal to inserted item.");
            }

            Assert.AreEqual(0, stub.Count, "Expected _Count to be 0 after all inserted items removed, even after GC.");

            //removing by multiple threads.
            foreach (var kvp in filler)
            {
                KeyValuePair<int, string>? newItem = kvp;
                KeyValuePair<int, string>? replacedItem;

                Assert.IsFalse(stub.InsertItem(ref newItem, out replacedItem), "All unique item inserts, expected false returned by GetOldestItem");
                Assert.AreEqual(kvp, replacedItem.Value, "Expected oldest item to be equal to inserted item.");
            }

            stub = new HashtableStub();
            int runningThreads = 10;

            for (int i = 0; i < 10; ++i)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(
                    new WaitCallback(
                        delegate(object tgt)
                        {
                            int j = 0;

                            foreach (var kvp in filler)
                            {
                                int key = kvp.Key;
                                KeyValuePair<int, string>? newItem = kvp;
                                KeyValuePair<int, string>? replacedItem;

                                if (i % 2 == 0)
                                    stub.InsertItem(ref newItem, out replacedItem);
                                else
                                    stub.RemoveItem(ref key, out replacedItem);

                                if (j == i)
                                {
                                    j = 0;
                                    Thread.Sleep(0);
                                }
                                else
                                    ++j;
                            }

                            Interlocked.Decrement(ref runningThreads);
                        }
                    )
                );
            }

            {
                int wait = 200;

                do
                { Thread.Sleep(100); }
                while (Interlocked.Decrement(ref wait) > 0 && runningThreads != 0);
            }

            Assert.AreEqual(0, runningThreads, "Expected all threads to be finished by now.");

        }

        [TestMethod]
        public void HashtableClear()
        {
            var filler = GetFiller();
            var tiller = GetTiller();
            var stub = new HashtableStub();

            foreach (var kvp in filler)
            {
                KeyValuePair<int, string>? newItem = kvp;
                KeyValuePair<int, string>? replacedItem;

                Assert.IsFalse(stub.InsertItem(ref newItem, out replacedItem), "All unique item inserts, expected false returned by GetOldestItem");
                Assert.AreEqual(kvp, replacedItem.Value, "Expected oldest item to be equal to inserted item.");
            }

            Assert.AreEqual(filler.Count, stub.Count, "Expected _Count to be same as number of unique inserts.");

            GC.Collect();

            Thread.Sleep(1000); // 1 sec

            Assert.AreEqual(filler.Count, stub.Count, "Expected _Count to be same as number of unique inserts, even after GC.");

            stub.Clear();

            Assert.AreEqual(0, stub.Count, "Expected _Count to be 0 after clear, even after GC.");
        }

    }
}
