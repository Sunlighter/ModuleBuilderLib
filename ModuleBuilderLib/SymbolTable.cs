using Sunlighter.OptionLib;
using Sunlighter.TypeTraitsLib;
using Sunlighter.TypeTraitsLib.Building;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Sunlighter.ModuleBuilderLib
{
    [Record]
    public sealed class MethodInfoProxy
    {
        private readonly Type declaringType;
        private readonly string name;
        private readonly bool isPublic;
        private readonly bool isStatic;
        private readonly ImmutableList<Type> parameterTypes;

        public MethodInfoProxy
        (
            [Bind("$dt")] Type declaringType,
            [Bind("$name")] string name,
            [Bind("$isPublic")] bool isPublic,
            [Bind("$isStatic")] bool isStatic,
            [Bind("$parameterTypes")] ImmutableList<Type> parameterTypes
        )
        {
            this.declaringType = declaringType;
            this.name = name;
            this.isPublic = isPublic;
            this.isStatic = isStatic;
            this.parameterTypes = parameterTypes;
        }

        [Bind("$dt")]
        public Type DeclaringType => declaringType;

        [Bind("$name")]
        public string Name => name;

        [Bind("$isPublic")]
        public bool IsPublic => isPublic;

        [Bind("$isStatic")]
        public bool IsStatic => isStatic;

        [Bind("$parameterTypes")]
        public ImmutableList<Type> ParameterTypes => parameterTypes;

        public static explicit operator MethodInfoProxy(MethodInfo m)
        {
            return new MethodInfoProxy
            (
                m.DeclaringType.AssertNotNull(),
                m.Name,
                m.IsPublic,
                m.IsStatic,
                m.GetParameters().Select(pi => pi.ParameterType).ToImmutableList()
            );
        }

        public static explicit operator MethodInfo(MethodInfoProxy m)
        {
            BindingFlags f = 0;
            if (m.IsPublic) f |= BindingFlags.Public; else f |= BindingFlags.NonPublic;
            if (m.IsStatic) f |= BindingFlags.Static; else f |= BindingFlags.Instance;

#if NETSTANDARD2_0 || NETSTANDARD2_1
            MethodInfo mi = m.DeclaringType.GetRequiredMethod(m.Name, f, m.ParameterTypes.ToArray());
#else
            MethodInfo? mi = m.DeclaringType.GetRequiredMethod(m.Name, f, m.ParameterTypes.ToArray());
#endif

            if (mi == null) throw new Exception("Method not found");

            return mi;
        }
    }

    [Record]
    public sealed class ConstructorInfoProxy
    {
        private readonly Type declaringType;
        private readonly bool isPublic;
        private readonly ImmutableList<Type> parameterTypes;

        public ConstructorInfoProxy
        (
            [Bind("$dt")] Type declaringType,
            [Bind("$isPublic")] bool isPublic,
            [Bind("$parameterTypes")] ImmutableList<Type> parameterTypes
        )
        {
            this.declaringType = declaringType;
            this.isPublic = isPublic;
            this.parameterTypes = parameterTypes;
        }

        [Bind("$dt")]
        public Type DeclaringType => declaringType;

        [Bind("$isPublic")]
        public bool IsPublic => isPublic;

        [Bind("$parameterTypes")]
        public ImmutableList<Type> ParameterTypes => parameterTypes;

        public static explicit operator ConstructorInfoProxy(ConstructorInfo c)
        {
            return new ConstructorInfoProxy
            (
                c.DeclaringType.AssertNotNull(),
                c.IsPublic,
                c.GetParameters().Select(pi => pi.ParameterType).ToImmutableList()
            );
        }

        public static explicit operator ConstructorInfo(ConstructorInfoProxy c)
        {
            BindingFlags f = 0;
            if (c.IsPublic) f |= BindingFlags.Public; else f |= BindingFlags.NonPublic;

#if NETSTANDARD2_0 || NETSTANDARD2_1
            ConstructorInfo ci = c.DeclaringType.GetConstructor(f, Type.DefaultBinder, c.ParameterTypes.ToArray(), null);
#else
            ConstructorInfo? ci = c.DeclaringType.GetConstructor(f, Type.DefaultBinder, c.ParameterTypes.ToArray(), null);
#endif
            if (ci == null) throw new Exception($"Constructor not found: {TypeTraitsUtility.GetTypeName(c.DeclaringType)}({string.Join(", ", c.ParameterTypes.Select(t => TypeTraitsUtility.GetTypeName(t)))})");

            return ci;
        }
    }

    [Record]
    public sealed class FieldInfoProxy
    {
        private readonly Type declaringType;
        private readonly string name;
        private readonly bool isPublic;

        public FieldInfoProxy
        (
            [Bind("$dt")] Type declaringType,
            [Bind("$name")] string name,
            [Bind("$isPublic")] bool isPublic
        )
        {
            this.declaringType = declaringType;
            this.name = name;
            this.isPublic = isPublic;
        }

        [Bind("$dt")]
        public Type DeclaringType => declaringType;

        [Bind("$name")]
        public string Name => name;

        [Bind("$isPublic")]
        public bool IsPublic => isPublic;

        public static explicit operator FieldInfoProxy(FieldInfo f)
        {
            return new FieldInfoProxy
            (
                f.DeclaringType.AssertNotNull(),
                f.Name,
                f.IsPublic
            );
        }

        public static explicit operator FieldInfo(FieldInfoProxy f)
        {
            BindingFlags bf = 0;
            if (f.IsPublic) bf |= BindingFlags.Public; else bf |= BindingFlags.NonPublic;

#if NETSTANDARD2_0
            FieldInfo fi = f.DeclaringType.GetField(f.Name, bf);
#else
            FieldInfo? fi = f.DeclaringType.GetField(f.Name, bf);
#endif

            if (fi == null) throw new Exception($"Field not found: {TypeTraitsUtility.GetTypeName(f.DeclaringType)}.{f.Name}");

            return fi;
        }
    }

    public static class Tools
    {
        private class CompareWorkerCollection
        {
            private readonly ITypeTraits<ItemKey> itemKeyCompareWorker;
            private readonly ITypeTraits<TypeReference> typeReferenceCompareWorker;
            private readonly ITypeTraits<MethodReference> methodReferenceCompareWorker;
            private readonly ITypeTraits<ConstructorReference> constructorReferenceCompareWorker;
            private readonly ITypeTraits<FieldReference> fieldReferenceCompareWorker;

            public CompareWorkerCollection
            (
                ITypeTraits<ItemKey> itemKeyCompareWorker,
                ITypeTraits<TypeReference> typeReferenceCompareWorker,
                ITypeTraits<MethodReference> methodReferenceCompareWorker,
                ITypeTraits<ConstructorReference> constructorReferenceCompareWorker,
                ITypeTraits<FieldReference> fieldReferenceCompareWorker
            )
            {
                this.itemKeyCompareWorker = itemKeyCompareWorker;
                this.typeReferenceCompareWorker = typeReferenceCompareWorker;
                this.methodReferenceCompareWorker = methodReferenceCompareWorker;
                this.constructorReferenceCompareWorker = constructorReferenceCompareWorker;
                this.fieldReferenceCompareWorker = fieldReferenceCompareWorker;
            }

            public ITypeTraits<ItemKey> ItemKeyCompareWorker => itemKeyCompareWorker;
            public ITypeTraits<TypeReference> TypeReferenceCompareWorker => typeReferenceCompareWorker;
            public ITypeTraits<MethodReference> MethodReferenceCompareWorker => methodReferenceCompareWorker;
            public ITypeTraits<ConstructorReference> ConstructorReferenceCompareWorker => constructorReferenceCompareWorker;
            public ITypeTraits<FieldReference> FieldReferenceCompareWorker => fieldReferenceCompareWorker;
        }

        private static readonly Lazy<CompareWorkerCollection> compareWorkerCollection = new Lazy<CompareWorkerCollection>(GetCompareWorkerCollection, LazyThreadSafetyMode.ExecutionAndPublication);

        private static CompareWorkerCollection GetCompareWorkerCollection()
        {
            Builder.Instance.AddTypeTraits<Symbol>(Symbol.TypeTraits);

            ITypeTraits<MethodInfoProxy> mip_cw = Builder.Instance.GetTypeTraits<MethodInfoProxy>();

            Builder.Instance.AddTypeTraits
            (
                new ConvertTypeTraits<MethodInfo, MethodInfoProxy>
                (
                    mi => (MethodInfoProxy)mi,
                    mip_cw,
                    mip => (MethodInfo)mip
                )
            );

            ITypeTraits<ConstructorInfoProxy> cip_cw = Builder.Instance.GetTypeTraits<ConstructorInfoProxy>();

            Builder.Instance.AddTypeTraits
            (
                new ConvertTypeTraits<ConstructorInfo, ConstructorInfoProxy>
                (
                    ci => (ConstructorInfoProxy)ci,
                    cip_cw,
                    cip => (ConstructorInfo)cip
                )
            );

            ITypeTraits<FieldInfoProxy> fip_cw = Builder.Instance.GetTypeTraits<FieldInfoProxy>();

            Builder.Instance.AddTypeTraits
            (
                new ConvertTypeTraits<FieldInfo, FieldInfoProxy>
                (
                    fi => (FieldInfoProxy)fi,
                    fip_cw,
                    fip => (FieldInfo)fip
                )
            );

            return new CompareWorkerCollection
            (
                Builder.Instance.GetTypeTraits<ItemKey>(),
                Builder.Instance.GetTypeTraits<TypeReference>(),
                Builder.Instance.GetTypeTraits<MethodReference>(),
                Builder.Instance.GetTypeTraits<ConstructorReference>(),
                Builder.Instance.GetTypeTraits<FieldReference>()
            );
        }

        public static ITypeTraits<ItemKey> ItemKeyCompareWorker => compareWorkerCollection.Value.ItemKeyCompareWorker;

        public static ITypeTraits<TypeReference> TypeReferenceCompareWorker => compareWorkerCollection.Value.TypeReferenceCompareWorker;

        public static ITypeTraits<MethodReference> MethodReferenceCompareWorker => compareWorkerCollection.Value.MethodReferenceCompareWorker;

        public static ITypeTraits<ConstructorReference> ConstructorReferenceCompareWorker => compareWorkerCollection.Value.ConstructorReferenceCompareWorker;

        public static ITypeTraits<FieldReference> FieldReferenceCompareWorker => compareWorkerCollection.Value.FieldReferenceCompareWorker;
    }

    [Obsolete]
    enum ItemHashDelimiters : byte
    {
        TypeKey = (byte)0x3A,
        CompletedTypeKey = (byte)0x91,
        TypeKeyReference = (byte)0x3F,
        ExistingTypeReference = (byte)0x34,
        ExistingGenericTypeReference = (byte)0x37,
        MethodKey = (byte)0x36,
        ConstructorKey = (byte)0x31,
        FieldKey = (byte)0x3B,
        ArrayTypeReference = (byte)0x3D,
        MethodKeyReference = (byte)0x56,
        ExistingMethodReference = (byte)0x51,
        ExistingConstructorReference = (byte)0x52,
        ConstructorKeyReference = (byte)0x53,
        FieldKeyReference = (byte)0x54,
        ExistingFieldReference = (byte)0x55,
        PropertyKey = (byte)0x39,
    }

    [UnionOfDescendants]
    public abstract class ItemKey : IEquatable<ItemKey>, IComparable<ItemKey>
    {
#if NETSTANDARD2_0
        public override bool Equals(object obj)
#else
        public override bool Equals(object? obj)
#endif
        {
            if (obj is ItemKey ik)
            {
                return Tools.ItemKeyCompareWorker.Compare(this, ik) == 0;
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return Tools.ItemKeyCompareWorker.GetBasicHashCode(this);
        }

        public override string ToString()
        {
            return Tools.ItemKeyCompareWorker.ToDebugString(this);
        }

#if NETSTANDARD2_0
        public bool Equals(ItemKey other)
#else
        public bool Equals(ItemKey? other)
#endif
        {
            if (other is null) return false;
            return Tools.ItemKeyCompareWorker.Compare(this, other) == 0;
        }

#if NETSTANDARD2_0
        public int CompareTo(ItemKey other)
#else
        public int CompareTo(ItemKey? other)
#endif
        {
            if (other is null) return 1;
            return Tools.ItemKeyCompareWorker.Compare(this, other);
        }

#if NETSTANDARD2_0
        public static bool operator ==(ItemKey a, ItemKey b)
#else
        public static bool operator ==(ItemKey? a, ItemKey? b)
#endif
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return Tools.ItemKeyCompareWorker.Compare(a, b) == 0;
        }

#if NETSTANDARD2_0
        public static bool operator !=(ItemKey a, ItemKey b)
#else
        public static bool operator !=(ItemKey? a, ItemKey? b)
#endif
        {
            if (a is null && b is null) return false;
            if (a is null || b is null) return true;
            return Tools.ItemKeyCompareWorker.Compare(a, b) != 0;
        }

        public static bool operator <(ItemKey a, ItemKey b) => Tools.ItemKeyCompareWorker.Compare(a, b) < 0;
        public static bool operator >(ItemKey a, ItemKey b) => Tools.ItemKeyCompareWorker.Compare(a, b) > 0;
        public static bool operator <=(ItemKey a, ItemKey b) => Tools.ItemKeyCompareWorker.Compare(a, b) <= 0;
        public static bool operator >=(ItemKey a, ItemKey b) => Tools.ItemKeyCompareWorker.Compare(a, b) >= 0;
    }

    [Record]
    public class TypeKey : ItemKey
    {
        private readonly Symbol name;

        public TypeKey([Bind("$name")] Symbol name)
        {
            this.name = name;
        }

        [Bind("$name")]
        public Symbol Name => name;
    }

    [Record]
    public sealed class CompletedTypeKey : ItemKey
    {
        private readonly Symbol name;

        public CompletedTypeKey(Symbol name)
        {
            this.name = name;
        }

        [Bind("name")]
        public Symbol Name => name;
    }

    /// <summary>
    /// Refers to a type whether it already exists, or still hasn't been built
    /// </summary>
    [UnionOfDescendants]
    public abstract class TypeReference : IEquatable<TypeReference>, IComparable<TypeReference>
    {
#if NETSTANDARD2_0
        public override bool Equals(object obj)
#else
        public override bool Equals(object? obj)
#endif
        {
            if (obj is TypeReference t)
            {
                return Tools.TypeReferenceCompareWorker.Compare(this, t) == 0;
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return Tools.TypeReferenceCompareWorker.GetBasicHashCode(this);
        }

#if NETSTANDARD2_0
        public bool Equals(TypeReference other)
#else
        public bool Equals(TypeReference? other)
#endif
        {
            if (other is null) return false;
            return Tools.TypeReferenceCompareWorker.Compare(this, other) == 0;
        }

#if NETSTANDARD2_0
        public int CompareTo(TypeReference other)
#else
        public int CompareTo(TypeReference? other)
#endif
        {
            if (other is null) return 1;
            return Tools.TypeReferenceCompareWorker.Compare(this, other);
        }

#if NETSTANDARD2_0
        public static bool operator ==(TypeReference a, TypeReference b)
#else
        public static bool operator ==(TypeReference? a, TypeReference? b)
#endif
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return Tools.TypeReferenceCompareWorker.Compare(a, b) == 0;
        }

#if NETSTANDARD2_0
        public static bool operator !=(TypeReference a, TypeReference b)
#else
        public static bool operator !=(TypeReference? a, TypeReference? b)
#endif
        {
            if (a is null && b is null) return false;
            if (a is null || b is null) return true;
            return Tools.TypeReferenceCompareWorker.Compare(a, b) != 0;
        }

        public static bool operator <(TypeReference a, TypeReference b) => Tools.TypeReferenceCompareWorker.Compare(a, b) < 0;
        public static bool operator >(TypeReference a, TypeReference b) => Tools.TypeReferenceCompareWorker.Compare(a, b) > 0;
        public static bool operator <=(TypeReference a, TypeReference b) => Tools.TypeReferenceCompareWorker.Compare(a, b) <= 0;
        public static bool operator >=(TypeReference a, TypeReference b) => Tools.TypeReferenceCompareWorker.Compare(a, b) >= 0;

        public abstract ImmutableSortedSet<ItemKey> GetReferences();

        public abstract Type Resolve(ImmutableSortedDictionary<ItemKey, SaBox<object>> references);

        public static TypeReference GetActionType(ImmutableList<TypeReference> arguments)
        {
            Type t;
            switch (arguments.Count)
            {
                case 0:
                    return new ExistingTypeReference(typeof(Action));
                case 1:
                    t = typeof(Action<>);
                    break;
                case 2:
                    t = typeof(Action<,>);
                    break;
                case 3:
                    t = typeof(Action<,,>);
                    break;
                case 4:
                    t = typeof(Action<,,,>);
                    break;
                case 5:
                    t = typeof(Action<,,,,>);
                    break;
                case 6:
                    t = typeof(Action<,,,,,>);
                    break;
                case 7:
                    t = typeof(Action<,,,,,,>);
                    break;
                case 8:
                    t = typeof(Action<,,,,,,,>);
                    break;
                case 9:
                    t = typeof(Action<,,,,,,,,>);
                    break;
                case 10:
                    t = typeof(Action<,,,,,,,,,>);
                    break;
                case 11:
                    t = typeof(Action<,,,,,,,,,,>);
                    break;
                case 12:
                    t = typeof(Action<,,,,,,,,,,,>);
                    break;
                case 13:
                    t = typeof(Action<,,,,,,,,,,,,>);
                    break;
                case 14:
                    t = typeof(Action<,,,,,,,,,,,,,>);
                    break;
                case 15:
                    t = typeof(Action<,,,,,,,,,,,,,,>);
                    break;
                case 16:
                    t = typeof(Action<,,,,,,,,,,,,,,,>);
                    break;
                default:
                    throw new Exception("Too many Action<...> type arguments");
            }
            return new ExistingGenericTypeReference(t, arguments);
        }

        public static TypeReference GetFuncType(ImmutableList<TypeReference> arguments)
        {
            Type t;
            switch (arguments.Count)
            {
                case 0:
                    throw new Exception("Func<...> requires at least one type argument");
                case 1:
                    t = typeof(Func<>);
                    break;
                case 2:
                    t = typeof(Func<,>);
                    break;
                case 3:
                    t = typeof(Func<,,>);
                    break;
                case 4:
                    t = typeof(Func<,,,>);
                    break;
                case 5:
                    t = typeof(Func<,,,,>);
                    break;
                case 6:
                    t = typeof(Func<,,,,,>);
                    break;
                case 7:
                    t = typeof(Func<,,,,,,>);
                    break;
                case 8:
                    t = typeof(Func<,,,,,,,>);
                    break;
                case 9:
                    t = typeof(Func<,,,,,,,,>);
                    break;
                case 10:
                    t = typeof(Func<,,,,,,,,,>);
                    break;
                case 11:
                    t = typeof(Func<,,,,,,,,,,>);
                    break;
                case 12:
                    t = typeof(Func<,,,,,,,,,,,>);
                    break;
                case 13:
                    t = typeof(Func<,,,,,,,,,,,,>);
                    break;
                case 14:
                    t = typeof(Func<,,,,,,,,,,,,,>);
                    break;
                case 15:
                    t = typeof(Func<,,,,,,,,,,,,,,>);
                    break;
                case 16:
                    t = typeof(Func<,,,,,,,,,,,,,,,>);
                    break;
                case 17:
                    t = typeof(Func<,,,,,,,,,,,,,,,,>);
                    break;
                default:
                    throw new Exception("Too many Func<...> type arguments");
            }

            return new ExistingGenericTypeReference(t, arguments);
        }

        public abstract bool IsDelegate { get; }

        public abstract TypeReference[] GetDelegateParameterTypes();

        public abstract TypeReference GetDelegateReturnType();

#if false
        public static TypeReference MakeBoxedType(TypeReference t)
        {
            if (IsBoxedType(t)) throw new ArgumentException("Nested boxes are not allowed");
            return new ExistingGenericTypeReference(typeof(Box<>), ImmutableList<TypeReference>.Empty.Add(t));
        }

        public static bool IsBoxedType(TypeReference t)
        {
            if (!(t is ExistingGenericTypeReference)) return false;
            ExistingGenericTypeReference et = (ExistingGenericTypeReference)t;
            if (!(et.OpenGenericType == typeof(Box<>))) return false;
            if (et.TypeArguments.Count != 1) return false;
            return true;
        }

        public static TypeReference GetBoxContentType(TypeReference t)
        {
            if (!(t is ExistingGenericTypeReference)) throw new ArgumentException("Box<T> expected");
            ExistingGenericTypeReference et = (ExistingGenericTypeReference)t;
            if (!(et.OpenGenericType == typeof(Box<>))) throw new ArgumentException("Box<T> expected");
            if (et.TypeArguments.Count != 1) throw new ArgumentException("Ill-formed Box<T>");
            return et.TypeArguments[0];
        }
#endif

        public abstract bool IsArray { get; }

        public abstract TypeReference GetElementType();

        public abstract TypeReference MakeArrayType();

        public abstract bool IsValueType(SymbolTable s);

        public abstract bool IsInterface(SymbolTable s);

        public abstract Option<TypeReference> GetBaseType(SymbolTable s);

        public abstract IEnumerable<TypeReference> GetInterfaces(SymbolTable s);

        public abstract string FullName { get; }

        public static bool IsAssignable(SymbolTable s, TypeReference dest, TypeReference src)
        {
            if (dest is ExistingTypeReference eDest && src is ExistingTypeReference eSrc)
            {
                return eDest.ExistingType.IsAssignableFrom(eSrc.ExistingType);
            }
            else if (src is ExistingTypeReference)
            {
                // dest is a new type, so src, being an existing type, cannot inherit from it or implement it as an interface.
                // This could be different if I were to take implicit typecast operators into account.
                return false;
            }
            else
            {
                if (dest.IsInterface(s))
                {
                    return src.GetInterfaces(s).Any(x => x == dest);
                }
                else
                {
                    TypeReference d = src;
                    while (true)
                    {
                        d = d.GetBaseType(s).Value;
                        if (dest == d) return true;
                        if (d == ExistingTypeReference.Object) return false;
                    }
                }
            }
        }
    }

    [Record]
    public sealed class TypeKeyReference : TypeReference
    {
        private readonly TypeKey typeKey;

        public TypeKeyReference(TypeKey typeKey)
        {
            this.typeKey = typeKey;
        }

        [Bind("typeKey")]
        public TypeKey TypeKey { get { return typeKey; } }

        public override ImmutableSortedSet<ItemKey> GetReferences()
        {
            return ImmutableSortedSet<ItemKey>.Empty.Add(typeKey);
        }

        public override Type Resolve(ImmutableSortedDictionary<ItemKey, SaBox<object>> references)
        {
            if (references.ContainsKey(typeKey))
            {
                if (references[typeKey].HasValue)
                {
                    return (TypeBuilder)(references[typeKey].Value);
                }
                else
                {
                    throw new Exception("Attempt to resolve undefined type " + this.ToString());
                }
            }
            else
            {
                throw new Exception("Attempt to resolve unknown type " + this.ToString());
            }
        }

        public override bool IsDelegate => false;

        public override TypeReference[] GetDelegateParameterTypes()
        {
            throw new InvalidOperationException("Not a delegate");
        }

        public override TypeReference GetDelegateReturnType()
        {
            throw new InvalidOperationException("Not a delegate");
        }

        public override bool IsArray
        {
            get { return false; }
        }

        public override TypeReference GetElementType()
        {
            throw new InvalidOperationException("Not an array");
        }

        public override TypeReference MakeArrayType()
        {
            return new ArrayTypeReference(this);
        }

        public override bool IsValueType(SymbolTable s)
        {
            return s[typeKey].IsValueType;
        }

        public override bool IsInterface(SymbolTable s)
        {
            return s[typeKey].IsInterface;
        }

        public override Option<TypeReference> GetBaseType(SymbolTable s)
        {
            return s[typeKey].BaseType;
        }

        public override IEnumerable<TypeReference> GetInterfaces(SymbolTable s)
        {
            return s[typeKey].Interfaces;
        }

        public override string FullName
        {
            get { return typeKey.Name.ToString(); }
        }
    }

    [Record]
    public sealed class ExistingTypeReference : TypeReference
    {
        private readonly Type existingType;

        public ExistingTypeReference(Type existingType)
        {
            this.existingType = existingType;
        }

        [Bind("existingType")]
        public Type ExistingType => existingType;

        public override ImmutableSortedSet<ItemKey> GetReferences()
        {
            return ImmutableSortedSet<ItemKey>.Empty;
        }

        public override Type Resolve(ImmutableSortedDictionary<ItemKey, SaBox<object>> references)
        {
            return existingType;
        }

        #region Constants

        private static readonly Lazy<ExistingTypeReference> theVoid = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(void)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference Void { get { return theVoid.Value; } }

        private static readonly Lazy<ExistingTypeReference> theBool = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(bool)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference Boolean { get { return theBool.Value; } }

        private static readonly Lazy<ExistingTypeReference> theChar = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(char)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference Char { get { return theChar.Value; } }

        private static readonly Lazy<ExistingTypeReference> theString = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(string)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference String { get { return theString.Value; } }

        private static readonly Lazy<ExistingTypeReference> theByte = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(byte)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference Byte { get { return theByte.Value; } }

        private static readonly Lazy<ExistingTypeReference> theInt16 = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(short)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference Int16 { get { return theInt16.Value; } }

        private static readonly Lazy<ExistingTypeReference> theInt32 = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(int)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference Int32 { get { return theInt32.Value; } }

        private static readonly Lazy<ExistingTypeReference> theInt64 = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(long)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference Int64 { get { return theInt64.Value; } }

        private static readonly Lazy<ExistingTypeReference> theIntPtr = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(IntPtr)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference IntPtr { get { return theIntPtr.Value; } }

        private static readonly Lazy<ExistingTypeReference> theSByte = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(sbyte)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference SByte { get { return theSByte.Value; } }

        private static readonly Lazy<ExistingTypeReference> theUInt16 = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(ushort)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference UInt16 { get { return theUInt16.Value; } }

        private static readonly Lazy<ExistingTypeReference> theUInt32 = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(uint)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference UInt32 { get { return theUInt32.Value; } }

        private static readonly Lazy<ExistingTypeReference> theUInt64 = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(ulong)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference UInt64 { get { return theUInt64.Value; } }

        private static readonly Lazy<ExistingTypeReference> theUIntPtr = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(UIntPtr)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference UIntPtr { get { return theUIntPtr.Value; } }

        private static readonly Lazy<ExistingTypeReference> theSingle = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(float)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference Single { get { return theSingle.Value; } }

        private static readonly Lazy<ExistingTypeReference> theDouble = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(double)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference Double { get { return theDouble.Value; } }

        private static readonly Lazy<ExistingTypeReference> theObject = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(object)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference Object { get { return theObject.Value; } }

        private static readonly Lazy<ExistingTypeReference> theType = new Lazy<ExistingTypeReference>(() => new ExistingTypeReference(typeof(Type)), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        public static ExistingTypeReference Type { get { return theType.Value; } }

        #endregion

        public override bool IsDelegate
        {
            get { return existingType.IsSubclassOf(typeof(Delegate)); }
        }

        public override TypeReference[] GetDelegateParameterTypes()
        {
            if (!(existingType.IsSubclassOf(typeof(Delegate)))) throw new InvalidOperationException("Not a delegate");
            MethodInfo mi = existingType.GetMethod("Invoke").AssertNotNull();
            return mi.GetParameters().Select(x => new ExistingTypeReference(x.ParameterType)).ToArray();
        }

        public override TypeReference GetDelegateReturnType()
        {
            if (!(existingType.IsSubclassOf(typeof(Delegate)))) throw new InvalidOperationException("Not a delegate");
            MethodInfo mi = existingType.GetMethod("Invoke").AssertNotNull();
            return new ExistingTypeReference(mi.ReturnType);
        }

        public override bool IsArray
        {
            get { return existingType.IsArray; }
        }

        public override TypeReference GetElementType()
        {
            if (existingType.IsArray)
            {
                return new ExistingTypeReference(existingType.GetElementType().AssertNotNull());
            }
            else
            {
                throw new InvalidOperationException("Call for element type of non-array");
            }
        }

        public override TypeReference MakeArrayType()
        {
            return new ExistingTypeReference(existingType.MakeArrayType());
        }

        public override bool IsValueType(SymbolTable s)
        {
            return existingType.IsValueType;
        }

        public override bool IsInterface(SymbolTable s)
        {
            return existingType.IsInterface;
        }

        public override Option<TypeReference> GetBaseType(SymbolTable s)
        {
            if (existingType.IsClass)
            {
                return Option<TypeReference>.Some(new ExistingTypeReference(existingType.BaseType.AssertNotNull()));
            }
            else
            {
                return Option<TypeReference>.None;
            }
        }

        public override IEnumerable<TypeReference> GetInterfaces(SymbolTable s)
        {
            return existingType.GetInterfaces().Select(x => new ExistingTypeReference(x));
        }

        public override string FullName
        {
            get { return existingType.FullNameNotNull(); }
        }
    }

    [Record]
    public sealed class ExistingGenericTypeReference : TypeReference
    {
        private readonly Type openGenericType;
        private readonly ImmutableList<TypeReference> typeArguments;

        public ExistingGenericTypeReference(Type openGenericType, ImmutableList<TypeReference> typeArguments)
        {
            this.openGenericType = openGenericType;
            this.typeArguments = typeArguments;
        }

        [Bind("openGenericType")]
        public Type OpenGenericType => openGenericType;

        [Bind("typeArguments")]
        public ImmutableList<TypeReference> TypeArguments => typeArguments;

        public override ImmutableSortedSet<ItemKey> GetReferences()
        {
            return ImmutableSortedSet<ItemKey>.Empty.UnionAll(typeArguments.Select(x => x.GetReferences()));
        }

        public override Type Resolve(ImmutableSortedDictionary<ItemKey, SaBox<object>> references)
        {
            Type[] ts = typeArguments.Select(x => x.Resolve(references)).ToArray();
            return openGenericType.MakeGenericType(ts);
        }

        public override bool IsDelegate
        {
            get { return openGenericType.IsSubclassOf(typeof(Delegate)); }
        }

        public override TypeReference[] GetDelegateParameterTypes()
        {
            if (!(openGenericType.IsSubclassOf(typeof(Delegate)))) throw new InvalidOperationException("Not a delegate");
            MethodInfo mi = openGenericType.GetMethod("Invoke").AssertNotNull();
            Type[] genericParameters = openGenericType.GetGenericArguments();
            Func<Type, TypeReference> subst = delegate (Type t)
            {
                int iEnd = genericParameters.Length;
                for (int i = 0; i < iEnd; ++i)
                {
                    if (t == genericParameters[i]) return typeArguments[i];
                }
                return new ExistingTypeReference(t);
            };

            return mi.GetParameters().Select(x => subst(x.ParameterType)).ToArray();
        }

        public override TypeReference GetDelegateReturnType()
        {
            if (!(openGenericType.IsSubclassOf(typeof(Delegate)))) throw new InvalidOperationException("Not a delegate");
            MethodInfo mi = openGenericType.GetMethod("Invoke").AssertNotNull();
            Type[] genericParameters = openGenericType.GetGenericArguments();
            Func<Type, TypeReference> subst = delegate (Type t)
            {
                int iEnd = genericParameters.Length;
                for (int i = 0; i < iEnd; ++i)
                {
                    if (t == genericParameters[i]) return typeArguments[i];
                }
                return new ExistingTypeReference(t);
            };
            return subst(mi.ReturnType);
        }

        public override bool IsArray
        {
            get { return false; }
        }

        public override TypeReference GetElementType()
        {
            throw new InvalidOperationException("Not an array");
        }

        public override TypeReference MakeArrayType()
        {
            return new ArrayTypeReference(this);
        }

        public override bool IsValueType(SymbolTable s)
        {
            return openGenericType.IsValueType;
        }

        public override bool IsInterface(SymbolTable s)
        {
            return openGenericType.IsInterface;
        }

        public override Option<TypeReference> GetBaseType(SymbolTable s)
        {
            if (openGenericType.IsInterface)
            {
                return Option<TypeReference>.None;
            }
            else
            {
                return Option<TypeReference>.Some(new ExistingTypeReference(openGenericType.BaseType.AssertNotNull()));
            }
        }

        public override IEnumerable<TypeReference> GetInterfaces(SymbolTable s)
        {
            return openGenericType.GetInterfaces().Select(x => new ExistingTypeReference(x));
        }

        public override string FullName
        {
            get { return openGenericType.FullNameNotNull(); }
        }
    }

    [Record]
    public sealed class ArrayTypeReference : TypeReference
    {
        private readonly TypeReference elementType;

        public ArrayTypeReference(TypeReference elementType)
        {
            this.elementType = elementType;
        }

        [Bind("elementType")]
        public TypeReference ElementType => elementType;

        public override ImmutableSortedSet<ItemKey> GetReferences()
        {
            return elementType.GetReferences();
        }

        public override Type Resolve(ImmutableSortedDictionary<ItemKey, SaBox<object>> references)
        {
            Type eType = elementType.Resolve(references);
            return eType.MakeArrayType();
        }

        public override bool IsDelegate
        {
            get { return false; }
        }

        public override TypeReference[] GetDelegateParameterTypes()
        {
            throw new InvalidOperationException("Not a delegate");
        }

        public override TypeReference GetDelegateReturnType()
        {
            throw new InvalidOperationException("Not a delegate");
        }

        public override bool IsArray
        {
            get { return true; }
        }

        public override TypeReference GetElementType()
        {
            return elementType;
        }

        public override TypeReference MakeArrayType()
        {
            return new ArrayTypeReference(this);
        }

        public override bool IsValueType(SymbolTable s)
        {
            return false;
        }

        public override bool IsInterface(SymbolTable s)
        {
            return false;
        }

        public override Option<TypeReference> GetBaseType(SymbolTable s)
        {
            return Option<TypeReference>.Some(new ExistingTypeReference(typeof(Array)));
        }

        public override IEnumerable<TypeReference> GetInterfaces(SymbolTable s)
        {
            return typeof(Array).GetInterfaces().Select(x => new ExistingTypeReference(x));
        }

        public override string FullName
        {
            get { return elementType.FullName + "[]"; }
        }
    }

    [Record]
    public sealed class MethodKey : ItemKey
    {
        private readonly TypeKey owner;
        private readonly Symbol name;
        private readonly bool isInstance;
        private readonly ImmutableList<TypeReference> parameters;

        public MethodKey(TypeKey owner, Symbol name, bool isInstance, ImmutableList<TypeReference> parameters)
        {
            this.owner = owner;
            this.name = name;
            this.isInstance = isInstance;
            this.parameters = parameters;
        }

        [Bind("owner")]
        public TypeKey Owner { get { return owner; } }

        [Bind("name")]
        public Symbol Name { get { return name; } }

        [Bind("isInstance")]
        public bool IsInstance { get { return isInstance; } }

        [Bind("parameters")]
        public ImmutableList<TypeReference> Parameters { get { return parameters; } }
    }

    [UnionOfDescendants]
    public abstract class MethodReference : IEquatable<MethodReference>, IComparable<MethodReference>
    {
        public abstract ImmutableSortedSet<ItemKey> GetReferences();

        public abstract MethodInfo Resolve(ImmutableSortedDictionary<ItemKey, SaBox<object>> references);

        public abstract int ParameterCount { get; }

        public abstract TypeReference GetParameterType(int index);

        public abstract TypeReference GetReturnType(SymbolTable s);

#if NETSTANDARD2_0
        public override bool Equals(object obj)
#else
        public override bool Equals(object? obj)
#endif
        {
            if (obj is MethodReference mr)
            {
                return Tools.MethodReferenceCompareWorker.Compare(this, mr) == 0;
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return Tools.MethodReferenceCompareWorker.GetBasicHashCode(this);
        }

        public override string ToString()
        {
            return Tools.MethodReferenceCompareWorker.ToDebugString(this);
        }

#if NETSTANDARD2_0
        public bool Equals(MethodReference other)
#else
        public bool Equals(MethodReference? other)
#endif
        {
            if (other is null) return false;
            return Tools.MethodReferenceCompareWorker.Compare(this, other) == 0;
        }

#if NETSTANDARD2_0
        public int CompareTo(MethodReference other)
#else
        public int CompareTo(MethodReference? other)
#endif
        {
            if (other is null) return 1;
            return Tools.MethodReferenceCompareWorker.Compare(this, other);
        }

#if NETSTANDARD2_0
        public static bool operator ==(MethodReference a, MethodReference b)
#else
        public static bool operator ==(MethodReference? a, MethodReference? b)
#endif
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return Tools.MethodReferenceCompareWorker.Compare(a, b) == 0;
        }

#if NETSTANDARD2_0
        public static bool operator !=(MethodReference a, MethodReference b)
#else
        public static bool operator !=(MethodReference? a, MethodReference? b)
#endif
        {
            if (a is null && b is null) return false;
            if (a is null || b is null) return true;
            return Tools.MethodReferenceCompareWorker.Compare(a, b) != 0;
        }

        public static bool operator <(MethodReference a, MethodReference b) => Tools.MethodReferenceCompareWorker.Compare(a, b) < 0;
        public static bool operator >(MethodReference a, MethodReference b) => Tools.MethodReferenceCompareWorker.Compare(a, b) > 0;
        public static bool operator <=(MethodReference a, MethodReference b) => Tools.MethodReferenceCompareWorker.Compare(a, b) <= 0;
        public static bool operator >=(MethodReference a, MethodReference b) => Tools.MethodReferenceCompareWorker.Compare(a, b) >= 0;
    }

    [Record]
    public sealed class MethodKeyReference : MethodReference
    {
        private readonly MethodKey methodKey;

        public MethodKeyReference(MethodKey methodKey)
        {
            this.methodKey = methodKey;
        }
        
        [Bind("methodKey")]
        public MethodKey MethodKey => methodKey;

        public override ImmutableSortedSet<ItemKey> GetReferences()
        {
            return ImmutableSortedSet<ItemKey>.Empty.Add(methodKey);
        }

        public override MethodInfo Resolve(ImmutableSortedDictionary<ItemKey, SaBox<object>> references)
        {
            return (MethodInfo)(references[methodKey].Value);
        }

        public override int ParameterCount
        {
            get { return methodKey.IsInstance ? methodKey.Parameters.Count + 1 : methodKey.Parameters.Count; }
        }

        public override TypeReference GetParameterType(int index)
        {
            if (methodKey.IsInstance)
            {
                if (index == 0) return new TypeKeyReference(methodKey.Owner);
                else return methodKey.Parameters[index - 1];
            }
            else
            {
                return methodKey.Parameters[index];
            }
        }

        public override TypeReference GetReturnType(SymbolTable s)
        {
            return s[methodKey].ReturnType;
        }
    }

    [Record]
    public sealed class ExistingMethodReference : MethodReference
    {
        private readonly MethodInfo methodInfo;

        public ExistingMethodReference(MethodInfo methodInfo)
        {
            this.methodInfo = methodInfo;
        }

        [Bind("methodInfo")]
        public MethodInfo ExistingMethod => methodInfo;

        public override ImmutableSortedSet<ItemKey> GetReferences()
        {
            return ImmutableSortedSet<ItemKey>.Empty;
        }

        public override MethodInfo Resolve(ImmutableSortedDictionary<ItemKey, SaBox<object>> references)
        {
            return methodInfo;
        }

        public override int ParameterCount
        {
            get { return methodInfo.IsStatic ? methodInfo.GetParameters().Length : (methodInfo.GetParameters().Length + 1); }
        }

        public override TypeReference GetParameterType(int index)
        {
            if (methodInfo.IsStatic)
            {
                return new ExistingTypeReference(methodInfo.GetParameters()[index].ParameterType);
            }
            else
            {
                if (index == 0) return new ExistingTypeReference(methodInfo.ReturnType);
                else return new ExistingTypeReference(methodInfo.GetParameters()[index - 1].ParameterType);
            }
        }

        public override TypeReference GetReturnType(SymbolTable s)
        {
            return new ExistingTypeReference(methodInfo.ReturnType);
        }
    }

    [Record]
    public class ConstructorKey : ItemKey
    {
        private readonly TypeKey owner;
        private readonly ImmutableList<TypeReference> parameters;

        public ConstructorKey(TypeKey owner, ImmutableList<TypeReference> parameters)
        {
            this.owner = owner;
            this.parameters = parameters;
        }

        [Bind("owner")]
        public TypeKey Owner => owner;

        [Bind("parameters")]
        public ImmutableList<TypeReference> Parameters => parameters;
    }

    [UnionOfDescendants]
    public abstract class ConstructorReference : IEquatable<ConstructorReference>, IComparable<ConstructorReference>
    {
#if NETSTANDARD2_0
        public override bool Equals(object obj)
#else
        public override bool Equals(object? obj)
#endif
        {
            if (obj is ConstructorReference ik)
            {
                return Tools.ConstructorReferenceCompareWorker.Compare(this, ik) == 0;
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return Tools.ConstructorReferenceCompareWorker.GetBasicHashCode(this);
        }

#if NETSTANDARD2_0
        public bool Equals(ConstructorReference other)
#else
        public bool Equals(ConstructorReference? other)
#endif
        {
            if (other is null) return false;
            return Tools.ConstructorReferenceCompareWorker.Compare(this, other) == 0;
        }

#if NETSTANDARD2_0
        public int CompareTo(ConstructorReference other)
#else
        public int CompareTo(ConstructorReference? other)
#endif
        {
            if (other is null) return 1;
            return Tools.ConstructorReferenceCompareWorker.Compare(this, other);
        }

#if NETSTANDARD2_0
        public static bool operator ==(ConstructorReference a, ConstructorReference b)
#else
        public static bool operator ==(ConstructorReference? a, ConstructorReference? b)
#endif
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return Tools.ConstructorReferenceCompareWorker.Compare(a, b) == 0;
        }

#if NETSTANDARD2_0
        public static bool operator !=(ConstructorReference a, ConstructorReference b)
#else
        public static bool operator !=(ConstructorReference? a, ConstructorReference? b)
#endif
        {
            if (a is null && b is null) return false;
            if (a is null || b is null) return true;
            return Tools.ConstructorReferenceCompareWorker.Compare(a, b) != 0;
        }

        public static bool operator <(ConstructorReference a, ConstructorReference b) => Tools.ConstructorReferenceCompareWorker.Compare(a, b) < 0;
        public static bool operator >(ConstructorReference a, ConstructorReference b) => Tools.ConstructorReferenceCompareWorker.Compare(a, b) > 0;
        public static bool operator <=(ConstructorReference a, ConstructorReference b) => Tools.ConstructorReferenceCompareWorker.Compare(a, b) <= 0;
        public static bool operator >=(ConstructorReference a, ConstructorReference b) => Tools.ConstructorReferenceCompareWorker.Compare(a, b) >= 0;

        public abstract ImmutableSortedSet<ItemKey> GetReferences();

        public abstract ConstructorInfo Resolve(ImmutableSortedDictionary<ItemKey, SaBox<object>> references);

        public abstract int ParameterCount { get; }

        public abstract TypeReference GetParameterType(int index);

        public abstract TypeReference ConstructorOfWhat { get; }
    }

    [Record]
    public sealed class ConstructorKeyReference : ConstructorReference
    {
        private readonly ConstructorKey constructorKey;

        public ConstructorKeyReference(ConstructorKey constructorKey)
        {
            this.constructorKey = constructorKey;
        }

        [Bind("constructorKey")]
        public ConstructorKey ConstructorKey => constructorKey;

        public override ImmutableSortedSet<ItemKey> GetReferences()
        {
            return ImmutableSortedSet<ItemKey>.Empty.Add(constructorKey);
        }

        public override ConstructorInfo Resolve(ImmutableSortedDictionary<ItemKey, SaBox<object>> references)
        {
            return (ConstructorInfo)(references[constructorKey].Value);
        }

        public override string ToString()
        {
            return "[ConstructorKeyReference, key = " + constructorKey.ToString() + "]";
        }

        public override int ParameterCount
        {
            get { return constructorKey.Parameters.Count; }
        }

        public override TypeReference GetParameterType(int index)
        {
            return constructorKey.Parameters[index];
        }

        public override TypeReference ConstructorOfWhat
        {
            get { return new TypeKeyReference(constructorKey.Owner); }
        }
    }

    [Record]
    public sealed class ExistingConstructorReference : ConstructorReference
    {
        private readonly ConstructorInfo constructorInfo;

        public ExistingConstructorReference(ConstructorInfo constructorInfo)
        {
            this.constructorInfo = constructorInfo;
        }

        [Bind("constructorInfo")]
        public ConstructorInfo ConstructorInfo => constructorInfo;

        public override ImmutableSortedSet<ItemKey> GetReferences()
        {
            return ImmutableSortedSet<ItemKey>.Empty;
        }

        public override ConstructorInfo Resolve(ImmutableSortedDictionary<ItemKey, SaBox<object>> references)
        {
            return constructorInfo;
        }

        public override string ToString()
        {
            return "[ExistingConstructorInfo]";
        }

        public override int ParameterCount
        {
            get { return constructorInfo.GetParameters().Length; }
        }

        public override TypeReference GetParameterType(int index)
        {
            return new ExistingTypeReference(constructorInfo.GetParameters()[index].ParameterType);
        }

        public override TypeReference ConstructorOfWhat
        {
            get { return new ExistingTypeReference(constructorInfo.DeclaringType.AssertNotNull()); }
        }
    }

    [Record]
    public sealed class FieldKey : ItemKey
    {
        private readonly TypeKey owner;
        private readonly Symbol name;
        private readonly TypeReference fieldType;

        public FieldKey(TypeKey owner, Symbol name, TypeReference fieldType)
        {
            this.owner = owner;
            this.name = name;
            this.fieldType = fieldType;
        }

        [Bind("owner")]
        public TypeKey Owner { get { return owner; } }

        [Bind("name")]
        public Symbol Name { get { return name; } }

        [Bind("fieldType")]
        public TypeReference FieldType { get { return fieldType; } }
    }

    [UnionOfDescendants]
    public abstract class FieldReference : IEquatable<FieldReference>, IComparable<FieldReference>
    {
#if NETSTANDARD2_0
        public override bool Equals(object obj)
#else
        public override bool Equals(object? obj)
#endif
        {
            if (obj is FieldReference ik)
            {
                return Tools.FieldReferenceCompareWorker.Compare(this, ik) == 0;
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return Tools.FieldReferenceCompareWorker.GetBasicHashCode(this);
        }

        public override string ToString()
        {
            return Tools.FieldReferenceCompareWorker.ToDebugString(this);
        }

#if NETSTANDARD2_0
        public bool Equals(FieldReference other)
#else
        public bool Equals(FieldReference? other)
#endif
        {
            if (other is null) return false;
            return Tools.FieldReferenceCompareWorker.Compare(this, other) == 0;
        }

#if NETSTANDARD2_0
        public int CompareTo(FieldReference other)
#else
        public int CompareTo(FieldReference? other)
#endif
        {
            if (other is null) return 1;
            return Tools.FieldReferenceCompareWorker.Compare(this, other);
        }

#if NETSTANDARD2_0
        public static bool operator ==(FieldReference a, FieldReference b)
#else
        public static bool operator ==(FieldReference? a, FieldReference? b)
#endif
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return Tools.FieldReferenceCompareWorker.Compare(a, b) == 0;
        }

#if NETSTANDARD2_0
        public static bool operator !=(FieldReference a, FieldReference b)
#else
        public static bool operator !=(FieldReference? a, FieldReference? b)
#endif
        {
            if (a is null && b is null) return false;
            if (a is null || b is null) return true;
            return Tools.FieldReferenceCompareWorker.Compare(a, b) != 0;
        }

        public static bool operator <(FieldReference a, FieldReference b) => Tools.FieldReferenceCompareWorker.Compare(a, b) < 0;
        public static bool operator >(FieldReference a, FieldReference b) => Tools.FieldReferenceCompareWorker.Compare(a, b) > 0;
        public static bool operator <=(FieldReference a, FieldReference b) => Tools.FieldReferenceCompareWorker.Compare(a, b) <= 0;
        public static bool operator >=(FieldReference a, FieldReference b) => Tools.FieldReferenceCompareWorker.Compare(a, b) >= 0;

        public abstract ImmutableSortedSet<ItemKey> GetReferences();

        public abstract FieldInfo Resolve(ImmutableSortedDictionary<ItemKey, SaBox<object>> references);

        public abstract TypeReference Owner { get; }

        public abstract TypeReference FieldType { get; }
    }

    [Record]
    public sealed class FieldKeyReference : FieldReference
    {
        private readonly FieldKey fieldKey;

        public FieldKeyReference(FieldKey fieldKey)
        {
            this.fieldKey = fieldKey;
        }

        [Bind("fieldKey")]
        public FieldKey FieldKey => fieldKey;

        public override ImmutableSortedSet<ItemKey> GetReferences()
        {
            return ImmutableSortedSet<ItemKey>.Empty.Add(fieldKey);
        }

        public override FieldInfo Resolve(ImmutableSortedDictionary<ItemKey, SaBox<object>> references)
        {
            return (FieldInfo)(references[fieldKey].Value);
        }

        public override TypeReference Owner
        {
            get { return new TypeKeyReference(fieldKey.Owner); }
        }

        public override TypeReference FieldType
        {
            get { return fieldKey.FieldType; }
        }
    }

    [Record]
    public sealed class ExistingFieldReference : FieldReference
    {
        private readonly FieldInfo fieldInfo;

        public ExistingFieldReference(FieldInfo fieldInfo)
        {
            this.fieldInfo = fieldInfo;
        }

        [Bind("fieldInfo")]
        public FieldInfo FieldInfo => fieldInfo;

        public override ImmutableSortedSet<ItemKey> GetReferences()
        {
            return ImmutableSortedSet<ItemKey>.Empty;
        }

        public override FieldInfo Resolve(ImmutableSortedDictionary<ItemKey, SaBox<object>> references)
        {
            return fieldInfo;
        }

        public override TypeReference Owner
        {
            get { return new ExistingTypeReference(fieldInfo.DeclaringType.AssertNotNull()); }
        }

        public override TypeReference FieldType
        {
            get { return new ExistingTypeReference(fieldInfo.FieldType); }
        }
    }

    [Record]
    public class PropertyKey : ItemKey
    {
        private readonly TypeKey owner;
        private readonly Symbol name;
        private readonly TypeReference propertyType;
        private readonly ImmutableList<TypeReference> propertyArgs;

        public PropertyKey
        (
        	TypeKey owner,
        	Symbol name,
        	TypeReference propertyType,
        	ImmutableList<TypeReference> propertyArgs
        )
        {
            this.owner = owner;
            this.name = name;
            this.propertyType = propertyType;
            this.propertyArgs = propertyArgs;
        }

        [Bind("owner")]
        public TypeKey Owner => owner;

        [Bind("name")]
        public Symbol Name => name;

        [Bind("propertyType")]
        public TypeReference PropertyType => propertyType;

        [Bind("propertyArgs")]
        public ImmutableList<TypeReference> PropertyArgs => propertyArgs;
    }

    public abstract class ItemAux
    {
    }

    public sealed class TypeAux : ItemAux
    {
        private readonly bool isValueType;
        private readonly bool isInterface;
        private readonly Option<TypeReference> baseType;
        private readonly ImmutableList<TypeReference> interfaces;

        public TypeAux(bool isValueType, bool isInterface, Option<TypeReference> baseType, ImmutableList<TypeReference> interfaces)
        {
            this.isValueType = isValueType;
            this.isInterface = isInterface;
            this.baseType = baseType;
            this.interfaces = interfaces;
        }

        public bool IsValueType => isValueType;

        public bool IsInterface => isInterface;

        public Option<TypeReference> BaseType => baseType;

        public ImmutableList<TypeReference> Interfaces => interfaces;
    }

    public sealed class MethodAux : ItemAux
    {
        private readonly MethodAttributes attributes;
        private readonly TypeReference returnType;

        public MethodAux(MethodAttributes attributes, TypeReference returnType)
        {
            this.attributes = attributes;
            this.returnType = returnType;
        }

        public MethodAttributes Attributes { get { return attributes; } }

        public TypeReference ReturnType { get { return returnType; } }
    }

    public sealed class ConstructorAux : ItemAux
    {
        private readonly MethodAttributes attributes;

        public ConstructorAux(MethodAttributes attributes)
        {
            this.attributes = attributes;
        }

        public MethodAttributes Attributes { get { return attributes; } }
    }

    public sealed class FieldAux : ItemAux
    {
    }

    public sealed class PropertyAux : ItemAux
    {
    }

    public sealed class SymbolTable
    {
        private static readonly SymbolTable empty = new SymbolTable();

        public static SymbolTable Empty => empty;

        private readonly ImmutableSortedDictionary<ItemKey, ItemAux> dict;

        private SymbolTable()
        {
            dict = ImmutableSortedDictionary<ItemKey, ItemAux>.Empty;
        }

        private SymbolTable(ImmutableSortedDictionary<ItemKey, ItemAux> dict)
        {
            this.dict = dict;
        }

        public TypeAux this[TypeKey t]
        {
            get
            {
                return (TypeAux)(dict[t]);
            }
        }

        public SymbolTable SetItem(TypeKey t, TypeAux v)
        {
            if (dict.ContainsKey(t)) throw new InvalidOperationException("Single assignment violation");
            return new SymbolTable(dict.Add(t, v));
        }

        public MethodAux this[MethodKey m]
        {
            get
            {
                return (MethodAux)(dict[m]);
            }
        }

        public SymbolTable SetItem(MethodKey m, MethodAux v)
        {
            if (dict.ContainsKey(m)) throw new InvalidOperationException("Single assignment violation");
            return new SymbolTable(dict.Add(m, v));
        }

        public ConstructorAux this[ConstructorKey c]
        {
            get
            {
                return (ConstructorAux)(dict[c]);
            }
        }

        public SymbolTable SetItem(ConstructorKey c, ConstructorAux v)
        {
            if (dict.ContainsKey(c)) throw new InvalidOperationException("Single assignment violation");
            return new SymbolTable(dict.Add(c, v));
        }

        public FieldAux this[FieldKey f]
        {
            get
            {
                return (FieldAux)(dict[f]);
            }
            set
            {
                if (dict.ContainsKey(f)) throw new InvalidOperationException("Single assignment violation");
                dict.Add(f, value);
            }
        }

        public SymbolTable SetItem(FieldKey f, FieldAux v)
        {
            if (dict.ContainsKey(f)) throw new InvalidOperationException("Single assignment violation");
            return new SymbolTable(dict.Add(f, v));
        }

        public PropertyAux this[PropertyKey p]
        {
            get
            {
                return (PropertyAux)(dict[p]);
            }
        }

        public SymbolTable SetItem(PropertyKey p, PropertyAux v)
        {
            if (dict.ContainsKey(p)) throw new InvalidOperationException("Single assignment violation");
            return new SymbolTable(dict.Add(p, v));
        }
    }
}