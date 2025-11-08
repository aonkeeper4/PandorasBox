using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.PandorasBox
{
    public class ValueHolder<T>
    {
        public T value;

        public ValueHolder(T value)
        {
            this.value = value;
        }
    }
}
