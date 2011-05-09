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
    using DTA = DictionaryTestAdapter<Tuple<object, object, object, int>, object, WeakKeyDictionary<object, object, object, int, object>>;
    
    [TestClass]
    public class WeakKeyDictionary3Test
    {
        class WeakKeyDictionary3Adaper : IDictionaryTestAdapter<Tuple<object, object, object,int>,object,WeakKeyDictionary<object, object, object,int,object>>
        {
            #region IDictionaryTestAdapter<Tuple<object, object, object,int>,object,WeakKeyDictionary<object, object, object,int,object>> Members

            public Tuple<object, object, object, int> GetKey(int ix)
            { return Tuple.Create(new Object(), new Object(), new Object(), ix); }

            public object GetValue(int ix)
            { return new Object(); }

            public WeakKeyDictionary<object, object, object, int, object> GetDictionary()
            { return new WeakKeyDictionary<object, object, object, int, object>(); }

            public bool ContainsKey(WeakKeyDictionary<object, object, object, int, object> d, Tuple<object, object, object, int> k)
            { return d.ContainsKey(k.Item1, k.Item2, k.Item3, k.Item4); }

            public bool TryGetValue(WeakKeyDictionary<object, object, object, int, object> d, Tuple<object, object, object, int> k, out object v)
            { return d.TryGetValue( k.Item1, k.Item2, k.Item3, k.Item4, out v ); }

            public object GetItem(WeakKeyDictionary<object, object, object, int, object> d, Tuple<object, object, object, int> k)
            { return d[ k.Item1, k.Item2, k.Item3, k.Item4 ]; }

            public void SetItem(WeakKeyDictionary<object, object, object, int, object> d, Tuple<object, object, object, int> k, object v)
            { d[k.Item1, k.Item2, k.Item3, k.Item4] = v; }

            public bool IsEmpty(WeakKeyDictionary<object, object, object, int, object> d)
            { return d.IsEmpty; }

            public object AddOrUpdate(WeakKeyDictionary<object, object, object, int, object> d, Tuple<object, object, object, int> k, Func<Tuple<object, object, object, int>, object> addValueFactory, Func<Tuple<object, object, object, int>, object, object> updateValueFactory)
            {
                return 
                    d.AddOrUpdate(
                        k.Item1, 
                        k.Item2,
                        k.Item3,
                        k.Item4,
                        (kp1, kp2, kp3, kp4) => addValueFactory(Tuple.Create(kp1, kp2, kp3, kp4)), 
                        (kp1, kp2, kp3, kp4, v) => updateValueFactory( Tuple.Create(kp1 ,kp2, kp3, kp4), v ) 
                    )
                ;
            }

            public object AddOrUpdate(WeakKeyDictionary<object, object, object, int, object> d, Tuple<object, object, object, int> k, object addValue, Func<Tuple<object, object, object, int>, object, object> updateValueFactory)
            {
                return
                    d.AddOrUpdate(
                        k.Item1,
                        k.Item2,
                        k.Item3,
                        k.Item4,
                        addValue,
                        (kp1, kp2, kp3, kp4, v) => updateValueFactory(Tuple.Create(kp1, kp2, kp3, kp4), v)
                    )
                ;
            }

            public object GetOrAdd(WeakKeyDictionary<object, object, object, int, object> d, Tuple<object, object, object, int> k, object v)
            { return d.GetOrAdd( k.Item1, k.Item2, k.Item3, k.Item4, v ); }

            public object GetOrAdd(WeakKeyDictionary<object, object, object, int, object> d, Tuple<object, object, object, int> k, Func<Tuple<object, object, object, int>, object> valueFactory)
            { return d.GetOrAdd(k.Item1, k.Item2, k.Item3, k.Item4, (kp1, kp2, kp3, kp4) => valueFactory(Tuple.Create(kp1, kp2, kp3, kp4))); }

            public KeyValuePair<Tuple<object, object, object, int>, object>[] ToArray(WeakKeyDictionary<object, object, object, int, object> d)
            { return d.ToArray(); }

            public bool TryAdd(WeakKeyDictionary<object, object, object, int, object> d, Tuple<object, object, object, int> k, object v)
            { return d.TryAdd(k.Item1, k.Item2, k.Item3, k.Item4, v ); }

            public bool TryRemove(WeakKeyDictionary<object, object, object, int, object> d, Tuple<object, object, object, int> k, out object v)
            { return d.TryRemove(k.Item1, k.Item2, k.Item3, k.Item4, out v); }

            public bool TryUpdate(WeakKeyDictionary<object, object, object, int, object> d, Tuple<object, object, object, int> k, object newValue, object comparisonValue)
            { return d.TryUpdate(k.Item1, k.Item2, k.Item3, k.Item4, newValue, comparisonValue) ; }

            #endregion
        }

        DTA DTA = new DTA { _adapter = new WeakKeyDictionary3Adaper() } ;

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
            object value1Obj = new Object();

            object key2Obj1 = new Object();
            object key2Obj3 = new Object();
            object value2Obj = new Object();

            object key3Obj2 = new Object();
            object key3Obj3 = new Object();
            object value3Obj = new Object();

            object key4Obj1 = new Object();
            object key4Obj2 = new Object();
            object key4Obj3 = new Object();

            dict.TryAdd(key1Obj1, key1Obj2, new Object(), 1, value1Obj);
            dict.TryAdd(key2Obj1, new Object(), key2Obj3, 2, value2Obj);
            dict.TryAdd(new Object(), key3Obj2, key3Obj3, 3, value3Obj);
            dict.TryAdd(key4Obj1, key4Obj2, key4Obj3, 4, new Object());

            GC.Collect();

            Assert.AreEqual(1, dict.Count());
        }
    }
}
