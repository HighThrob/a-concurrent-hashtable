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
            if (Monitor.TryEnter(_TableList,50))
                try
                {
                    if (!_TableMaintenanceIsPending && _Level2GCCount != GC.CollectionCount(2))
                    {
                        _TableMaintenanceIsPending = true;
                        _Level2GCCount = GC.CollectionCount(2);
                        MaintainTables();
                    }
                }
                finally 
                {
                    Monitor.Exit(_TableList);
                }
        }

        #endregion

        #region Maintaining Tables

        /// <summary>
        /// The number of tables of which the DoMaintenance method still needs to be called
        /// in this maintenance session.
        /// </summary>
        static int _NumberOfTablesStillToBeMaintained;

        /// <summary>
        /// Maintain a single table. This is a WaitCallback method that will be scheduled in the thread pool.
        /// </summary>
        /// <param name="tableAsObject"></param>
        private static void MaintainTable(object tableAsObject)
        {
            var table = (IMaintainable)tableAsObject;

            try
            {
                if( table != null )
                    table.DoMaintenance();
            }
            finally
            {
                //when last table is processed. ready for next session.
                if (Interlocked.Decrement(ref _NumberOfTablesStillToBeMaintained) == 0)
                    _TableMaintenanceIsPending = false;
            }
        }

        /// <summary>
        /// Check all table registrations for continued existence. Then schedule TableMaintenance 
        /// jobs for each existing table. This method is to be scheduled in the thread pool whenever
        /// a GC run has been detected.
        /// </summary>
        /// <param name="nothing"></param>
        private static void MaintainTables()
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

            //check live tables on alternative threads. Schedule jobs.
            _NumberOfTablesStillToBeMaintained = ctr;
            
            for (int i = 0; i < ctr; ++i)
                ThreadPool.QueueUserWorkItem( new WaitCallback( MaintainTable ), _TableList[i].Target );
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
