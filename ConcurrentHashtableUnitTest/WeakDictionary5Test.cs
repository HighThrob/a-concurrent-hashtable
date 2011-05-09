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
using TvdP.Collections;

namespace ConcurrentHashtableUnitTest
{
    using DTA = DictionaryTestAdapter<Tuple<object, object, object, object, int>, object, WeakKeyDictionary<object, object, object, object, int, object>>;
    
    [TestClass]
    public class WeakKeyDictionary4Test
    {
        class WeakKeyDictionary4Adaper : IDictionaryTestAdapter<Tuple<object, object, object, object,int>,object,WeakKeyDictionary<object, object, object, object,int,object>>
        {
            #region IDictionaryTestAdapter<Tuple<object, object, object, object,int>,object,WeakKeyDictionary<object, object, object, object,int,object>> Members

            public Tuple<object, object, object, object, int> GetKey(int ix)
            { return Tuple.Create(new Object(), new Object(), new Object(), new Object(), ix); }

            public object GetValue(int ix)
            { return new Object(); }

            public WeakKeyDictionary<object, object, object, object, int, object> GetDictionary()
            { return new WeakKeyDictionary<object, object, object, object, int, object>(); }

            public bool ContainsKey(WeakKeyDictionary<object, object, object, object, int, object> d, Tuple<object, object, object, object, int> k)
            { return d.ContainsKey(k.Item1, k.Item2, k.Item3, k.Item4, k.Item5); }

            public bool TryGetValue(WeakKeyDictionary<object, object, object, object, int, object> d, Tuple<object, object, object, object, int> k, out object v)
            { return d.TryGetValue( k.Item1, k.Item2, k.Item3, k.Item4, k.Item5, out v ); }

            public object GetItem(WeakKeyDictionary<object, object, object, object, int, object> d, Tuple<object, object, object, object, int> k)
            { return d[ k.Item1, k.Item2, k.Item3, k.Item4, k.Item5 ]; }

            public void SetItem(WeakKeyDictionary<object, object, object, object, int, object> d, Tuple<object, object, object, object, int> k, object v)
            { d[k.Item1, k.Item2, k.Item3, k.Item4, k.Item5] = v; }

            public bool IsEmpty(WeakKeyDictionary<object, object, object, object, int, object> d)
            { return d.IsEmpty; }

            public object AddOrUpdate(WeakKeyDictionary<object, object, object, object, int, object> d, Tuple<object, object, object, object, int> k, Func<Tuple<object, object, object, object, int>, object> addValueFactory, Func<Tuple<object, object, object, object, int>, object, object> updateValueFactory)
            {
                return 
                    d.AddOrUpdate(
                        k.Item1, 
                        k.Item2,
                        k.Item3,
                        k.Item4,
                        k.Item5,
                        (kp1, kp2, kp3, kp4, kp5) => addValueFactory(Tuple.Create(kp1, kp2, kp3, kp4, kp5)), 
                        (kp1, kp2, kp3, kp4, kp5, v) => updateValueFactory( Tuple.Create(kp1 ,kp2, kp3, kp4, kp5), v ) 
                    )
                ;
            }

            public object AddOrUpdate(WeakKeyDictionary<object, object, object, object, int, object> d, Tuple<object, object, object, object, int> k, object addValue, Func<Tuple<object, object, object, object, int>, object, object> updateValueFactory)
            {
                return
                    d.AddOrUpdate(
                        k.Item1,
                        k.Item2,
                        k.Item3,
                        k.Item4,
                        k.Item5,
                        addValue,
                        (kp1, kp2, kp3, kp4, kp5, v) => updateValueFactory(Tuple.Create(kp1, kp2, kp3, kp4, kp5), v)
                    )
                ;
            }

            public object GetOrAdd(WeakKeyDictionary<object, object, object, object, int, object> d, Tuple<object, object, object, object, int> k, object v)
            { return d.GetOrAdd( k.Item1, k.Item2, k.Item3, k.Item4, k.Item5, v ); }

