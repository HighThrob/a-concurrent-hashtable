using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TvdP.Collections
{
    struct KeyStruct<TKey>
    {
        public TKey _Key;
        public uint _Hash;
    }
    class Slot<TKey>
    {
        public object _Item;
        public KeyStruct<TKey> _Key;
        public int _GC2WhenLastUsed;
    }

    sealed class Level1CacheClass<TKey> : ConcurrentWeakHashtable<Slot<TKey>, TKey>
    {
        public Level1CacheClass(IEqualityComparer<TKey> keyComparer)
        {
            _KeyComparer = keyComparer;
            _GC2Count = GC.CollectionCount(2);
            base.Initialize();
        }

        int _GC2Count;
        IEqualityComparer<TKey> _KeyComparer;

        public override bool DoMaintenance()
        {
            _GC2Count = GC.CollectionCount(2);
            return base.DoMaintenance();
        }

        protected internal override bool IsGarbage(ref Slot<TKey> item)
        {
            unchecked
            {
                return item != null && item._GC2WhenLastUsed - _GC2Count < -1;
            }
        }

        protected internal override uint GetItemHashCode(ref Slot<TKey> item)
        {
            return item._Key._Hash;
        }

        protected internal override uint GetKeyHashCode(ref TKey key)
        {
            return Hasher.Rehash(_KeyComparer.GetHashCode(key));
        }

        protected internal override bool ItemEqualsKey(ref Slot<TKey> item, ref TKey key)
        {
            return _KeyComparer.Equals(item._Key._Key, key);
        }

        protected internal override bool ItemEqualsItem(ref Slot<TKey> item1, ref Slot<TKey> item2)
        {
            return _KeyComparer.Equals(item1._Key._Key, item2._Key._Key);
        }

        protected internal override bool IsEmpty(ref Slot<TKey> item)
        {
            return item == null;
        }

        public bool TryGetItem(TKey key, out object item)
        {
            Slot<TKey> slot;

            if (base.FindItem(ref key, out slot))
            {
                item = slot._Item;
                slot._GC2WhenLastUsed = _GC2Count;
                return true;
            }

            item = null;
            return false;
        }

        public bool GetOldestItem(TKey key, ref object item)
        {
            Slot<TKey> searchSlot = new Slot<TKey> { _Item = item, _GC2WhenLastUsed = _GC2Count, _Key = new KeyStruct<TKey> { _Key = key, _Hash = GetKeyHashCode(ref key) } };
            Slot<TKey> foundSlot;

            bool res = base.GetOldestItem(ref searchSlot, out foundSlot);

            foundSlot._GC2WhenLastUsed = _GC2Count;

            item = foundSlot._Item;

            return res;
        }
    }

    class ObjectComparerClass<TKey> : IEqualityComparer<object>
    {
        public IEqualityComparer<TKey> _KeyComparer;

        #region IEqualityComparer<object> Members

        public bool Equals(object x, object y)
        { return _KeyComparer.Equals(((TKey)x), ((TKey)y)); }

        public int GetHashCode(object obj)
        { return _KeyComparer.GetHashCode(((TKey)obj)); }

        #endregion
    }

    /// <summary>
    /// Cache; Retains values longer than WeakDictionary
    /// </summary>
    /// <typeparam name="TKey">Type of key</typeparam>
    /// <typeparam name="TValue">Type of value</typeparam>
    /// <remarks>
    /// Use only for expensive values.
    /// </remarks>
    public sealed class Cache<TKey, TValue>
    {      
        Level1CacheClass<TKey> _Level1Cache;
        ConcurrentWeakDictionary<object, object> _Level2Cache;

        public Cache(IEqualityComparer<TKey> keyComparer)
        {
            _Level1Cache = new Level1CacheClass<TKey>(keyComparer);
            _Level2Cache = new ConcurrentWeakDictionary<object, object>(new ObjectComparerClass<TKey> { _KeyComparer = keyComparer });
        }

        public Cache()
            : this(EqualityComparer<TKey>.Default)
        { }

        public bool TryGetItem(TKey key, out TValue item)
        {
            object storedItem;

            bool found = _Level1Cache.TryGetItem(key, out storedItem);

            if (!found && _Level2Cache.TryGetValue((object)key, out storedItem))
            {
                found = true;
                _Level1Cache.GetOldestItem(key, ref storedItem);
            }

            if (found)
            {
                item = (TValue)storedItem;
                return true;
            }

            item = default(TValue);
            return false;
        }

        public TValue GetOldest(TKey key, TValue newItem)
        {
            object item = newItem;

            if (!_Level1Cache.GetOldestItem(key, ref item)) //return false.. no existing item was found
            {
                _Level2Cache.Insert((object)key, item);
                return (TValue)item;
            }

            return newItem;
        }
    }
}
