using System;

namespace Sunlighter.ModuleBuilderLib
{
    public class Symbol : IEquatable<Symbol>, IComparable<Symbol>, IComparable, IHashable
    {
        private readonly string name;
        private readonly bool interned;
        private readonly long gensymIndex;

        private static long nextGensymIndex = 0L;

        public Symbol(string x) { name = x; interned = true; gensymIndex = 0L; }

        public static implicit operator Symbol(string x) { return new Symbol(x); }

        public Symbol()
        {
            name = null;
            interned = false;
            gensymIndex = System.Threading.Interlocked.Increment(ref nextGensymIndex);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Symbol)) return false;
            Symbol s = (Symbol)obj;
            if (interned != s.interned) return false;
            if (interned) return name.Equals(s.name);
            else return gensymIndex == s.gensymIndex;
        }

        public override int GetHashCode()
        {
            if (interned) return name.GetHashCode() ^ 0x5A5A5A5A;
            else return gensymIndex.GetHashCode() ^ unchecked((int)0xA5A5A5A5);
        }

        public static bool operator <(Symbol one, Symbol two)
        {
            if (one.interned)
            {
                if (two.interned)
                {
                    return one.name.CompareTo(two.name) < 0;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if (two.interned)
                {
                    return false;
                }
                else
                {
                    return one.gensymIndex < two.gensymIndex;
                }
            }
        }

        public static bool operator >(Symbol one, Symbol two)
        {
            if (one.interned)
            {
                if (two.interned)
                {
                    return one.name.CompareTo(two.name) > 0;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (two.interned)
                {
                    return true;
                }
                else
                {
                    return one.gensymIndex > two.gensymIndex;
                }
            }
        }

        public static bool operator ==(Symbol one, Symbol two)
        {
            if (object.ReferenceEquals(one, null) && object.ReferenceEquals(two, null)) return true;
            if (object.ReferenceEquals(one, null)) return false;
            if (object.ReferenceEquals(two, null)) return false;
            if (one.interned != two.interned) return false;
            if (one.interned)
            {
                if (one.name.CompareTo(two.name) != 0) return false;
            }
            else
            {
                if (one.gensymIndex != two.gensymIndex) return false;
            }
            return true;
        }

        public static bool operator !=(Symbol one, Symbol two)
        {
            if (object.ReferenceEquals(one, null) && object.ReferenceEquals(two, null)) return false;
            if (object.ReferenceEquals(one, null)) return true;
            if (object.ReferenceEquals(two, null)) return true;
            if (one.interned != two.interned) return true;
            if (one.interned)
            {
                if (one.name.CompareTo(two.name) != 0) return true;
            }
            else
            {
                if (one.gensymIndex != two.gensymIndex) return true;
            }
            return false;
        }

        public int CompareTo(object obj)
        {
            Symbol other = (Symbol)obj;
            return (this < other) ? -1 : (this > other) ? 1 : 0;
        }

        public string Name { get { if (interned) return name; else return "g$" + gensymIndex; } }

        public bool IsInterned { get { return interned; } }

        public override string ToString()
        {
            if (interned)
                return name;
            else return "g$" + gensymIndex;
        }

        public bool IsSymbol(string name)
        {
            return interned && (this.name == name);
        }

        private static readonly byte[] internedHeaderHashBytes = new byte[] { 0x1A, 0x2B, 0x3F };
        private static readonly byte[] uninternedHeaderHashBytes = new byte[] { 0x3E, 0xAD, 0x0E };

        public void AddToHash(IHashGenerator hg)
        {
            if (interned)
            {
                hg.Add(internedHeaderHashBytes);
                hg.Add(name);
            }
            else
            {
                hg.Add(uninternedHeaderHashBytes);
                hg.Add(BitConverter.GetBytes(gensymIndex));
            }
        }

        public bool Equals(Symbol other)
        {
            return this == other;
        }

        public int CompareTo(Symbol other)
        {
            return (this < other) ? -1 : (this > other) ? 1 : 0;
        }
    }
}
