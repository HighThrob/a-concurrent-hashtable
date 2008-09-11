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
    /// <summary>
    /// Entry item type for <see cref="WeakDictionaryStrongKeys{TKey,TValue}"/>. 
    /// </summary>
    /// <typeparam name="TKey">Type of keys of the <see cref="WeakDictionaryStrongKeys{TKey,TValue}"/>.</typeparam>
    public struct ConcurrentWeakDictionaryStrongKeysItem<TKey>
    {
        internal UInt32 _Hash;
        internal TKey _Key;
        internal WeakReference _Value;

        internal ConcurrentWeakDictionaryStrongKeysItem(UInt32 hash, TKey key, WeakReference value)
        {
            _Hash = hash;
            _Key = key;
            _Value = value;
        }
    }

    /// <summary>
    /// Search key for <see cref="WeakDictionaryStrongKeys{TKey,TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of keys of the <see cref="WeakDictionaryStrongKeys{TKey,TValue}"/>.</typeparam>
    public struct ConcurrentWeakDictionaryStrongKeysKey<TKey>
    {
        internal UInt32 _Hash;
        internal TKey _Key;

        internal ConcurrentWeakDictionaryStrongKeysKey(UInt32 hash, TKey key)
        {
            _Hash = hash;
            _Key = key;
        }
    }


    /// <summary>
    /// A dictionary that has weakreferences to it's values. If a value gets garbage collected
    /// then the entry will be removed from the dictionary. 
    /// </summary>
    /// <typeparam name="TKey">Type of the keys.</typeparam>
    /// <typeparam name="TValue">Type of the values. This must be a reference type.</typeparam>
    public sealed class ConcurrentWeakDictionaryStrongKeys<TKey, TValue> : ConcurrentWeakHashtable<ConcurrentWeakDictionaryStrongKeysItem<TKey>, ConcurrentWeakDictionaryStrongKeysKey<TKey>>
        where TValue : class
    {
        #region Constructors

        public ConcurrentWeakDictionaryStrongKeys()
            : this(EqualityComparer<TKey>.Default)
        { }

        public ConcurrentWeakDictionaryStrongKeys(IEqualityComparer<TKey> comparer)
            : base()
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            _Comparer = comparer;

            Initialize();
        }

        #endregion

        #region Traits

        internal protected override UInt32 GetHashCode(ref ConcurrentWeakDictionaryStrongKeysItem<TKey> item)
        { return item._Hash; }

        internal protected override UInt32 GetHashCode(ref ConcurrentWeakDictionaryStrongKeysKey<TKey> key)
        { return key._Hash; }

        internal protected override bool Equals(ref ConcurrentWeakDictionaryStrongKeysItem<TKey> item, ref ConcurrentWeakDictionaryStrongKeysKey<TKey> key)
        { return _Comparer.Equals(item._Key, key._Key); }

        internal protected override bool Equals(ref ConcurrentWeakDictionaryStrongKeysItem<TKey> item1, ref ConcurrentWeakDictionaryStrongKeysItem<TKey> item2)
        { return _Comparer.Equals(item1._Key, item2._Key); }

        internal protected override bool IsEmpty(ref ConcurrentWeakDictionaryStrongKeysItem<TKey> item)
        { return item._Value == null; }

        internal protected override bool IsGarbage(ref ConcurrentWeakDictionaryStrongKeysItem<TKey> item)
        { return item._Value != null && item._Value.Target == null; }

        internal protected override ConcurrentWeakDictionaryStrongKeysItem<TKey> EmptyItem
        { get { return default(ConcurrentWeakDictionaryStrongKeysItem<TKey>); } }

        #endregion

        public IEqualityComparer<TKey> _Comparer;

        UInt32 GetHashCode(TKey key)
        { return Hasher.Rehash(_Comparer.GetHashCode(key)); }

        #region Public accessors


        /// <summary>
        /// Add
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Insert(TKey key, TValue value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            var item = new ConcurrentWeakDictionaryStrongKeysItem<TKey>(GetHashCode(key), key, new WeakReference(value));
            ConcurrentWeakDictionaryStrongKeysItem<TKey> oldItem;
            base.InsertItem(ref item, out oldItem);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public TValue GetOldest(TKey key, TValue newValue)
        {
            if (newValue == null)
                throw new ArgumentNullException("newValue");

            var item = new ConcurrentWeakDictionaryStrongKeysItem<TKey>(GetHashCode(key), key, new WeakReference(newValue));
            ConcurrentWeakDictionaryStrongKeysItem<TKey> oldItem;
            TValue res;

            do
            {
                base.GetOldestItem(ref item, out oldItem);

                res = (TValue)oldItem._Value.Target;
            }
            while (res == null);

            return res;
        }

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="key"></param>
        public void Remove(TKey key)
        {
            var item = new ConcurrentWeakDictionaryStrongKeysKey<TKey>(GetHashCode(key), key);
            ConcurrentWeakDictionaryStrongKeysItem<TKey> oldItem;

            base.RemoveItem(ref item, out oldItem);
        }

        /// <summary>
        /// TryGetValue
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            var item = new ConcurrentWeakDictionaryStrongKeysKey<TKey>(GetHashCode(key), key);
            ConcurrentWeakDictionaryStrongKeysItem<TKey> oldItem;

            if (base.FindItem(ref item, out oldItem))
            {
                value = (TValue)oldItem._Value.Target;
                return value != null;
            }

            value = null;
            return false;
        }


        /// <summary>
        /// TryPopValue
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryPopValue(TKey key, out TValue value)
        {
            var item = new ConcurrentWeakDictionaryStrongKeysKey<TKey>(GetHashCode(key), key);
            ConcurrentWeakDictionaryStrongKeysItem<TKey> oldItem;

            if (base.RemoveItem(ref item, out oldItem))
            {
                value = (TValue)oldItem._Value.Target;
                return value != null;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Get value
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get
            {
                TValue res;
                TryGetValue(key, out res);
                return res;
            }
            set
            { Insert(key, value); }
        }

        /// <summary>
        /// GetCurrentValues
        /// </summary>
        public TValue[] GetCurrentValues()
        {
            lock (SyncRoot)
                return
                    Items
                    .Select(item => (TValue)item._Value.Target)
                    .Where(v => v != null)
                    .ToArray();
        }

        /// <summary>
        /// GetCurrentKeys
        /// </summary>
        public TKey[] GetCurrentKeys()
        {
            var comparer = _Comparer;
            lock (SyncRoot)
                return
                    Items
                    .Select(item => item._Key)
                    .ToArray();
        }

        /// <summary>
        /// Clear
        /// </summary>
        public new void Clear()
        { base.Clear(); }

        #endregion
    }
}
