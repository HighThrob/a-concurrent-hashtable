using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConcurrentHashtable
{
    public abstract class HashtableBase
    {
        #region Garbage Collection tracking

        class GCSpy
        {
            ~GCSpy()
            { HashtableBase.GCDetected(); }
        }

        static bool TablesCleaningIsPending;
        static WeakReference GCSpyReference = new WeakReference(new GCSpy());

        static void GCDetected()
        {
            lock (GCSpyReference)
            {
                if (!TablesCleaningIsPending)
                {
                    TablesCleaningIsPending = true;
                    System.Threading.ThreadPool.QueueUserWorkItem(
                        new System.Threading.WaitCallback(CleanTables)
                    );
                }
            }
        }

        #endregion

        #region Cleaning Tables

        static int _TablesToBeCleaned;

        /// <summary>
        /// Implement in derived classes
        /// </summary>
        protected abstract void DoTableMaintenance();

        private static void CleanTable(object tableAsObject)
        {
            var table = (HashtableBase)tableAsObject;

            try
            {
                if( table != null )
                    table.DoTableMaintenance();
            }
            finally
            {
                //when last table is cleaned.. ready for next sweep.
                if (Interlocked.Decrement(ref _TablesToBeCleaned) == 0)
                    lock (GCSpyReference)
                    {
                        TablesCleaningIsPending = false;

                        if (GCSpyReference.Target == null)
                            GCSpyReference.Target = new GCSpy();
                    }
            }
        }

        private static void CleanTables(object nothing)
        {                        
            lock (TableList)
            {
                int ctr = 0;

                //trim list.
                for (int i = 0; i < TableList.Count; ++i)
                {
                    WeakReference wr = TableList[i];

                    if (wr.Target != null)
                        TableList[ctr++] = wr;
                }

                TableList.RemoveRange(ctr, TableList.Count - ctr);

                //check live tables on alternative threads.
                _TablesToBeCleaned = ctr;
                
                for (int i = 0; i < ctr; ++i)
                    ThreadPool.QueueUserWorkItem( new WaitCallback( CleanTable ), TableList[i].Target );
            }
        }

        #endregion

        #region maintaining Tables list

        static List<WeakReference> TableList = new List<WeakReference>();

        /// <summary>
        /// this is to be called from derived final (sealed) constructor
        /// </summary>
        protected void Initialize()
        {
            lock (TableList)
                TableList.Add(new WeakReference(this));

            lock (GCSpyReference)
                if (GCSpyReference.Target == null)
                    GCDetected();
        }

        #endregion

    }
}
