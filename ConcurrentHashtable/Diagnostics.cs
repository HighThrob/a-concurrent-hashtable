using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace TvdP.Collections
{
    internal static class Diagnostics
    {
        internal static TraceSwitch ConcurrentHashtableSwitch = new TraceSwitch("ConcurrentHashtable", "ConcurrentHashtable diagnostics");
        internal static Dictionary<Type, bool> TypeBadHashReportMap = new Dictionary<Type, bool>();

        static Diagnostics()
        {            
            if( ConcurrentHashtableSwitch.TraceVerbose )
                Trace.TraceInformation("ConcurrentHashtable diagnostics initialized.");
        }

        internal static void JustToWakeUp()
        { }
    }
}
