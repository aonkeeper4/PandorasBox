using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.PandorasBox
{
    class NoteHelper
    {
        public static float relativeA4ToFreq(int offset)
        {
            return 440f * (float)Math.Pow(2f, offset / 12f);
        }
    }
}
