using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConcurrentHashtable
{
    public struct WeakDictionaryStrongValuesItem<TValue>
    {
        internal Int32 _Hash;
        internal WeakReference _Key;
        internal TValue _Value;

        internal WeakDictionaryStrongValuesItem(Int32 hash, WeakReference key, TValue value )
        {
            _Hash = hash;
            _Key = key;
            _Value = value;
        }
    }

    public struct WeakDictionaryStrongValuesKey<TKey>
    {
        internal Int32 _Hash;
        internal TKey _Key;

        internal WeakDictionaryStrongValuesKey(Int32 hash, TKey key )
        {
            _Hash = hash;
            _Key = key;
        }
    }


    public class WeakDictionaryStrongValues<TKey,TValue> : Hashtable<WeakDictionaryStrongValuesItem<TValue>,WeakDictionaryStrongValuesKey<TKey>>
        where TKey : class
    {
        const int MinSegments = 16;
        const int SegmentFill = 16;

        #region Constructors

        public WeakDictionaryStrongValues()
            : this(EqualityComparer<TKey>.Default)
        { }

        public WeakDictionaryStrongValues(IEqualityComparer<TKey> comparer)
            : base(MinSegments)
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            _Comparer = comparer;

            Initialize();
        }

        #endregion

        #region Traits

        internal protected override int GetHashCode(ref WeakDictionaryStrongValuesItem<TValue> item)
        { return item._Hash; }

        internal protected override int GetHashCode(ref WeakDictionaryStrongValuesKey<TKey> key)
        { return key._Hash; }

        internal protected override bool Equals(ref WeakDictionaryStrongValuesItem<TValue> item, ref WeakDictionaryStrongValuesKey<TKey> key)
        {
            var key1 = (TKey)item._Key.Target;
            return _Comparer.Equals(key1, key._Key);
        }

        internal protected override bool Equals(ref WeakDictionaryStrongValuesItem<TValue> item1, ref WeakDictionaryStrongValuesItem<TValue> item2)
        {
            var key1 = (TKey)item1._Key.Target;
            var key2 = (TKey)item2._Key.Target;

            return key1 == null && key2 == null ? item1._Key == item2._Key : _Comparer.Equals(key1, key2);
        }

        internal protected override bool IsEmpty(ref WeakDictionaryStrongValuesItem<TValue> item)
        { return item._Key == null; }

        internal protected override bool IsGarbage(ref WeakDictionaryStrongValuesItem<TValue> item)
        { return item._Key != null && item._Key.Target == null; }

        internal protected override WeakDictionaryStrongValuesItem<TValue> EmptyItem
        { get { return default(WeakDictionaryStrongValuesItem<TValue>); } }

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

            var item = new WeakDictionaryStrongValuesItem<TValue>(GetHashCode(key), new WeakReference(key), value );
            WeakDictionaryStrongValuesItem<TValue> oldItem;
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

            var item = new WeakDictionaryStrongValuesItem<TValue>(GetHashCode(key), new WeakReference(key), newValue);
            WeakDictionaryStrongValuesItem<TValue> oldItem;

            base.GetOldestItem(ref item, out oldItem);

            return oldItem._Value;
        }

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="key"></param>
        public void Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var item = new WeakDictionaryStrongValuesKey<TKey>(GetHashCode(key), key);
            WeakDictionaryStrongValuesItem<TValue> oldItem;

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

            var item = new WeakDictionaryStrongValuesKey<TKey>(GetHashCode(key), key);
            WeakDictionaryStrongValuesItem<TValue> oldItem;

            if (base.FindItem(ref item, out oldItem))
            {
                value = oldItem._Value;
                return true;
            }

            value = default(TValue);
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

            var item = new WeakDictionaryStrongValuesKey<TKey>(GetHashCode(key), key);
            WeakDictionaryStrongValuesItem<TValue> oldItem;

            if (base.RemoveItem(ref item, out oldItem))
            {
                value = oldItem._Value;
                return true;
            }

            value = default(TValue);
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
                    .Select(item => item._Value)
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
                    .Select(item => (TKey)item._Key.Target )
                    .Where(key => comparer.Equals(key, null))
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
