using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheCollective;

namespace Chroniton
{
    public interface IContinuum
    {
        Guid Add(ScheduledJobBase scheduledJob);

        bool Remove(Guid jobId);

        Task CleanUp();

        ScheduledJobBase GetNext();
    }

    public class InMemoryContinuum : IContinuum
    {
        Dictionary<Guid, ScheduledJobBase> _byID = new Dictionary<Guid, ScheduledJobBase>();
        MinHeap<ScheduledJobBase> _scheduledQueue = new MinHeap<ScheduledJobBase>();

        public Guid Add(ScheduledJobBase scheduledJob)
        {
            if (scheduledJob.ID == default(Guid))
            {
                scheduledJob.ID = Guid.NewGuid();
            }
            _byID.Add(scheduledJob.ID, scheduledJob);
            _scheduledQueue.Add(scheduledJob);
            return scheduledJob.ID;
        }

        public Task CleanUp()
        {
            return Task.CompletedTask;
        }

        public ScheduledJobBase GetNext()
        {
            ScheduledJobBase retVal;
            var sj = _scheduledQueue.Peek();
            if (sj != null && sj.RunTime <= DateTime.UtcNow && _scheduledQueue.TryExtract(out retVal))
            {
                _byID.Remove(retVal.ID);
                return retVal;
            }
            return null;
        }

        public bool Remove(Guid jobId)
        {
            var items = _scheduledQueue.FindWhere(j => j.ID == jobId);
            if (items.Count() == 1)
            {
                return _scheduledQueue.FindExtract(items.First());
            }
            return false;
        }

        public void ClearAll()
        {
            _byID.Clear();
            _scheduledQueue = new MinHeap<ScheduledJobBase>();
        }
    }

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
                _multiverse[typeof(T)] = new T();
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
