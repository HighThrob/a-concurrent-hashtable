using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        private static void CleanTables(object nothing)
        {
            try
            {
                int ctr = 0;

                //trim list
                lock (TableList)
                {
                    for (int i = 0; i < TableList.Count; ++i)
                    {
                        WeakReference wr = TableList[i];

                        if (wr.Target != null)
                            TableList[ctr++] = wr;
                    }

                    TableList.RemoveRange(ctr, TableList.Count - ctr);
                }

                //check live tables
                for (int i = 0; i < ctr; ++i)
                {
                    HashtableBase wdb;

                    lock (TableList)
                        wdb = (HashtableBase)(TableList[i].Target);

                    if (wdb != null)
                        wdb.DoTableMaintenance();
                }
            }
            finally
            {
                lock (GCSpyReference)
                {
                    TablesCleaningIsPending = false;

                    if (GCSpyReference.Target == null)
                        GCSpyReference.Target = new GCSpy();
                }
            }
        }

        /// <summary>
        /// Implement in derived classes
        /// </summary>
        protected abstract void DoTableMaintenance();

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
