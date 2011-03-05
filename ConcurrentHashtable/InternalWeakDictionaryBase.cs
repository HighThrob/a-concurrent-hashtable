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
    internal abstract class InternalWeakDictionaryBase<IK, IV, EK, EV, HK> : ConcurrentDictionary<IK, IV>, IMaintainable, IDictionary<EK, EV>, ICollection<KeyValuePair<EK, EV>>, IEnumerable<KeyValuePair<EK, EV>>
        where IK : ITrashable
        where IV : ITrashable
    {
        protected InternalWeakDictionaryBase(int concurrencyLevel, int capacity, IEqualityComparer<IK> keyComparer)
#if SILVERLIGHT
            : base(keyComparer)
#else
            : base(concurrencyLevel, capacity, keyComparer)
#endif
        { MaintenanceWorker.Register(this); }

        protected InternalWeakDictionaryBase(IEqualityComparer<IK> keyComparer)
            : base(keyComparer)
        { MaintenanceWorker.Register(this); }

        protected abstract IK FromExternalKeyToSearchKey(EK externalKey);
        protected abstract IK FromExternalKeyToStorageKey(EK externalKey);
        protected abstract IK FromHeapKeyToSearchKey(HK externalKey);
        protected abstract IK FromHeapKeyToStorageKey(HK externalKey);
        protected abstract bool FromInternalKeyToExternalKey(IK internalKey, out EK externalKey);
        protected abstract IV FromExternalValueToInternalValue(EV externalValue);
        protected abstract bool FromInternalValueToExternalValue(IV internalValue, out EV externalValue);

        #region IMaintainable Members

        void IMaintainable.DoMaintenance()
        {
            foreach (var kvp in (IEnumerable<KeyValuePair<IK,IV>>)this)
                if (kvp.Key.IsGarbage || kvp.Value.IsGarbage)
                {
                    IV value;
                    this.TryRemove(kvp.Key, out value);
                }
        }

        #endregion

        #region IDictionary<Tuple<TWeakKey1,TStrongKey>,TValue> Members

        void IDictionary<EK, EV>.Add(EK key, EV value)
        {
            ((IDictionary<IK, IV>)this).Add(FromExternalKeyToStorageKey(key), FromExternalValueToInternalValue(value));
        }

        bool IDictionary<EK, EV>.ContainsKey(EK key)
        {
            return ((IDictionary<IK, IV>)this).ContainsKey(FromExternalKeyToSearchKey(key));
        }

        IEnumerable<EK> KeysEnumerable
        {
            get
            {
                foreach (var kvp in (IEnumerable<KeyValuePair<IK, IV>>)this)
                {
                    EK externalKey;
                    EV externalValue;

                    if (
                        FromInternalKeyToExternalKey(kvp.Key, out externalKey)
                        && FromInternalValueToExternalValue(kvp.Value, out externalValue)
                    )
                        yield return externalKey;
                }
            }
        }

        ICollection<EK> IDictionary<EK, EV>.Keys
        {
            get
            {
                return new TransformedCollection<EK> { _source = KeysEnumerable };
            }
        }

        bool IDictionary<EK, EV>.Remove(EK key)
        { return ((IDictionary<IK, IV>)this).Remove(FromExternalKeyToSearchKey(key)); }

        bool IDictionary<EK, EV>.TryGetValue(EK key, out EV value)
        {
            IV internalValue;
            if (((IDictionary<IK, IV>)this).TryGetValue(FromExternalKeyToSearchKey(key), out internalValue))
            {
                return FromInternalValueToExternalValue(internalValue, out value);
            }
            else
            {
                value = default(EV);
                return false;
            }
        }

        IEnumerable<EV> ValuesEnumerable
        {
            get
            {
                foreach (var kvp in (IEnumerable<KeyValuePair<IK, IV>>)this)
                {
                    EK externalKey;
                    EV externalValue;

                    if (
                        FromInternalKeyToExternalKey(kvp.Key, out externalKey)
                        && FromInternalValueToExternalValue(kvp.Value, out externalValue)
                    )
                        yield return externalValue;
                }
            }
        }

        ICollection<EV> IDictionary<EK, EV>.Values
        {
            get { return new TransformedCollection<EV> { _source = ValuesEnumerable }; }
        }

        EV IDictionary<EK, EV>.this[EK key]
        {
            get
            {
                EV externalValue;
                if (!((IDictionary<EK, EV>)this).TryGetValue(key, out externalValue))
                    throw new KeyNotFoundException();
                return externalValue;
            }
            set
            {
                ((IDictionary<IK, IV>)this)[FromExternalKeyToStorageKey(key)]
                    = FromExternalValueToInternalValue(value);
            }
        }

        #endregion

        #region ICollection<KeyValuePair<Tuple<TWeakKey1,TStrongKey>,TValue>> Members

        void ICollection<KeyValuePair<EK, EV>>.Add(KeyValuePair<EK, EV> item)
        {
            ((IDictionary<EK, EV>)this).Add(item.Key, item.Value);
        }

        void ICollection<KeyValuePair<EK, EV>>.Clear()
        { Clear(); }

        bool ICollection<KeyValuePair<EK, EV>>.Contains(KeyValuePair<EK, EV> item)
        {
            EV value;

            if (((IDictionary<EK, EV>)this).TryGetValue(item.Key, out value))
                return EqualityComparer<EV>.Default.Equals(value, item.Value);

            return false;
        }

        void ICollection<KeyValuePair<EK, EV>>.CopyTo(KeyValuePair<EK, EV>[] array, int arrayIndex)
        {
            foreach (var kvp in (IEnumerable<KeyValuePair<EK, EV>>)this)
                array[arrayIndex++] = kvp;
        }

        int ICollection<KeyValuePair<EK, EV>>.Count
        { get { return ((IEnumerable<KeyValuePair<EV, EK>>)this).Count(); } }

        bool ICollection<KeyValuePair<EK, EV>>.IsReadOnly
        { get { return false; } }

        bool ICollection<KeyValuePair<EK, EV>>.Remove(KeyValuePair<EK, EV> item)
        {
            return
                ((IDictionary<IK, IV>)this)
                    .Remove(
                        new KeyValuePair<IK, IV>(
                            FromExternalKeyToSearchKey(item.Key),
                            FromExternalValueToInternalValue(item.Value)
                        )
                    )
            ;
        }

        #endregion

        #region IEnumerable<KeyValuePair<Tuple<TWeakKey1,TStrongKey>,TValue>> Members

        public IEnumerator<KeyValuePair<EK, EV>> GetEnumerator()
        {
            foreach (var kvp in (IEnumerable<KeyValuePair<IK, IV>>)this)
            {
                EK externalKey;
                EV externalValue;

                if (
                    FromInternalKeyToExternalKey(kvp.Key, out externalKey)
                    && FromInternalValueToExternalValue(kvp.Value, out externalValue)
                )
                    yield return new KeyValuePair<EK, EV>(externalKey, externalValue);
            }
        }

        #endregion

        public bool ContainsKey(HK key)
        {
            return ((IDictionary<IK, IV>)this).ContainsKey(FromHeapKeyToSearchKey(key));
        }

        public bool TryGetValue(HK key, out EV value)
        {
            IV itm;

            if (((IDictionary<IK, IV>)this).TryGetValue(FromHeapKeyToSearchKey(key), out itm))
                return FromInternalValueToExternalValue(itm, out value);

            value = default(EV);
            return false;
        }

        public EV GetItem(HK key)
        {
            EV externalValue;
            if (!TryGetValue(key, out externalValue))
                throw new KeyNotFoundException();
            return externalValue;
        }

        public void SetItem(HK key, EV value)
        {
            ((IDictionary<IK, IV>)this)[FromHeapKeyToStorageKey(key)]
                = FromExternalValueToInternalValue(value);
        }

        public bool IsEmpty
        { get { return ((ICollection<KeyValuePair<EV, EK>>)this).Count == 0; } }

        public EV AddOrUpdate(HK key, Func<HK, EV> addValueFactory, Func<HK, EV, EV> updateValueFactory)
        {
            EV hold = default(EV);

            FromInternalValueToExternalValue(
            base.AddOrUpdate(
                    FromHeapKeyToStorageKey(key),
                    sKey => FromExternalValueToInternalValue(hold = addValueFactory(key)),
                    (sKey, oldItm) =>
                    {
                        EV oldValue;
                        return
                            FromExternalValueToInternalValue(
                                FromInternalValueToExternalValue(oldItm, out oldValue) ? updateValueFactory(key, oldValue) :
                                addValueFactory(key)
                            )
                        ;
                    }
                ),
                out hold
            );


            return hold;
        }

        public EV AddOrUpdate(HK key, EV addValue, Func<HK, EV, EV> updateValueFactory)
        {
            EV hold = default(EV);

            FromInternalValueToExternalValue(
            base.AddOrUpdate(
                    FromHeapKeyToStorageKey(key),
                    sKey => FromExternalValueToInternalValue(addValue),
                    (sKey, oldItm) =>
                    {
                        EV oldValue;
                        return
                            FromExternalValueToInternalValue(
                                FromInternalValueToExternalValue(oldItm, out oldValue) ? updateValueFactory(key, oldValue) :
                                addValue
                            )
                        ;
                    }
                ),
                out hold
            );


            return hold;
        }

        public EV GetOrAdd(HK key, EV value)
        {
            EV hold;

            var storedValue = FromExternalValueToInternalValue(value);

            FromInternalValueToExternalValue(
                base.AddOrUpdate(
                    FromHeapKeyToStorageKey(key),
                    storedValue,
                    (sKey, oldItm) =>
                        FromInternalValueToExternalValue(oldItm, out hold) ? oldItm : storedValue
                ),
                out hold
            );

            return hold;
        }

        public EV GetOrAdd(HK key, Func<HK, EV> valueFactory)
        {
            EV hold;

            return this.TryGetValue(key, out hold) ? hold : GetOrAdd(key, valueFactory(key));
        }

        public KeyValuePair<EK, EV>[] ToArray()
        { return ((IEnumerable<KeyValuePair<EK, EV>>)this).ToArray(); }

        public bool TryAdd(HK key, EV value)
        { return base.TryAdd(FromHeapKeyToStorageKey(key), FromExternalValueToInternalValue(value)); }

        public bool TryRemove(HK key, out EV value)
        {
            IV hold;

            if (base.TryRemove(FromHeapKeyToSearchKey(key), out hold))
                return FromInternalValueToExternalValue(hold, out value);

            value = default(EV);
            return false;
        }

        public bool TryUpdate(HK key, EV newValue, EV comparisonValue)
        {
            return
                base.TryUpdate(
                    FromHeapKeyToSearchKey(key),
                    FromExternalValueToInternalValue(newValue),
                    FromExternalValueToInternalValue(comparisonValue)
                )
            ;
        }

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


    internal abstract class InternalWeakDictionaryWeakValueBase<IK, EK, EV, HK> : InternalWeakDictionaryBase<IK, WeakKey<EV>, EK, EV, HK>
        where IK : ITrashable
        where EV : class
    {
        protected InternalWeakDictionaryWeakValueBase(int concurrencyLevel, int capacity, IEqualityComparer<IK> keyComparer)
            : base(concurrencyLevel, capacity, keyComparer)
        { }

        protected InternalWeakDictionaryWeakValueBase(IEqualityComparer<IK> keyComparer)
            : base(keyComparer)
        { }

        protected sealed override WeakKey<EV> FromExternalValueToInternalValue(EV externalValue)
        {
            var res = new WeakKey<EV>();
            res.SetValue(externalValue, true);
            return res;
        }

        protected sealed override bool FromInternalValueToExternalValue(WeakKey<EV> internalValue, out EV externalValue)
        {
            return internalValue.GetValue(out externalValue, true);
        }
    }

    internal abstract class InternalWeakDictionaryStrongValueBase<IK, EK, EV, HK> : InternalWeakDictionaryBase<IK, StrongKey<EV>, EK, EV, HK>
        where IK : ITrashable
    {
        protected InternalWeakDictionaryStrongValueBase(int concurrencyLevel, int capacity, IEqualityComparer<IK> keyComparer)
            : base(concurrencyLevel, capacity, keyComparer)
        { }

        protected InternalWeakDictionaryStrongValueBase(IEqualityComparer<IK> keyComparer)
            : base(keyComparer)
        { }

        protected sealed override StrongKey<EV> FromExternalValueToInternalValue(EV externalValue)
        {
            return new StrongKey<EV>() { _element = externalValue };
        }

        protected sealed override bool FromInternalValueToExternalValue(StrongKey<EV> internalValue, out EV externalValue)
        {
            externalValue = internalValue._element;
            return true;
        }
    }
}