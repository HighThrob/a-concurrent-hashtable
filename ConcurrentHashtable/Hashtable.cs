using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConcurrentHashtable
{
    public abstract class Hashtable<TStored, TSearch> : HashtableBase
    {
        internal Hashtable(Int32 segmentCount)
        {
            _CurrentRange = new Segmentrange<TStored, TSearch>(segmentCount);
            _NewRange = _CurrentRange;
            _SwitchPoint = 0;         
        }

        /// <summary>
        /// While adjusting the segmentation, _NewRange will hold a reference to the new range of segments.
        /// when the adjustment is complete this reference will be copied to _CurrentRange.
        /// </summary>
        Segmentrange<TStored, TSearch> _NewRange;

        /// <summary>
        /// Will hold the most current reange of segments. When busy adjusting the segmentation, this
        /// field will hold a reference to the old range.
        /// </summary>
        Segmentrange<TStored, TSearch> _CurrentRange;

        /// <summary>
        /// While adjusting the segmentation this field will hold a boundary
        /// Clients accessing items with a key hash value below this boundary (unsigned compared)
        /// will access _NewRange. The others will access _CurrentRange
        /// </summary>
        Int32 _SwitchPoint;

        #region Traits

        internal protected abstract Int32 GetHashCode(ref TStored item);
        internal protected abstract Int32 GetHashCode(ref TSearch key);
        internal protected abstract bool Equals(ref TStored item, ref TSearch key);
        internal protected abstract bool Equals(ref TStored item1, ref TStored item2);

        /// <summary>
        /// Indicates if a specific item reference contains a valid item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        internal protected abstract bool IsEmpty(ref TStored item);

        /// <summary>
        /// Indicates if a specific content item should be treated as garbage and removed.
        /// </summary>
        /// <param name="item">The item to judge.</param>
        /// <returns>A boolean value that is true if the item is not empty and should be treated as garbage; otherwise false.</returns>
        internal protected abstract bool IsGarbage(ref TStored item);
        internal protected abstract TStored EmptyItem { get; }

        #endregion

        #region SyncRoot

        readonly object _SyncRoot = new object();

        /// <summary>
        /// Returns an object that serves as a lock for range operations 
        /// </summary>
        /// <remarks>
        /// Clients use this primarily for enumerating over the Tables contents.
        /// Locking doesn't guarantee that the contents don't change, but prevents operations that would
        /// disrupt the enumeration process.
        /// Operations that use this lock:
        /// Count, Clear, DisposeGarbage and DoTableMaintenance.
        /// Keeping this lock will prevent the table from re-segmenting.
        /// </remarks>
        protected object SyncRoot { get { return _SyncRoot; } }

        #endregion

        #region Per segment accessors

        /// <summary>
        /// Gets a segment out of either _NewRange or _CurrentRange based on the hash value.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        Segment<TStored, TSearch> GetSegment(Int32 hash)
        { return ((UInt32)hash < (UInt32)_SwitchPoint ? _NewRange : _CurrentRange).GetSegment(hash); }

        /// <summary>
        /// Gets a LOCKED segment out of either _NewRange or _CurrentRange based on the hash value.
        /// Unlock needs to be called on this segment before it can be used by other clients.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        Segment<TStored, TSearch> GetLockedSegment(Int32 hash)
        {
            //If we can't get the lock directly re-aquire the segment.
            while (true)
            {
                var segment = GetSegment(hash);

                if (segment.Lock())
                    return segment;

                Thread.Sleep(0);
            }
        }

        /// <summary>
        /// Finds an item in the table collection that maches the given searchKey
        /// </summary>
        /// <param name="searchKey">The key to the item.</param>
        /// <param name="item">Out reference to a field that will receive the found item.</param>
        /// <returns>A boolean that will be true if an item has been found and false otherwise.</returns>
        protected bool FindItem(ref TSearch searchKey, out TStored item)
        {
            var segment = GetLockedSegment(this.GetHashCode(ref searchKey));

            try
            {
                return segment.FindItem(ref searchKey, out item, this);
            }
            finally
            { segment.Unlock(); }
        }

        /// <summary>
        /// Looks for an existing item in the table contents using an alternative copy. If it can be found it will be returned. 
        /// If not then the alternative copy will be added to the table contents and the alternative copy will be returned.
        /// </summary>
        /// <param name="searchKey">A copy to search an already existing instance with</param>
        /// <param name="item">Out reference to receive the found item or the alternative copy</param>
        /// <returns>A boolean that will be true if an existing copy was found and false otherwise.</returns>
        protected bool GetOldestItem(ref TStored searchKey, out TStored item)
        {
            var segment = GetLockedSegment(this.GetHashCode(ref searchKey));

            try
            {
                return segment.GetOldestItem(ref searchKey, out item, this);
            }
            finally
            { segment.Unlock(); }
        }

        /// <summary>
        /// Inserts an item in the table contents possibly replacing an existing item.
        /// </summary>
        /// <param name="searchKey">The item to insert in the table</param>
        /// <param name="replacedItem">Out reference to a field that will receive any possibly replaced item.</param>
        /// <returns>A boolean that will be true if an existing copy was found and replaced and false otherwise.</returns>
        protected bool InsertItem(ref TStored searchKey, out TStored replacedItem)
        {
            var segment = GetLockedSegment(this.GetHashCode(ref searchKey));

            try
            {
                return segment.InsertItem(ref searchKey, out replacedItem, this);
            }
            finally
            { segment.Unlock(); }
        }

        /// <summary>
        /// Removes an item from the table contents.
        /// </summary>
        /// <param name="searchKey">The key to find the item with.</param>
        /// <param name="removedItem">Out reference to a field that will receive the found and removed item.</param>
        /// <returns>A boolean that will be rue if an item was found and removed and false otherwise.</returns>
        protected bool RemoveItem(ref TSearch searchKey, out TStored removedItem)
        {
            var segment = GetLockedSegment(this.GetHashCode(ref searchKey));

            try
            {
                return segment.RemoveItem(ref searchKey, out removedItem, this);
            }
            finally
            { segment.Unlock(); }
        }

        #endregion

        #region Collection wide accessors

        //These methods require a lock on SyncRoot. They will not block regular per segment accessors (for long)

        /// <summary>
        /// Gets an IEnumerable to iterate over all items in all segments.
        /// A lock should be aquired and held on SyncRoot while this IEnumerable is being used
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<TStored> Items
        {
            get
            {
                for (int i = 0, end = _CurrentRange.Count; i != end; ++i)
                {
                    var segment = _CurrentRange.GetSegmentByIndex(i);

                    while (!segment.Lock())
                        Thread.Sleep(0);

                    int j = -1;
                    TStored foundItem;

                    while ((j = segment.GetNextItem(j, out foundItem, this)) >= 0)
                        yield return foundItem;

                    segment.Unlock();
                }
            }
        }

        /// <summary>
        /// Removes all items from the collection. 
        /// Aquires a lock on SyncRoot before it does it's thing.
        /// </summary>
        protected void Clear()
        {
            lock (SyncRoot)
            {
                for( int i=0, end = _CurrentRange.Count; i != end; ++i )
                {
                    var segment = _CurrentRange.GetSegmentByIndex(i);

                    while (!segment.Lock())
                        Thread.Sleep(0);

                    segment.Clear(this);

                    segment.Unlock();
                }
            }
        }

        /// <summary>
        /// Returns a count of all items in teh collection. This may not be
        /// aqurate when multiple threads are accessing this table.
        /// Aquires a lock on SyncRoot before it does it's thing.
        /// </summary>
        protected int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    Int32 count = 0;

                    for (int i = 0, end = _CurrentRange.Count; i != end; ++i)
                        count += _CurrentRange.GetSegmentByIndex(i).Count;

                    return count;
                }
            }
        }

        #endregion

        #region Table Maintenance methods

        /// <summary>
        /// Seeps all items currently in the collection. Any item it finds where the
        /// <see cref="ISegmentTraits{TStored,TSearch}.IsGarbage"/> methods returns false for
        /// will be removed from the collection.
        /// Aquires a lock on SyncRoot before it does it's thing.
        /// </summary>
        protected void DisposeGarbage()
        {
            lock (SyncRoot)
            {
                for (int i = 0, end = _CurrentRange.Count; i != end; ++i)
                {
                    var segment = _CurrentRange.GetSegmentByIndex(i);

                    while (!segment.Lock())
                        Thread.Sleep(0);

                    segment.DisposeGarbage(this);

                    segment.Unlock();
                }
            }
        }

        /// <summary>
        /// Adjusts the segmentation to the new segment count
        /// </summary>
        /// <param name="newSegmentCount">The new number of segments to use. This must be a power of 2.</param>
        void SetSegmentation(Int32 newSegmentCount)
        {
            unchecked
            {
                //create the new range
                Segmentrange<TStored, TSearch> newRange = new Segmentrange<TStored, TSearch>(newSegmentCount);

                //lock all new segments
                //we are going to release these locks while we migrate the items from the
                //old (current) range to the new range.
                for (int i = 0, end = newRange.Count; i != end; ++i)
                    newRange.GetSegmentByIndex(i).Lock();

                //set new (completely locked) range
                Interlocked.Exchange(ref _NewRange, newRange);


                //calculate the step sizes for our switch points            
                Int32 currentSwitchPointStep = 1 << _CurrentRange.Shift;
                Int32 newSwitchPointStep = 1 << newRange.Shift;

                //position in new range up from where the new segments are locked
                Int32 newLockedPoint = 0;

                //At this moment _SwitchPoint should be 0
                Int32 switchPoint = _SwitchPoint;

                do
                {
                    //aquire segment to migrate
                    var currentSegment = _CurrentRange.GetSegment(switchPoint);

                    //lock segment (never to release it)
                    while (!currentSegment.Lock())
                        Thread.Sleep(0);

                    //migrate all items in the segment to the new range
                    TStored currentKey;

                    int it = -1;

                    while ((it = currentSegment.GetNextItem(it, out currentKey, this)) >= 0)
                    {
                        Int32 currentKeyHash = this.GetHashCode(ref currentKey);

                        //get the new segment. this is already locked.
                        var newSegment = _NewRange.GetSegment(currentKeyHash);

                        TStored dummyKey;
                        newSegment.InsertItem(ref currentKey, out dummyKey, this);
                    }

                    if (switchPoint == 0 - currentSwitchPointStep)
                    {
                        //we are about to wrap _SwitchPoint arround.
                        //We have migrated all items from the intere table to the
                        //new range.
                        //replace current with new before advancing, otherwise
                        //we would create a completely blocked table.
                        Interlocked.Exchange(ref _CurrentRange, newRange);
                    }

                    //advance _SwitchPoint
                    switchPoint = Interlocked.Add(ref _SwitchPoint, currentSwitchPointStep);

                    //release lock of new segments upto the point where we can still add new items
                    //during this migration.
                    while ((UInt32)newLockedPoint + (UInt32)newSwitchPointStep <= (UInt32)switchPoint)
                    {
                        newRange.GetSegment(newLockedPoint).Unlock();
                        newLockedPoint += newSwitchPointStep;
                    }
                }
                while (switchPoint != 0);

                //unlock any remaining new segments
                while (newLockedPoint != 0)
                {
                    newRange.GetSegment(newLockedPoint).Unlock();
                    newLockedPoint += newSwitchPointStep;
                }
            }
        }

        /// <summary>
        /// Determines the number of segments the table should be broken up in.
        /// Each segment can be accessed by 1 thread simultaneously. The more segments
        /// there are the more concurrent the table gets.
        /// </summary>
        /// <param name="count">Roughly the number of items contained in the table.</param>
        /// <returns>The number of segments. The actual number of segments will be the first power of 2 that is greater than or equal to the returned value.</returns>
        protected abstract Int32 DetermineSegmentation(Int32 count);

        protected override void DoTableMaintenance()
        {
            lock (SyncRoot)
            {
                //determine prefered level of segmentation
                Int32 count = 0;

                for (int i = 0, end = _CurrentRange.Count; i != end; ++i)
                    count += _CurrentRange.GetSegmentByIndex(i).Count;

                Int32 lowerLimit = DetermineSegmentation(count);
                Int32 segmentation = 1;

                while (lowerLimit > segmentation)
                    segmentation <<= 1;

                if (segmentation != _CurrentRange.Count)
                    SetSegmentation(segmentation);
            }
        }

        #endregion
    }
}
