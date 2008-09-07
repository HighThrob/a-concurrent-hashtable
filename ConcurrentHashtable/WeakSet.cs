using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConcurrentHashtable
{
    public sealed class WeakSet<TItem> : Hashtable<KeyValuePair<UInt32, WeakReference>, KeyValuePair<UInt32, TItem>> 
        where TItem : class
    {
        const int MinSegments = 16;
        const int SegmentFill = 16;

        #region Constructors

        public WeakSet()
            : this(EqualityComparer<TItem>.Default)
        { }

        public WeakSet(IEqualityComparer<TItem> comparer)
            : base(MinSegments)
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            _Comparer = comparer;

            Initialize();
        }

        #endregion

        #region Traits

        internal protected override UInt32 GetHashCode(ref KeyValuePair<UInt32, WeakReference> item)
        { return item.Key; }

        internal protected override UInt32 GetHashCode(ref KeyValuePair<UInt32, TItem> key)
        { return key.Key; }

        internal protected override bool Equals(ref KeyValuePair<UInt32, WeakReference> item, ref KeyValuePair<UInt32, TItem> key)
        {
            var storedItemValue = (TItem)item.Value.Target;
            return storedItemValue != null && _Comparer.Equals(storedItemValue, key.Value);
        }

        internal protected override bool Equals(ref KeyValuePair<UInt32, WeakReference> item1, ref KeyValuePair<UInt32, WeakReference> item2)
        {
            var storedItemValue1 = (TItem)item1.Value.Target;
            var storedItemValue2 = (TItem)item2.Value.Target;

            return storedItemValue1 == null && storedItemValue2 == null ? object.ReferenceEquals(item1.Value, item2.Value) : _Comparer.Equals(storedItemValue1, storedItemValue2);
        }

        internal protected override bool IsEmpty(ref KeyValuePair<UInt32, WeakReference> item)
        { return item.Value == null; }

        internal protected override bool IsGarbage(ref KeyValuePair<UInt32, WeakReference> item)
        { return item.Value != null && item.Value.Target == null; }

        internal protected override KeyValuePair<UInt32, WeakReference> EmptyItem
        { get { return default(KeyValuePair<UInt32, WeakReference>); } }

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

        public readonly IEqualityComparer<TItem> _Comparer;

        UInt32 GetHashCode(TItem item)
        { return Hasher.Rehash(_Comparer.GetHashCode(item)); }

        #region Public accessors

        /// <summary>
        /// Add
        /// </summary>
        /// <param name="value"></param>
        public void Insert(TItem value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            var item = new KeyValuePair<UInt32, WeakReference>(GetHashCode(value), new WeakReference(value));
            KeyValuePair<UInt32, WeakReference> oldItem;

            base.InsertItem(ref item, out oldItem);
        }

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="value"></param>
        public void Remove(TItem value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            var item = new KeyValuePair<UInt32, TItem>(GetHashCode(value), value);
            KeyValuePair<UInt32, WeakReference> oldItem;

            base.RemoveItem(ref item, out oldItem);
        }

        /// <summary>
        /// TryGetValue
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(TItem key, out TItem value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var item = new KeyValuePair<UInt32, TItem>(GetHashCode(key), key);
            KeyValuePair<UInt32, WeakReference> oldItem;

            if (base.FindItem(ref item, out oldItem))
            {
                value = (TItem)oldItem.Value.Target;
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
        public bool TryPopValue(TItem key, out TItem value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var item = new KeyValuePair<UInt32, TItem>(GetHashCode(key), key);
            KeyValuePair<UInt32, WeakReference> oldItem;

            if (base.RemoveItem(ref item, out oldItem))
            {
                value = (TItem)oldItem.Value.Target;
                return value != null;
            }

            value = null;
            return false;
        }


        public TItem GetOldest(TItem newValue)
        {
            if (newValue == null)
                throw new ArgumentNullException("newValue");

            var item = new KeyValuePair<UInt32, WeakReference>(GetHashCode(newValue), new WeakReference(newValue));
            KeyValuePair<UInt32, WeakReference> oldItem;
            TItem res;

            do
            {
                base.GetOldestItem(ref item, out oldItem);
                res = (TItem)oldItem.Value.Target;
            }
            while (_Comparer.Equals(res,null));

            return res;
        }

        /// <summary>
        /// GetCurrentValues
        /// </summary>
        public TItem[] GetCurrentValues()
        {
            lock (SyncRoot)
                return
                    Items
                        .Select(kvp => (TItem)kvp.Value.Target)
                        .Where(tgt => !_Comparer.Equals(tgt, null))
                        .ToArray();
        }

        /// <summary>
        /// Clear
        /// </summary>
        public new void Clear()
        {
            base.Clear();
        }

        #endregion

    }
}
