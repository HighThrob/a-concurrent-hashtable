/*  
 Copyright 2008 The 'A Concurrent Hashtable' development team  
 (http://www.codeplex.com/CH/People/ProjectPeople.aspx)

 This library is licensed under the GNU Library General Public License (LGPL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://www.codeplex.com/CH/license.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace TvdP.Collections
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

        public void Welcome(ConcurrentHashtable<TStored, TSearch> traits)
        { traits.EffectTotalAllocatedSpace(_List.Length); }

        public void Bye(ConcurrentHashtable<TStored, TSearch> traits)
        { traits.EffectTotalAllocatedSpace(-_List.Length); }

        public bool FindItem(ref TSearch key, out TStored item, ConcurrentHashtable<TStored, TSearch> traits)
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

        public bool GetOldestItem(ref TStored key, out TStored item, ConcurrentHashtable<TStored, TSearch> traits)
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

        public bool InsertItem(ref TStored key, out TStored item, ConcurrentHashtable<TStored, TSearch> traits)
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

        public bool RemoveItem(ref TSearch key, out TStored item, ConcurrentHashtable<TStored, TSearch> traits)
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

        public int GetNextItem(int beyond, out TStored item, ConcurrentHashtable<TStored, TSearch> traits)
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

        public void Clear(ConcurrentHashtable<TStored, TSearch> traits)
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

        protected void RemoveAtIndex(UInt32 index, ConcurrentHashtable<TStored, TSearch> traits)
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

        private void InsertItemAtIndex(UInt32 mask, UInt32 i, TStored itemCopy, ConcurrentHashtable<TStored, TSearch> traits)
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

        private void ResizeList(ConcurrentHashtable<TStored, TSearch> traits, int oldListLength)
        {
            var oldList = _List;

            var newListLength = 2;

            while (newListLength < _Count)
                newListLength <<= 1;

            newListLength <<= 1;

            _List = new TStored[newListLength];

            var mask = (UInt32)(newListLength - 1);

            for (int i = 0; i != oldListLength; ++i)
                if (!traits.IsEmpty(ref oldList[i]))
                {                    
                    var searchHash = traits.GetHashCode(ref oldList[i]);

                    //j is prefered insertion pos in new list.
                    var j = searchHash & mask;

                    if (traits.IsEmpty(ref _List[j]))
                        _List[j] = oldList[i];
                    else
                    {
                        var firstHash = traits.GetHashCode(ref _List[j]);
                        var storedItemHash = firstHash;
                        var searchHashDiff = (searchHash - firstHash) & mask;

                        while (true)
                        {
                            j = (j + 1) & mask;

                            if (traits.IsEmpty(ref _List[j]))
                            {
                                _List[j] = oldList[i];
                                break;
                            }

                            storedItemHash = traits.GetHashCode(ref _List[j]);

                            if (((storedItemHash - firstHash) & mask) > searchHashDiff)
                            {
                                InsertItemAtIndex(mask, j, oldList[i], traits);
                                break;
                            }
                        }
                    }
                }                   

            traits.EffectTotalAllocatedSpace(newListLength - oldListLength);
        }

        protected void DecrementCount(ConcurrentHashtable<TStored, TSearch> traits, int amount)
        {
            var oldListLength = _List.Length;
            _Count -= amount;

            if (oldListLength > 4 && _Count < (oldListLength >> 2))
                //Shrink
                ResizeList(traits, oldListLength);
        }

        protected void DecrementCount(ConcurrentHashtable<TStored, TSearch> traits)
        { DecrementCount(traits, 1); }

        private void IncrementCount(ConcurrentHashtable<TStored, TSearch> traits)
        {
            var oldListLength = _List.Length;

            if (++_Count >= (oldListLength - (oldListLength >> 2)))
                //Grow
                ResizeList(traits, oldListLength);
        }

        /// <summary>
        /// Remove any excess allocated space
        /// </summary>
        /// <param name="traits"></param>
        internal void Trim(ConcurrentHashtable<TStored, TSearch> traits)
        { DecrementCount(traits, 0); }
    }
}
