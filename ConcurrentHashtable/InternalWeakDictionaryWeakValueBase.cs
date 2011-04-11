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

#if !SILVERLIGHT
using System.Collections.Concurrent;
#endif

namespace TvdP.Collections
{
    internal abstract class InternalWeakDictionaryWeakValueBase<IK, IV, EK, EV, HK> : ConcurrentDictionary<IK, IV>, IMaintainable, IDictionary<EK, EV>, ICollection<KeyValuePair<EK, EV>>, IEnumerable<KeyValuePair<EK, EV>>
        where IK : ITrashable
        where IV : IWeakValueRef<EV>, IEquatable<IV>
        where EV : class
        where HK : struct
    {
        protected InternalWeakDictionaryWeakValueBase(int concurrencyLevel, int capacity, IEqualityComparer<IK> keyComparer)
#if SILVERLIGHT
            : base(keyComparer)
#else
            : base(concurrencyLevel, capacity, keyComparer)
#endif
        { MaintenanceWorker.Register(this); }

        protected InternalWeakDictionaryWeakValueBase(IEqualityComparer<IK> keyComparer)
            : base(keyComparer)
        { MaintenanceWorker.Register(this); }

        protected abstract IK FromExternalKeyToSearchKey(EK externalKey);
        protected abstract IK FromExternalKeyToStorageKey(EK externalKey);
        protected abstract IK FromHeapKeyToSearchKey(HK externalKey);
        protected abstract IK FromHeapKeyToStorageKey(HK externalKey);
        protected abstract bool FromInternalKeyToExternalKey(IK internalKey, out EK externalKey);
        protected abstract bool FromInternalKeyToHeapKey(IK internalKey, out HK externalKey);
        protected abstract IV FromExternalValueToInternalValue(EV externalValue);

        bool FromInternalValueToExternalValue(IV internalValue, out EV externalValue)
        { return internalValue.GetValue(out externalValue); }

        #region IMaintainable Members

        void IMaintainable.DoMaintenance()
        {
            foreach (var kvp in (IEnumerable<KeyValuePair<IK, IV>>)this)
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
            var newItm = FromExternalValueToInternalValue(value);

            var storedItm =
                base.AddOrUpdate(
                    FromExternalKeyToStorageKey(key),
                    newItm,
                    (sKey, oldItm) =>
                    {
                        if (!sKey.IsGarbage && !oldItm.IsGarbage)
                            return oldItm;
                        else
                        {
                            //boyscout
                            ((ICollection<KeyValuePair<IK, IV>>)this).Remove(new KeyValuePair<IK, IV>(sKey, oldItm));
                            return newItm;
                        }
                    }
                )
            ;

            if (!object.ReferenceEquals(newItm.Reference, storedItm.Reference))
                throw new ArgumentException();
        }

        bool IDictionary<EK, EV>.ContainsKey(EK key)
        {
            EV dummy;
            return ((IDictionary<EK, EV>)this).TryGetValue(key, out dummy);
        }

        ICollection<EK> IDictionary<EK, EV>.Keys
        { get { return new TransformedCollection<EK> { _source = ((IEnumerable<KeyValuePair<EK, EV>>)this).Select(kvp => kvp.Key) }; } }

        bool IDictionary<EK, EV>.Remove(EK key)
        {
            IV itm;
            bool res = this.TryRemove(FromExternalKeyToSearchKey(key), out itm);
            return res && !itm.IsGarbage;
        }

        bool IDictionary<EK, EV>.TryGetValue(EK key, out EV value)
        {
            IV internalValue;
            IK searchKey = FromExternalKeyToSearchKey(key);
            if (((IDictionary<IK, IV>)this).TryGetValue(searchKey, out internalValue))
            {
                if (FromInternalValueToExternalValue(internalValue, out value))
                    return true;

                //boyscout
                ((IDictionary<IK, IV>)this).Remove(new KeyValuePair<IK, IV>(searchKey, internalValue));
            }

            value = default(EV);
            return false;
        }

        ICollection<EV> IDictionary<EK, EV>.Values
        { get { return new TransformedCollection<EV> { _source = ((IEnumerable<KeyValuePair<EK, EV>>)this).Select(kvp => kvp.Value) }; } }

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
        { ((IDictionary<EK, EV>)this).Add(item.Key, item.Value); }

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

        #region IEnumerable<KeyValuePair<EK, EV>> Members

        public new IEnumerator<KeyValuePair<EK, EV>> GetEnumerator()
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
                else
                    //boyscout
                    ((ICollection<KeyValuePair<IK, IV>>)this).Remove(kvp);
            }
        }

        #endregion

        public bool ContainsKey(HK key)
        {
            IV itm;
            return ((IDictionary<IK, IV>)this).TryGetValue(FromHeapKeyToSearchKey(key), out itm) && !itm.IsGarbage;
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

        public new bool IsEmpty
        { get { return !((ICollection<KeyValuePair<EK, EV>>)this).GetEnumerator().MoveNext(); } }

        public EV AddOrUpdate(HK key, Func<HK, EV> addValueFactory, Func<HK, EV, EV> updateValueFactory)
        {
            //variables to hold references to newly created values so they will remain alive
            EV hold1 ;
            EV hold2 ;

            FromInternalValueToExternalValue(
                base.AddOrUpdate(
                    FromHeapKeyToStorageKey(key),
                    sKey => FromExternalValueToInternalValue(hold1 = addValueFactory(key)),
                    (sKey, oldItm) =>
                    {
                        EV oldValue;
                        HK oldKey;
                        EV newValue;

                        if (
                            FromInternalKeyToHeapKey(sKey, out oldKey)
                            && FromInternalValueToExternalValue(oldItm, out oldValue)
                        )
                            newValue = updateValueFactory(oldKey, oldValue);
                        else
                        {
                            //boyscout
                            ((ICollection<KeyValuePair<IK, IV>>)this).Remove(new KeyValuePair<IK, IV>(sKey, oldItm));
                            newValue = addValueFactory(key);
                        }

                        return FromExternalValueToInternalValue(hold2 = newValue);
                    }
                ),
                out hold1
            );


            return hold1;
        }

        public EV AddOrUpdate(HK key, EV addValue, Func<HK, EV, EV> updateValueFactory)
        {
            var newItm = FromExternalValueToInternalValue(addValue);
            var internalKey = FromHeapKeyToStorageKey(key);

            if (base.TryAdd(internalKey, newItm))
                return addValue;

            EV hold = default(EV);

            FromInternalValueToExternalValue(
                base.AddOrUpdate(
                    FromHeapKeyToStorageKey(key),
                    newItm,
                    (sKey, oldItm) =>
                    {
                        EV oldValue;
                        HK oldKey;

                        if (
                            FromInternalKeyToHeapKey(sKey, out oldKey)
                            && FromInternalValueToExternalValue(oldItm, out oldValue)
                        )
                            return FromExternalValueToInternalValue(hold = updateValueFactory(oldKey, oldValue));
                        else
                        {
                            //boyscout
                            ((ICollection<KeyValuePair<IK, IV>>)this).Remove(new KeyValuePair<IK, IV>(sKey, oldItm));
                            return newItm;
                        }
                    }
                ),
                out hold
            );


            return hold;
        }

        public EV GetOrAdd(HK key, EV value)
        {
            var newItm = FromExternalValueToInternalValue(value);
            var internalKey = FromHeapKeyToStorageKey(key);

            EV hold;

            if(FromInternalValueToExternalValue(base.GetOrAdd(internalKey, newItm), out hold)) 
                return hold;

            FromInternalValueToExternalValue(
                base.AddOrUpdate(
                    internalKey,
                    newItm,
                    (sKey, oldItm) =>
                    {
                        if (sKey.IsGarbage || oldItm.IsGarbage)
                        {
                            //boyscout
                            ((ICollection<KeyValuePair<IK, IV>>)this).Remove(new KeyValuePair<IK, IV>(sKey, oldItm));
                            return newItm;
                        }
                        else
                            return oldItm;
                    }
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

        public new KeyValuePair<EK, EV>[] ToArray()
        { return ((IEnumerable<KeyValuePair<EK, EV>>)this).ToArray(); }

        public bool TryAdd(HK key, EV value)
        { 
            var newItm = FromExternalValueToInternalValue(value);
            var internalKey = FromHeapKeyToStorageKey(key);

            if (base.TryAdd(internalKey, newItm))
                return true;

            var storedItm =
                base.AddOrUpdate(
                    internalKey,
                    newItm,
                    (k, itm) => 
                    {
                        if (k.IsGarbage || itm.IsGarbage)
                        {
                            //boyscout
                            ((ICollection<KeyValuePair<IK, IV>>)this).Remove(new KeyValuePair<IK, IV>(k, itm));
                            return newItm;
                        }
                        else
                            return itm;
                    }
                )
            ;

            return object.ReferenceEquals(storedItm.Reference, newItm.Reference);
        }

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

    internal abstract class InternalWeakDictionaryWeakValueBase<IK, EK, EV, HK> : InternalWeakDictionaryWeakValueBase<IK, WeakValueRef<EV>, EK, EV, HK>
        where IK : ITrashable
        where EV : class
        where HK : struct
    {
        protected InternalWeakDictionaryWeakValueBase(int concurrencyLevel, int capacity, IEqualityComparer<IK> keyComparer)
#if SILVERLIGHT
            : base(keyComparer)
#else
            : base(concurrencyLevel, capacity, keyComparer)
#endif
        { MaintenanceWorker.Register(this); }

        protected InternalWeakDictionaryWeakValueBase(IEqualityComparer<IK> keyComparer)
            : base(keyComparer)
        { MaintenanceWorker.Register(this); }

        protected override WeakValueRef<EV> FromExternalValueToInternalValue(EV externalValue)
        { return WeakValueRef<EV>.Create( externalValue ); }
    }
}