using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConcurrentHashtable
{
    /// <summary>
    /// Entry item for ConcurrentWeakDictionary
    /// </summary>
    public struct ConcurrentWeakDictionaryItem
    {
        internal UInt32 _Hash;
        internal WeakReference _Key;
        internal WeakReference _Value;

        internal ConcurrentWeakDictionaryItem(UInt32 hash, WeakReference key, WeakReference value)
        {
            _Hash = hash;
            _Key = key;
            _Value = value;
        }
    }

    /// <summary>
    /// Search key for ConcurrentWeakDictionary
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public struct ConcurrentWeakDictionaryKey<TKey>
    {
        internal UInt32 _Hash;
        internal TKey _Key;

        internal ConcurrentWeakDictionaryKey(UInt32 hash, TKey key)
        {
            _Hash = hash;
            _Key = key;
        }
    }

    /// <summary>
    /// A dictionary that has weakreferences to it's keys and values. If either a key or its associated value gets garbage collected
    /// then the entry will be removed from the dictionary. 
    /// </summary>
    /// <typeparam name="TKey">Type of the keys. This must be a reference type.</typeparam>
    /// <typeparam name="TValue">Type of the values. This must be a reference type.</typeparam>
    public sealed class ConcurrentWeakDictionary<TKey, TValue> : ConcurrentWeakHashtable<ConcurrentWeakDictionaryItem, ConcurrentWeakDictionaryKey<TKey>>
        where TKey : class
        where TValue : class
    {
        #region Constructors

        /// <summary>
        /// Instantiates a ConcurrentWeakDictionary with the default comparer for <typeparamref name="TKey"/>.
        /// </summary>
        public ConcurrentWeakDictionary()
            : this(EqualityComparer<TKey>.Default)
        { }

        /// <summary>
        /// Instatiates a ConcurrentWeakDictionary with an explicit comparer for <typeparamref name="TKey"/>.
        /// </summary>
        /// <param name="comparer">An <see cref="IEqualityComparer{TKey}"/> to comparer keys.</param>
        public ConcurrentWeakDictionary(IEqualityComparer<TKey> comparer)
            : base()
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            _Comparer = comparer;

            Initialize();
        }

        #endregion

        #region Traits

        internal protected override UInt32 GetHashCode(ref ConcurrentWeakDictionaryItem item)
        { return item._Hash; }

        internal protected override UInt32 GetHashCode(ref ConcurrentWeakDictionaryKey<TKey> key)
        { return key._Hash; }

        internal protected override bool Equals(ref ConcurrentWeakDictionaryItem item, ref ConcurrentWeakDictionaryKey<TKey> key)
        {
            var key1 = (TKey)item._Key.Target;
            return _Comparer.Equals(key1, key._Key);
        }

        internal protected override bool Equals(ref ConcurrentWeakDictionaryItem item1, ref ConcurrentWeakDictionaryItem item2)
        {
            var key1 = (TKey)item1._Key.Target;
            var key2 = (TKey)item2._Key.Target;

            return key1 == null && key2 == null ? item1._Key == item2._Key : _Comparer.Equals(key1, key2);
        }

        internal protected override bool IsEmpty(ref ConcurrentWeakDictionaryItem item)
        { return item._Key == null; }

        internal protected override bool IsGarbage(ref ConcurrentWeakDictionaryItem item)
        { return item._Key != null && ( item._Key.Target == null || (item._Value != null && item._Value.Target == null) ); }

        internal protected override ConcurrentWeakDictionaryItem EmptyItem
        { get { return default(ConcurrentWeakDictionaryItem); } }


        #endregion

        public IEqualityComparer<TKey> _Comparer;

        UInt32 GetHashCode(TKey key)
        { return Hasher.Rehash(_Comparer.GetHashCode(key)); }

        #region Public accessors

        /// <summary>
        /// Add
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Insert(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var item = new ConcurrentWeakDictionaryItem(GetHashCode(key), new WeakReference(key), value == null ? null : new WeakReference(value));
            ConcurrentWeakDictionaryItem oldItem;
            base.InsertItem(ref item, out oldItem);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public TValue GetOldest(TKey key, TValue newValue)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var item = new ConcurrentWeakDictionaryItem(GetHashCode(key), new WeakReference(key), newValue == null ? null : new WeakReference(newValue));
            ConcurrentWeakDictionaryItem oldItem;
            TValue res;

            do
            {
                base.GetOldestItem(ref item, out oldItem);

                if (oldItem._Value == null)
                    return null;

                res = (TValue)oldItem._Value.Target;
            }
            while (res == null);

            return res;
        }

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="key"></param>
        public void Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var item = new ConcurrentWeakDictionaryKey<TKey>(GetHashCode(key), key);
            ConcurrentWeakDictionaryItem oldItem;

            base.RemoveItem(ref item, out oldItem);
        }

        /// <summary>
        /// TryGetValue
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var item = new ConcurrentWeakDictionaryKey<TKey>(GetHashCode(key), key);
            ConcurrentWeakDictionaryItem oldItem;

            if (base.FindItem(ref item, out oldItem))
            {
                if (oldItem._Value == null)
                {
                    value = null;
                    return true;
                }
                else
                {
                    value = (TValue)oldItem._Value.Target;
                    return value != null;
                }
            }

            value = null;
            return false;
        }


        /// <summary>
        /// TryPopValue
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryPopValue(TKey key, out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var item = new ConcurrentWeakDictionaryKey<TKey>(GetHashCode(key), key);
            ConcurrentWeakDictionaryItem oldItem;

            if (base.RemoveItem(ref item, out oldItem))
            {
                if (oldItem._Value == null)
                {
                    value = null;
                    return true;
                }
                else
                {
                    value = (TValue)oldItem._Value.Target;
                    return value != null;
                }
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Get value
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get
            {
                TValue res;
                TryGetValue(key, out res);
                return res;
            }
            set
            { Insert(key, value); }
        }

        /// <summary>
        /// GetCurrentValues
        /// </summary>
        public TValue[] GetCurrentValues()
        {
            lock (SyncRoot)
                return
                    Items
                        .Select(item => item._Value != null ? new KeyValuePair<bool, TValue>(true, (TValue)item._Value.Target) : new KeyValuePair<bool, TValue>(false, null))
                        .Where(kvp => !kvp.Key || kvp.Value != null)
                        .Select(kvp => kvp.Value)
                        .ToArray();
        }

        /// <summary>
        /// GetCurrentKeys
        /// </summary>
        public TKey[] GetCurrentKeys()
        {
            var comparer = _Comparer;
            lock (SyncRoot)
                return
                    Items
                        .Select(item => (TKey)item._Key.Target)
                        .Where(key => !comparer.Equals(key, null))
                        .ToArray();
        }

        /// <summary>
        /// Clear, remove all items
        /// </summary>
        public new void Clear()
        { base.Clear(); }

        #endregion
    }
}
