using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace ConcurrentHashtable
{
    /// <summary>
    /// A singlethreaded segment in a hashtable. 
    /// </summary>
    /// <typeparam name="TStored"></typeparam>
    /// <typeparam name="TSearch"></typeparam>
    /// <remarks>
    /// Though each segment can be accessed
    /// by 1 thread simultaneously, the hashtable becomes concurrent by containing many segments so that collisions
    /// are rare.
    /// Each segment is itself a small hashtable that can grow and shrink individualy. This prevents blocking of
    /// the intire hashtable when growing or shrinking is needed. Becuase each segment is relatively small (depending on
    /// the quality of the hash) resizing of the individual segments will not take much time.
    /// </remarks>
    internal class Segment<TStored,TSearch>
    {
        protected Segment( )
        {}

        public static Segment<TStored, TSearch> Create(Int32 initialSize)
        {
            var instance = new Segment<TStored, TSearch>();
            instance.Initialize(initialSize);
            return instance;
        }

        protected virtual void Initialize(Int32 initialSize)
        {
            _List = new TStored[Math.Max(4, initialSize)];
        }

        Int32 _Token;
        Int32 _Count;

        public Int32 Count
        { get { return _Count; } }

        internal TStored[] _List;

        public void Welcome(Hashtable<TStored, TSearch> traits)
        { traits.EffectTotalAllocatedSpace(_List.Length); }

        public void Bye(Hashtable<TStored, TSearch> traits)
        { traits.EffectTotalAllocatedSpace(-_List.Length); }

        public bool FindItem(ref TSearch key, out TStored item, Hashtable<TStored, TSearch> traits)
        {
            var searchHash = traits.GetHashCode(ref key);
            var mask = (UInt32)(_List.Length - 1);
            var i = searchHash & mask;

            if (traits.IsEmpty(ref _List[i]))
            {
                item = traits.EmptyItem;
                return false;
            }

            var firstHash = traits.GetHashCode(ref _List[i]);
            var storedItemHash = firstHash ;
            var searchHashDiff = (searchHash - firstHash) & mask;

            while (true)
            {
                if (storedItemHash == searchHash && traits.Equals(ref _List[i], ref key))
                {
                    item = _List[i];
                    return true;
                }

                i = (i + 1) & mask;

                if (
                    traits.IsEmpty(ref _List[i]) 
                    || (((storedItemHash = traits.GetHashCode(ref _List[i])) - firstHash) & mask) > searchHashDiff)
                {
                    item = traits.EmptyItem;
                    return false;
                }
            }
        }

        public bool GetOldestItem(ref TStored key, out TStored item, Hashtable<TStored, TSearch> traits)
        {
            var searchHash = traits.GetHashCode(ref key);
            var mask = (UInt32)(_List.Length - 1);
            var i = searchHash & mask;

            if (traits.IsEmpty(ref _List[i]))
                goto empty_spot;

            var firstHash = traits.GetHashCode(ref _List[i]);
            var storedItemHash = firstHash ;
            var searchHashDiff = (searchHash - firstHash) & mask;

            while (true)
            {
                if (storedItemHash == searchHash && traits.Equals(ref _List[i], ref key))
                {
                    item = _List[i];
                    return true;
                }

                i = (i + 1) & mask;

                if (traits.IsEmpty(ref _List[i]))
                    goto empty_spot;

                storedItemHash = traits.GetHashCode(ref _List[i]);

                if (((storedItemHash - firstHash) & mask) > searchHashDiff)
                {
                    //insert
                    InsertItemAtIndex(mask, i, key, traits);
                    IncrementCount(traits);
                    item = key;
                    return false;
                }
            }

        empty_spot:
            item = _List[i] = key;
            IncrementCount(traits);
            return false;
        }

        public bool InsertItem(ref TStored key, out TStored item, Hashtable<TStored, TSearch> traits)
        {
            var searchHash = traits.GetHashCode(ref key);
            var mask = (UInt32)(_List.Length - 1);
            var i = searchHash & mask;

            if (traits.IsEmpty(ref _List[i]))
                goto empty_spot;

            var firstHash = traits.GetHashCode(ref _List[i]);
            var storedItemHash = firstHash ;
            var searchHashDiff = (searchHash - firstHash) & mask;

            while (true)
            {
                if (storedItemHash == searchHash && traits.Equals(ref _List[i], ref key))
                {
                    item = _List[i];
                    _List[i] = key;
                    return true;
                }

                i = (i + 1) & mask;

                if (traits.IsEmpty(ref _List[i]))
                    goto empty_spot;

                storedItemHash = traits.GetHashCode(ref _List[i]);

                if (((storedItemHash - firstHash) & mask) > searchHashDiff)
                {
                    //insert                   
                    InsertItemAtIndex(mask, i, key, traits);
                    IncrementCount(traits);
                    item = key;
                    return false;
                }
            }

        empty_spot:

            item = _List[i] = key;
            IncrementCount(traits);
            return false;
        }

        public bool RemoveItem(ref TSearch key, out TStored item, Hashtable<TStored, TSearch> traits)
        {
            var searchHash = traits.GetHashCode(ref key);
            var mask = (UInt32)(_List.Length - 1);
            var i = searchHash & mask;

            if (traits.IsEmpty(ref _List[i]))
            {
                item = traits.EmptyItem;
                return false;
            }

            var firstHash = traits.GetHashCode(ref _List[i]);
            var storedItemHash = firstHash ;
            var searchHashDiff = (searchHash - firstHash) & mask;

            while (true)
            {
                if (storedItemHash == searchHash && traits.Equals(ref _List[i], ref key))
                {
                    item = _List[i];
                    RemoveAtIndex(i, traits);
                    DecrementCount(traits);
                    return true;
                }

                i = (i + 1) & mask;

                if (traits.IsEmpty(ref _List[i]))
                {
                    item = traits.EmptyItem;
                    return false;
                }

                storedItemHash = traits.GetHashCode(ref _List[i]);

                if (((storedItemHash - firstHash) & mask) > searchHashDiff)
                {
                    item = traits.EmptyItem;
                    return false;
                }
            }
        }

        public int GetNextItem(int beyond, out TStored item, Hashtable<TStored, TSearch> traits)
        {
            for (int end = _List.Length; ++beyond < end;)
            {
                if (!traits.IsEmpty(ref _List[beyond]))
                {
                    item = _List[beyond];
                    return beyond;
                }
            }

            item = traits.EmptyItem;
            return -1;
        }

        public void Clear(Hashtable<TStored, TSearch> traits)
        {
            _List = new TStored[4];
            _Count = 0;
        }

        public bool Lock()
        {
            return Interlocked.CompareExchange(ref _Token, 1, 0) == 0;
        }

        public void Unlock()
        {
            Interlocked.Exchange(ref _Token, 0);
        }

        protected void RemoveAtIndex(UInt32 index, Hashtable<TStored, TSearch> traits)
        {
            var mask = (UInt32)(_List.Length - 1);
            var i = index;
            var j = (index + 1) & mask;

            while(true)
            {
                if (traits.IsEmpty(ref _List[j]) || (traits.GetHashCode(ref _List[j]) & mask) == j)
                {
                    _List[i] = traits.EmptyItem;                    
                    break;
                }

                _List[i] = _List[j];

                i = j;
                j = (j + 1) & mask;            
            }            
        }

        protected void DecrementCount(Hashtable<TStored, TSearch> traits)
        { DecrementCount(traits, 1); }

        Int32 GetPreferedListLength()
        {
            var newListLength = 2;

            while (newListLength < _Count)
                newListLength <<= 1;

            return newListLength << 1;
        }

        protected void DecrementCount(Hashtable<TStored, TSearch> traits, int amount)
        {
            var oldListLength = _List.Length;
            _Count -= amount;

            if (oldListLength > 4 && _Count < (oldListLength >> 2))
            {
                //Shrink
                var oldList = _List;
                var newListLength = GetPreferedListLength();

                _List = new TStored[newListLength];

                for (int i = 0; i != oldListLength; ++i)
                    if (!traits.IsEmpty(ref oldList[i]))
                        DirectInsert(ref oldList[i], traits);

                traits.EffectTotalAllocatedSpace(newListLength - oldListLength);
            }
        }

        private void IncrementCount(Hashtable<TStored, TSearch> traits)
        {
            var oldListLength = _List.Length;

            if (++_Count >= (oldListLength - (oldListLength >> 2)))
            {
                //Grow
                var oldList = _List;
                var newListLength = GetPreferedListLength();

                _List = new TStored[newListLength];

                for (int i = 0; i != oldListLength; ++i)
                    if (!traits.IsEmpty(ref oldList[i]))
                        DirectInsert(ref oldList[i], traits);

                traits.EffectTotalAllocatedSpace(newListLength - oldListLength);
            }
        }


        private void DirectInsert(ref TStored item, Hashtable<TStored, TSearch> traits)
        {
            var mask = (UInt32)(_List.Length - 1);
            var searchHash = traits.GetHashCode(ref item);
            var i = searchHash & mask;

            if (traits.IsEmpty(ref _List[i]))
            {
                _List[i] = item;
                return;
            }

            var firstHash = traits.GetHashCode(ref _List[i]);
            var storedItemHash = firstHash;
            var searchHashDiff = (searchHash - firstHash) & mask;

            while(true)
            {
                i = (i + 1) & mask;

                if (traits.IsEmpty(ref _List[i]))
                {
                    _List[i] = item;
                    return;
                }

                storedItemHash = traits.GetHashCode(ref _List[i]);

                if (((storedItemHash - firstHash) & mask) > searchHashDiff)
                {
                    InsertItemAtIndex(mask, i, item, traits);
                    return;
                }                
            }
        }

        private void InsertItemAtIndex(UInt32 mask, UInt32 i, TStored itemCopy, Hashtable<TStored, TSearch> traits)
        {
            while (true)
            {
                //swap
                {
                    TStored temp = _List[i];
                    _List[i] = itemCopy;
                    itemCopy = temp;
                }

                i = (i + 1) & mask;

                if (traits.IsEmpty(ref _List[i]))
                {
                    _List[i] = itemCopy;                    
                    return;
                }
            }
        }

        internal void Trim(Hashtable<TStored, TSearch> traits)
        { DecrementCount(traits, 0); }
    }
}
