using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Sunlighter.ModuleBuilderLib
{
    public interface IHashGenerator
    {
        void Add(byte b);
        void Add(byte[] b);
        void Add(byte[] b, int off, int len);
        void Add(char ch);
        void Add(char[] ch);
        void Add(string s);
        int Hash { get; }
    }

    public interface IHashable
    {
        void AddToHash(IHashGenerator hg);
    }

    public class HashGenerator : IHashGenerator
    {
        private int hash;

        public HashGenerator()
        {
            hash = 0x23F3071B;
        }

        public void Add(byte b)
        {
            hash = unchecked((hash * 729) + b + 1);
        }

        public void Add(byte[] b)
        {
            int iend = b.Length;
            for (int i = 0; i < iend; ++i)
            {
                Add(b[i]);
            }
        }

        public void Add(byte[] b, int off, int len)
        {
            int iEnd = off + len;
            for (int i = off; i < iEnd; ++i)
            {
                Add(b[i]);
            }
        }

        public void Add(char ch)
        {
            Add(BitConverter.GetBytes(ch));
        }

        public void Add(char[] ch)
        {
            int iend = ch.Length;
            for (int i = 0; i < iend; ++i)
            {
                Add(ch[i]);
            }
        }

        public void Add(string s)
        {
            int iend = s.Length;
            for (int i = 0; i < iend; ++i)
            {
                Add(s[i]);
            }
        }

        public int Hash { get { return unchecked(hash * 0x3A5E4215); } }
    }

    public static partial class Extensions
    {
        public static void Add(this IHashGenerator hg, MemberInfo mi)
        {
            hg.Add(BitConverter.GetBytes(mi.Module.MetadataToken));
            hg.Add(BitConverter.GetBytes(mi.MetadataToken));
        }

        public static void Add(this IHashGenerator hg, int i)
        {
            byte[] b = BitConverter.GetBytes(i);
            hg.Add(b, 0, 4);
        }
    }
}
