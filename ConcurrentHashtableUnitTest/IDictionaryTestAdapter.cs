using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrentHashtableUnitTest
{
    interface IDictionaryTestAdapter<K,V,D>
    {
        K GetKey(int ix);
        V GetValue(int ix);
        D GetDictionary();

        bool ContainsKey(D d, K k);
        bool TryGetValue(D d, K k, out V v);
        V GetItem(D d, K k);
        void SetItem(D d, K k, V v);
        bool IsEmpty(D d);
        V AddOrUpdate(D d, K k, Func<K, V> addValueFactory, Func<K, V, V> updateValueFactory);
        V AddOrUpdate(D d, K k, V addValue, Func<K, V, V> updateValueFactory);
        V GetOrAdd(D d, K k, V v);
        V GetOrAdd(D d, K k, Func<K, V> valueFactory);
        KeyValuePair<K, V>[] ToArray(D d);
        bool TryAdd(D d, K k, V v);
        bool TryRemove(D d, K k, out V v);
        bool TryUpdate(D d, K k, V newValue, V comparisonValue);
    }

    class DictionaryTestAdapter<K,V,D>
    {
        public IDictionaryTestAdapter<K,V,D> _adapter;

        public void TestContainsKey()
        {
            var dictionary = _adapter.GetDictionary();

            K key = _adapter.GetKey(1);

            Assert.IsFalse(_adapter.ContainsKey(dictionary, key));

            V value = _adapter.GetValue(1);

            _adapter.TryAdd(dictionary, key, value);

            Assert.IsTrue(_adapter.ContainsKey(dictionary, key));

            _adapter.TryRemove(dictionary, key, out value);

            Assert.IsFalse(_adapter.ContainsKey(dictionary, key));
        }

        public void TestTryGetValue()
        {
            D dict = _adapter.GetDictionary();

            K key = _adapter.GetKey(1);
            V value;

            Assert.IsFalse(_adapter.TryGetValue(dict, key, out value));

            V insertedValue = _adapter.GetValue(1);

            _adapter.TryAdd(dict, key, insertedValue);

            Assert.IsTrue(_adapter.TryGetValue(dict, key, out value));

            Assert.AreEqual(insertedValue, value);
        }

        public void TestGetItem()
        {
            D dict = _adapter.GetDictionary();

            K key = _adapter.GetKey(1);

            try
            {
                _adapter.GetItem(dict, key);
                Assert.Fail();
            }
            catch (KeyNotFoundException)
            {}

            V insertedValue = _adapter.GetValue(1);

            _adapter.TryAdd(dict, key, insertedValue);

            Assert.AreEqual(insertedValue, _adapter.GetItem(dict, key));
        }

        public void TestSetItem()
        {
            D dict = _adapter.GetDictionary();

            K key = _adapter.GetKey(1);
            V value = _adapter.GetValue(1);

            _adapter.SetItem(dict, key, value);

            Assert.AreEqual(value, _adapter.GetItem(dict, key));

            V value2 = _adapter.GetValue(2);

            _adapter.SetItem(dict, key, value2);

            Assert.AreEqual(value2, _adapter.GetItem(dict, key));
        }

        public void TestIsEmpty()
        {
            var dict = _adapter.GetDictionary();

            Assert.IsTrue(_adapter.IsEmpty(dict));

            K key = _adapter.GetKey(1);
            V value = _adapter.GetValue(1);

            _adapter.SetItem(dict, key, value);

            Assert.IsFalse(_adapter.IsEmpty(dict));

            _adapter.TryRemove(dict, key, out value);

            Assert.IsTrue(_adapter.IsEmpty(dict));
        }

        public void TestAddOrUpdate1()
        {
            var dict = _adapter.GetDictionary();

            K key = _adapter.GetKey(1);
            V value1 = _adapter.GetValue(1);
            V value2 = _adapter.GetValue(2);

            Assert.AreEqual(value1, _adapter.AddOrUpdate(dict, key, k => value1, (k, v) => value2));
            Assert.AreEqual(value1, _adapter.GetItem(dict, key));

            Assert.AreEqual(value2, _adapter.AddOrUpdate(dict, key, k => value1, (k, v) => value2));
            Assert.AreEqual(value2, _adapter.GetItem(dict, key));
        }

        public void TestAddOrUpdate2()
        {
            var dict = _adapter.GetDictionary();

            K key = _adapter.GetKey(1);
            V value1 = _adapter.GetValue(1);
            V value2 = _adapter.GetValue(2);

            Assert.AreEqual(value1, _adapter.AddOrUpdate(dict, key, value1, (k, v) => value2));
            Assert.AreEqual(value1, _adapter.GetItem(dict, key));

            Assert.AreEqual(value2, _adapter.AddOrUpdate(dict, key, value1, (k, v) => value2));
            Assert.AreEqual(value2, _adapter.GetItem(dict, key));
        }

        public void TestGetOrAdd1()
        {
            var dict = _adapter.GetDictionary();

            K key = _adapter.GetKey(1);
            V value1 = _adapter.GetValue(1);
            V value2 = _adapter.GetValue(2);

            Assert.AreEqual(value1, _adapter.GetOrAdd(dict, key, k => value1));
            Assert.AreEqual(value1, _adapter.GetOrAdd(dict, key, k => value2));
        }

        public void TestGetOrAdd2()
        {
            var dict = _adapter.GetDictionary();

            K key = _adapter.GetKey(1);
            V value1 = _adapter.GetValue(1);
            V value2 = _adapter.GetValue(2);

            Assert.AreEqual(value1, _adapter.GetOrAdd(dict, key, value1));
            Assert.AreEqual(value1, _adapter.GetOrAdd(dict, key, value2));
        }

        public void TestToArray()
        {
            var dict = _adapter.GetDictionary();

            var array = _adapter.ToArray(dict);

            Assert.AreEqual(0, array.Length);

            K key1 = _adapter.GetKey(1);
            K key2 = _adapter.GetKey(2);
            K key3 = _adapter.GetKey(3);

            V value1 = _adapter.GetValue(1);
            V value2 = _adapter.GetValue(2);
            V value3 = _adapter.GetValue(3);

            _adapter.TryAdd(dict, key1, value1);
            _adapter.TryAdd(dict, key2, value2);
            _adapter.TryAdd(dict, key3, value3);

            array = _adapter.ToArray(dict);

            Assert.AreEqual(3, array.Length);

            Assert.IsTrue(array.Contains(new KeyValuePair<K, V>(key1, value1)));
            Assert.IsTrue(array.Contains(new KeyValuePair<K, V>(key2, value2)));
            Assert.IsTrue(array.Contains(new KeyValuePair<K, V>(key3, value3)));
        }

        public void TestTryAdd()
        {
            var dict = _adapter.GetDictionary();
            K key = _adapter.GetKey(1);
            V value1 = _adapter.GetValue(1);
            V value2 = _adapter.GetValue(2);

            Assert.IsTrue(_adapter.TryAdd(dict, key, value1));
            Assert.IsFalse(_adapter.TryAdd(dict, key, value2));
            Assert.IsTrue(_adapter.TryGetValue(dict, key, out value2));
            Assert.AreEqual(value1, value2);
        }

        public void TestTryRemove()
        {
            var dict = _adapter.GetDictionary();

            K key = _adapter.GetKey(1);
            V value ;

            Assert.IsFalse(_adapter.TryRemove(dict, key, out value));

            value = _adapter.GetValue(1);

            _adapter.SetItem(dict, key, value);

            V value2;

            Assert.IsTrue(_adapter.TryRemove(dict, key, out value2));

            Assert.AreEqual(value, value2);

            Assert.IsTrue(_adapter.IsEmpty(dict));

            Assert.IsFalse(_adapter.TryRemove(dict, key, out value2));
        }

        public void TestTryUpdate()
        {
            var dict = _adapter.GetDictionary();


            K key = _adapter.GetKey(1);
            V value1 = _adapter.GetValue(1);
            V value2 = _adapter.GetValue(2);
            V value3 = _adapter.GetValue(2);

            Assert.IsFalse(_adapter.TryUpdate(dict, key, value2, value1));

            _adapter.SetItem(dict, key, value1);

            Assert.IsTrue(_adapter.TryUpdate(dict, key, value2, value1));

            Assert.AreEqual(value2, _adapter.GetItem(dict, key));

            Assert.IsFalse(_adapter.TryUpdate(dict, key, value3, value1));

            Assert.AreEqual(value2, _adapter.GetItem(dict, key));
        }
    }
}
