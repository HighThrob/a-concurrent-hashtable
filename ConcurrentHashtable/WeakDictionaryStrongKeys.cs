using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConcurrentHashtable
{
    public struct WeakDictionaryStrongKeysItem<TKey>
    {
        internal Int32 _Hash;
        internal TKey _Key;
        internal WeakReference _Value;

        internal WeakDictionaryStrongKeysItem(Int32 hash, TKey key, WeakReference value )
        {
            _Hash = hash;
            _Key = key;
            _Value = value;
        }
    }

    public struct WeakDictionaryStrongKeysKey<TKey>
    {
        internal Int32 _Hash;
        internal TKey _Key;

        internal WeakDictionaryStrongKeysKey(Int32 hash, TKey key )
        {
            _Hash = hash;
            _Key = key;
        }
    }


    public class WeakDictionaryStrongKeys<TKey,TValue> : Hashtable<WeakDictionaryStrongKeysItem<TKey>,WeakDictionaryStrongKeysKey<TKey>>
        where TValue : class
    {
        const int MinSegments = 16;
        const int SegmentFill = 16;

        #region Constructors

        public WeakDictionaryStrongKeys()
            : this(EqualityComparer<TKey>.Default)
        { }

        public WeakDictionaryStrongKeys(IEqualityComparer<TKey> comparer)
            : base(MinSegments)
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            _Comparer = comparer;

            Initialize();
        }

        #endregion

        #region Traits

        internal protected override int GetHashCode(ref WeakDictionaryStrongKeysItem<TKey> item)
        { return item._Hash; }

        internal protected override int GetHashCode(ref WeakDictionaryStrongKeysKey<TKey> key)
        { return key._Hash; }

        internal protected override bool Equals(ref WeakDictionaryStrongKeysItem<TKey> item, ref WeakDictionaryStrongKeysKey<TKey> key)
        { return _Comparer.Equals(item._Key, key._Key); }

        internal protected override bool Equals(ref WeakDictionaryStrongKeysItem<TKey> item1, ref WeakDictionaryStrongKeysItem<TKey> item2)
        { return _Comparer.Equals(item1._Key, item2._Key); }

        internal protected override bool IsEmpty(ref WeakDictionaryStrongKeysItem<TKey> item)
        { return item._Value == null; }

        internal protected override bool IsGarbage(ref WeakDictionaryStrongKeysItem<TKey> item)
        { return item._Value != null && item._Value.Target == null; }

        internal protected override WeakDictionaryStrongKeysItem<TKey> EmptyItem
        { get { return default(WeakDictionaryStrongKeysItem<TKey>); } }

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
            if (value == null)
                throw new ArgumentNullException("value");

            var item = new WeakDictionaryStrongKeysItem<TKey>(GetHashCode(key), key, new WeakReference(value));
            WeakDictionaryStrongKeysItem<TKey> oldItem;
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
            if (newValue == null)
                throw new ArgumentNullException("newValue");

            var item = new WeakDictionaryStrongKeysItem<TKey>(GetHashCode(key), key, new WeakReference(newValue));
            WeakDictionaryStrongKeysItem<TKey> oldItem;
            TValue res;

            do
            {
                base.GetOldestItem(ref item, out oldItem);

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
            var item = new WeakDictionaryStrongKeysKey<TKey>(GetHashCode(key), key);
            WeakDictionaryStrongKeysItem<TKey> oldItem;

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
            var item = new WeakDictionaryStrongKeysKey<TKey>(GetHashCode(key), key);
            WeakDictionaryStrongKeysItem<TKey> oldItem;

            if (base.FindItem(ref item, out oldItem))
            {
                value = (TValue)oldItem._Value.Target;
                return value != null;
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
            var item = new WeakDictionaryStrongKeysKey<TKey>(GetHashCode(key), key);
            WeakDictionaryStrongKeysItem<TKey> oldItem;

            if (base.RemoveItem(ref item, out oldItem))
            {
                value = (TValue)oldItem._Value.Target;
                return value != null;
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
                    .Select(item => (TValue)item._Value.Target)
                    .Where(v => v != null)
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
                    .Select(item => item._Key)
                    .ToArray();
        }

        /// <summary>
        /// Clear
        /// </summary>
        public new void Clear()
        { base.Clear(); }

        #endregion

    }
}
