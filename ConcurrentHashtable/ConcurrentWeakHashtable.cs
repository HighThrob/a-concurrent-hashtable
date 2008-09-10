using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConcurrentHashtable
{
    public abstract class ConcurrentWeakHashtable<TStored, TSearch> : ConcurrentHashtable<TStored, TSearch>, IMaintainable
    {
        /// <summary>
        /// Table maintenance, removes all GC'd entries.
        /// </summary>
        public virtual void DoMaintenance()
        {
            lock (SyncRoot)
            {
                for (int i = 0, end = _CurrentRange.Count; i != end; ++i)
                {
                    var segment = (WeakSegment<TStored, TSearch>)_CurrentRange.GetSegmentByIndex(i);

                    while (!segment.Lock())
                        Thread.Sleep(0);

                    segment.DisposeGarbage(this);

                    segment.Unlock();
                }
            }
        }

        protected override void Initialize()
        {
            base.Initialize();
            ConcurrentWeakHashtableHelper.Register(this);
        }

        /// <summary>
        /// Indicates if a specific content item should be treated as garbage and removed.
        /// </summary>
        /// <param name="item">The item to judge.</param>
        /// <returns>A boolean value that is true if the item is not empty and should be treated as garbage; otherwise false.</returns>
        internal protected abstract bool IsGarbage(ref TStored item);

        /// <summary>
        /// CreateSegmentRange override that returns a WeakSegmentrange
        /// </summary>
        /// <param name="segmentCount"></param>
        /// <param name="initialSegmentSize"></param>
        /// <returns></returns>
        internal override Segmentrange<TStored, TSearch> CreateSegmentRange(int segmentCount, int initialSegmentSize)
        { return WeakSegmentrange<TStored, TSearch>.Create(segmentCount, initialSegmentSize); }
    }
}
