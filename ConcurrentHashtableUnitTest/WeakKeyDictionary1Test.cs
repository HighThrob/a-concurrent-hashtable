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
    using DTA = DictionaryTestAdapter<Tuple<object, int>, object, WeakKeyDictionary<object, int, object>>;
    
    [TestClass]
    public class WeakKeyDictionary1Test
    {
        class WeakKeyDictionary1Adaper : IDictionaryTestAdapter<Tuple<object,int>,object,WeakKeyDictionary<object,int,object>>
        {
            #region IDictionaryTestAdapter<Tuple<object,int>,object,WeakKeyDictionary<object,int,object>> Members

            public Tuple<object, int> GetKey(int ix)
            { return Tuple.Create(new Object(), ix); }

            public object GetValue(int ix)
            { return new Object(); }

            public WeakKeyDictionary<object, int, object> GetDictionary()
            { return new WeakKeyDictionary<object, int, object>(); }

            public bool ContainsKey(WeakKeyDictionary<object, int, object> d, Tuple<object, int> k)
            { return d.ContainsKey(k.Item1, k.Item2); }

            public bool TryGetValue(WeakKeyDictionary<object, int, object> d, Tuple<object, int> k, out object v)
            { return d.TryGetValue( k.Item1, k.Item2, out v ); }

            public object GetItem(WeakKeyDictionary<object, int, object> d, Tuple<object, int> k)
            { return d[ k.Item1, k.Item2 ]; }

            public void SetItem(WeakKeyDictionary<object, int, object> d, Tuple<object, int> k, object v)
            { d[k.Item1, k.Item2] = v; }

            public bool IsEmpty(WeakKeyDictionary<object, int, object> d)
            { return d.IsEmpty; }

            public object AddOrUpdate(WeakKeyDictionary<object, int, object> d, Tuple<object, int> k, Func<Tuple<object, int>, object> addValueFactory, Func<Tuple<object, int>, object, object> updateValueFactory)
            {
                return 
                    d.AddOrUpdate(
                        k.Item1, 
                        k.Item2, 
                        (kp1, kp2) => addValueFactory( Tuple.Create(kp1, kp2) ), 
                        (kp1, kp2, v) => updateValueFactory( Tuple.Create(kp1 ,kp2), v ) 
                    )
                ;
            }

            public object AddOrUpdate(WeakKeyDictionary<object, int, object> d, Tuple<object, int> k, object addValue, Func<Tuple<object, int>, object, object> updateValueFactory)
            {
                return
                    d.AddOrUpdate(
                        k.Item1,
                        k.Item2,
                        addValue,
                        (kp1, kp2, v) => updateValueFactory(Tuple.Create(kp1, kp2), v)
                    )
                ;
            }

            public object GetOrAdd(WeakKeyDictionary<object, int, object> d, Tuple<object, int> k, object v)
            { return d.GetOrAdd( k.Item1, k.Item2, v ); }

            public object GetOrAdd(WeakKeyDictionary<object, int, object> d, Tuple<object, int> k, Func<Tuple<object, int>, object> valueFactory)
            { return d.GetOrAdd(k.Item1, k.Item2, (kp1, kp2) => valueFactory(Tuple.Create(kp1, kp2))); }

            public KeyValuePair<Tuple<object, int>, object>[] ToArray(WeakKeyDictionary<object, int, object> d)
            { return d.ToArray(); }

            public bool TryAdd(WeakKeyDictionary<object, int, object> d, Tuple<object, int> k, object v)
            { return d.TryAdd(k.Item1, k.Item2, v ); }

            public bool TryRemove(WeakKeyDictionary<object, int, object> d, Tuple<object, int> k, out object v)
            { return d.TryRemove(k.Item1, k.Item2, out v); }

            public bool TryUpdate(WeakKeyDictionary<object, int, object> d, Tuple<object, int> k, object newValue, object comparisonValue)
            { return d.TryUpdate(k.Item1, k.Item2, newValue, comparisonValue) ; }

            #endregion
        }

        DTA DTA = new DTA { _adapter = new WeakKeyDictionary1Adaper() } ;

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

            object key2Obj = new Object();
            object value1Obj = new Object();

            dict.TryAdd(new Object(), 1, value1Obj);
            dict.TryAdd(key2Obj, 2, new Object());

            GC.Collect();

            Assert.AreEqual(1, dict.Count());
        }

    }
}
