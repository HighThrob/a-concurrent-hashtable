using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConcurrentHashtable
{
    static class Hasher
    {
        public static Int32 Rehash(Int32 hash)
        {
            unchecked
            {
                Int64 prod = ((Int64)hash ^ 0x00000000691ac2e9L) * 0x00000000a931b975L;
                return (Int32)(prod ^ (prod >> 32));
            }
        }
    }
}
