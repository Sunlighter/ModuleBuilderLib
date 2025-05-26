using Sunlighter.TypeTraitsLib;
using System;
using System.Collections.Immutable;
using System.Threading;

namespace Sunlighter.ModuleBuilderLib
{
    public abstract class Symbol : IEquatable<Symbol>, IComparable<Symbol>
    {
        private static readonly Lazy<ITypeTraits<Symbol>> typeTraits = new Lazy<ITypeTraits<Symbol>>(GetTypeTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        private static ITypeTraits<Symbol> GetTypeTraits()
        {
            return new UnionTypeTraits<bool, Symbol>
            (
                BooleanTypeTraits.Value,
                ImmutableList<IUnionCaseTypeTraits<bool, Symbol>>.Empty.Add
                (
                    new UnionCaseTypeTraits2<bool, Symbol, NamedSymbol>
                    (
                        true,
                        new ConvertTypeTraits<NamedSymbol, string>
                        (
                            ns => ns.Name,
                            StringTypeTraits.Value,
                            name => new NamedSymbol(name)
                        )
                    )
                )
                .Add
                (
                    new UnionCaseTypeTraits2<bool, Symbol, UnnamedSymbol>
                    (
                        false,
                        UnnamedSymbol.TypeTraits
                    )
                )
            );
        }

        public static ITypeTraits<Symbol> TypeTraits => typeTraits.Value;

        private static Lazy<Adapter<Symbol>> adapter = new Lazy<Adapter<Symbol>>
        (
            () => Adapter<Symbol>.Create(typeTraits.Value),
            LazyThreadSafetyMode.ExecutionAndPublication
        );

        public static Adapter<Symbol> Adapter => adapter.Value;

#if NETSTANDARD2_0
        public override bool Equals(object obj)
#else
        public override bool Equals(object? obj)
#endif
        {
            if (obj is Symbol other)
            {
                return typeTraits.Value.Compare(this, other) == 0;
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return typeTraits.Value.GetBasicHashCode(this);
        }

        public override string ToString()
        {
            return typeTraits.Value.ToDebugString(this);
        }

#if NETSTANDARD2_0
        public bool Equals(Symbol other)
#else
        public bool Equals(Symbol? other)
#endif
        {
            if (other is null) return false;
            return typeTraits.Value.Compare(this, other) == 0;
        }

#if NETSTANDARD2_0
        public int CompareTo(Symbol other)
#else
        public int CompareTo(Symbol? other)
#endif
        {
            if (other is null) return 1;
            return typeTraits.Value.Compare(this, other);
        }

#if NETSTANDARD2_0
        public static bool operator ==(Symbol a, Symbol b)
#else
        public static bool operator ==(Symbol? a, Symbol? b)
#endif
        {

            if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return typeTraits.Value.Compare(a, b) == 0;
        }

#if NETSTANDARD2_0
        public static bool operator !=(Symbol a, Symbol b)
#else
        public static bool operator !=(Symbol? a, Symbol? b)
#endif
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return false;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return true;
            return typeTraits.Value.Compare(a, b) != 0;
        }

        public static bool operator <(Symbol a, Symbol b) => typeTraits.Value.Compare(a, b) < 0;
        public static bool operator >(Symbol a, Symbol b) => typeTraits.Value.Compare(a, b) > 0;
        public static bool operator <=(Symbol a, Symbol b) => typeTraits.Value.Compare(a, b) <= 0;
        public static bool operator >=(Symbol a, Symbol b) => typeTraits.Value.Compare(a, b) >= 0;

        public static implicit operator Symbol(string name)
        {
            if (name == null) throw new ArgumentException("Name cannot be null", nameof(name));
            return new NamedSymbol(name);
        }

        public static Symbol Gensym() => new UnnamedSymbol();
    }

    public sealed class NamedSymbol : Symbol
    {
        private readonly string name;

        public NamedSymbol(string name)
        {
            this.name = name;
        }

        public string Name => name;
    }

    public sealed class UnnamedSymbol : Symbol
    {
        private static readonly object staticSyncRoot = new object();
        private static ulong nextId = 0ul;

        private readonly ulong id;

        private static readonly Lazy<SerializerStateID> serializerStateID = new Lazy<SerializerStateID>
        (
            SerializerStateID.Next,
            LazyThreadSafetyMode.ExecutionAndPublication
        );

        public UnnamedSymbol()
        {
            lock (staticSyncRoot)
            {
                this.id = nextId;
                ++nextId;
            }
        }

