using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Chroniton
{
    public interface IContinuumFactory : IEnumerable<IContinuum>
    {
        T GetContinuum<T>() where T : IContinuum, new();
    }

    public class ContinuumFactory : IContinuumFactory
    {
        static ConcurrentDictionary<Type, IContinuum> _multiverse = new ConcurrentDictionary<Type, IContinuum>();

        public T GetContinuum<T>() where T : IContinuum, new()
        {
            if (!_multiverse.ContainsKey(typeof(T)))
            {
                try
                {
                    var newContinuum = new T();
                    newContinuum.Initialize();
                    _multiverse[typeof(T)] = newContinuum;
                }
                catch (Exception)
                {
                    
                }
            };
            return (T)_multiverse[typeof(T)];
        }

        public IEnumerable<IContinuum> GetAll()
        {
            return _multiverse.Values;
        }

        public IEnumerator<IContinuum> GetEnumerator()
        {
            return _multiverse.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _multiverse.Values.ToArray().GetEnumerator();
        }
    }
}
