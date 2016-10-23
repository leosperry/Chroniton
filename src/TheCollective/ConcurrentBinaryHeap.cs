using System;
using System.Collections.Generic;
using System.Linq;

namespace TheCollective
{
	public abstract class ConcurrentBinaryHeap<T> where T : IComparable<T>
	{
		protected T[] _internal;

		object _loc = new { };

		int _count;

		public int Count
		{
			get { return _count; }
		}

		public int Capacity
		{
			get
			{
				return _internal.Length;
			}
		}

		public ConcurrentBinaryHeap() : this(16) { }

		public ConcurrentBinaryHeap(int capacity)
		{
			if (capacity < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(capacity), $"{nameof(capacity)} must be greater than 0");
			}
			_internal = new T[capacity];
		}

		public ConcurrentBinaryHeap(IEnumerable<T> items)
		{
			//definite refactor here
			_internal = new T[(int)(items.Count() * 1.5)];
			AddRange(items);
		}

		public T Peek()
		{
			return _internal[0];
		}

		public void Add(T item)
		{
			lock (_loc)
			{
				if (item == null)
				{
					throw new ArgumentNullException(nameof(item));
				}
				if (_internal.Length < ++_count)
				{
					var newInternal = new T[_internal.Length * 2];
					_internal.CopyTo(newInternal, 0);
					_internal = newInternal;
				}
				_internal[_count - 1] = item;
				shiftUp(_count - 1);
			}
		}

		public void AddRange(IEnumerable<T> items)
		{
			//possible refactor here
			foreach (var item in items)
			{
				Add(item);
			}
		}

		public T Extract()
		{
			lock (_loc)
			{
				if (_count < 1)
				{
					throw new Exception("no items to extract");
				}
				var item = _internal[0];
				_internal[0] = _internal[--_count];
				_internal[_count] = default(T);
				shiftDown(0);

				return item;
			}
		}

		public bool TryExtract(out T item)
		{
			lock (_loc)
			{
				if (_count > 0)
				{
					item = Extract();
					return true;
				}
				else
				{
					item = default(T);
					return false;
				}
			}
		}

		/// <summary>
		/// finds the given item and removes it from the heap.
		/// this is an O(n log n) operation
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool FindExtract(T item)
		{
			var success = false;
			lock (_loc)
			{
				for (int i = 0; i < _count; i++)
				{
					if (item.Equals(_internal[i]))
					{
						success = true;

						_internal[i] = _internal[--_count];
						_internal[_count] = default(T);
						shiftDown(i);

						break;
					}
				}
			}
			return success;
		}

		public IEnumerable<T> FindWhere(Func<T, bool> predicate)
		{
			lock (_loc)
			{
				return _internal.Take(_count).Where(predicate).ToList();
			}
		}

		private void shiftDown(int index)
		{
			int childIndex = getFirstChildIndex(index);
			if (childIndex < _count)
			{
				int smallest = shiftDownCompare(index, childIndex) ? childIndex : index;
				smallest = ++childIndex < _count && shiftDownCompare(smallest, childIndex) ? childIndex : smallest;
				if (smallest != index)
				{
					swap(index, smallest);
					shiftDown(smallest);

				}

			}
		}

		private void shiftUp(int index)
		{
			int parent = getParentIndex(index);
			if (shiftUpCompare(index, parent))
			{
				swap(index, parent);
			}
			if (parent > 0)
			{
				shiftUp(parent);
			}
		}

		private void swap(int index, int parent)
		{
			T temp = _internal[index];
			_internal[index] = _internal[parent];
			_internal[parent] = temp;
		}

		private int getFirstChildIndex(int index)
		{
			return index * 2 + 1;
		}

		private int getParentIndex(int index)
		{
			return (index - 1) / 2;
		}

		protected abstract bool shiftDownCompare(int index1, int index2);
		protected abstract bool shiftUpCompare(int index1, int index2);
	}

	public class MinHeap<T> : ConcurrentBinaryHeap<T> where T : IComparable<T>
	{
		public MinHeap() : base() { }
		public MinHeap(int size) : base(size) { }
		public MinHeap(IEnumerable<T> items) : base(items) { }

		protected override bool shiftDownCompare(int index1, int index2)
		{
			return _internal[index1].CompareTo(_internal[index2]) > 0;
		}

		protected override bool shiftUpCompare(int index1, int index2)
		{
			return _internal[index1].CompareTo(_internal[index2]) < 0;
		}
	}

	public class MaxHeap<T> : ConcurrentBinaryHeap<T> where T : IComparable<T>
	{
		public MaxHeap() : base() { }
		public MaxHeap(int size) : base(size) { }
		public MaxHeap(IEnumerable<T> items) : base(items) { }

		protected override bool shiftDownCompare(int index1, int index2)
		{
			return _internal[index1].CompareTo(_internal[index2]) < 0;
		}

		protected override bool shiftUpCompare(int index1, int index2)
		{
			return _internal[index1].CompareTo(_internal[index2]) > 0;
		}
	}
}
