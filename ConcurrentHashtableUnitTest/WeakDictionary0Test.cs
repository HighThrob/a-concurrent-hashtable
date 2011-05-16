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
    using DTA = DictionaryTestAdapter<int, object, WeakDictionary<int, object>>;
    
    [TestClass]
    public class WeakDictionary0Test
    {
        class WeakDictionary0Adaper : IDictionaryTestAdapter<int,object,WeakDictionary<int,object>>
        {
            #region IDictionaryTestAdapter<int,object,WeakDictionary<int,object>> Members

            public int GetKey(int ix)
            { return ix; }

            public object GetValue(int ix)
            { return new Object(); }

            public WeakDictionary<int, object> GetDictionary()
            { return new WeakDictionary<int, object>(); }

            public bool ContainsKey(WeakDictionary<int, object> d, int k)
            { return d.ContainsKey(k); }

            public bool TryGetValue(WeakDictionary<int, object> d, int k, out object v)
            { return d.TryGetValue(k, out v ); }

            public object GetItem(WeakDictionary<int, object> d, int k)
            { return d[k]; }

            public void SetItem(WeakDictionary<int, object> d, int k, object v)
            { d[k] = v; }

            public bool IsEmpty(WeakDictionary<int, object> d)
            { return d.IsEmpty; }

            public object AddOrUpdate(WeakDictionary<int, object> d, int k, Func<int, object> addValueFactory, Func<int, object, object> updateValueFactory)
            {
                return 
                    d.AddOrUpdate(
                        k, 
                        (kp1) => addValueFactory( kp1 ), 
                        (kp1, v) => updateValueFactory( kp1, v ) 
                    )
                ;
            }

            public object AddOrUpdate(WeakDictionary<int, object> d, int k, object addValue, Func<int, object, object> updateValueFactory)
            {
                return
                    d.AddOrUpdate(
                        k,
                        addValue,
                        (kp1, v) => updateValueFactory(kp1, v)
                    )
                ;
            }

            public object GetOrAdd(WeakDictionary<int, object> d, int k, object v)
            { return d.GetOrAdd( k, v ); }

            public object GetOrAdd(WeakDictionary<int, object> d, int k, Func<int, object> valueFactory)
            { return d.GetOrAdd(k, (kp1) => valueFactory(kp1)); }

            public KeyValuePair<int, object>[] ToArray(WeakDictionary<int, object> d)
            { return d.ToArray(); }

            public bool TryAdd(WeakDictionary<int, object> d, int k, object v)
            { return d.TryAdd(k, v ); }

            public bool TryRemove(WeakDictionary<int, object> d, int k, out object v)
            { return d.TryRemove(k, out v); }

            public bool TryUpdate(WeakDictionary<int, object> d, int k, object newValue, object comparisonValue)
            { return d.TryUpdate(k, newValue, comparisonValue) ; }

            #endregion
        }

        DTA DTA = new DTA { _adapter = new WeakDictionary0Adaper() } ;

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

            object value1Obj = new Object();

            dict.TryAdd(1, value1Obj);
            dict.TryAdd(2, new Object());

            GC.Collect();

            Assert.AreEqual(1, dict.Count());
        }

    }
}
