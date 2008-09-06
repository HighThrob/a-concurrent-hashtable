using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConcurrentHashtable
{
    public struct WeakDictionaryItem
    {
        internal Int32 _Hash;
        internal WeakReference _Key;
        internal WeakReference _Value;

        internal WeakDictionaryItem(Int32 hash, WeakReference key, WeakReference value )
        {
            _Hash = hash;
            _Key = key;
            _Value = value;
        }
    }

    public struct WeakDictionaryKey<TKey>
    {
        internal Int32 _Hash;
        internal TKey _Key;

        internal WeakDictionaryKey(Int32 hash, TKey key )
        {
            _Hash = hash;
            _Key = key;
        }
    }

    public class WeakDictionary<TKey, TValue> : Hashtable<WeakDictionaryItem, WeakDictionaryKey<TKey>>
        where TKey : class
        where TValue : class
    {
        const int MinSegments = 16;
        const int SegmentFill = 16;

        #region Constructors

        public WeakDictionary()
            : this(EqualityComparer<TKey>.Default)
        { }

        public WeakDictionary(IEqualityComparer<TKey> comparer)
            : base(MinSegments)
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            _Comparer = comparer;

            Initialize();
        }

        #endregion

        #region Traits

        internal protected override int GetHashCode(ref WeakDictionaryItem item)
        { return item._Hash; }

        internal protected override int GetHashCode(ref WeakDictionaryKey<TKey> key)
        { return key._Hash; }

        internal protected override bool Equals(ref WeakDictionaryItem item, ref WeakDictionaryKey<TKey> key)
        {
            var key1 = (TKey)item._Key.Target;
            return _Comparer.Equals(key1, key._Key);
        }

        internal protected override bool Equals(ref WeakDictionaryItem item1, ref WeakDictionaryItem item2)
        {
            var key1 = (TKey)item1._Key.Target;
            var key2 = (TKey)item2._Key.Target;

            return key1 == null && key2 == null ? item1._Key == item2._Key : _Comparer.Equals(key1, key2);
        }

        internal protected override bool IsEmpty(ref WeakDictionaryItem item)
        { return item._Key == null; }

        internal protected override bool IsGarbage(ref WeakDictionaryItem item)
        { return item._Key != null && ( item._Key.Target == null || (item._Value != null && item._Value.Target == null) ); }

        internal protected override WeakDictionaryItem EmptyItem
        { get { return default(WeakDictionaryItem); } }


        #endregion

        #region DetermineSegmentation

        int _CountHistory;

        protected override int DetermineSegmentation(int count)
        {
            if (count > _CountHistory)
                _CountHistory = count;
            else
                _CountHistory = count = _CountHistory / 2 + count / 2; //shrink more slowly

            return Math.Max(MinSegments, count / SegmentFill);
        }

        #endregion

        protected override void DoTableMaintenance()
        {
            base.DisposeGarbage();
            base.DoTableMaintenance();
        }

        public IEqualityComparer<TKey> _Comparer;

        Int32 GetHashCode(TKey key)
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

            var item = new WeakDictionaryItem(GetHashCode(key), new WeakReference(key), value == null ? null : new WeakReference(value));
            WeakDictionaryItem oldItem;
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

            var item = new WeakDictionaryItem(GetHashCode(key), new WeakReference(key), newValue == null ? null : new WeakReference(newValue));
            WeakDictionaryItem oldItem;
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

            var item = new WeakDictionaryKey<TKey>(GetHashCode(key), key);
            WeakDictionaryItem oldItem;

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

            var item = new WeakDictionaryKey<TKey>(GetHashCode(key), key);
            WeakDictionaryItem oldItem;

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

            var item = new WeakDictionaryKey<TKey>(GetHashCode(key), key);
            WeakDictionaryItem oldItem;

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
