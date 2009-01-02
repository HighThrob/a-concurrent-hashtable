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
    /// Helper class for ConcurrentWeakHashtable. Makes sure that its DoMaintenance method
    /// gets called when the GarbageCollector has collected garbage.
    /// </summary>
    internal static class ConcurrentWeakHashtableHelper
    {
        #region Garbage Collection tracking

        /// <summary>
        /// Check all table registrations for continued existence. Removes the entries for
        /// tables that have been GC'd
        /// </summary>
        private static void RemoveVoidTables()
        {
            int ctr = 0;

            //trim list. remove all GC'd lists
            for (int i = 0; i < _TableList.Count; ++i)
            {
                var wr = _TableList[i];

                if (wr.Target != null)
                    _TableList[ctr++] = wr;
            }

            _TableList.RemoveRange(ctr, _TableList.Count - ctr);
        }

        /// <summary>
        /// true if a table maintenance session is scheduled or ongoing.
        /// </summary>
        static bool _TableMaintenanceIsPending;

        /// <summary>
        /// Small timer that will
        /// check every 1/10 second if second level GC count increased. 
        /// </summary>
        static Timer _Timer = new Timer(CheckGCCount, null, 100, 100);

        static int _Level2GCCount;

        /// <summary>
        /// Checks if a garbage collection has indeed taken place and schedules
        /// a table maintenance session.
        /// </summary>
        /// <param name="dummy"></param>
        static internal void CheckGCCount(object dummy)
        {
            WeakReference[] res = null;

            if (Monitor.TryEnter(_TableList,50))
                try
                {
                    if (!_TableMaintenanceIsPending && _Level2GCCount != GC.CollectionCount(2))
                    {
                        _TableMaintenanceIsPending = true;
                        _Level2GCCount = GC.CollectionCount(2);

                        RemoveVoidTables();

                        res = _TableList.ToArray();
                    }
                }
                finally 
                {
                    Monitor.Exit(_TableList);
                }
            
            if (res != null)
            {
                var thread = new System.Threading.Thread(
                    new ThreadStart(
                        delegate
                        {
                            try
                            {
                                Queue<IMaintainable> delayedQueue = null;

                                for (int i = 0, end = res.Length; i < end; ++i)
                                {
                                    var target = (IMaintainable)res[i].Target;

                                    if (target != null)
                                    {
                                        if (!target.DoMaintenance())
                                        {
                                            if (delayedQueue == null)
                                                delayedQueue = new Queue<IMaintainable>();

                                            delayedQueue.Enqueue(target);
                                        }
                                    }
                                }

                                if (delayedQueue != null)
                                {
                                    int sleepCounter = delayedQueue.Count;
                                    while (delayedQueue.Count > 0)
                                    {
                                        if (sleepCounter-- == 0)
                                        {
                                            Thread.Sleep(0);
                                            sleepCounter = delayedQueue.Count;
                                        }

                                        var target = delayedQueue.Dequeue();

                                        if (!target.DoMaintenance())
                                            delayedQueue.Enqueue(target);
                                    }
                                }
                            }
                            finally
                            {
                                _TableMaintenanceIsPending = false;
                            }
                        }
                    )
                );

                thread.Priority = ThreadPriority.Lowest;

                thread.Start();
            }
        }

        #endregion


        #region maintaining Tables list

        /// <summary>
        /// a list of all WeakHashtables
        /// </summary>
        static List<WeakReference> _TableList = new List<WeakReference>();

        /// <summary>
        /// this is to be called from the constructor or initializer of a ConcurrentWeakHashtable instance
        /// </summary>
        internal static void Register(IMaintainable table)
        {
            lock (_TableList)
                _TableList.Add(new WeakReference(table));
        }

        #endregion
    }
}
