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
    /// Base class for concurrent hashtable implementations
    /// </summary>
    /// <typeparam name="TStored">Type of the items stored in the hashtable.</typeparam>
    /// <typeparam name="TSearch">Type of the key to search with.</typeparam>
    public abstract class ConcurrentHashtable<TStored, TSearch>
    {
        /// <summary>
        /// Constructor (protected)
        /// </summary>
        /// <remarks>Use Initialize method after construction.</remarks>
        protected ConcurrentHashtable()
        {}

        /// <summary>
        /// Initialize the newly created ConcurrentHashtable. Invoke in final (sealed) constructor
        /// or Create method.
        /// </summary>
        protected virtual void Initialize()
        {
            var minSegments = MinSegments;
            var segmentAllocatedSpace = MinSegmentAllocatedSpace;

            _CurrentRange = CreateSegmentRange(minSegments, segmentAllocatedSpace);
            _NewRange = _CurrentRange;
            _SwitchPoint = 0;
            _AllocatedSpace = minSegments * segmentAllocatedSpace;
        }

        /// <summary>
        /// Create a segment range
        /// </summary>
        /// <param name="segmentCount">Number of segments in range.</param>
        /// <param name="initialSegmentSize">Number of slots allocated initialy in each segment.</param>
        /// <returns>The created <see cref="Segmentrange{TStored,TSearch}"/> instance.</returns>
        internal virtual Segmentrange<TStored, TSearch> CreateSegmentRange(int segmentCount, int initialSegmentSize)
        { return Segmentrange<TStored, TSearch>.Create(segmentCount, initialSegmentSize); }

        /// <summary>
        /// While adjusting the segmentation, _NewRange will hold a reference to the new range of segments.
        /// when the adjustment is complete this reference will be copied to _CurrentRange.
        /// </summary>
        internal Segmentrange<TStored, TSearch> _NewRange;

        /// <summary>
        /// Will hold the most current reange of segments. When busy adjusting the segmentation, this
        /// field will hold a reference to the old range.
        /// </summary>
        internal Segmentrange<TStored, TSearch> _CurrentRange;

        /// <summary>
        /// While adjusting the segmentation this field will hold a boundary.
        /// Clients accessing items with a key hash value below this boundary (unsigned compared)
        /// will access _NewRange. The others will access _CurrentRange
        /// </summary>
        Int32 _SwitchPoint;

        #region Traits
        
        //Methods used by Segment objects that tell them how to treat stored items and search keys.

        /// <summary>
        /// Get a hashcode for given storeable item.
        /// </summary>
        /// <param name="item">Reference to the item to get a hash value for.</param>
        /// <returns>The hash value as an <see cref="UInt32"/>.</returns>
        /// <remarks>
        /// The hash returned should be properly randomized hash. The standard GetHashCode methods are usually not good enough.
        /// A storeable item and a matching search key should return the same hash code.
        /// So the statement <code>Equals(storeableItem, searchKey) ? GetHashCode(storeableItem) == GetHashCode(searchKey) : true </code> should always be true;
        /// </remarks>
        internal protected abstract UInt32 GetHashCode(ref TStored item);

        /// <summary>
        /// Get a hashcode for given search key.
        /// </summary>
        /// <param name="key">Reference to the key to get a hash value for.</param>
        /// <returns>The hash value as an <see cref="UInt32"/>.</returns>
        /// <remarks>
        /// The hash returned should be properly randomized hash. The standard GetHashCode methods are usually not good enough.
        /// A storeable item and a matching search key should return the same hash code.
        /// So the statement <code>Equals(storeableItem, searchKey) ? GetHashCode(storeableItem) == GetHashCode(searchKey) : true </code> should always be true;
        /// </remarks>
        internal protected abstract UInt32 GetHashCode(ref TSearch key);

        /// <summary>
        /// Compares a storeable item to a search key. Should return true if they match.
        /// </summary>
        /// <param name="item">Reference to the storeable item to compare.</param>
        /// <param name="key">Reference to the search key to compare.</param>
        /// <returns>True if the storeable item and search key match; false otherwise.</returns>
        internal protected abstract bool Equals(ref TStored item, ref TSearch key);

        /// <summary>
        /// Compares two storeable items for equality.
        /// </summary>
        /// <param name="item1">Reference to the first storeable item to compare.</param>
        /// <param name="item2">Reference to the second storeable item to compare.</param>
        /// <returns>True if the two soreable items should be regarded as equal.</returns>
        internal protected abstract bool Equals(ref TStored item1, ref TStored item2);

        /// <summary>
        /// Indicates if a specific item reference contains a valid item.
        /// </summary>
        /// <param name="item">The storeable item reference to check.</param>
        /// <returns>True if the reference doesn't refer to a valid item; false otherwise.</returns>
        /// <remarks>The statement <code>IsEmpty(default(TStoredI))</code> should always be true.</remarks>
        internal protected abstract bool IsEmpty(ref TStored item);

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
        /// _Count, Clear, DisposeGarbage and DoTableMaintenance.
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
        internal Segment<TStored, TSearch> GetSegment(UInt32 hash)
        { return ((UInt32)hash < (UInt32)_SwitchPoint ? _NewRange : _CurrentRange).GetSegment(hash); }

        /// <summary>
        /// Gets a LOCKED segment out of either _NewRange or _CurrentRange based on the hash value.
        /// Unlock needs to be called on this segment before it can be used by other clients.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        internal Segment<TStored, TSearch> GetLockedSegment(UInt32 hash)
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
        protected virtual bool GetOldestItem(ref TStored searchKey, out TStored item)
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
        /// Enumerates all segments in _CurrentRange and locking them before yielding them and resleasing the lock afterwards
        /// The order in which the segments are returned is undefined.
        /// Lock SyncRoot before using this enumerable.
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<Segment<TStored, TSearch>> EnumerateAmorphLockedSegments()
        {
            //if segments are locked a queue will be created to try them later
            //this is so that we can continue with other not locked segments.
            Queue<Segment<TStored, TSearch>> lockedSegmentIxs = null;

            for (int i = 0, end = _CurrentRange.Count; i != end; ++i)
            {
                var segment = _CurrentRange.GetSegmentByIndex(i);

                if (segment.Lock())
                {
                    try { yield return segment; }
                    finally { segment.Unlock(); }
                }
                else
                {
                    if (lockedSegmentIxs == null)
                        lockedSegmentIxs = new Queue<Segment<TStored, TSearch>>();

                    lockedSegmentIxs.Enqueue(segment);
                }
            }

            if (lockedSegmentIxs != null)
            {
                var ctr = lockedSegmentIxs.Count;

                while (lockedSegmentIxs.Count != 0)
                {
                    //once we retried them all and we are still not done.. wait a bit.
                    if (ctr-- == 0)
                    {
                        Thread.Sleep(0);
                        ctr = lockedSegmentIxs.Count;
                    }

                    var segment = lockedSegmentIxs.Dequeue();

                    if (segment.Lock())
                    {
                        try { yield return segment; }
                        finally { segment.Unlock(); }
                    }
                    else
                        lockedSegmentIxs.Enqueue(segment);
                }
            }
        }

        /// <summary>
        /// Gets an IEnumerable to iterate over all items in all segments.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// A lock should be aquired and held on SyncRoot while this IEnumerable is being used.
        /// The order in which the items are returned is undetermined.
        /// </remarks>
        protected IEnumerable<TStored> Items
        {
            get
            {
                foreach (var segment in EnumerateAmorphLockedSegments())
                {
                    int j = -1;
                    TStored foundItem;

                    while ((j = segment.GetNextItem(j, out foundItem, this)) >= 0)
                        yield return foundItem;
                }
            }
        }

        /// <summary>
        /// Removes all items from the collection. 
        /// Aquires a lock on SyncRoot before it does it's thing.
        /// When this method returns and multiple threads have access to this table it
        /// is not guaranteed that the table is actually empty.
        /// </summary>
        protected void Clear()
        { 
            lock(SyncRoot)
                foreach(var segment in EnumerateAmorphLockedSegments())
                    segment.Clear(this);
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

                    //Don't need to lock a segment to get the count.
                    for (int i = 0, end = _CurrentRange.Count; i != end; ++i)
                        count += _CurrentRange.GetSegmentByIndex(i)._Count;

                    return count;
                }
            }
        }

        #endregion

        #region Table Maintenance methods

        /// <summary>
        /// Gives the minimum number of segments a hashtable can contain. This should be 1 or more and always a power of 2.
        /// </summary>
        protected virtual Int32 MinSegments { get { return 4; } }

        /// <summary>
        /// Gives the minimum number of allocated item slots per segment. This should be 1 or more and always a power of 2.
        /// </summary>
        protected virtual Int32 MinSegmentAllocatedSpace { get { return 4; } }

        /// <summary>
        /// Gives the prefered number of allocated item slots per segment. This should 4 or more and always a power of 2.
        /// </summary>
        protected virtual Int32 MeanSegmentAllocatedSpace { get { return 32; } }

        /// <summary>
        /// Determines if a segmentation adjustment is needed.
        /// </summary>
        /// <returns>True</returns>
        bool SegmentationAdjustmentNeeded()
        {
            var minSegments = MinSegments;
            var meanSegmentAllocatedSpace = MeanSegmentAllocatedSpace;

            var newSpace = Math.Max(_AllocatedSpace, minSegments * meanSegmentAllocatedSpace);
            var meanSpace = _CurrentRange.Count * meanSegmentAllocatedSpace;

            return newSpace > (meanSpace << 1) || newSpace <= (meanSpace >> 1);            
        }

        /// <summary>
        /// Bool as int (for interlocked functions) that is true if a Segmentation assesment is pending.
        /// </summary>
        Int32 _AssessSegmentationPending;

        /// <summary>
        /// The total allocated number of item slots. Filled with nonempty items or not.
        /// </summary>
        Int32 _AllocatedSpace;

        /// <summary>
        /// When a segment resizes it uses this method to inform the hashtable of the change in allocated space.
        /// </summary>
        /// <param name="effect"></param>
        internal void EffectTotalAllocatedSpace(Int32 effect)
        {
            //this might be a point of contention. But resizing of segments should happen (far) less often
            //than inserts and removals and therefore this should not pose a problem. 
            Interlocked.Add(ref _AllocatedSpace, effect);

            if ( SegmentationAdjustmentNeeded() && Interlocked.Exchange(ref _AssessSegmentationPending, 1) == 0 )
                    ThreadPool.QueueUserWorkItem(AssessSegmentation);
        }

        /// <summary>
        /// Checks if segmentation needs to be adjusted and if so performs the adjustment.
        /// </summary>
        /// <param name="dummy"></param>
        void AssessSegmentation(object dummy)
        {
            try
            {
                //in case of a sudden loss of almost all content we
                //may need to do this muliple times.
                while (SegmentationAdjustmentNeeded())
                {
                    var meanSegmentAllocatedSpace = MeanSegmentAllocatedSpace;
                   
                    int allocatedSpace = _AllocatedSpace;
                    int atleastSegments = allocatedSpace / meanSegmentAllocatedSpace;

                    Int32 segments = MinSegments;

                    while (atleastSegments > segments)
                        segments <<= 1;

                    SetSegmentation(segments, meanSegmentAllocatedSpace);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _AssessSegmentationPending, 0);
                EffectTotalAllocatedSpace(0);
            }
        }
              
        /// <summary>
        /// Adjusts the segmentation to the new segment count
        /// </summary>
        /// <param name="newSegmentCount">The new number of segments to use. This must be a power of 2.</param>
        /// <param name="segmentSize">The number of item slots to reserve in each segment.</param>
        void SetSegmentation(Int32 newSegmentCount, Int32 segmentSize)
        {
            lock (SyncRoot)
            {
                unchecked
                {
                    //create the new range
                    Segmentrange<TStored, TSearch> newRange = CreateSegmentRange(newSegmentCount, segmentSize);

                    //increase total allocated space now. We can do this safely
                    //because at this point _AssessSegmentationPending flag will be true,
                    //preventing an inmediate reassesment.
                    Interlocked.Add(ref _AllocatedSpace, newSegmentCount * segmentSize);

                    //lock all new segments
                    //we are going to release these locks while we migrate the items from the
                    //old (current) range to the new range.
                    for (int i = 0, end = newRange.Count; i != end; ++i)
                        newRange.GetSegmentByIndex(i).Lock();

                    //set new (completely locked) range
                    Interlocked.Exchange(ref _NewRange, newRange);


                    //calculate the step sizes for our switch points            
                    var currentSwitchPointStep = (UInt32)(1 << _CurrentRange.Shift);
                    var newSwitchPointStep = (UInt32)(1 << newRange.Shift);

                    //position in new range up from where the new segments are locked
                    var newLockedPoint = (UInt32)0;

                    //At this moment _SwitchPoint should be 0
                    var switchPoint = (UInt32)_SwitchPoint;

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
                            var currentKeyHash = this.GetHashCode(ref currentKey);

                            //get the new segment. this is already locked.
                            var newSegment = _NewRange.GetSegment(currentKeyHash);

                            TStored dummyKey;
                            newSegment.InsertItem(ref currentKey, out dummyKey, this);
                        }

                        //substract allocated space from allocated space count.
                        currentSegment.Bye(this);

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
                        switchPoint = (UInt32)Interlocked.Add(ref _SwitchPoint, (Int32)currentSwitchPointStep);

                        //release lock of new segments upto the point where we can still add new items
                        //during this migration.
                        while (true)
                        {
                            var nextNewLockedPoint = newLockedPoint + newSwitchPointStep;

                            if (nextNewLockedPoint > switchPoint || nextNewLockedPoint == 0)
                                break;
                            
                            var newSegment = newRange.GetSegment(newLockedPoint);
                            newSegment.Trim(this);
                            newSegment.Unlock();
                            newLockedPoint = nextNewLockedPoint;
                        }
                    }
                    while (switchPoint != 0);

                    //unlock any remaining new segments
                    while (newLockedPoint != 0)
                    {
                        var newSegment = newRange.GetSegment(newLockedPoint);
                        newSegment.Trim(this);
                        newSegment.Unlock();
                        newLockedPoint += newSwitchPointStep;
                    }
                }
            }
        }

        #endregion
    }
}
