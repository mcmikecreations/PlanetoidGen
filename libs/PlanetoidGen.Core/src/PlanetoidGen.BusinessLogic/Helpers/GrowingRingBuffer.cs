using System;
using System.Collections;
using System.Collections.Generic;

namespace PlanetoidGen.BusinessLogic.Helpers
{
    /// <summary>
    /// A ring buffer that grows when full.
    /// Removing while empty produces undefined behavior.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GrowingRingBuffer<T> : IEnumerable<T>
    {
        private long _head;
        private long _tail;
        private T[] _items;

        public GrowingRingBuffer(long capacity = 1)
        {
            _items = new T[Math.Max(1, capacity)];
            Clear();
        }

        public void Clear()
        {
            _head = 0;
            _tail = 0;
        }

        public long Count
        {
            get
            {
                return _head >= _tail ? _head - _tail : _items.LongLength - _tail + _head;
            }
        }

        public long Capacity => _items.LongLength;

        public long Head => _head;

        public long Tail => _tail;

        public T this[long index]
        {
            get
            {
                var realIndex = (index + _tail) % _items.LongLength;
                return _items[realIndex];
            }
        }

        public void Add(T item)
        {
            _items[_head] = item;
            AdvanceHead();
        }

        public T Remove()
        {
            var result = _items[_tail];
            RetreatTail();
            return result;
        }

        public void RetreatTailWhile(Func<T, bool> condition)
        {
            long i;
            if (_head >= _tail)
            {
                for (i = _tail; i < _head; ++i)
                {
                    if (condition(_items[i])) ++_tail;
                    else return;
                }
            }
            else
            {
                for (i = _tail; i < _items.LongLength; ++i)
                {
                    if (condition(_items[i])) ++_tail;
                    else return;
                    if (_tail >= _items.LongLength) _tail = 0;
                }

                for (i = 0; i < _head; ++i)
                {
                    if (condition(_items[i])) ++_tail;
                    else return;
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            long i;
            if (_head >= _tail)
            {
                for (i = _tail; i < _head; ++i)
                {
                    yield return _items[i];
                }
            }
            else
            {
                for (i = _tail; i < _items.LongLength; ++i)
                {
                    yield return _items[i];
                }

                for (i = 0; i < _head; ++i)
                {
                    yield return _items[i];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void AdvanceHead()
        {
            if (++_head >= _items.LongLength)
            {
                _head = 0;
            }

            // Buffer is full, resize it and order items.
            if (_head == _tail)
            {
                // Use 1.5 new capacity approach from C++ STL
                var newCapacity = Math.Max(_items.LongLength + _items.LongLength / 2, 2);
                var newBuffer = new T[newCapacity];

                var tailLength = _items.LongLength - _tail;
                Array.Copy(_items, _tail, newBuffer, 0, tailLength);
                Array.Copy(_items, 0, newBuffer, tailLength, _head);
                _tail = 0;
                _head = _items.LongLength;
                _items = newBuffer;
            }
        }

        private void RetreatTail()
        {
            if (++_tail >= _items.LongLength)
            {
                _tail = 0;
            }
        }
    }
}
