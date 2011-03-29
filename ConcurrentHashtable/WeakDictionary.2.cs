﻿/*  
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
using System.Collections;
using System.Runtime.Serialization;
using System.Security;

namespace TvdP.Collections
{
#if !SILVERLIGHT
    [Serializable]
#endif
    public class WeakDictionary<TWeakKey1, TWeakKey2, TStrongKey, TValue> : DictionaryBase<Tuple<TWeakKey1, TWeakKey2, TStrongKey>, TValue>
#if !SILVERLIGHT
    , ISerializable
#endif
        where TWeakKey1 : class
        where TWeakKey2 : class
        where TValue : class
    {
        class InternalWeakDictionary :
            InternalWeakDictionaryWeakValueBase<
                Key<TWeakKey1, TWeakKey2, TStrongKey>, 
                Tuple<TWeakKey1, TWeakKey2, TStrongKey>, 
                TValue, 
                HeapType<TWeakKey1, TWeakKey2, TStrongKey>
            >
        {
            public InternalWeakDictionary(int concurrencyLevel, int capacity, KeyComparer<TWeakKey1, TWeakKey2, TStrongKey> keyComparer)
                : base(concurrencyLevel, capacity, keyComparer)
            { _comparer = keyComparer; }

            public InternalWeakDictionary(KeyComparer<TWeakKey1, TWeakKey2, TStrongKey> keyComparer)
                : base(keyComparer)
            { _comparer = keyComparer; }

            public KeyComparer<TWeakKey1, TWeakKey2, TStrongKey> _comparer;

            protected override Key<TWeakKey1, TWeakKey2, TStrongKey> FromExternalKeyToSearchKey(Tuple<TWeakKey1, TWeakKey2, TStrongKey> externalKey)
            { return new SearchKey<TWeakKey1, TWeakKey2, TStrongKey>().Set(externalKey, _comparer); }

            protected override Key<TWeakKey1, TWeakKey2, TStrongKey> FromExternalKeyToStorageKey(Tuple<TWeakKey1, TWeakKey2, TStrongKey> externalKey)
            { return new StorageKey<TWeakKey1, TWeakKey2, TStrongKey>().Set(externalKey, _comparer); }

            protected override Key<TWeakKey1, TWeakKey2, TStrongKey> FromHeapKeyToSearchKey(HeapType<TWeakKey1, TWeakKey2, TStrongKey> externalKey)
            { return new SearchKey<TWeakKey1, TWeakKey2, TStrongKey>().Set(externalKey, _comparer); }

            protected override Key<TWeakKey1, TWeakKey2, TStrongKey> FromHeapKeyToStorageKey(HeapType<TWeakKey1, TWeakKey2, TStrongKey> externalKey)
            { return new StorageKey<TWeakKey1, TWeakKey2, TStrongKey>().Set(externalKey, _comparer); }

            protected override bool FromInternalKeyToExternalKey(Key<TWeakKey1, TWeakKey2, TStrongKey> internalKey, out Tuple<TWeakKey1, TWeakKey2, TStrongKey> externalKey)
            { return internalKey.Get(out externalKey); }

            protected override bool FromInternalKeyToHeapKey(Key<TWeakKey1, TWeakKey2, TStrongKey> internalKey, out HeapType<TWeakKey1, TWeakKey2, TStrongKey> externalKey)
            { return internalKey.Get(out externalKey); }
        }

        readonly InternalWeakDictionary _internalDictionary;

        protected override IDictionary<Tuple<TWeakKey1, TWeakKey2, TStrongKey>, TValue> InternalDictionary
        { get { return _internalDictionary; } }

#if !SILVERLIGHT
        WeakDictionary(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            var comparer = (KeyComparer<TWeakKey1, TWeakKey2, TStrongKey>)serializationInfo.GetValue("Comparer", typeof(KeyComparer<TWeakKey1, TWeakKey2, TStrongKey>));
            var items = (List<KeyValuePair<Tuple<TWeakKey1, TWeakKey2, TStrongKey>, TValue>>)serializationInfo.GetValue("Items", typeof(List<KeyValuePair<Tuple<TWeakKey1, TWeakKey2, TStrongKey>, TValue>>));
            _internalDictionary = new InternalWeakDictionary(comparer);
            _internalDictionary.InsertContents(items);
        }

        #region ISerializable Members

        [SecurityCritical]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Comparer", _internalDictionary._comparer);
            info.AddValue("Items", _internalDictionary.GetContents());
        }
        #endregion
#endif

        public WeakDictionary()
            : this(EqualityComparer<TWeakKey1>.Default, EqualityComparer<TWeakKey2>.Default, EqualityComparer<TStrongKey>.Default)
        {}

        public WeakDictionary(IEqualityComparer<TWeakKey1> weakKey1Comparer, IEqualityComparer<TWeakKey2> weakKey2Comparer, IEqualityComparer<TStrongKey> strongKeyComparer)
            : this(Enumerable.Empty<KeyValuePair<Tuple<TWeakKey1, TWeakKey2, TStrongKey>, TValue>>(), weakKey1Comparer, weakKey2Comparer, strongKeyComparer)
        {}

        public WeakDictionary(IEnumerable<KeyValuePair<Tuple<TWeakKey1, TWeakKey2, TStrongKey>, TValue>> collection)
            : this(collection, EqualityComparer<TWeakKey1>.Default, EqualityComparer<TWeakKey2>.Default, EqualityComparer<TStrongKey>.Default)
        {}

        public WeakDictionary(IEnumerable<KeyValuePair<Tuple<TWeakKey1, TWeakKey2, TStrongKey>, TValue>> collection, IEqualityComparer<TWeakKey1> weakKey1Comparer, IEqualityComparer<TWeakKey2> weakKey2Comparer, IEqualityComparer<TStrongKey> strongKeyComparer)
        {
            _internalDictionary = 
                new InternalWeakDictionary(
                    new KeyComparer<TWeakKey1, TWeakKey2, TStrongKey>(weakKey1Comparer, weakKey2Comparer, strongKeyComparer)
                )
            ;

            _internalDictionary.InsertContents(collection);
        }

        public WeakDictionary(int concurrencyLevel, int capacity)
            : this(concurrencyLevel, capacity, EqualityComparer<TWeakKey1>.Default, EqualityComparer<TWeakKey2>.Default, EqualityComparer<TStrongKey>.Default)
        {}

        public WeakDictionary(int concurrencyLevel, IEnumerable<KeyValuePair<Tuple<TWeakKey1, TWeakKey2, TStrongKey>, TValue>> collection, IEqualityComparer<TWeakKey1> weakKey1Comparer, IEqualityComparer<TWeakKey2> weakKey2Comparer, IEqualityComparer<TStrongKey> strongKeyComparer)
        {
            var contentsList = collection.ToList();
            _internalDictionary =
                new InternalWeakDictionary(
                    concurrencyLevel,
                    contentsList.Count,
                    new KeyComparer<TWeakKey1, TWeakKey2, TStrongKey>(weakKey1Comparer, weakKey2Comparer, strongKeyComparer)
                )
            ;
            _internalDictionary.InsertContents(contentsList);
        }

        public WeakDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TWeakKey1> weakKey1Comparer, IEqualityComparer<TWeakKey2> weakKey2Comparer, IEqualityComparer<TStrongKey> strongKeyComparer)
        {
            _internalDictionary =
                new InternalWeakDictionary(
                    concurrencyLevel,
                    capacity,
                    new KeyComparer<TWeakKey1, TWeakKey2, TStrongKey>(weakKey1Comparer, weakKey2Comparer, strongKeyComparer)
                )
            ;
        }


        public bool ContainsKey(TWeakKey1 weakKey1, TWeakKey2 weakKey2, TStrongKey strongKey)
        { return _internalDictionary.ContainsKey(HeapType.Create(weakKey1, weakKey2, strongKey)); }

        public bool TryGetValue(TWeakKey1 weakKey1, TWeakKey2 weakKey2, TStrongKey strongKey, out TValue value)
        { return _internalDictionary.TryGetValue(HeapType.Create(weakKey1, weakKey2, strongKey), out value); }

        public TValue this[TWeakKey1 weakKey1, TWeakKey2 weakKey2, TStrongKey strongKey]
        {
            get { return _internalDictionary.GetItem(HeapType.Create(weakKey1, weakKey2, strongKey)); }
            set { _internalDictionary.SetItem(HeapType.Create(weakKey1, weakKey2, strongKey), value); }
        }

        public bool IsEmpty
        { get { return _internalDictionary.IsEmpty; } }

        public TValue AddOrUpdate(TWeakKey1 weakKey1, TWeakKey2 weakKey2, TStrongKey strongKey, Func<TWeakKey1, TWeakKey2, TStrongKey, TValue> addValueFactory, Func<TWeakKey1, TWeakKey2, TStrongKey, TValue, TValue> updateValueFactory)
        {
            if (null == addValueFactory)
                throw new ArgumentNullException("addValueFactory");

            if (null == updateValueFactory)
                throw new ArgumentNullException("updateValueFactory");

            return
                _internalDictionary.AddOrUpdate(
                    HeapType.Create(weakKey1, weakKey2, strongKey), 
                    hr => addValueFactory(hr.Item1, hr.Item2, hr.Item3), 
                    (hr, v) => updateValueFactory(hr.Item1, hr.Item2, hr.Item3, v)
                )
            ;
        }

        public TValue AddOrUpdate(TWeakKey1 weakKey1, TWeakKey2 weakKey2, TStrongKey strongKey, TValue addValue, Func<TWeakKey1, TWeakKey2, TStrongKey, TValue, TValue> updateValueFactory)
        {
            if (null == updateValueFactory)
                throw new ArgumentNullException("updateValueFactory");

            return
                _internalDictionary.AddOrUpdate(
                    HeapType.Create(weakKey1, weakKey2, strongKey),
                    addValue,
                    (hr, v) => updateValueFactory(hr.Item1, hr.Item2, hr.Item3, v)
                )
            ;
        }

        public TValue GetOrAdd(TWeakKey1 weakKey1, TWeakKey2 weakKey2, TStrongKey strongKey, TValue value)
        { return _internalDictionary.GetOrAdd(HeapType.Create(weakKey1, weakKey2, strongKey), value); }

        public TValue GetOrAdd(TWeakKey1 weakKey1, TWeakKey2 weakKey2, TStrongKey strongKey, Func<TWeakKey1, TWeakKey2, TStrongKey, TValue> valueFactory)
        {
            if (null == valueFactory)
                throw new ArgumentNullException("valueFactory");

            return _internalDictionary.GetOrAdd(HeapType.Create(weakKey1, weakKey2, strongKey), hr => valueFactory(hr.Item1, hr.Item2, hr.Item3));
        }
        
        public KeyValuePair<Tuple<TWeakKey1, TWeakKey2, TStrongKey>, TValue>[] ToArray()
        { return _internalDictionary.ToArray(); }

        public bool TryAdd(TWeakKey1 weakKey1, TWeakKey2 weakKey2, TStrongKey strongKey, TValue value)
        { return _internalDictionary.TryAdd(HeapType.Create(weakKey1, weakKey2, strongKey), value); }

        public bool TryRemove(TWeakKey1 weakKey1, TWeakKey2 weakKey2, TStrongKey strongKey, out TValue value)
        { return _internalDictionary.TryRemove(HeapType.Create(weakKey1, weakKey2, strongKey), out value); }

        public bool TryUpdate(TWeakKey1 weakKey1, TWeakKey2 weakKey2, TStrongKey strongKey, TValue newValue, TValue comparisonValue)
        { return _internalDictionary.TryUpdate(HeapType.Create(weakKey1, weakKey2, strongKey), newValue, comparisonValue ); }
    }
}