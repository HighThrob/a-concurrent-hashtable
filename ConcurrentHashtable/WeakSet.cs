using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConcurrentHashtable
{
    public sealed class WeakSet<TItem> : Hashtable<KeyValuePair<Int32, WeakReference>, KeyValuePair<Int32,TItem>> 
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

        internal protected override int GetHashCode(ref KeyValuePair<int, WeakReference> item)
        { return item.Key; }

        internal protected override int GetHashCode(ref KeyValuePair<int, TItem> key)
        { return key.Key; }

        internal protected override bool Equals(ref KeyValuePair<int, WeakReference> item, ref KeyValuePair<int, TItem> key)
        {
            var storedItemValue = (TItem)item.Value.Target;
            return storedItemValue != null && _Comparer.Equals(storedItemValue, key.Value);
        }

        internal protected override bool Equals(ref KeyValuePair<int, WeakReference> item1, ref KeyValuePair<int, WeakReference> item2)
        {
            var storedItemValue1 = (TItem)item1.Value.Target;
            var storedItemValue2 = (TItem)item2.Value.Target;

            return storedItemValue1 == null && storedItemValue2 == null ? object.ReferenceEquals(item1.Value, item2.Value) : _Comparer.Equals(storedItemValue1, storedItemValue2);
        }

        internal protected override bool IsEmpty(ref KeyValuePair<int, WeakReference> item)
        { return item.Value == null; }

        internal protected override bool IsGarbage(ref KeyValuePair<int, WeakReference> item)
        { return item.Value != null && item.Value.Target == null; }

        internal protected override KeyValuePair<int, WeakReference> EmptyItem
        { get { return default(KeyValuePair<int, WeakReference>); } }

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

        Int32 GetHashCode(TItem item)
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

            KeyValuePair<Int32, WeakReference> item = new KeyValuePair<int, WeakReference>(GetHashCode(value), new WeakReference(value)); 
            KeyValuePair<Int32, WeakReference> oldItem;

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

            KeyValuePair<Int32, TItem> item = new KeyValuePair<int, TItem>(GetHashCode(value), value);
            KeyValuePair<Int32, WeakReference> oldItem;

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

            KeyValuePair<Int32, TItem> item = new KeyValuePair<int, TItem>(GetHashCode(key), key);
            KeyValuePair<Int32, WeakReference> oldItem;

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

            KeyValuePair<Int32, TItem> item = new KeyValuePair<int, TItem>(GetHashCode(key), key);
            KeyValuePair<Int32, WeakReference> oldItem;

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

            KeyValuePair<Int32, WeakReference> item = new KeyValuePair<int, WeakReference>(GetHashCode(newValue), new WeakReference(newValue));
            KeyValuePair<Int32, WeakReference> oldItem;
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
