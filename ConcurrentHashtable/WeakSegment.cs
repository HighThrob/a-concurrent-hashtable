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

namespace TvdP.Collections
{
    internal class WeakSegment<TStored, TSearch> : Segment<TStored, TSearch>
    {
        protected WeakSegment()
        { }

        public new static WeakSegment<TStored, TSearch> Create(Int32 initialSize)
        {
            var instance = new WeakSegment<TStored, TSearch>();
            instance.Initialize(initialSize);
            return instance;
        }

        /// <summary>
        /// Remove all items in the segment that are Garbage.
        /// </summary>
        /// <param name="traits">The <see cref="ConcurrentHashtable{TStored,TSearch}"/> that determines how to treat each individual item.</param>
        public void DisposeGarbage(ConcurrentWeakHashtable<TStored, TSearch> traits)
        {
            var garbageCount = 0;

            for (UInt32 i = 0, end = (UInt32)(_List.Length); i != end; ++i)
            {
                while (traits.IsGarbage(ref _List[i]))
                {
                    ++garbageCount;
                    RemoveAtIndex(i, traits);
                }
            }

            DecrementCount(traits, garbageCount);
        }
    }
}
