using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.PandorasBox
{
    class PeekAQueue<T>
    {
        public int Length { get; protected set; } = 0;
        public int Count { get; protected set; } = 0;

        private T[] data;
        
        public int current;

        public PeekAQueue(int length)
        {
            Length = length;
            data = new T[length];
        }

        // This hurts my soul. Thanks .Net.
        private int mod(int n, int m)
        {
            return ((n % m) + m) % m;
        }

        private int wrapIndex(int index)
        {
            return mod(index, Length);
        }

        // Starts at current = 1, doesn't matter since we will most likely overfill the backing array
        public void Enqueue(T item)
        {
            current = wrapIndex(current + 1);
            data[current] = item;
            Count = Math.Min(Count + 1, Length);
        }

        public T Dequeue()
        {
            T res = default(T);

            if (Count > 0)
            {
                res = data[current];
                current = wrapIndex(current - 1);
                Count--;
            }

            return res;
        }

        public T Peek(int offset=0)
        {
            T res = default(T);

            if (Count > offset)
            {
                res = data[wrapIndex(current - offset)];
            }

            return res;
        }
    }
}
