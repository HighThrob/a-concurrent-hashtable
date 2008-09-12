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
        /// Small class just to detect garbage collections. When an instance of this class
        /// is garbage collected it will notify WeakHashtableHelper.
        /// </summary>
        class GCSpy
        {
            ~GCSpy()
            { ConcurrentWeakHashtableHelper.VerifyGCDetected(null); }
        }

        /// <summary>
        /// true if a table maintenance session is scheduled or ongoing.
        /// </summary>
        static bool _TableMaintenanceIsPending;

        /// <summary>
        /// WeakReference to a GCSpy object. If its Target property == null
        /// then the GCSpy object has been garbage collected.
        /// </summary>
        static WeakReference _GCSpyReference = new WeakReference(new GCSpy());

        /// <summary>
        /// Because it is not guaranteed that the finalizer of GCSpy will be called
        /// check every second if _GCSpyReference.Target == null. 
        /// </summary>
        static Timer _Timer = new Timer(VerifyGCDetected, null, 1000, 1000);

        /// <summary>
        /// Checks if a garbage collection has indeed taken place and schedules
        /// a table maintenance session.
        /// </summary>
        /// <param name="dummy"></param>
        static internal void VerifyGCDetected(object dummy)
        {
            lock (_GCSpyReference)
            {
                if (!_TableMaintenanceIsPending && _GCSpyReference.Target == null)
                {
                    _TableMaintenanceIsPending = true;
                    System.Threading.ThreadPool.QueueUserWorkItem(
                        new System.Threading.WaitCallback(MaintainTables)
                    );
                }
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
                    lock (_GCSpyReference)
                    {
                        _TableMaintenanceIsPending = false;

                        if (_GCSpyReference.Target == null)
                            _GCSpyReference.Target = new GCSpy();
                    }
            }
        }

        /// <summary>
        /// Check all table registrations for continued existence. Then schedule TableMaintenance 
        /// jobs for each existing table. This method is to be scheduled in the thread pool whenever
        /// a GC run has been detected.
        /// </summary>
        /// <param name="nothing"></param>
        private static void MaintainTables(object nothing)
        {                       
            lock (_TableList)
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
