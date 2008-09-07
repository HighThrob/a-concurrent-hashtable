using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConcurrentHashtable
{
    internal class Segmentrange<TStored, TSearch> 
    {
        public Segmentrange(int size)
        {
            _Segments = new Segment<TStored, TSearch>[size];

            for (int i = 0, end = _Segments.Length; i != end; ++i)
                _Segments[i] = new Segment<TStored, TSearch>();

            for (int w = size; w != 0; w <<= 1)
                ++_Shift;
        }

        Segment<TStored, TSearch>[] _Segments;
        Int32 _Shift;

        public Segment<TStored, TSearch> GetSegment(UInt32 hash)
        { return _Segments[hash >> _Shift]; }

        public Segment<TStored, TSearch> GetSegmentByIndex(Int32 index)
        { return _Segments[index]; }

        public Int32 Count { get { return _Segments.Length; } }

        public Int32 Shift { get { return _Shift; } }
    }
}
