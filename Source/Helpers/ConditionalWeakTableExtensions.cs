using Microsoft.Xna.Framework;
using System;
using Monocle;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.PandorasBox
{
    public static class ConditionalWeakTableExtensions
    {
        public static TValue GetOrDefault<TKey, TValue>(this ConditionalWeakTable<TKey, TValue> table, TKey key, TValue def) where TKey : class where TValue : class
        {
            if (table.TryGetValue(key, out var value))
            {
                return value;
            }

            return def;
        }

        public static void AddOrUpdate<TKey, TValue>(this ConditionalWeakTable<TKey, TValue> table, TKey key, TValue value) where TKey : class where TValue : class
        {
            if (table.TryGetValue(key, out var existing))
            {
                table.Remove(key);
            }

            table.Add(key, value);
        }
    }
}
