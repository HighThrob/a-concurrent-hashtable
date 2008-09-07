using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace ConcurrentHashtable
{
    internal class Segment<TStored,TSearch>
    {
        public Segment()
        {
            _List = new TStored[4];
        }

        Int32 _Token;
        Int32 _Count;

        public Int32 Count
        { get { return _Count; } }

        TStored[] _List;

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

        /// <summary>
        /// Remove all items in the segment that are Garbage.
        /// </summary>
        /// <param name="traits">The <see cref="Hashtable{TStored,TSearch}"/> that determines how to treat each individual item.</param>
        public void DisposeGarbage(Hashtable<TStored, TSearch> traits)
        {
            for ( UInt32 i = 0, end = (UInt32)(_List.Length); i != end; ++i)
            {
                while (traits.IsGarbage(ref _List[i]))
                    RemoveAtIndex(i, traits); 
            }
        }

        public bool Lock()
        {
            return Interlocked.CompareExchange(ref _Token, 1, 0) == 0;
        }

        public void Unlock()
        {
            Interlocked.Exchange(ref _Token, 0);
        }

        private void RemoveAtIndex(UInt32 index, Hashtable<TStored, TSearch> traits)
        {
            var mask = (UInt32)(_List.Length - 1);
            var i = index;
            var j = (index + 1) & mask;

            while(true)
            {
                if (traits.IsEmpty(ref _List[j]) || (traits.GetHashCode(ref _List[j]) & mask) == j)
                {
                    _List[i] = traits.EmptyItem;
                    return;
                }

                _List[i] = _List[j];

                i = j;
                j = (j + 1) & mask;            
            }
        }

        private void DecrementCount(Hashtable<TStored, TSearch> traits)
        {
            if (--_Count < (_List.Length >> 2))
            {
                //Shrink
                var oldList = _List;
                _List = new TStored[_List.Length >> 1];

                for (int i = 0, end = oldList.Length; i != end; ++i)
                    if (!traits.IsEmpty(ref oldList[i]))
                        DirectInsert(ref oldList[i], traits);
            }
        }

        private void IncrementCount(Hashtable<TStored, TSearch> traits)
        {
            if (++_Count >= (_List.Length - (_List.Length >> 2)))
            {
                //Grow
                var oldList = _List;
                _List = new TStored[_List.Length << 1];

                for (int i = 0, end = oldList.Length; i != end; ++i)
                    if (!traits.IsEmpty(ref oldList[i]))
                        DirectInsert(ref oldList[i], traits);
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
    }
}