            public object GetOrAdd(WeakKeyDictionary<object, object, object, object, int, object> d, Tuple<object, object, object, object, int> k, Func<Tuple<object, object, object, object, int>, object> valueFactory)
            { return d.GetOrAdd(k.Item1, k.Item2, k.Item3, k.Item4, k.Item5, (kp1, kp2, kp3, kp4, kp5) => valueFactory(Tuple.Create(kp1, kp2, kp3, kp4, kp5))); }

            public KeyValuePair<Tuple<object, object, object, object, int>, object>[] ToArray(WeakKeyDictionary<object, object, object, object, int, object> d)
            { return d.ToArray(); }

            public bool TryAdd(WeakKeyDictionary<object, object, object, object, int, object> d, Tuple<object, object, object, object, int> k, object v)
            { return d.TryAdd(k.Item1, k.Item2, k.Item3, k.Item4, k.Item5, v ); }

            public bool TryRemove(WeakKeyDictionary<object, object, object, object, int, object> d, Tuple<object, object, object, object, int> k, out object v)
            { return d.TryRemove(k.Item1, k.Item2, k.Item3, k.Item4, k.Item5, out v); }

            public bool TryUpdate(WeakKeyDictionary<object, object, object, object, int, object> d, Tuple<object, object, object, object, int> k, object newValue, object comparisonValue)
            { return d.TryUpdate(k.Item1, k.Item2, k.Item3, k.Item4, k.Item5, newValue, comparisonValue) ; }

            #endregion
        }

        DTA DTA = new DTA { _adapter = new WeakKeyDictionary4Adaper() } ;

        [TestMethod]
        public void ContainsKeyTest()
        { DTA.TestContainsKey(); }

        [TestMethod]
        public void TryGetValueTest()
        { DTA.TestTryGetValue(); }

        [TestMethod]
        public void GetItemTest()
        { DTA.TestGetItem(); }

        [TestMethod]
        public void SetItemTest()
        { DTA.TestSetItem(); }

        [TestMethod]
        public void IsEmptyTest()
        { DTA.TestIsEmpty(); }

        [TestMethod]
        public void AddOrUpdate1Test()
        { DTA.TestAddOrUpdate1(); }

        [TestMethod]
        public void AddOrUpdate2Test()
        { DTA.TestAddOrUpdate2(); }

        [TestMethod]
        public void GetOrAdd1Test()
        { DTA.TestGetOrAdd1(); }

        [TestMethod]
        public void GetOrAdd2Test()
        { DTA.TestGetOrAdd2(); }

        [TestMethod]
        public void ToArrayTest()
        { DTA.TestToArray(); }

        [TestMethod]
        public void TryAddTest()
        { DTA.TestTryAdd(); }

        [TestMethod]
        public void TryRemoveTest()
        { DTA.TestTryRemove(); }

        [TestMethod]
        public void TryUpdateTest()
        { DTA.TestTryUpdate(); }

        [TestMethod]
        public void WeaknessTest()
        {
            var dict = DTA._adapter.GetDictionary();

            object key1Obj1 = new Object();
            object key1Obj2 = new Object();
            object key1Obj3 = new Object();
            object value1Obj = new Object();

            object key2Obj1 = new Object();
            object key2Obj2 = new Object();
            object key2Obj4 = new Object();
            object value2Obj = new Object();

            object key3Obj1 = new Object();
            object key3Obj3 = new Object();
            object key3Obj4 = new Object();
            object value3Obj = new Object();

            object key4Obj2 = new Object();
            object key4Obj3 = new Object();
            object key4Obj4 = new Object();
            object value4Obj = new Object();

            object key5Obj1 = new Object();
            object key5Obj2 = new Object();
            object key5Obj3 = new Object();
            object key5Obj4 = new Object();

            dict.TryAdd(key1Obj1, key1Obj2, key1Obj3, new Object(), 1, value1Obj);
            dict.TryAdd(key2Obj1, key2Obj2, new Object(), key2Obj4, 2, value2Obj);
            dict.TryAdd(key3Obj1, new Object(), key3Obj3, key3Obj4, 3, value3Obj);
            dict.TryAdd(new Object(), key4Obj2, key4Obj3, key4Obj4, 4, value4Obj);
            dict.TryAdd(key5Obj1, key5Obj2, key5Obj3, key5Obj4, 5, new Object());

            GC.Collect();

            Assert.AreEqual(1, dict.Count());
        }
    }
}