        public ulong Id => id;

        private static readonly Lazy<ITypeTraits<UnnamedSymbol>> internalTypeTraits = new Lazy<ITypeTraits<UnnamedSymbol>>
        (
            () => new ConvertTypeTraits<UnnamedSymbol, ulong>
            (
                us => us.Id,
                UInt64TypeTraits.Value,
                id => throw new InvalidOperationException("Cannot deserialize with the internal type traits")
            ),
            LazyThreadSafetyMode.ExecutionAndPublication
        );

        private static readonly Lazy<Adapter<UnnamedSymbol>> internalAdapter = new Lazy<Adapter<UnnamedSymbol>>
        (
            () => Adapter<UnnamedSymbol>.Create(internalTypeTraits.Value),
            LazyThreadSafetyMode.ExecutionAndPublication
        );

        private sealed class DeserializationState
        {
            private ImmutableSortedDictionary<int, UnnamedSymbol> idDict;

            public DeserializationState()
            {
                idDict = ImmutableSortedDictionary<int, UnnamedSymbol>.Empty;
            }

            public UnnamedSymbol GetById(int id)
            {
#if NETSTANDARD2_0
                if (idDict.TryGetValue(id, out UnnamedSymbol us))
#else
                if (idDict.TryGetValue(id, out UnnamedSymbol? us))
#endif
                {
                    return us;
                }
                else
                {
                    us = new UnnamedSymbol();
                    idDict = idDict.Add(id, us);
                    return us;
                }
            }
        }

        private sealed class SerializationState
        {
            private ImmutableSortedDictionary<UnnamedSymbol, int> symDict;
            private int nextId;

            public SerializationState()
            {
                symDict = ImmutableSortedDictionary<UnnamedSymbol, int>.Empty.WithComparers(internalAdapter.Value);
                nextId = 0;
            }

            public int GetSerializedId(UnnamedSymbol us)
            {
                if (symDict.TryGetValue(us, out int id))
                {
                    return id;
                }
                else
                {
                    int id2 = nextId;
                    symDict = symDict.Add(us, id2);
                    ++nextId;
                    return id2;
                }
            }
        }

        private sealed class UnnamedSymbolTypeTraits : ITypeTraits<UnnamedSymbol>
        {
            private static readonly UnnamedSymbolTypeTraits value = new UnnamedSymbolTypeTraits();

            private UnnamedSymbolTypeTraits()
            {
            }

            public static UnnamedSymbolTypeTraits Value => value;

            public void AddToHash(HashBuilder b, UnnamedSymbol a)
            {
                UInt64TypeTraits.Value.AddToHash(b, a.Id);
            }

            public void AppendDebugString(DebugStringBuilder sb, UnnamedSymbol a)
            {
                sb.Builder.Append($"(UnnamedSymbol {a.Id})");
            }

            public bool CanSerialize(UnnamedSymbol a)
            {
                return true;
            }

            public int Compare(UnnamedSymbol a, UnnamedSymbol b)
            {
                return UInt64TypeTraits.Value.Compare(a.Id, b.Id);
            }

            public UnnamedSymbol Deserialize(Deserializer src)
            {
                DeserializationState deserializationState = src.GetSerializerState
                (
                    serializerStateID.Value,
                    () => new DeserializationState()
                );


                int id = Int32TypeTraits.Value.Deserialize(src);
                return deserializationState.GetById(id);
            }

            public void MeasureBytes(ByteMeasurer measurer, UnnamedSymbol a)
            {
                measurer.AddBytes(4);
            }

            public void Serialize(Serializer dest, UnnamedSymbol a)
            {
                SerializationState serializationState = dest.GetSerializerState
                (
                    serializerStateID.Value,
                    () => new SerializationState()
                );

                int id = serializationState.GetSerializedId(a);
                Int32TypeTraits.Value.Serialize(dest, id);
            }
        }

        public static new ITypeTraits<UnnamedSymbol> TypeTraits => UnnamedSymbolTypeTraits.Value;
    }

    public static partial class Extensions
    {
        public static string SymbolName(this Symbol s)
        {
            if (s is NamedSymbol ns)
            {
                return ns.Name;
            }
            else if (s is UnnamedSymbol us)
            {
                return $"g${us.Id})";
            }
            else
            {
                throw new InvalidOperationException("Unknown symbol type: " + TypeTraitsUtility.GetTypeName(s.GetType()));
            }
        }
    }
}
