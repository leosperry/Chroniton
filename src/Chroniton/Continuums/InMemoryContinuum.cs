using System;
using System.Linq;
using System.Threading.Tasks;
using TheCollective;

namespace Chroniton
{
	public class InMemoryContinuum : IContinuum
	{
		protected MinHeap<ScheduledJobBase> _scheduledQueue = new MinHeap<ScheduledJobBase>();

		public Guid Add(ScheduledJobBase scheduledJob)
		{
			if (scheduledJob.ID == default(Guid))
			{
				scheduledJob.ID = Guid.NewGuid();
			}
			_scheduledQueue.Add(scheduledJob);
			return scheduledJob.ID;
		}

		public virtual Task CleanUp()
		{
			return Task.CompletedTask;
		}

		public ScheduledJobBase ExtactNextReady()
		{
			ScheduledJobBase retVal;
			var sj = _scheduledQueue.Peek();
			if (sj != null && sj.RunTime <= DateTime.UtcNow && _scheduledQueue.TryExtract(out retVal))
			{
				return retVal;
			}
			return null;
		}

		public virtual void Initialize() { }

		public bool Remove(Guid jobId)
		{
			var items = _scheduledQueue.FindWhere(j => j.ID == jobId);
			if (!items.Any())
			{
				return false;
			}
			bool success = true;
			foreach (var item in items)
			{
				item.PreventReschedule = true;
				success &= _scheduledQueue.FindExtract(item);
			}
			return success;
		}

		public ScheduledJobBase GetJob(Guid id)
		{
			return _scheduledQueue.FindWhere(j => j.ID == id).FirstOrDefault();
		}

		public void ClearAll()
		{
			_scheduledQueue = new MinHeap<ScheduledJobBase>();
		}
	}
}
