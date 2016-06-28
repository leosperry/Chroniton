using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace TheCollective
{
    public class ConcurrentHashSet<T> : ICollection<T>
    {
        HashSet<T> _hashSet;
        ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public ConcurrentHashSet()
        {
            _hashSet = new HashSet<T>();
        }

        public ConcurrentHashSet(IEnumerable<T> collection)
        {
            _hashSet = new HashSet<T>(collection);
        }

        public int Count => _hashSet.Count;

        //public int Count
        //{
        //    get
        //    {
        //        int cnt = 0;
        //        readLock(() => cnt = _hashSet.Count);
        //        return cnt;
        //    }
        //}

        bool ICollection<T>.IsReadOnly => false;

        public void Add(T item)
        {
            writeLock(()=>_hashSet.Add(item));
        }

        public void Clear()
        {
            writeLock(() => _hashSet.Clear());
        }

        public bool Contains(T item)
        {
            bool found = false;
            readLock(() => found = _hashSet.Contains(item));
            return found;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            readLock(() => _hashSet.CopyTo(array, arrayIndex));
        }

        public IEnumerator<T> GetEnumerator()
        {
            IEnumerator<T> enumerator = null;
            readLock(() => enumerator = _hashSet.GetEnumerator());
            return enumerator;
        }

        public bool Remove(T item)
        {
            bool success = false;
            writeLock(() => success = _hashSet.Remove(item));
            return success;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void writeLock(Action a)
        {
            _lock.EnterWriteLock();
            try
            {
                a();
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        void readLock(Action a)
        {
            _lock.EnterReadLock();
            try
            {
                a();
            }
            finally
            {
                if (_lock.IsReadLockHeld) _lock.ExitReadLock();
            }
        }
    }
}
