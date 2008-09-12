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
    /// Search key structure for <see cref="ConcurrentDictionary{TKey,TValue}"/>
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    public struct ConcurrentDictionaryKey<TKey, TValue>
    {
        internal TKey _Key;
        internal TValue _Value;
        internal bool _IgnoreValue;

        internal ConcurrentDictionaryKey(TKey key)
        {
            _Key = key;
            _IgnoreValue = true;
            _Value = default(TValue);
        }

        internal ConcurrentDictionaryKey(TKey key, TValue value)
        {
            _Key = key;
            _IgnoreValue = false ;
            _Value = value;
        }
    }

    /// <summary>
    /// A Concurrent <see cref="IDictionary{TKey,TValue}"/> implementation.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys.</typeparam>
    /// <typeparam name="TValue">Type of the values.</typeparam>
    /// <remarks>
    /// This class is threadsafe and highly concurrent. This means that multiple threads can do lookup and insert operations
    /// on this dictionary simultaneously. 
    /// It is not guaranteed that collisions will not occur. The dictionary is partitioned in segments. A segment contains
    /// a set of items based on a hash of those items. The more segments there are and the beter the hash, the fewer collisions will occur.
    /// This means that a nearly empty ConcurrentDictionary is not as concurrent as one containing many items. 
    /// </remarks>
    public sealed class ConcurrentDictionary<TKey, TValue> : ConcurrentHashtable<KeyValuePair<TKey, TValue>?, ConcurrentDictionaryKey<TKey, TValue>>, IDictionary<TKey, TValue> 
    {
        #region Constructors

        /// <summary>
        /// Constructs a <see cref="ConcurrentDictionary{TKey,TValue}"/> instance using the default <see cref="IEqualityComparer{TKey}"/> to compare keys.
        /// </summary>
        public ConcurrentDictionary()
            : this(EqualityComparer<TKey>.Default)
        { }

        /// <summary>
        /// Constructs a <see cref="ConcurrentDictionary{TKey,TValue}"/> instance using the specified <see cref="IEqualityComparer{TKey}"/> to compare keys.
        /// </summary>
        /// <param name="comparer">The <see cref="IEqualityComparer{TKey}"/> tp compare keys with.</param>
        public ConcurrentDictionary(IEqualityComparer<TKey> comparer)
            : base()
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            _Comparer = comparer;

            Initialize();
        }

        #endregion

        #region Traits

        readonly IEqualityComparer<TKey> _Comparer;

        /// <summary>
        /// Get a hashcode for given storeable item.
        /// </summary>
        /// <param name="item">Reference to the item to get a hash value for.</param>
        /// <returns>The hash value as an <see cref="UInt32"/>.</returns>
        /// <remarks>
        /// The hash returned should be properly randomized hash. The standard GetHashCode methods are usually not good enough.
        /// A storeable item and a matching search key should return the same hash code.
        /// So the statement <code>Equals(storeableItem, searchKey) ? GetHashCode(storeableItem) == GetHashCode(searchKey) : true </code> should always be true;
        /// </remarks>
        internal protected override UInt32 GetHashCode(ref KeyValuePair<TKey, TValue>? item)
        { return item.HasValue ? Hasher.Rehash(_Comparer.GetHashCode(item.Value.Key)) : 0; }

        /// <summary>
        /// Get a hashcode for given search key.
        /// </summary>
        /// <param name="key">Reference to the key to get a hash value for.</param>
        /// <returns>The hash value as an <see cref="UInt32"/>.</returns>
        /// <remarks>
        /// The hash returned should be properly randomized hash. The standard GetHashCode methods are usually not good enough.
        /// A storeable item and a matching search key should return the same hash code.
        /// So the statement <code>Equals(storeableItem, searchKey) ? GetHashCode(storeableItem) == GetHashCode(searchKey) : true </code> should always be true;
        /// </remarks>
        internal protected override UInt32 GetHashCode(ref ConcurrentDictionaryKey<TKey, TValue> key)
        { return Hasher.Rehash(_Comparer.GetHashCode(key._Key)); }

        /// <summary>
        /// Compares a storeable item to a search key. Should return true if they match.
        /// </summary>
        /// <param name="item">Reference to the storeable item to compare.</param>
        /// <param name="key">Reference to the search key to compare.</param>
        /// <returns>True if the storeable item and search key match; false otherwise.</returns>
        internal protected override bool Equals(ref KeyValuePair<TKey, TValue>? item, ref ConcurrentDictionaryKey<TKey, TValue> key)
        { return item.HasValue && _Comparer.Equals(item.Value.Key, key._Key) && (key._IgnoreValue || EqualityComparer<TValue>.Default.Equals(item.Value.Value, key._Value)); }

        /// <summary>
        /// Compares two storeable items for equality.
        /// </summary>
        /// <param name="item1">Reference to the first storeable item to compare.</param>
        /// <param name="item2">Reference to the second storeable item to compare.</param>
        /// <returns>True if the two soreable items should be regarded as equal.</returns>
        internal protected override bool Equals(ref KeyValuePair<TKey, TValue>? item1, ref KeyValuePair<TKey, TValue>? item2)
        { return item1.HasValue && item2.HasValue && _Comparer.Equals(item1.Value.Key, item2.Value.Key); }

        /// <summary>
        /// Indicates if a specific item reference contains a valid item.
        /// </summary>
        /// <param name="item">The storeable item reference to check.</param>
        /// <returns>True if the reference doesn't refer to a valid item; false otherwise.</returns>
        /// <remarks>The statement <code>IsEmpty(default(TStoredI))</code> should always be true.</remarks>
        internal protected override bool IsEmpty(ref KeyValuePair<TKey, TValue>? item)
        { return !item.HasValue; }

        #endregion

        #region IDictionary<TKey,TValue> Members

        /// <summary>
        /// Adds an element with the provided key and value to the dictionary.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="ArgumentException">An element with the same key already exists in the dictionary.</exception>
        public void Add(TKey key, TValue value)
        { Add( new KeyValuePair<TKey,TValue>(key,value) ); }

        /// <summary>
        /// Determines whether the dictionary
        /// contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the dictionary.</param>
        /// <returns>true if the dictionary contains
        /// an element with the key; otherwise, false.</returns>
        public bool ContainsKey(TKey key)
        {
            KeyValuePair<TKey,TValue>? presentItem;
            ConcurrentDictionaryKey<TKey, TValue> searchKey = new ConcurrentDictionaryKey<TKey, TValue>(key);
            return FindItem(ref searchKey, out presentItem);
        }

        /// <summary>
        /// Gets an <see cref="ICollection{TKey}"/>  containing the keys of
        /// the dictionary.           
        /// </summary>
        /// <returns>An <see cref="ICollection{TKey}"/> containing the keys of the dictionary.</returns>
        /// <remarks>This property takes a snapshot of the current keys collection of the dictionary at the moment of invocation.</remarks>
        public ICollection<TKey> Keys
        {
            get 
            {
                lock (SyncRoot)
                    return base.Items.Select(kvp => kvp.Value.Key).ToList();
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>true if the element is successfully removed; otherwise, false. This method
        /// also returns false if key was not found in the original dictionary.</returns>
        public bool Remove(TKey key)
        {
            KeyValuePair<TKey, TValue>? oldItem;
            ConcurrentDictionaryKey<TKey,TValue> searchKey = new ConcurrentDictionaryKey<TKey,TValue>(key);
            return base.RemoveItem(ref searchKey, out oldItem);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">
        /// When this method returns, the value associated with the specified key, if
        /// the key is found; otherwise, the default value for the type of the value
        /// parameter. This parameter is passed uninitialized.
        ///</param>
        /// <returns>
        /// true if the dictionary contains an element with the specified key; otherwise, false.
        /// </returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            KeyValuePair<TKey, TValue>? presentItem;
            ConcurrentDictionaryKey<TKey, TValue> searchKey = new ConcurrentDictionaryKey<TKey, TValue>(key);

            var res = FindItem(ref searchKey, out presentItem);

            if (res)
            {
                value = presentItem.Value.Value;
                return true;
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }

        /// <summary>
        /// Gets an <see cref="ICollection{TKey}"/> containing the values in
        ///     the dictionary.
        /// </summary>
        /// <returns>
        /// An <see cref="ICollection{TKey}"/> containing the values in the dictionary.
        /// </returns>
        /// <remarks>This property takes a snapshot of the current keys collection of the dictionary at the moment of invocation.</remarks>
        public ICollection<TValue> Values
        {
            get 
            {
                lock (SyncRoot)
                    return base.Items.Select(kvp => kvp.Value.Value).ToList();
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                KeyValuePair<TKey, TValue>? presentItem;
                ConcurrentDictionaryKey<TKey, TValue> searchKey = new ConcurrentDictionaryKey<TKey, TValue>(key);

                if (!FindItem(ref searchKey, out presentItem))
                    throw new KeyNotFoundException("The property is retrieved and key is not found.");
                return presentItem.Value.Value;
            }
            set
            {
                KeyValuePair<TKey, TValue>? newItem = new KeyValuePair<TKey, TValue>(key, value);
                KeyValuePair<TKey, TValue>? presentItem;
                InsertItem(ref newItem, out presentItem);
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            KeyValuePair<TKey, TValue>? newItem = item;
            KeyValuePair<TKey, TValue>? presentItem;

            if (GetOldestItem(ref newItem, out presentItem))
                throw new ArgumentException("An element with the same key already exists.");
        }

        public new void Clear()
        { base.Clear(); }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            KeyValuePair<TKey, TValue>? presentItem;
            ConcurrentDictionaryKey<TKey, TValue> searchKey = new ConcurrentDictionaryKey<TKey, TValue>(item.Key,item.Value);

            return
                FindItem(ref searchKey, out presentItem);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            lock (SyncRoot)
                foreach (var item in Items)
                    array[arrayIndex++] = item.Value;
        }

        public new int Count
        { get { return base.Count; } }

        public bool IsReadOnly
        { get { return false; } }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            KeyValuePair<TKey, TValue>? oldItem;
            ConcurrentDictionaryKey<TKey, TValue> searchKey = new ConcurrentDictionaryKey<TKey, TValue>(item.Key,item.Value);
            return base.RemoveItem(ref searchKey, out oldItem);
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            lock (SyncRoot)
                return Items.Select(nkvp => nkvp.Value).ToList().GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return GetEnumerator(); }

        #endregion
    }
}
