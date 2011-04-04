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

#if !SILVERLIGHT
using System.Collections.Concurrent;
#endif

namespace TvdP.Collections
{
    internal abstract class InternalWeakDictionaryStrongValueBase<IK, EK, EV, HK> : ConcurrentDictionary<IK, EV>, IMaintainable, IDictionary<EK, EV>, ICollection<KeyValuePair<EK, EV>>, IEnumerable<KeyValuePair<EK, EV>>
        where IK : ITrashable
        where HK : struct
    {
        protected InternalWeakDictionaryStrongValueBase(int concurrencyLevel, int capacity, IEqualityComparer<IK> keyComparer)
#if SILVERLIGHT
            : base(keyComparer)
#else
            : base(concurrencyLevel, capacity, keyComparer)
#endif
        { MaintenanceWorker.Register(this); }

        protected InternalWeakDictionaryStrongValueBase(IEqualityComparer<IK> keyComparer)
            : base(keyComparer)
        { MaintenanceWorker.Register(this); }

        protected abstract IK FromExternalKeyToSearchKey(EK externalKey);
        protected abstract IK FromExternalKeyToStorageKey(EK externalKey);
        protected abstract IK FromHeapKeyToSearchKey(HK externalKey);
        protected abstract IK FromHeapKeyToStorageKey(HK externalKey);
        protected abstract bool FromInternalKeyToExternalKey(IK internalKey, out EK externalKey);
        protected abstract bool FromInternalKeyToHeapKey(IK internalKey, out HK externalKey);

        #region IMaintainable Members

        void IMaintainable.DoMaintenance()
        {
            foreach (var kvp in (IEnumerable<KeyValuePair<IK, EV>>)this)
                if (kvp.Key.IsGarbage)
                {
                    EV value;
                    base.TryRemove(kvp.Key, out value);
                }
        }

        #endregion

        #region IDictionary<Tuple<TWeakKey1,TStrongKey>,TValue> Members

        void IDictionary<EK, EV>.Add(EK key, EV value)
        { ((IDictionary<IK, EV>)this).Add(FromExternalKeyToStorageKey(key), value); }

        bool IDictionary<EK, EV>.ContainsKey(EK key)
        { return ((IDictionary<IK, EV>)this).ContainsKey(FromExternalKeyToSearchKey(key)); }

        ICollection<EK> IDictionary<EK, EV>.Keys
        { get { return new TransformedCollection<EK> { _source = ((IEnumerable<KeyValuePair<EK, EV>>)this).Select(kvp => kvp.Key) }; } }

        bool IDictionary<EK, EV>.Remove(EK key)
        { return ((IDictionary<IK, EV>)this).Remove(FromExternalKeyToSearchKey(key)); }

        bool IDictionary<EK, EV>.TryGetValue(EK key, out EV value)
        { return base.TryGetValue(FromExternalKeyToSearchKey(key), out value); }

        ICollection<EV> IDictionary<EK, EV>.Values
        { get { return new TransformedCollection<EV> { _source = ((IEnumerable<KeyValuePair<EK, EV>>)this).Select(kvp => kvp.Value) }; } }

        EV IDictionary<EK, EV>.this[EK key]
        {
            get { return ((IDictionary<IK, EV>)this)[FromExternalKeyToSearchKey(key)]; }
            set { ((IDictionary<IK, EV>)this)[FromExternalKeyToStorageKey(key)] = value; }
        }

        #endregion

        #region ICollection<KeyValuePair<Tuple<TWeakKey1,TStrongKey>,TValue>> Members

        void ICollection<KeyValuePair<EK, EV>>.Add(KeyValuePair<EK, EV> item)
        { ((IDictionary<EK, EV>)this).Add(item.Key, item.Value); }

        void ICollection<KeyValuePair<EK, EV>>.Clear()
        { base.Clear(); }

        bool ICollection<KeyValuePair<EK, EV>>.Contains(KeyValuePair<EK, EV> item)
        { return ((IDictionary<IK, EV>)this).Contains(new KeyValuePair<IK, EV>(FromExternalKeyToSearchKey(item.Key), item.Value)); }

        //HIERO:

        void ICollection<KeyValuePair<EK, EV>>.CopyTo(KeyValuePair<EK, EV>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new IndexOutOfRangeException();

            int i = 0;
            int end = array.Length - arrayIndex;
            var buffer = new KeyValuePair<EK, EV>[end];

            using (var it = ((IEnumerable<KeyValuePair<EK, EV>>)this).GetEnumerator())
                while (it.MoveNext())
                {
                    if (i == end)
                        throw new ArgumentException();

                    buffer[i++] = it.Current;
                }

            buffer.CopyTo(array, arrayIndex);
        }

        int ICollection<KeyValuePair<EK, EV>>.Count
        {
            get
            {
                int ct = 0;

                using (var it = ((IEnumerable<KeyValuePair<EK, EV>>)this).GetEnumerator())
                    while (it.MoveNext())
                        ++ct;

                return ct;
            }
        }

        bool ICollection<KeyValuePair<EK, EV>>.IsReadOnly
        { get { return false; } }

        bool ICollection<KeyValuePair<EK, EV>>.Remove(KeyValuePair<EK, EV> item)
        {
            return
                ((IDictionary<IK, EV>)this)
                    .Remove(
                        new KeyValuePair<IK, EV>(
                            FromExternalKeyToSearchKey(item.Key),
                            item.Value
                        )
                    )
            ;
        }

        #endregion

        #region IEnumerable<KeyValuePair<EK, EV>> Members

        public new IEnumerator<KeyValuePair<EK, EV>> GetEnumerator()
        {
            foreach (var kvp in (IEnumerable<KeyValuePair<IK, EV>>)this)
            {
                EK externalKey;

                if (
                    FromInternalKeyToExternalKey(kvp.Key, out externalKey)
                )
                    yield return new KeyValuePair<EK, EV>(externalKey, kvp.Value);
                else
                    //boyscout
                    ((ICollection<KeyValuePair<IK, EV>>)this).Remove(kvp);
            }
        }

        #endregion

        public bool ContainsKey(HK key)
        { return ((IDictionary<IK, EV>)this).ContainsKey(FromHeapKeyToSearchKey(key)); }

        public bool TryGetValue(HK key, out EV value)
        { return ((IDictionary<IK, EV>)this).TryGetValue(FromHeapKeyToSearchKey(key), out value); }

        public EV GetItem(HK key)
        { return ((IDictionary<IK, EV>)this)[FromHeapKeyToStorageKey(key)]; }

        public void SetItem(HK key, EV value)
        { ((IDictionary<IK, EV>)this)[FromHeapKeyToStorageKey(key)] = value; }

        public new bool IsEmpty
        { get { return !((ICollection<KeyValuePair<EV, EK>>)this).GetEnumerator().MoveNext(); } }

        public EV AddOrUpdate(HK key, Func<HK, EV> addValueFactory, Func<HK, EV, EV> updateValueFactory)
        {
            return
                base.AddOrUpdate(
                    FromHeapKeyToStorageKey(key),
                    sKey => addValueFactory(key),
                    (sKey, oldValue) =>
                    {
                        HK oldKey;

                        if (FromInternalKeyToHeapKey(sKey, out oldKey))
                            return updateValueFactory(oldKey, oldValue);
                        else
                        {
                            //boyscout
                            ((ICollection<KeyValuePair<IK, EV>>)this).Remove(new KeyValuePair<IK, EV>(sKey, oldValue));
                            return addValueFactory(key);
                        }
                    }
                )
            ;
        }

        public EV AddOrUpdate(HK key, EV addValue, Func<HK, EV, EV> updateValueFactory)
        {
            return
                AddOrUpdate(
                    FromHeapKeyToStorageKey(key),
                    addValue,
                    (sKey, oldValue) =>
                    {
                        HK oldKey;

                        if (FromInternalKeyToHeapKey(sKey, out oldKey))
                            return updateValueFactory(oldKey, oldValue);
                        else
                        {
                            //boyscout
                            ((ICollection<KeyValuePair<IK, EV>>)this).Remove(new KeyValuePair<IK, EV>(sKey, oldValue));
                            return addValue;
                        }
                    }
                )
            ;
        }

        public EV GetOrAdd(HK key, EV value)
        { return base.GetOrAdd(FromHeapKeyToStorageKey(key), value); }

        public EV GetOrAdd(HK key, Func<HK, EV> valueFactory)
        {
            EV hold;
            return this.TryGetValue(key, out hold) ? hold : GetOrAdd(key, valueFactory(key));
        }

        public new KeyValuePair<EK, EV>[] ToArray()
        { return ((IEnumerable<KeyValuePair<EK, EV>>)this).ToArray(); }

        public bool TryAdd(HK key, EV value)
        { return base.TryAdd(FromHeapKeyToStorageKey(key), value); }

        public bool TryRemove(HK key, out EV value)
        { return base.TryRemove(FromHeapKeyToSearchKey(key), out value); }

        public bool TryUpdate(HK key, EV newValue, EV comparisonValue)
        { return base.TryUpdate(FromHeapKeyToSearchKey(key), newValue, comparisonValue); }

        public List<KeyValuePair<EK, EV>> GetContents()
        { return ((IEnumerable<KeyValuePair<EK, EV>>)this).ToList(); }

        public void InsertContents(IEnumerable<KeyValuePair<EK, EV>> collection)
        {
            if (null == collection)
                throw new ArgumentNullException("collection");

            foreach (var kvp in collection)
                ((IDictionary<EK, EV>)this).Add(kvp);
        }
    }
}