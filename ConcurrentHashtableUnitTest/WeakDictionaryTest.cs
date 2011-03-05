/*  
 Copyright 2008 The 'A Concurrent Hashtable' development team  
 (http://www.codeplex.com/CH/People/ProjectPeople.aspx)

 This library is licensed under the GNU Library General Public License (LGPL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://www.codeplex.com/CH/license.
*/

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TvdP.Collections;

namespace ConcurrentHashtableUnitTest
{
    [TestClass]
    public class WeakDictionaryTest
    {
        [TestMethod]
        public void TryAddTest()
        {
            var dictionary = new WeakDictionary<object, int, object>();

            var obj1 = new object();
            var obj2 = new object();
            var obj3 = new object();
            var obj4 = new object();

            Assert.IsTrue(dictionary.TryAdd(obj1, 10, obj2));
            Assert.IsTrue(dictionary.TryAdd(obj1, 11, obj3));
            Assert.IsTrue(dictionary.TryAdd(obj2, 10, obj4));
            Assert.IsFalse(dictionary.TryAdd(obj1, 10, obj2));
            Assert.IsFalse(dictionary.TryAdd(obj1, 11, obj3));
            Assert.IsFalse(dictionary.TryAdd(obj2, 10, obj4));

            Assert.AreEqual(obj2, dictionary[obj1, 10]);
            Assert.AreEqual(obj3, dictionary[obj1, 11]);
            Assert.AreEqual(obj4, dictionary[obj2, 10]);
        }
    }
}
