using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConcurrentHashtable
{
    internal class WeakSegmentrange<TStored, TSearch> : Segmentrange<TStored, TSearch> 
    {
        protected WeakSegmentrange()
        {}

        public new static Segmentrange<TStored, TSearch> Create(int segmentCount, int initialSegmentSize)
        {
            var instance = new WeakSegmentrange<TStored, TSearch>();
            instance.Initialize(segmentCount, initialSegmentSize);
            return instance;
        }

        protected override Segment<TStored, TSearch> CreateSegment(int initialSegmentSize)
        { return WeakSegment<TStored, TSearch>.Create(initialSegmentSize); }
    }
}
