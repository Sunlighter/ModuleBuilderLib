using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace Sunlighter.ModuleBuilderLib.Pascalesque
{
#if NETSTANDARD2_0 || NETSTANDARD2_1
    [Serializable]
#endif
    public class PascalesqueException : Exception
    {
        public PascalesqueException() { }
        public PascalesqueException(string message) : base(message) { }
        public PascalesqueException(string message, Exception inner) : base(message, inner) { }

        #if NET6_0_OR_GREATER
        [Obsolete]
        #endif
        protected PascalesqueException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    public static class ExtMethods
    {
        public static bool HasDuplicates<T>(this IEnumerable<T> items)
        {
            HashSet<T> h = new HashSet<T>();
            foreach (T item in items)
            {
                if (h.Contains(item)) return true;
                h.Add(item);
            }
            return false;
        }

        public static IEnumerable<T> AndAlso<T>(this IEnumerable<T> items, T another)
        {
            foreach (T item in items)
            {
                yield return item;
            }
            yield return another;
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> items)
        {
            HashSet<T> h = new HashSet<T>();
            h.UnionWith(items);
            return h;
        }

        public static T Last<T>(this List<T> list)
        {
            if (list.Count == 0) throw new IndexOutOfRangeException();
            return list[list.Count - 1];
        }

        public static EnvSpec EnvSpecUnion(this IEnumerable<EnvSpec> envSpecs)
        {
            EnvSpec e = EnvSpec.Empty();
            foreach (EnvSpec i in envSpecs)
            {
                e |= i;
            }
            return e;
        }
    }


    public readonly struct VarSpec
    {
        private readonly bool isWritten;
        private readonly bool isCaptured;

        public VarSpec(bool isWritten, bool isCaptured)
        {
            this.isWritten = isWritten;
            this.isCaptured = isCaptured;
        }

        public bool IsWritten { get { return isWritten; } }

        public bool IsCaptured { get { return isCaptured; } }

        public static VarSpec operator |(VarSpec a, VarSpec b)
        {
            return new VarSpec(a.IsWritten || b.IsWritten, a.IsCaptured || b.IsCaptured);
        }
    }

    public sealed class EnvSpec
    {
        private readonly Dictionary<Symbol, VarSpec> data;

        private EnvSpec()
        {
            data = new Dictionary<Symbol, VarSpec>();
        }

        private EnvSpec(Symbol s, VarSpec v)
        {
            data = new Dictionary<Symbol, VarSpec>();
            data.Add(s, v);
        }

        public static EnvSpec Empty()
        {
            return new EnvSpec();
        }

        public bool IsEmpty { get { return data.Count == 0; } }

        public static EnvSpec Singleton(Symbol s, VarSpec v)
        {
            return new EnvSpec(s, v);
        }

        public static EnvSpec FromSequence(IEnumerable<Tuple<Symbol, VarSpec>> seq)
        {
            EnvSpec r = new EnvSpec();
            foreach (Tuple<Symbol, VarSpec> item in seq)
            {
                if (r.data.ContainsKey(item.Item1))
                {
                    r.data[item.Item1] |= item.Item2;
                }
                else
                {
                    r.data.Add(item.Item1, item.Item2);
                }
            }
            return r;
        }

        public static EnvSpec CaptureAll(EnvSpec e)
        {
            EnvSpec r = new EnvSpec();
            foreach (KeyValuePair<Symbol, VarSpec> item in e.data)
            {
                r.data.Add(item.Key, new VarSpec(item.Value.IsWritten, true));
            }
            return r;
        }

        public bool ContainsKey(Symbol s) { return data.ContainsKey(s); }

        public ImmutableHashSet<Symbol> Keys
        {
            get
            {
                return data.Keys.ToImmutableHashSet();
            }
        }

        public IEnumerable<VarSpec> Values
        {
            get
            {
                return data.Values;
            }
        }

        public VarSpec this[Symbol s]
        {
            get
            {
                return data[s];
            }
        }

        public static EnvSpec operator |(EnvSpec a, EnvSpec b)
        {
            EnvSpec r = new EnvSpec();
            foreach (KeyValuePair<Symbol, VarSpec> d in a.data)
            {
                r.data.Add(d.Key, d.Value);
            }
            foreach (KeyValuePair<Symbol, VarSpec> d in b.data)
            {
                if (r.data.ContainsKey(d.Key))
                {
                    VarSpec dOld = r.data[d.Key];
                    r.data[d.Key] = dOld | d.Value;
                }
                else
                {
                    r.data.Add(d.Key, d.Value);
                }
            }
            return r;
        }

        public static EnvSpec operator -(EnvSpec a, IEnumerable<Symbol> b)
        {
            HashSet<Symbol> hs = new HashSet<Symbol>();
            hs.UnionWith(b);

            EnvSpec r = new EnvSpec();
            foreach (KeyValuePair<Symbol, VarSpec> kvp in a.data)
            {
                if (!(hs.Contains(kvp.Key)))
                {
                    r.data.Add(kvp.Key, kvp.Value);
                }
            }

            return r;
        }

        public static EnvSpec operator -(EnvSpec a, Symbol b)
        {
            EnvSpec r = new EnvSpec();
            foreach (KeyValuePair<Symbol, VarSpec> kvp in a.data)
            {
                if (kvp.Key != b)
                {
                    r.data.Add(kvp.Key, kvp.Value);
                }
            }

            return r;
        }

        public static EnvSpec Add(EnvSpec a, Symbol s, VarSpec v)
        {
            EnvSpec r = new EnvSpec();
            foreach (KeyValuePair<Symbol, VarSpec> d in a.data)
            {
                r.data.Add(d.Key, d.Value);
            }
            if (r.data.ContainsKey(s))
            {
                VarSpec vOld = r.data[s];
                r.data[s] = v | vOld;
            }
            else
            {
                r.data.Add(s, v);
            }
            return r;
        }

        public static EnvSpec Add(EnvSpec a, IEnumerable<Tuple<Symbol, VarSpec>> vars)
        {
            EnvSpec r = new EnvSpec();
            foreach (KeyValuePair<Symbol, VarSpec> d in a.data)
            {
                r.data.Add(d.Key, d.Value);
            }
            foreach (Tuple<Symbol, VarSpec> d in vars)
            {
                if (r.data.ContainsKey(d.Item1))
                {
                    VarSpec vOld = r.data[d.Item1];
                    r.data[d.Item1] = d.Item2 | vOld;
                }
                else
                {
                    r.data.Add(d.Item1, d.Item2);
                }
            }
            return r;
        }

        public IEnumerable<KeyValuePair<Symbol, VarSpec>> AsEnumerable()
        {
            return data.AsEnumerable();
        }
    }

}
