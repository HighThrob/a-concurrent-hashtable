﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConcurrentHashtable
{
    public struct DictionaryKey<TKey, TValue>
    {
        internal TKey _Key;
        internal TValue _Value;
        internal bool _IgnoreValue;

        internal DictionaryKey(TKey key)
        {
            _Key = key;
            _IgnoreValue = true;
            _Value = default(TValue);
        }

        internal DictionaryKey(TKey key, TValue value)
        {
            _Key = key;
            _IgnoreValue = false ;
            _Value = value;
        }
    }
    /// <summary>
    /// A Concurrent <see cref="IDictionary{TKey,TValue}"/> implementation.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <remarks>
    /// This class is threadsafe and highly concurrent. This means that multiple threads can do lookup and insert operations
    /// on this dictionary simultaneously. 
    /// It is not guaranteed that collisions will not occur. The dictionary is partitioned is segments. A segments contains
    /// a set of items based on a hash of those items. The more segments there are and the beter the hash, the fewer collisions will occur.
    /// This means that a nearly empty Dictionary is not as concurrent as one containing many items. 
    /// </remarks>
    public sealed class Dictionary<TKey, TValue> : Hashtable<KeyValuePair<TKey, TValue>?, DictionaryKey<TKey, TValue>>, IDictionary<TKey, TValue> 
    {
        #region Constructors

        public Dictionary()
            : this(EqualityComparer<TKey>.Default)
        { }

        public Dictionary(IEqualityComparer<TKey> comparer)
            : base()
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            _Comparer = comparer;

            Initialize();
        }

        #endregion

        #region Traits

        readonly IEqualityComparer<TKey> _Comparer;

        internal protected override UInt32 GetHashCode(ref KeyValuePair<TKey, TValue>? item)
        { return item.HasValue ? Hasher.Rehash(_Comparer.GetHashCode(item.Value.Key)) : 0; }

        internal protected override UInt32 GetHashCode(ref DictionaryKey<TKey, TValue> key)
        { return Hasher.Rehash(_Comparer.GetHashCode(key._Key)); }

        internal protected override bool Equals(ref KeyValuePair<TKey, TValue>? item, ref DictionaryKey<TKey, TValue> key)
        { return item.HasValue && _Comparer.Equals(item.Value.Key, key._Key) && (key._IgnoreValue || EqualityComparer<TValue>.Default.Equals(item.Value.Value, key._Value)); }

        internal protected override bool Equals(ref KeyValuePair<TKey, TValue>? item1, ref KeyValuePair<TKey, TValue>? item2)
        { return item1.HasValue && item2.HasValue && _Comparer.Equals(item1.Value.Key, item2.Value.Key); }

        internal protected override bool IsEmpty(ref KeyValuePair<TKey, TValue>? item)
        { return !item.HasValue; }

        internal protected override KeyValuePair<TKey, TValue>? EmptyItem
        { get { return null; } }


        #endregion

        #region IDictionary<TKey,TValue> Members

        public void Add(TKey key, TValue value)
        { Add( new KeyValuePair<TKey,TValue>(key,value) ); }

        public bool ContainsKey(TKey key)
        {
            KeyValuePair<TKey,TValue>? presentItem;
            DictionaryKey<TKey, TValue> searchKey = new DictionaryKey<TKey, TValue>(key);
            return FindItem(ref searchKey, out presentItem);
        }

        public ICollection<TKey> Keys
        {
            get 
            {
                lock (SyncRoot)
                    return base.Items.Select(kvp => kvp.Value.Key).ToList();
            }
        }

        public bool Remove(TKey key)
        {
            KeyValuePair<TKey, TValue>? oldItem;
            DictionaryKey<TKey,TValue> searchKey = new DictionaryKey<TKey,TValue>(key);
            return base.RemoveItem(ref searchKey, out oldItem);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            KeyValuePair<TKey, TValue>? presentItem;
            DictionaryKey<TKey, TValue> searchKey = new DictionaryKey<TKey, TValue>(key);

            var res = FindItem(ref searchKey, out presentItem);

            if (res)
            {
                value = presentItem.Value.Value;
                return true;
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }

        public ICollection<TValue> Values
        {
            get 
            {
                lock (SyncRoot)
                    return base.Items.Select(kvp => kvp.Value.Value).ToList();
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                KeyValuePair<TKey, TValue>? presentItem;
                DictionaryKey<TKey, TValue> searchKey = new DictionaryKey<TKey, TValue>(key);

                if (!FindItem(ref searchKey, out presentItem))
                    throw new KeyNotFoundException("The property is retrieved and key is not found.");
                return presentItem.Value.Value;
            }
            set
            {
                KeyValuePair<TKey, TValue>? newItem = new KeyValuePair<TKey, TValue>(key, value);
                KeyValuePair<TKey, TValue>? presentItem;
                InsertItem(ref newItem, out presentItem);
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            KeyValuePair<TKey, TValue>? newItem = item;
            KeyValuePair<TKey, TValue>? presentItem;

            if (GetOldestItem(ref newItem, out presentItem))
                throw new ArgumentException("An element with the same key already exists.");
        }

        public new void Clear()
        { base.Clear(); }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            KeyValuePair<TKey, TValue>? presentItem;
            DictionaryKey<TKey, TValue> searchKey = new DictionaryKey<TKey, TValue>(item.Key,item.Value);

            return
                FindItem(ref searchKey, out presentItem);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            lock (SyncRoot)
                foreach (var item in Items)
                    array[arrayIndex++] = item.Value;
        }

        public new int Count
        { get { return base.Count; } }

        public bool IsReadOnly
        { get { return false; } }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            KeyValuePair<TKey, TValue>? oldItem;
            DictionaryKey<TKey, TValue> searchKey = new DictionaryKey<TKey, TValue>(item.Key,item.Value);
            return base.RemoveItem(ref searchKey, out oldItem);
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            lock (SyncRoot)
                return Items.Select(nkvp => nkvp.Value).ToList().GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return GetEnumerator(); }

        #endregion
    }
}