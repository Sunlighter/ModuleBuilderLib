using Sunlighter.ModuleBuilderLib;
using Sunlighter.TypeTraitsLib;
using Sunlighter.TypeTraitsLib.Building;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace Sunlighter.ModuleBuilderLib.Pascalesque
{
    public sealed class EnvDescTypesOnly2
    {
        private readonly ImmutableSortedDictionary<Symbol, TypeReference> data;

        private EnvDescTypesOnly2(ImmutableSortedDictionary<Symbol, TypeReference> data)
        {
            this.data = data;
        }

        private static readonly EnvDescTypesOnly2 empty = new EnvDescTypesOnly2(ImmutableSortedDictionary<Symbol, TypeReference>.Empty);

        public static EnvDescTypesOnly2 Empty => empty;

        public EnvDescTypesOnly2 Add(Symbol s, TypeReference t)
        {
            return new EnvDescTypesOnly2(data.Add(s, t));
        }

        public static EnvDescTypesOnly2 FromSequence(IEnumerable<ParamInfo> seq)
        {
            ImmutableSortedDictionary<Symbol, TypeReference> eData = ImmutableSortedDictionary<Symbol, TypeReference>.Empty;
            foreach (ParamInfo t in seq)
            {
                eData = eData.Add(t.Name, t.ParamType);
            }
            return new EnvDescTypesOnly2(eData);
        }

        public static EnvDescTypesOnly2 Shadow(EnvDescTypesOnly2 e, Symbol s, TypeReference t)
        {
            ImmutableSortedDictionary<Symbol, TypeReference> eData = e.data;
            eData.SetItem(s, t);
            return new EnvDescTypesOnly2(eData);
        }

        public static EnvDescTypesOnly2 Shadow(EnvDescTypesOnly2 i, IEnumerable<ParamInfo> symbols)
        {
            ImmutableSortedDictionary<Symbol, TypeReference> iData = i.data;
            foreach (ParamInfo t in symbols)
            {
                iData = iData.SetItem(t.Name, t.ParamType);
            }
            return new EnvDescTypesOnly2(iData);
        }

        public bool ContainsKey(Symbol s) { return data.ContainsKey(s); }

        public ImmutableHashSet<Symbol> Keys
        {
            get
            {
                return data.Keys.ToImmutableHashSet();
            }
        }

        public TypeReference this[Symbol s]
        {
            get
            {
                return data[s];
            }
        }
    }

    public static partial class PascalesqueExtensions
    {
        public static bool IsBoxedType(this TypeReference t)
        {
            if (!(t is ExistingGenericTypeReference)) return false;
            ExistingGenericTypeReference et = (ExistingGenericTypeReference)t;
            if (!(et.OpenGenericType == typeof(Box<>))) return false;
            if (et.TypeArguments.Count != 1) return false;
            return true;
        }

        public static TypeReference MakeBoxedType(this TypeReference t)
        {
            if (IsBoxedType(t)) throw new ArgumentException("Nested boxes are not allowed");
            return new ExistingGenericTypeReference(typeof(Box<>), ImmutableList<TypeReference>.Empty.Add(t));
        }
    }
    public interface IVarDesc2
    {
        TypeReference VarType { get; }
        bool IsBoxed { get; }
        void Fetch(CompileContext2 cc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail);
        void Store(CompileContext2 cc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, Action exprToStore, bool tail);
        void FetchBox(CompileContext2 cc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail);
    }

    public sealed class LocalVarDesc2 : IVarDesc2
    {
        private readonly TypeReference varType;
        private readonly bool isBoxed;
        private readonly int index;

        public LocalVarDesc2(TypeReference varType, bool isBoxed, int localIndex)
        {
            this.varType = varType;
            this.isBoxed = isBoxed;
            index = localIndex;
        }

        public int LocalIndex { get { return index; } }

        #region IVarDesc Members

        public TypeReference VarType { get { return varType; } }

        public bool IsBoxed { get { return isBoxed; } }

        public void Fetch(CompileContext2 cc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            ilg.LoadLocal(index);
            if (isBoxed)
            {
                if (tail) ilg.Tail();
                ilg.Call(PascalesqueExtensions.MakeBoxedType(varType).Resolve(references).GetProperty("Value").AssertNotNull().GetGetMethod().AssertNotNull());
                if (tail) ilg.Return();
            }
            else
            {
                if (tail) ilg.Return();
            }
        }

        public void Store(CompileContext2 cc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, Action writeExprToStore, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            if (isBoxed)
            {
                ilg.LoadLocal(index);
                writeExprToStore();
                if (tail) ilg.Tail();
                ilg.Call(PascalesqueExtensions.MakeBoxedType(varType).Resolve(references).GetProperty("Value").AssertNotNull().GetSetMethod().AssertNotNull());
                if (tail) ilg.Return();
            }
            else
            {
                writeExprToStore();
                ilg.StoreLocal(index);
                if (tail) ilg.Return();
            }
        }

        public void FetchBox(CompileContext2 cc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;
            if (isBoxed)
            {
                ilg.LoadLocal(index);
                if (tail) ilg.Return();
            }
            else
            {
                throw new PascalesqueException("Tried to fetch the box of an unboxed local");
            }
        }

        #endregion
    }

    public sealed class ArgVarDesc2 : IVarDesc2
    {
        private readonly TypeReference varType;
        private readonly bool isBoxed;
        private readonly int index;

        public ArgVarDesc2(TypeReference varType, bool isBoxed, int index)
        {
            this.varType = varType;
            this.isBoxed = isBoxed;
            this.index = index;
        }

        #region IVarDesc2 Members

        public TypeReference VarType { get { return varType; } }

        public bool IsBoxed { get { return isBoxed; } }

        public void Fetch(CompileContext2 cc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            ilg.LoadArg(index);
            if (isBoxed)
            {
                if (tail) ilg.Tail();
                ilg.Call(PascalesqueExtensions.MakeBoxedType(varType).Resolve(references).GetProperty("Value").AssertNotNull().GetGetMethod().AssertNotNull());
                if (tail) ilg.Return();
            }
            else
            {
                if (tail) ilg.Return();
            }
        }

        public void Store(CompileContext2 cc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, Action writeExprToStore, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            if (isBoxed)
            {
                ilg.LoadArg(index);
                writeExprToStore();
                if (tail) ilg.Tail();
                ilg.Call(PascalesqueExtensions.MakeBoxedType(varType).Resolve(references).GetProperty("Value").AssertNotNull().GetSetMethod().AssertNotNull());
                if (tail) ilg.Return();
            }
            else
            {
                writeExprToStore();
                ilg.StoreArg(index);
                if (tail) ilg.Return();
            }
        }

        public void FetchBox(CompileContext2 cc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;
            if (isBoxed)
            {
                ilg.LoadLocal(index);
                if (tail) ilg.Return();
            }
            else
            {
                throw new PascalesqueException("Tried to fetch the box of an unboxed argument");
            }
        }

        #endregion
    }

    public sealed class FieldVarDesc2 : IVarDesc2
    {
        private readonly IVarDesc2 fieldOfWhat;
        private readonly FieldInfo fieldInfo;
        private readonly TypeReference varType;
        private readonly bool isBoxed;

        public FieldVarDesc2(IVarDesc2 fieldOfWhat, FieldInfo fieldInfo, TypeReference varType, bool isBoxed)
        {
            this.fieldOfWhat = fieldOfWhat;
            this.fieldInfo = fieldInfo;
            this.varType = varType;
            this.isBoxed = isBoxed;
        }

        #region IVarDesc Members

        public TypeReference VarType
        {
            get
            {
                return varType;
            }
        }

        public bool IsBoxed { get { return isBoxed; } }

        public void Fetch(CompileContext2 cc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            if (isBoxed && fieldInfo.FieldType != PascalesqueExtensions.MakeBoxedType(varType).Resolve(references)) throw new PascalesqueException("Field type isn't boxed type");
            ILGenerator ilg = cc.ILGenerator;

            fieldOfWhat.Fetch(cc, references, false);
            ilg.LoadField(fieldInfo);
            if (tail) ilg.Tail();
            ilg.Call(fieldInfo.FieldType.GetProperty("Value").AssertNotNull().GetGetMethod().AssertNotNull());
            if (tail) ilg.Return();
        }

        public void Store(CompileContext2 cc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, Action writeExprToStore, bool tail)
        {
            if (isBoxed && fieldInfo.FieldType != PascalesqueExtensions.MakeBoxedType(varType).Resolve(references)) throw new PascalesqueException("Field type isn't boxed type");

            ILGenerator ilg = cc.ILGenerator;

            fieldOfWhat.Fetch(cc, references, false);
            if (isBoxed)
            {
                ilg.LoadField(fieldInfo);
                writeExprToStore();
                if (tail) ilg.Tail();
                ilg.Call(fieldInfo.FieldType.GetProperty("Value").AssertNotNull().GetSetMethod().AssertNotNull());
            }
            else
            {
                writeExprToStore();
                ilg.StoreField(fieldInfo);
            }
            if (tail) ilg.Return();
        }

        public void FetchBox(CompileContext2 cc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            if (isBoxed && fieldInfo.FieldType != PascalesqueExtensions.MakeBoxedType(varType).Resolve(references)) throw new PascalesqueException("Field type isn't boxed type");

            ILGenerator ilg = cc.ILGenerator;

            if (IsBoxed)
            {
                fieldOfWhat.Fetch(cc, references, false);
                if (tail) ilg.Tail();
                ilg.LoadField(fieldInfo);
                if (tail) ilg.Return();
            }
            else
            {
                throw new PascalesqueException("Tried to fetch the box of an unboxed field");
            }
        }

        #endregion
    }

    public sealed class EnvDesc2
    {
        private readonly ImmutableSortedDictionary<Symbol, IVarDesc2> data;

        private EnvDesc2(ImmutableSortedDictionary<Symbol, IVarDesc2> data)
        {
            this.data = data;
        }

        private static readonly EnvDesc2 empty = new EnvDesc2(ImmutableSortedDictionary<Symbol, IVarDesc2>.Empty);

        public static EnvDesc2 Empty => empty;

        public EnvDesc2 Add(Symbol s, IVarDesc2 v)
        {
            return new EnvDesc2(data.Add(s, v));
        }

        public static EnvDesc2 FromSequence(IEnumerable<Tuple<Symbol, IVarDesc2>> seq)
        {
            ImmutableSortedDictionary<Symbol, IVarDesc2> data = ImmutableSortedDictionary<Symbol, IVarDesc2>.Empty;
            foreach (Tuple<Symbol, IVarDesc2> t in seq)
            {
                data = data.Add(t.Item1, t.Item2);
            }
            return new EnvDesc2(data);
        }

        public static EnvDesc2 Shadow(EnvDesc2 e, Symbol s, IVarDesc2 v)
        {
            return new EnvDesc2(e.data.SetItem(s, v));
        }

        public static EnvDesc2 Shadow(EnvDesc2 a, IEnumerable<Tuple<Symbol, IVarDesc2>> symbols)
        {
            ImmutableSortedDictionary<Symbol, IVarDesc2> data = a.data;
            foreach (Tuple<Symbol, IVarDesc2> t in symbols)
            {
                data = data.SetItem(t.Item1, t.Item2);
            }
            return new EnvDesc2(data);
        }

        public EnvDescTypesOnly2 TypesOnly()
        {
            return EnvDescTypesOnly2.FromSequence(data.Select(x => new ParamInfo(x.Key, x.Value.VarType)));
        }

        public bool ContainsKey(Symbol s) { return data.ContainsKey(s); }

        public ImmutableHashSet<Symbol> Keys
        {
            get
            {
                return data.Keys.ToImmutableHashSet();
            }
        }

        public IVarDesc2 this[Symbol s]
        {
            get
            {
                return data[s];
            }
        }
    }

    public sealed class CompileContext2
    {
        private readonly ILGenerator ilg;
        private readonly bool isConstructor;

        public CompileContext2(ILGenerator ilg, bool isConstructor)
        {
            this.ilg = ilg;
            this.isConstructor = isConstructor;
        }

        public ILGenerator ILGenerator => ilg;

        public bool IsConstructor => isConstructor;
    }

    [UnionOfDescendants]
    public abstract class Expression2
    {
        public abstract EnvSpec GetEnvSpec();
        public abstract TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc);
        public abstract ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc);
        public abstract ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc);
        public abstract void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail);
    }

    public static class LiteralArgumentTraits
    {
        private static readonly Lazy<ITypeTraits<object>> value = new Lazy<ITypeTraits<object>>(GetLiteralArgumentTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        private static ITypeTraits<object> GetLiteralArgumentTraits()
        {
            return new UnionTypeTraits<byte, object>
            (
                ByteTypeTraits.Value,
                ImmutableList<IUnionCaseTypeTraits<byte, object>>.Empty.AddRange
                (
                    new IUnionCaseTypeTraits<byte, object>[]
                    {
                        new UnionCaseTypeTraits<byte, object, byte>
                        (
                            1,
                            x => x is byte,
                            x => (byte)x,
                            ByteTypeTraits.Value,
                            b => b
                        ),
                        new UnionCaseTypeTraits<byte, object, sbyte>
                        (
                            2,
                            x => x is sbyte,
                            x => (sbyte)x,
                            SByteTypeTraits.Value,
                            b => b
                        ),
                        new UnionCaseTypeTraits<byte, object, short>
                        (
                            3,
                            x => x is short,
                            x => (short)x,
                            Int16TypeTraits.Value,
                            b => b
                        ),
                        new UnionCaseTypeTraits<byte, object, ushort>
                        (
                            4,
                            x => x is ushort,
                            x => (ushort)x,
                            UInt16TypeTraits.Value,
                            b => b
                        ),
                        new UnionCaseTypeTraits<byte, object, int>
                        (
                            5,
                            x => x is int,
                            x => (int)x,
                            Int32TypeTraits.Value,
                            b => b
                        ),
                        new UnionCaseTypeTraits<byte, object, uint>
                        (
                            6,
                            x => x is uint,
                            x => (uint)x,
                            UInt32TypeTraits.Value,
                            b => b
                        ),
                        new UnionCaseTypeTraits<byte, object, long>
                        (
                            7,
                            x => x is long,
                            x => (long)x,
                            Int64TypeTraits.Value,
                            b => b
                        ),
                        new UnionCaseTypeTraits<byte, object, ulong>
                        (
                            8,
                            x => x is ulong,
                            x => (ulong)x,
                            UInt64TypeTraits.Value,
                            b => b
                        ),
                        new UnionCaseTypeTraits<byte, object, long>
                        (
                            9,
                            x => x is IntPtr,
                            x => (long)(IntPtr)x,
                            Int64TypeTraits.Value,
                            x => unchecked((IntPtr)x)
                        ),
                        new UnionCaseTypeTraits<byte, object, ulong>
                        (
                            10,
                            x => x is UIntPtr,
                            x => (ulong)(UIntPtr)x,
                            UInt64TypeTraits.Value,
                            x => unchecked((UIntPtr)x)
                        ),
                        new UnionCaseTypeTraits<byte, object, bool>
                        (
                            11,
                            x => x is bool,
                            x => (bool)x,
                            BooleanTypeTraits.Value,
                            b => b
                        ),
                        new UnionCaseTypeTraits<byte, object, char>
                        (
                            12,
                            x => x is char,
                            x => (char)x,
                            CharTypeTraits.Value,
                            b => b
                        ),
                        new UnionCaseTypeTraits<byte, object, float>
                        (
                            13,
                            x => x is float,
                            x => (float)x,
                            SingleTypeTraits.Value,
                            b => b
                        ),
                        new UnionCaseTypeTraits<byte, object, double>
                        (
                            14,
                            x => x is double,
                            x => (double)x,
                            DoubleTypeTraits.Value,
                            b => b
                        ),
                        new UnionCaseTypeTraits<byte, object, string>
                        (
                            15,
                            x => x is string,
                            x => (string)x,
                            StringTypeTraits.Value,
                            b => b
                        ),
                        new UnionCaseTypeTraits<byte, object, Type>
                        (
                            16,
                            x => x is Type,
                            x => (Type)x,
                            TypeTypeTraits.Value,
                            b => b
                        ),
                        new UnionCaseTypeTraits<byte, object, (Type, byte)>
                        (
                            17,
                            x => x is Enum && Enum.GetUnderlyingType(x.GetType()) == typeof(byte),
                            x => (x.GetType(), Convert.ToByte(x)),
                            new ValueTupleTypeTraits<Type, byte>(TypeTypeTraits.Value, ByteTypeTraits.Value),
                            x => Enum.ToObject(x.Item1, x.Item2)
                        ),
                        new UnionCaseTypeTraits<byte, object, (Type, sbyte)>
                        (
                            18,
                            x => x is Enum && Enum.GetUnderlyingType(x.GetType()) == typeof(sbyte),
                            x => (x.GetType(), Convert.ToSByte(x)),
                            new ValueTupleTypeTraits<Type, sbyte>(TypeTypeTraits.Value, SByteTypeTraits.Value),
                            x => Enum.ToObject(x.Item1, x.Item2)
                        ),
                        new UnionCaseTypeTraits<byte, object, (Type, ushort)>
                        (
                            19,
                            x => x is Enum && Enum.GetUnderlyingType(x.GetType()) == typeof(ushort),
                            x => (x.GetType(), Convert.ToUInt16(x)),
                            new ValueTupleTypeTraits<Type, ushort>(TypeTypeTraits.Value, UInt16TypeTraits.Value),
                            x => Enum.ToObject(x.Item1, x.Item2)
                        ),
                        new UnionCaseTypeTraits<byte, object, (Type, short)>
                        (
                            20,
                            x => x is Enum && Enum.GetUnderlyingType(x.GetType()) == typeof(short),
                            x => (x.GetType(), Convert.ToInt16(x)),
                            new ValueTupleTypeTraits<Type, short>(TypeTypeTraits.Value, Int16TypeTraits.Value),
                            x => Enum.ToObject(x.Item1, x.Item2)
                        ),
                        new UnionCaseTypeTraits<byte, object, (Type, uint)>
                        (
                            21,
                            x => x is Enum && Enum.GetUnderlyingType(x.GetType()) == typeof(uint),
                            x => (x.GetType(), Convert.ToUInt32(x)),
                            new ValueTupleTypeTraits<Type, uint>(TypeTypeTraits.Value, UInt32TypeTraits.Value),
                            x => Enum.ToObject(x.Item1, x.Item2)
                        ),
                        new UnionCaseTypeTraits<byte, object, (Type, int)>
                        (
                            22,
                            x => x is Enum && Enum.GetUnderlyingType(x.GetType()) == typeof(int),
                            x => (x.GetType(), Convert.ToInt32(x)),
                            new ValueTupleTypeTraits<Type, int>(TypeTypeTraits.Value, Int32TypeTraits.Value),
                            x => Enum.ToObject(x.Item1, x.Item2)
                        ),
                        new UnionCaseTypeTraits<byte, object, (Type, ulong)>
                        (
                            23,
                            x => x is Enum && Enum.GetUnderlyingType(x.GetType()) == typeof(ulong),
                            x => (x.GetType(), Convert.ToUInt64(x)),
                            new ValueTupleTypeTraits<Type, ulong>(TypeTypeTraits.Value, UInt64TypeTraits.Value),
                            x => Enum.ToObject(x.Item1, x.Item2)
                        ),
                        new UnionCaseTypeTraits<byte, object, (Type, long)>
                        (
                            24,
                            x => x is Enum && Enum.GetUnderlyingType(x.GetType()) == typeof(long),
                            x => (x.GetType(), Convert.ToInt64(x)),
                            new ValueTupleTypeTraits<Type, long>(TypeTypeTraits.Value, Int64TypeTraits.Value),
                            x => Enum.ToObject(x.Item1, x.Item2)
                        ),
                    }
                )
            );
        }

        public static ITypeTraits<object> Value => value.Value;
    }

    [Record]
    public sealed class LiteralExpr2 : Expression2
    {
        private readonly object val;

        public LiteralExpr2(object val)
        {
            this.val = val;
        }

        #warning Need to extend TypeTraitsLib to allow specifying that LiteralArgumentTraits should be used for this value
        [Bind("val")]
        public object Value => val;

        public override EnvSpec GetEnvSpec()
        {
            return EnvSpec.Empty();
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            return new ExistingTypeReference(val.GetType());
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return ImmutableList<ICompileStep>.Empty;
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return ImmutableSortedSet<ItemKey>.Empty;
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            object val2 = val;

            if (val.GetType().IsEnum)
            {
                Type ut = Enum.GetUnderlyingType(val.GetType());
                if (ut == typeof(byte))
                {
                    val2 = Convert.ToByte(val);
                }
                else if (ut == typeof(sbyte))
                {
                    val2 = Convert.ToSByte(val);
                }
                else if (ut == typeof(ushort))
                {
                    val2 = Convert.ToUInt16(val);
                }
                else if (ut == typeof(short))
                {
                    val2 = Convert.ToInt16(val);
                }
                else if (ut == typeof(uint))
                {
                    val2 = Convert.ToUInt32(val);
                }
                else if (ut == typeof(int))
                {
                    val2 = Convert.ToInt32(val);
                }
                else if (ut == typeof(ulong))
                {
                    val2 = Convert.ToUInt64(val);
                }
                else if (ut == typeof(long))
                {
                    val2 = Convert.ToInt64(val);
                }
                else
                {
                    throw new PascalesqueException("Enum with unsupported underlying type");
                }
            }

            if (val2.GetType() == typeof(byte))
            {
                ilg.LoadInt((int)(byte)val);
            }
            else if (val2.GetType() == typeof(sbyte))
            {
                ilg.LoadInt((int)(sbyte)val);
            }
            else if (val2.GetType() == typeof(short))
            {
                ilg.LoadInt((int)(short)val);
            }
            else if (val2.GetType() == typeof(ushort))
            {
                ilg.LoadInt((int)(ushort)val);
            }
            else if (val2.GetType() == typeof(int))
            {
                ilg.LoadInt((int)val);
            }
            else if (val2.GetType() == typeof(uint))
            {
                ilg.LoadInt((int)(uint)val);
            }
            else if (val2.GetType() == typeof(IntPtr))
            {
                ilg.LoadLong((long)(IntPtr)val);
                ilg.Conv_I();
            }
            else if (val2.GetType() == typeof(UIntPtr))
            {
                ilg.LoadLong((long)(ulong)(UIntPtr)val);
                ilg.Conv_U();
            }
            else if (val2.GetType() == typeof(long))
            {
                ilg.LoadLong((long)val);
            }
            else if (val2.GetType() == typeof(ulong))
            {
                ilg.LoadLong((long)(ulong)val);
            }
            else if (val2.GetType() == typeof(bool))
            {
                ilg.LoadInt(((bool)val) ? 1 : 0);
            }
            else if (val2.GetType() == typeof(float))
            {
                ilg.LoadFloat((float)val);
            }
            else if (val2.GetType() == typeof(double))
            {
                ilg.LoadDouble((double)val);
            }
            else if (val2.GetType() == typeof(char))
            {
                ilg.LoadInt((int)(char)val);
            }
            else if (val2.GetType() == typeof(string))
            {
                ilg.LoadString((string)val);
            }
            else if (val2.GetType() == typeof(Type))
            {
                ilg.LoadToken((Type)val2);
                ilg.Call(typeof(Type).GetMethod("GetTypeFromHandle").AssertNotNull());
            }
            else
            {
                throw new PascalesqueException("Literal of unsupported type");
            }

            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class VarRefExpr2 : Expression2
    {
        private readonly Symbol name;

        public VarRefExpr2(Symbol name)
        {
            this.name = name;
        }

        [Bind("name")]
        public Symbol Name => name;

        public override EnvSpec GetEnvSpec()
        {
            return EnvSpec.Singleton(name, new VarSpec(false, false));
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            return envDesc[name];
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return ImmutableList<ICompileStep>.Empty;
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return GetReturnType(s, envDesc).GetReferences();
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            envDesc[name].Fetch(cc, references, tail);
        }
    }

    [Record]
    public sealed class VarSetExpr2 : Expression2
    {
        private readonly Symbol name;
        private readonly Expression2 val;

        public VarSetExpr2(Symbol name, Expression2 val)
        {
            this.name = name;
            this.val = val;
        }

        [Bind("name")]
        public Symbol Name => name;

        [Bind("val")]
        public Expression2 Val => val;

        public override EnvSpec GetEnvSpec()
        {
            EnvSpec es = val.GetEnvSpec();
            return EnvSpec.Add(es, name, new VarSpec(true, false));
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            if (envDesc[name] != val.GetReturnType(s, envDesc)) throw new PascalesqueException("Type mismatch in VarSet");
            return ExistingTypeReference.Void;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return val.GetCompileSteps(s, owner, envDesc);
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return GetReturnType(s, envDesc).GetReferences();
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            if (envDesc[name].VarType != val.GetReturnType(s, envDesc.TypesOnly())) throw new PascalesqueException("Type mismatch in VarSet");

            envDesc[name].Store(cc, references, delegate () { val.Compile(s, owner, cc, envDesc, references, false); }, tail);
        }
    }

    [Record]
    public sealed class BeginExpr2 : Expression2
    {
        private readonly ImmutableList<Expression2> body;

        public BeginExpr2(ImmutableList<Expression2> body)
        {
            this.body = body;
        }

        [Bind("body")]
        public ImmutableList<Expression2> Body => body;

        public override EnvSpec GetEnvSpec()
        {
            EnvSpec e = EnvSpec.Empty();

            foreach (Expression2 expr in body)
            {
                e |= expr.GetEnvSpec();
            }

            return e;
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            return body[body.Count - 1].GetReturnType(s, envDesc);
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return body.SelectMany(b => b.GetCompileSteps(s, owner, envDesc)).ToImmutableList();
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return body.Select(expr => expr.GetReferences(s, owner, envDesc)).UnionAll();
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            int iEnd = body.Count;
            EnvDescTypesOnly2 edto = envDesc.TypesOnly();

            for (int i = 0; i < iEnd; ++i)
            {
                bool isLast = (i + 1 == iEnd);

                body[i].Compile(s, owner, cc, envDesc, references, tail && isLast);
                if (!isLast && body[i].GetReturnType(s, edto) != ExistingTypeReference.Void)
                {
                    ilg.Pop();
                }
            }

            if (tail) ilg.Return();
        }

        public static Expression2 FromList(ImmutableList<Expression2> exprs)
        {
            if (exprs.Count == 0)
            {
                return EmptyExpr2.Value;
            }
            else if (exprs.Count == 1)
            {
                return exprs[0];
            }
            else
            {
                return new BeginExpr2(exprs);
            }
        }
    }

    [Singleton(0x77127836u)]
    public sealed class EmptyExpr2 : Expression2
    {
        private static readonly EmptyExpr2 value = new EmptyExpr2();

        public static EmptyExpr2 Value => value;

        private EmptyExpr2() { }

        public override EnvSpec GetEnvSpec()
        {
            return EnvSpec.Empty();
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            return ExistingTypeReference.Void;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return ImmutableList<ICompileStep>.Empty;
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return ImmutableSortedSet<ItemKey>.Empty;
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            if (tail) cc.ILGenerator.Return();
        }
    }

    [Record]
    public sealed class AndExpr2 : Expression2
    {
        private readonly ImmutableList<Expression2> body;

        public AndExpr2(ImmutableList<Expression2> body)
        {
            this.body = body;
        }

        [Bind("body")]
        public ImmutableList<Expression2> Body => body;

        public override EnvSpec GetEnvSpec()
        {
            EnvSpec e = EnvSpec.Empty();

            foreach (Expression2 expr in body)
            {
                e |= expr.GetEnvSpec();
            }

            return e;
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            foreach (Expression2 e in body)
            {
                if (e.GetReturnType(s, envDesc) != ExistingTypeReference.Boolean) throw new PascalesqueException("Elements in an \"and\" must be boolean");
            }
            return ExistingTypeReference.Boolean;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return body.SelectMany(b => b.GetCompileSteps(s, owner, envDesc)).ToImmutableList();
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return body.Select(expr => expr.GetReferences(s, owner, envDesc)).UnionAll();
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            int iEnd = body.Count;

            if (iEnd == 0)
            {
                ilg.LoadInt(1);
            }
            else
            {
                Label lEnd = ilg.DefineLabel();

                for (int i = 0; i < iEnd; ++i)
                {
                    bool isLast = (i + 1 == iEnd);

                    body[i].Compile(s, owner, cc, envDesc, references, tail && isLast);
                    if (!isLast)
                    {
                        ilg.Dup();
                        ilg.Emit(OpCodes.Brfalse, lEnd);
                    }
                }
                ilg.MarkLabel(lEnd);
            }
            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class OrExpr2 : Expression2
    {
        private readonly ImmutableList<Expression2> body;

        public OrExpr2(ImmutableList<Expression2> body)
        {
            this.body = body;
        }

        [Bind("body")]
        public ImmutableList<Expression2> Body => body;

        public override EnvSpec GetEnvSpec()
        {
            EnvSpec e = EnvSpec.Empty();

            foreach (Expression2 expr in body)
            {
                e |= expr.GetEnvSpec();
            }

            return e;
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            foreach (Expression2 e in body)
            {
                if (e.GetReturnType(s, envDesc) != ExistingTypeReference.Boolean) throw new PascalesqueException("Elements in an \"and\" must be boolean");
            }
            return ExistingTypeReference.Boolean;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return body.SelectMany(b => b.GetCompileSteps(s, owner, envDesc)).ToImmutableList();
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return body.Select(expr => expr.GetReferences(s, owner, envDesc)).UnionAll();
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            int iEnd = body.Count;

            if (iEnd == 0)
            {
                ilg.LoadInt(1);
            }
            else
            {
                Label lEnd = ilg.DefineLabel();

                for (int i = 0; i < iEnd; ++i)
                {
                    bool isLast = (i + 1 == iEnd);

                    body[i].Compile(s, owner, cc, envDesc, references, tail && isLast);
                    if (!isLast)
                    {
                        ilg.Dup();
                        ilg.Emit(OpCodes.Brtrue, lEnd);
                    }
                }
                ilg.MarkLabel(lEnd);
            }
            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class IfThenElseExpr2 : Expression2
    {
        private readonly Expression2 condition;
        private readonly Expression2 consequent;
        private readonly Expression2 alternate;

        public IfThenElseExpr2(Expression2 condition, Expression2 consequent, Expression2 alternate)
        {
            this.condition = condition;
            this.consequent = consequent;
            this.alternate = alternate;
        }

        [Bind("condition")]
        public Expression2 Condition => condition;

        [Bind("consequent")]
        public Expression2 Consequent => consequent;

        [Bind("alternate")]
        public Expression2 Alternate => alternate;

        public override EnvSpec GetEnvSpec()
        {
            EnvSpec e = condition.GetEnvSpec() | consequent.GetEnvSpec() | alternate.GetEnvSpec();
            return e;
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            if (condition.GetReturnType(s, envDesc) != ExistingTypeReference.Boolean) throw new PascalesqueException("type of condition must be bool");
            if (consequent.GetReturnType(s, envDesc) != alternate.GetReturnType(s, envDesc)) throw new PascalesqueException("type of consequent and alternate must match");

            return consequent.GetReturnType(s, envDesc);
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return ImmutableList<ICompileStep>.Empty
                .AddRange(condition.GetCompileSteps(s, owner, envDesc))
                .AddRange(consequent.GetCompileSteps(s, owner, envDesc))
                .AddRange(alternate.GetCompileSteps(s, owner, envDesc));
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return condition.GetReferences(s, owner, envDesc).Union(consequent.GetReferences(s, owner, envDesc)).Union(alternate.GetReferences(s, owner, envDesc));
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            Label one = ilg.DefineLabel();
            Label two = ilg.DefineLabel();

            condition.Compile(s, owner, cc, envDesc, references, false);
            ilg.Emit(OpCodes.Brfalse, one);
            consequent.Compile(s, owner, cc, envDesc, references, tail);
            if (!tail) ilg.Emit(OpCodes.Br, two);
            ilg.MarkLabel(one);
            alternate.Compile(s, owner, cc, envDesc, references, tail);
            ilg.MarkLabel(two);
        }
    }

    [Record]
    public sealed class SwitchExpr2 : Expression2
    {
        private readonly Expression2 switchOnWhat;
        private readonly Expression2 defaultExpr;
        private readonly ImmutableList<Tuple<ImmutableHashSet<uint>, Expression2>> targetExprs;

        public SwitchExpr2(Expression2 switchOnWhat, Expression2 defaultExpr, ImmutableList<Tuple<ImmutableHashSet<uint>, Expression2>> targetExprs)
        {
            this.switchOnWhat = switchOnWhat;
            this.defaultExpr = defaultExpr;
            this.targetExprs = targetExprs;
        }

        [Bind("switchOnWhat")]
        public Expression2 SwitchOnWhat => switchOnWhat;

        [Bind("defaultExpr")]
        public Expression2 DefaultExpression => defaultExpr;

        [Bind("targetExprs")]
        public ImmutableList<Tuple<ImmutableHashSet<uint>, Expression2>> TargetExpressions => targetExprs;

        public override EnvSpec GetEnvSpec()
        {
            EnvSpec e = switchOnWhat.GetEnvSpec() | defaultExpr.GetEnvSpec();
            foreach (Tuple<ImmutableHashSet<uint>, Expression2> kvp in targetExprs)
            {
                e |= kvp.Item2.GetEnvSpec();
            }
            return e;
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            if (switchOnWhat.GetReturnType(s, envDesc) != ExistingTypeReference.UInt32) throw new PascalesqueException("SwitchExpr: SwitchOnWhat must be of type uint");

            uint max = targetExprs.Select(x => x.Item1.Max()).Max();
            if (max > 255u) throw new PascalesqueException("Switch has more than 256 destinations");

            bool[] b1 = new bool[max + 1];

            TypeReference t = defaultExpr.GetReturnType(s, envDesc);
            foreach (Tuple<ImmutableHashSet<uint>, Expression2> kvp in targetExprs)
            {
                foreach (uint u in kvp.Item1)
                {
                    if (b1[(int)u]) throw new PascalesqueException("Switch error: A value can go to only one expression");
                    b1[(int)u] = true;
                }

                TypeReference t2 = kvp.Item2.GetReturnType(s, envDesc);
                if (t != t2) throw new PascalesqueException("SwitchExpr: All alternatives must be of the same type");
            }

            return t;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return ImmutableList<ICompileStep>.Empty
                .AddRange(switchOnWhat.GetCompileSteps(s, owner, envDesc))
                .AddRange(defaultExpr.GetCompileSteps(s, owner, envDesc))
                .AddRange(targetExprs.SelectMany(t => t.Item2.GetCompileSteps(s, owner, envDesc)));
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return switchOnWhat.GetReferences(s, owner, envDesc).Union(defaultExpr.GetReferences(s, owner, envDesc)).Union(targetExprs.Select(x => x.Item2.GetReferences(s, owner, envDesc)).UnionAll());
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            List<Label> labelList = new List<Label>();
            foreach (Tuple<ImmutableHashSet<uint>, Expression2> item in targetExprs)
            {
                Label l0 = cc.ILGenerator.DefineLabel();
                labelList.Add(l0);
            }

            Label lDefault = cc.ILGenerator.DefineLabel();

            uint max = targetExprs.Select(x => x.Item1.Max()).Max();
            if (max > 255u) throw new PascalesqueException("Switch has more than 256 destinations");

            bool[] assigned = new bool[max + 1];
            Label[] larr = new Label[max + 1];
            int iEnd = targetExprs.Count;
            for (int i = 0; i < iEnd; ++i)
            {
                Tuple<ImmutableHashSet<uint>, Expression2> item = targetExprs[i];
                foreach (uint u in item.Item1)
                {
                    if (assigned[u]) throw new PascalesqueException("A value can go to only one expression");
                    larr[u] = labelList[i];
                    assigned[u] = true;
                }
            }
            for (uint j = 0; j < max; ++j)
            {
                if (!assigned[j]) larr[j] = lDefault;
            }

            switchOnWhat.Compile(s, owner, cc, envDesc, references, false);

            Label? lEnd = tail ? (Label?)null : (Label?)(cc.ILGenerator.DefineLabel());

            cc.ILGenerator.Emit(OpCodes.Switch, larr);

            cc.ILGenerator.MarkLabel(lDefault);
            defaultExpr.Compile(s, owner, cc, envDesc, references, tail);

            for (int i = 0; i < iEnd; ++i)
            {
                if (!tail) { System.Diagnostics.Debug.Assert(lEnd.HasValue); cc.ILGenerator.Emit(OpCodes.Br, lEnd.Value); }
                cc.ILGenerator.MarkLabel(labelList[i]);
                targetExprs[i].Item2.Compile(s, owner, cc, envDesc, references, tail);
            }

            if (!tail) { System.Diagnostics.Debug.Assert(lEnd.HasValue); cc.ILGenerator.MarkLabel(lEnd.Value); }
        }
    }

    [Record]
    public sealed class LetClause2
    {
        private readonly Symbol name;
        private readonly TypeReference varType;
        private readonly Expression2 val;

        public LetClause2(Symbol name, TypeReference varType, Expression2 val)
        {
            this.name = name;
            this.varType = varType;
            this.val = val;
        }

        [Bind("name")]
        public Symbol Name => name;

        [Bind("varType")]
        public TypeReference VarType => varType;

        [Bind("val")]
        public Expression2 Value => val;
    }

    [Record]
    public sealed class LetExpr2 : Expression2
    {
        private readonly ImmutableList<LetClause2> clauses;
        private readonly Expression2 body;

        public LetExpr2(ImmutableList<LetClause2> clauses, Expression2 body)
        {
            this.clauses = clauses;
            this.body = body;

            if (clauses.Select(x => x.Name).HasDuplicates()) throw new PascalesqueException("Duplicate variables in letrec");
        }

        [Bind("clauses")]
        public ImmutableList<LetClause2> Clauses => clauses;

        [Bind("body")]
        public Expression2 Body => body;

        public override EnvSpec GetEnvSpec()
        {
            EnvSpec e = body.GetEnvSpec() - clauses.Select(x => x.Name);
            foreach (LetClause2 lc in clauses)
            {
                e |= lc.Value.GetEnvSpec();
            }
            return e;
        }

        private EnvDescTypesOnly2 MakeInnerEnvDesc(EnvDescTypesOnly2 outerEnvDesc)
        {
            return EnvDescTypesOnly2.Shadow(outerEnvDesc, clauses.Select(x => new ParamInfo(x.Name, x.VarType)));
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            if (clauses.Select(x => x.Name).HasDuplicates()) throw new PascalesqueException("let has two variables with the same name");
            if (clauses.Any(x => x.VarType != x.Value.GetReturnType(s, envDesc))) throw new PascalesqueException("a variable's type does not match that of its initializer");

            EnvDescTypesOnly2 e2 = MakeInnerEnvDesc(envDesc);

            return body.GetReturnType(s, e2);
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            EnvDescTypesOnly2 e2 = MakeInnerEnvDesc(envDesc);

            return ImmutableList<ICompileStep>.Empty
                .AddRange(clauses.SelectMany(c => c.Value.GetCompileSteps(s, owner, envDesc)))
                .AddRange(body.GetCompileSteps(s, owner, e2));
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            EnvDescTypesOnly2 e2 = MakeInnerEnvDesc(envDesc);
            return clauses.Select(x => x.Value.GetReferences(s, owner, envDesc)).UnionAll().Union(body.GetReferences(s, owner, e2));
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            EnvSpec e = body.GetEnvSpec();

            List<Tuple<Symbol, IVarDesc2>> theList = new List<Tuple<Symbol, IVarDesc2>>();

            foreach (LetClause2 l in clauses)
            {
                bool boxed = false;
                if (e.ContainsKey(l.Name))
                {
                    boxed = e[l.Name].IsCaptured;
                }

                LocalBuilder lb = ilg.DeclareLocal((boxed ? PascalesqueExtensions.MakeBoxedType(l.VarType) : l.VarType).Resolve(references));
                IVarDesc2 varDesc = new LocalVarDesc2(l.VarType, boxed, lb.LocalIndex);

                theList.Add(new Tuple<Symbol, IVarDesc2>(l.Name, varDesc));
                if (boxed)
                {
                    ilg.NewObj(PascalesqueExtensions.MakeBoxedType(l.VarType).Resolve(references).GetConstructor(Type.EmptyTypes).AssertNotNull());
                    ilg.StoreLocal(lb);
                }
                varDesc.Store(cc, references, delegate () { l.Value.Compile(s, owner, cc, envDesc, references, false); }, false);
            }

            EnvDesc2 innerEnvDesc = EnvDesc2.Shadow(envDesc, theList);

            body.Compile(s, owner, cc, innerEnvDesc, references, tail);
        }
    }

    [Record]
    public sealed class LetStarExpr2 : Expression2
    {
        private readonly ImmutableList<LetClause2> clauses;
        private readonly Expression2 body;

        public LetStarExpr2(ImmutableList<LetClause2> clauses, Expression2 body)
        {
            this.clauses = clauses;
            this.body = body;
        }

        [Bind("clauses")]
        public ImmutableList<LetClause2> Clauses => clauses;

        [Bind("body")]
        public Expression2 Body => body;

        private EnvSpec GetEnvSpec(int j)
        {
            EnvSpec e = body.GetEnvSpec();
            int i = clauses.Count;
            while (i > j)
            {
                --i;
                LetClause2 lc = clauses[i];
                e -= lc.Name;
                e |= lc.Value.GetEnvSpec();
            }
            return e;
        }

        public override EnvSpec GetEnvSpec()
        {
            return GetEnvSpec(0);
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            EnvDescTypesOnly2 e2 = envDesc;

            foreach (LetClause2 lc in clauses)
            {
                if (lc.Value.GetReturnType(s, e2) != lc.VarType) throw new PascalesqueException("a variable's type does not match that of its initializer");
                e2 = EnvDescTypesOnly2.Shadow(e2, lc.Name, lc.VarType);
            }

            return body.GetReturnType(s, e2);
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            ImmutableList<ICompileStep> results = ImmutableList<ICompileStep>.Empty;
            EnvDescTypesOnly2 e2 = envDesc;
            int iEnd = clauses.Count;
            for (int i = 0; i < iEnd; ++i)
            {
                results = results.AddRange(clauses[i].Value.GetCompileSteps(s, owner, e2));
                e2 = EnvDescTypesOnly2.Shadow(e2, clauses[i].Name, clauses[i].VarType);
            }
            results = results.AddRange(body.GetCompileSteps(s, owner, e2));
            return results;
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            ImmutableSortedSet<ItemKey> h = ImmutableSortedSet<ItemKey>.Empty;
            EnvDescTypesOnly2 e2 = envDesc;
            int iEnd = clauses.Count;
            for (int i = 0; i < iEnd; ++i)
            {
                h = h.Union(clauses[i].Value.GetReferences(s, owner, e2));
                e2 = EnvDescTypesOnly2.Shadow(e2, clauses[i].Name, clauses[i].VarType);
            }
            h = h.Union(body.GetReferences(s, owner, e2));
            return h;
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            EnvDesc2 e2 = envDesc;
            int iEnd = clauses.Count;
            for (int i = 0; i < iEnd; ++i)
            {
                LetClause2 l = clauses[i];
                EnvSpec e = GetEnvSpec(i + 1);
                bool boxed = false;
                if (e.ContainsKey(l.Name))
                {
                    boxed = e[l.Name].IsCaptured;
                }

                LocalBuilder lb = ilg.DeclareLocal((boxed ? PascalesqueExtensions.MakeBoxedType(l.VarType) : l.VarType).Resolve(references));
                IVarDesc2 varDesc = new LocalVarDesc2(l.VarType, boxed, lb.LocalIndex);

                if (boxed)
                {
                    ilg.NewObj(PascalesqueExtensions.MakeBoxedType(l.VarType).Resolve(references).GetConstructor(Type.EmptyTypes).AssertNotNull());
                    ilg.StoreLocal(lb);
                }
                varDesc.Store(cc, references, delegate () { l.Value.Compile(s, owner, cc, e2, references, false); }, false);

                e2 = EnvDesc2.Shadow(e2, l.Name, varDesc);
            }

            body.Compile(s, owner, cc, e2, references, tail);
        }
    }

    [Record]
    public sealed class LetRecExpr2 : Expression2
    {
        private readonly ImmutableList<LetClause2> clauses;
        private readonly Expression2 body;

        public LetRecExpr2(ImmutableList<LetClause2> clauses, Expression2 body)
        {
            this.clauses = clauses;
            this.body = body;

            if (clauses.Select(x => x.Name).HasDuplicates()) throw new PascalesqueException("Duplicate variables in letrec");
        }

        [Bind("clauses")]
        public ImmutableList<LetClause2> Clauses => clauses;

        [Bind("body")]
        public Expression2 Body => body;

        public override EnvSpec GetEnvSpec()
        {
            EnvSpec e = body.GetEnvSpec();
            foreach (LetClause2 lc in clauses)
            {
                e |= lc.Value.GetEnvSpec();
            }
            return e - clauses.Select(x => x.Name);
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            EnvDescTypesOnly2 innerEnvDesc = EnvDescTypesOnly2.Shadow(envDesc, clauses.Select(x => new ParamInfo(x.Name, x.VarType)));
            return body.GetReturnType(s, innerEnvDesc);
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            ImmutableList<ParamInfo> theList =
                clauses.Select(l => new ParamInfo(l.Name, l.VarType)).ToImmutableList();

            EnvDescTypesOnly2 innerEnvDesc = EnvDescTypesOnly2.Shadow(envDesc, theList);

            return ImmutableList<ICompileStep>.Empty
                .AddRange(clauses.SelectMany(c => c.Value.GetCompileSteps(s, owner, innerEnvDesc)))
                .AddRange(body.GetCompileSteps(s, owner, innerEnvDesc));
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            ImmutableSortedSet<ItemKey> h = ImmutableSortedSet<ItemKey>.Empty;
            List<ParamInfo> theList = new List<ParamInfo>();
            foreach (LetClause2 l in clauses)
            {
                theList.Add(new ParamInfo(l.Name, l.VarType));
            }
            EnvDescTypesOnly2 innerEnvDesc = EnvDescTypesOnly2.Shadow(envDesc, theList);
            foreach (LetClause2 l in clauses)
            {
                h = h.Union(l.Value.GetReferences(s, owner, innerEnvDesc));
            }
            h = h.Union(body.GetReferences(s, owner, innerEnvDesc));
            return h;
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            List<Tuple<Symbol, IVarDesc2>> theList = new List<Tuple<Symbol, IVarDesc2>>();

            foreach (LetClause2 l in clauses)
            {
                LocalBuilder lb = ilg.DeclareLocal(PascalesqueExtensions.MakeBoxedType(l.VarType).Resolve(references));
                IVarDesc2 varDesc = new LocalVarDesc2(l.VarType, true, lb.LocalIndex);

                theList.Add(new Tuple<Symbol, IVarDesc2>(l.Name, varDesc));
                ilg.NewObj(PascalesqueExtensions.MakeBoxedType(l.VarType).Resolve(references).GetConstructor(Type.EmptyTypes).AssertNotNull());
                ilg.StoreLocal(lb);
            }

            EnvDesc2 innerEnvDesc = EnvDesc2.Shadow(envDesc, theList);

            int iEnd = theList.Count;
            for (int i = 0; i < iEnd; ++i)
            {
                IVarDesc2 varDesc = theList[i].Item2;
                varDesc.Store(cc, references, delegate () { clauses[i].Value.Compile(s, owner, cc, innerEnvDesc, references, false); }, false);
            }

            body.Compile(s, owner, cc, innerEnvDesc, references, tail);
        }
    }

    [Record]
    public sealed class LetLoopExpr2 : Expression2
    {
        private readonly Symbol loopName;
        private readonly TypeReference loopReturnType;
        private readonly ImmutableList<LetClause2> clauses;
        private readonly Expression2 body;

        private readonly Lazy<Expression2> equivalency;

        public LetLoopExpr2(Symbol loopName, TypeReference loopReturnType, ImmutableList<LetClause2> clauses, Expression2 body)
        {
            this.loopName = loopName;
            this.loopReturnType = loopReturnType;
            this.clauses = clauses;
            this.body = body;

            if (clauses.Select(x => x.Name).HasDuplicates()) throw new PascalesqueException("Duplicate variables in let loop");

            equivalency = new Lazy<Expression2>(MakeEquivalency, false);
        }

        [Bind("loopName")]
        public Symbol LoopName => loopName;

        [Bind("loopReturnType")]
        public TypeReference LoopReturnType => loopReturnType;

        [Bind("clauses")]
        public ImmutableList<LetClause2> Clauses => clauses;

        [Bind("body")]
        public Expression2 Body => body;

        private Expression2 MakeEquivalency()
        {
            TypeReference funcType = GetFuncType();

            return new LetRecExpr2
            (
                ImmutableList<LetClause2>.Empty.Add
                (
                    new LetClause2
                    (
                        loopName, funcType,
                        new LambdaExpr2
                        (
                            clauses.Select(x => new ParamInfo(x.Name, x.VarType)).ToImmutableList(),
                            body
                        )
                    )
                ),
                new InvokeExpr2
                (
                    new VarRefExpr2(loopName),
                    clauses.Select(x => x.Value).ToImmutableList()
                )
            );
        }

        public override EnvSpec GetEnvSpec()
        {
            EnvSpec e = body.GetEnvSpec();
            foreach (LetClause2 lc in clauses)
            {
                e |= lc.Value.GetEnvSpec();
            }
            return e - clauses.Select(x => x.Name);
        }

        private TypeReference GetFuncType()
        {
            if (loopReturnType == ExistingTypeReference.Void)
            {
                return TypeReference.GetActionType(clauses.Select(x => x.VarType).ToImmutableList());
            }
            else
            {
                return TypeReference.GetFuncType(clauses.Select(x => x.VarType).AndAlso(loopReturnType).ToImmutableList());
            }
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            TypeReference funcType = GetFuncType();

            EnvDescTypesOnly2 innerEnvDesc = EnvDescTypesOnly2.Shadow(envDesc, clauses.Select(x => new ParamInfo(x.Name, x.VarType)).AndAlso(new ParamInfo(loopName, funcType)));
            TypeReference t = body.GetReturnType(s, innerEnvDesc);
            if (t != loopReturnType) throw new PascalesqueException("let loop: loop does not return expected type");

            return t;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return equivalency.Value.GetCompileSteps(s, owner, envDesc);
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return equivalency.Value.GetReferences(s, owner, envDesc);
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            equivalency.Value.Compile(s, owner, cc, envDesc, references, tail);
        }
    }

    [Record]
    public sealed class LambdaExpr2 : Expression2
    {
        private readonly ImmutableList<ParamInfo> parameters;
        private readonly Expression2 body;
        private readonly Symbol lambdaObjTypeName;

        public LambdaExpr2(ImmutableList<ParamInfo> parameters, Expression2 body)
        {
            this.parameters = parameters;
            this.body = body;
            this.lambdaObjTypeName = Symbol.Gensym();
        }

        [Bind("parameters")]
        public ImmutableList<ParamInfo> Parameters => parameters;

        [Bind("body")]
        public Expression2 Body => body;

        public override EnvSpec GetEnvSpec()
        {
            EnvSpec e = body.GetEnvSpec() - parameters.Select(x => x.Name);
            return EnvSpec.CaptureAll(e);
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            EnvDescTypesOnly2 innerEnvDesc = EnvDescTypesOnly2.Shadow(envDesc, parameters.Select(x => new ParamInfo(x.Name, x.ParamType)));
            TypeReference returnType = body.GetReturnType(s, innerEnvDesc);
            if (returnType == ExistingTypeReference.Void)
            {
                return TypeReference.GetActionType(parameters.Select(x => x.ParamType).ToImmutableList());
            }
            else
            {
                ImmutableList<TypeReference> t = parameters.Select(x => x.ParamType).ToImmutableList();
                t = t.Add(returnType);

                return TypeReference.GetFuncType(t);
            }
        }

        private sealed class CompileStepInfo
        {
            private readonly LambdaExpr2 parent;
            private readonly SymbolTable symbolTable;
            private readonly EnvDescTypesOnly2 envDesc;

            private readonly Lazy<TypeKey> classKey;
            private readonly Lazy<ConstructorKey> constructorKey;
            private readonly Lazy<MethodKey> methodKey;
            private readonly Lazy<ImmutableList<FieldKey>> fieldKeys;
            private readonly Lazy<TypeReference> methodReturnType;
            private readonly Lazy<EnvDescTypesOnly2> innerEnvDesc;
            private readonly Lazy<CompletedTypeKey> completedClassKey;

            public CompileStepInfo(LambdaExpr2 parent, SymbolTable symbolTable, EnvDescTypesOnly2 envDesc)
            {
                this.parent = parent;
                this.symbolTable = symbolTable;
                this.envDesc = envDesc;

                this.classKey = new Lazy<TypeKey>(MakeClassKey, false);
                this.constructorKey = new Lazy<ConstructorKey>(MakeConstructorKey, false);
                this.methodKey = new Lazy<MethodKey>(MakeMethodKey, false);
                this.fieldKeys = new Lazy<ImmutableList<FieldKey>>(MakeFieldKeys, false);
                this.methodReturnType = new Lazy<TypeReference>(MakeReturnType, false);
                this.innerEnvDesc = new Lazy<EnvDescTypesOnly2>(MakeInnerEnvDesc, false);
                this.completedClassKey = new Lazy<CompletedTypeKey>(MakeCompletedClassKey, false);
            }

            public SymbolTable SymbolTable { get { return symbolTable; } }

            private TypeKey MakeClassKey()
            {
                return new TypeKey(parent.lambdaObjTypeName);
            }

            public TypeKey ClassKey { get { return classKey.Value; } }

            private CompletedTypeKey MakeCompletedClassKey()
            {
                return new CompletedTypeKey(parent.lambdaObjTypeName);
            }

            public CompletedTypeKey CompletedClassKey { get { return completedClassKey.Value; } }

            private ConstructorKey MakeConstructorKey()
            {
                EnvSpec e = parent.body.GetEnvSpec() - parent.parameters.Select(x => x.Name);
                Symbol[] capturedVars = e.Keys.ToArray();

                ImmutableList<TypeReference> constructorParams = capturedVars.Select(s => PascalesqueExtensions.MakeBoxedType(envDesc[s])).ToImmutableList();

                return new ConstructorKey(classKey.Value, constructorParams);
            }

            public ConstructorKey ConstructorKey { get { return constructorKey.Value; } }

            private TypeReference MakeReturnType()
            {
                return parent.GetReturnType(symbolTable, envDesc);
            }

            public TypeReference MethodReturnType { get { return methodReturnType.Value; } }

            private MethodKey MakeMethodKey()
            {
                return new MethodKey(classKey.Value, "Invoke", true, parent.parameters.Select(x => x.ParamType).ToImmutableList());
            }

            public MethodKey MethodKey { get { return methodKey.Value; } }

            public Symbol ClassName { get { return parent.lambdaObjTypeName; } }

            private ImmutableList<FieldKey> MakeFieldKeys()
            {
                ConstructorKey c = constructorKey.Value;
                int iEnd = c.Parameters.Count;
                ImmutableList<FieldKey> farr = ImmutableList<FieldKey>.Empty;
                for (int i = 0; i < iEnd; ++i)
                {
                    farr = farr.Add(new FieldKey(classKey.Value, Symbol.Gensym(), c.Parameters[i]));
                }
                return farr;
            }

            public ImmutableList<FieldKey> FieldKeys { get { return fieldKeys.Value; } }

            private EnvDescTypesOnly2 MakeInnerEnvDesc()
            {
                return EnvDescTypesOnly2.Shadow(envDesc, parent.parameters.Select(x => new ParamInfo(x.Name, x.ParamType)));
            }

            public EnvDescTypesOnly2 InnerEnvDesc { get { return innerEnvDesc.Value; } }

            public ImmutableList<ParamInfo> Parameters { get { return parent.parameters; } }

            public Expression2 Body { get { return parent.body; } }
        }

        private sealed class MakeClass : ICompileStep
        {
            private readonly CompileStepInfo info;

            public MakeClass(CompileStepInfo info)
            {
                this.info = info;
            }

            #region ICompileStep Members

            public int Phase
            {
                get { return 1; }
            }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get { return ImmutableSortedSet<ItemKey>.Empty; }
            }

            public ImmutableSortedSet<ItemKey> Outputs
            {
                get { return ImmutableSortedSet<ItemKey>.Empty.Add(info.ClassKey); }
            }

            public void Compile(ModuleBuilder mb, ImmutableSortedDictionary<ItemKey, SaBox<object>> vars)
            {
                TypeKey t = info.ClassKey;
                TypeBuilder tyb = mb.DefineType(info.ClassName.SymbolName(), TypeAttributes.Public);
                vars[t].Value = tyb;
            }

            #endregion
        }

        private sealed class MakeConstructor : ICompileStep
        {
            private readonly CompileStepInfo info;

            public MakeConstructor(CompileStepInfo info)
            {
                this.info = info;
            }

            #region ICompileStep Members

            public int Phase
            {
                get { return 1; }
            }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get
                {
                    return info.ConstructorKey.Parameters.Select(x => x.GetReferences()).UnionAll().Add(info.ClassKey);
                }
            }

            public ImmutableSortedSet<ItemKey> Outputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty.Add(info.ConstructorKey);
                }
            }

            public void Compile(ModuleBuilder mb, ImmutableSortedDictionary<ItemKey, SaBox<object>> vars)
            {
                TypeBuilder t = (TypeBuilder)(vars[info.ClassKey].Value);
                ConstructorBuilder cb = t.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, info.ConstructorKey.Parameters.Select(x => x.Resolve(vars)).ToArray());
                vars[info.ConstructorKey].Value = cb;
            }

            #endregion
        }

        private sealed class MakeField : ICompileStep
        {
            private readonly CompileStepInfo info;
            private readonly int fieldIndex;

            public MakeField(CompileStepInfo info, int fieldIndex)
            {
                this.info = info;
                this.fieldIndex = fieldIndex;
            }

            #region ICompileStep Members

            public int Phase { get { return 1; } }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty.Add(info.ClassKey);
                }
            }

            public ImmutableSortedSet<ItemKey> Outputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty.Add(info.FieldKeys[fieldIndex]);
                }
            }

            public void Compile(ModuleBuilder mb, ImmutableSortedDictionary<ItemKey, SaBox<object>> vars)
            {
                TypeBuilder tyb = (TypeBuilder)(vars[info.ClassKey].Value);
                FieldKey fk = info.FieldKeys[fieldIndex];
                FieldBuilder fb = tyb.DefineField(fk.Name.SymbolName(), fk.FieldType.Resolve(vars), FieldAttributes.Private);
                vars[fk].Value = fb;
            }

            #endregion
        }

        private sealed class MakeInvokeMethod : ICompileStep
        {
            private readonly CompileStepInfo info;

            public MakeInvokeMethod(CompileStepInfo info)
            {
                this.info = info;
            }

            #region ICompileStep Members

            public int Phase { get { return 1; } }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty.Add(info.ClassKey).Union(info.MethodReturnType.GetReferences());
                }
            }

            public ImmutableSortedSet<ItemKey> Outputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty.Add(info.MethodKey);
                }
            }

            public void Compile(ModuleBuilder mb, ImmutableSortedDictionary<ItemKey, SaBox<object>> vars)
            {
                TypeBuilder tyb = (TypeBuilder)(vars[info.ClassKey].Value);
                MethodKey mk = info.MethodKey;
                TypeReference returnType = info.MethodReturnType;
                MethodBuilder meb = tyb.DefineMethod(mk.Name.SymbolName(), MethodAttributes.Public, returnType.Resolve(vars), mk.Parameters.Select(x => x.Resolve(vars)).ToArray());
                vars[mk].Value = meb;
            }

            #endregion
        }

        private sealed class MakeConstructorBody : ICompileStep
        {
            private readonly CompileStepInfo info;

            public MakeConstructorBody(CompileStepInfo info)
            {
                this.info = info;
            }

            #region ICompileStep Members

            public int Phase { get { return 1; } }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty.Add(info.ConstructorKey).Union(info.FieldKeys);
                }
            }

            public ImmutableSortedSet<ItemKey> Outputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty;
                }
            }

            public void Compile(ModuleBuilder mb, ImmutableSortedDictionary<ItemKey, SaBox<object>> vars)
            {
                ConstructorBuilder cb = (ConstructorBuilder)(vars[info.ConstructorKey].Value);
                FieldBuilder[] lambdaFields = info.FieldKeys.Select(x => vars[x].Value).Cast<FieldBuilder>().ToArray();

                ILGenerator cilg = cb.GetILGenerator();
                int iEnd = lambdaFields.Length;

                cilg.LoadArg(0);
                cilg.Call(typeof(object).GetConstructor(Type.EmptyTypes).AssertNotNull());

                for (int i = 0; i < iEnd; ++i)
                {
                    cilg.LoadArg(0);
                    cilg.LoadArg(i + 1);
                    cilg.StoreField(lambdaFields[i]);
                }

                cilg.Return();
            }

            #endregion
        }

        private sealed class MakeInvokeMethodBody : ICompileStep
        {
            private readonly CompileStepInfo info;

            public MakeInvokeMethodBody(CompileStepInfo info)
            {
                this.info = info;
            }

            #region ICompileStep Members

            public int Phase { get { return 1; } }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty.Add(info.ClassKey)
                        .Add(info.MethodKey)
                        .Union(info.FieldKeys)
                        .Union(info.Body.GetReferences(info.SymbolTable, info.ClassKey, info.InnerEnvDesc));
                }
            }

            public ImmutableSortedSet<ItemKey> Outputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty;
                }
            }

            public void Compile(ModuleBuilder mb, ImmutableSortedDictionary<ItemKey, SaBox<object>> vars)
            {
                TypeBuilder lambdaObj = (TypeBuilder)(vars[info.ClassKey].Value);
                MethodBuilder meb = (MethodBuilder)(vars[info.MethodKey].Value);
                FieldBuilder[] lambdaFields = info.FieldKeys.Select(x => vars[x].Value).Cast<FieldBuilder>().ToArray();
                Symbol[] capturedVars = info.FieldKeys.Select(x => x.Name).ToArray();
                TypeReference lambdaObjRef = new TypeKeyReference(info.ClassKey);
                EnvDescTypesOnly2 innerEnvDesc = info.InnerEnvDesc;

                int iEnd = lambdaFields.Length;
                List<Tuple<Symbol, IVarDesc2>> innerVars = new List<Tuple<Symbol, IVarDesc2>>();

                for (int i = 0; i < iEnd; ++i)
                {
                    innerVars.Add(new Tuple<Symbol, IVarDesc2>(capturedVars[i], new FieldVarDesc2(new ArgVarDesc2(lambdaObjRef, false, 0), lambdaFields[i], innerEnvDesc[capturedVars[i]], true)));
                }
                int jEnd = info.Parameters.Count;
                for (int j = 0; j < jEnd; ++j)
                {
                    innerVars.Add(new Tuple<Symbol, IVarDesc2>(info.Parameters[j].Name, new ArgVarDesc2(info.Parameters[j].ParamType, false, j + 1)));
                }

                ILGenerator milg = meb.GetILGenerator();

                EnvDesc2 innerEnvDesc2 = EnvDesc2.FromSequence(innerVars);

                CompileContext2 cc2 = new CompileContext2(milg, false);

                info.Body.Compile(info.SymbolTable, info.ClassKey, cc2, innerEnvDesc2, vars, true);
            }

            #endregion
        }

        private sealed class BakeClass : ICompileStep
        {
            private readonly CompileStepInfo info;

            public BakeClass(CompileStepInfo info)
            {
                this.info = info;
            }

            #region ICompileStep Members

            public int Phase
            {
                get { return 2; }
            }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty.Add(info.ClassKey).Add(info.ConstructorKey);
                }
            }

            public ImmutableSortedSet<ItemKey> Outputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty.Add(info.CompletedClassKey);
                }
            }

            public void Compile(ModuleBuilder mb, ImmutableSortedDictionary<ItemKey, SaBox<object>> vars)
            {
                TypeBuilder tyb = (TypeBuilder)(vars[info.ClassKey].Value);

#if NETSTANDARD2_0
                Type t = (Type)tyb.CreateTypeInfo();
#else
                Type t = tyb.CreateType();
#endif

                vars[info.CompletedClassKey].Value = t;
            }

#endregion
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            CompileStepInfo info = new CompileStepInfo(this, s, envDesc);

            EnvDescTypesOnly2 innerEnvDesc = EnvDescTypesOnly2.Shadow(envDesc, parameters.Select(x => new ParamInfo(x.Name, x.ParamType)));

            return ImmutableList<ICompileStep>.Empty
                .Add(new MakeClass(info))
                .Add(new MakeConstructor(info))
                .Add(new MakeInvokeMethod(info))
                .AddRange(Enumerable.Range(0, info.FieldKeys.Count).Select(i => new MakeField(info, i)))
                .Add(new MakeConstructorBody(info))
                .Add(new MakeInvokeMethodBody(info))
                .AddRange(body.GetCompileSteps(s, new TypeKey(lambdaObjTypeName), innerEnvDesc))
                .Add(new BakeClass(info));
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            ImmutableSortedSet<ItemKey> h = ImmutableSortedSet<ItemKey>.Empty;
            EnvDescTypesOnly2 innerEnvDesc = EnvDescTypesOnly2.Shadow(envDesc, parameters.Select(x => new ParamInfo(x.Name, x.ParamType)));
            h = h.Union(body.GetReferences(s, new TypeKey(lambdaObjTypeName), innerEnvDesc));
            h = h.Union(GetReturnType(s, envDesc).GetReferences());

            CompileStepInfo csi = new CompileStepInfo(this, s, envDesc);
            h = h.Add(csi.ClassKey);
            h = h.Add(csi.ConstructorKey);
            h = h.Add(csi.MethodKey);

            return h;
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            CompileStepInfo info = new CompileStepInfo(this, s, envDesc.TypesOnly());

            TypeBuilder lambdaObjType = (TypeBuilder)(references[info.ClassKey].Value);
            ConstructorBuilder constructor = (ConstructorBuilder)(references[info.ConstructorKey].Value);
            MethodBuilder invokeMethod = (MethodBuilder)(references[info.MethodKey].Value);

            Symbol[] capturedVars = info.FieldKeys.Select(x => x.Name).ToArray();
            int iEnd = capturedVars.Length;

            ILGenerator ilg = cc.ILGenerator;
            for (int i = 0; i < iEnd; ++i)
            {
                envDesc[capturedVars[i]].FetchBox(cc, references, false);
            }

            ilg.NewObj(constructor);

            ilg.LoadFunction(invokeMethod);

            TypeReference dTypeRef = GetReturnType(s, envDesc.TypesOnly());
            Type dType = dTypeRef.Resolve(references);

            ConstructorInfo[] dci = dType.GetConstructors();

            ilg.NewObj(dType.GetConstructor(new Type[] { typeof(object), typeof(IntPtr) }).AssertNotNull());
            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class InvokeExpr2 : Expression2
    {
        private readonly Expression2 func;
        private readonly ImmutableList<Expression2> args;

        public InvokeExpr2(Expression2 func, ImmutableList<Expression2> args)
        {
            this.func = func;
            this.args = args;
        }

        [Bind("func")]
        public Expression2 Func => func;

        [Bind("args")]
        public ImmutableList<Expression2> Args => args;

        public override EnvSpec GetEnvSpec()
        {
            EnvSpec e = func.GetEnvSpec();
            foreach (Expression2 arg in args)
            {
                e |= arg.GetEnvSpec();
            }
            return e;
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            TypeReference funcType = func.GetReturnType(s, envDesc);

            if (!(funcType.IsDelegate)) throw new PascalesqueException("Invocation of a non-delegate");

            TypeReference[] p = funcType.GetDelegateParameterTypes();
            if (p.Length != args.Count) throw new PascalesqueException("Argument count doesn't match parameter count");

            int iEnd = p.Length;
            for (int i = 0; i < iEnd; ++i)
            {
                if (p[i] != args[i].GetReturnType(s, envDesc)) throw new PascalesqueException("Argument " + i + " type doesn't match parameter type");
            }

            return funcType.GetDelegateReturnType();
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return func.GetCompileSteps(s, owner, envDesc)
                .AddRange(args.SelectMany(arg => arg.GetCompileSteps(s, owner, envDesc)));
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return func.GetReferences(s, owner, envDesc).Union(args.Select(x => x.GetReferences(s, owner, envDesc)).UnionAll());
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            TypeReference funcType = func.GetReturnType(s, envDesc.TypesOnly());

            func.Compile(s, owner, cc, envDesc, references, false);
            foreach (Expression2 arg in args)
            {
                arg.Compile(s, owner, cc, envDesc, references, false);
            }
            ILGenerator ilg = cc.ILGenerator;
            if (tail) ilg.Tail();
            ilg.CallVirt(funcType.Resolve(references).GetMethod("Invoke").AssertNotNull());
            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class BinaryOpExpr2 : Expression2
    {
        private readonly BinaryOp op;
        private readonly Expression2 addend1;
        private readonly Expression2 addend2;

        public BinaryOpExpr2(BinaryOp op, Expression2 addend1, Expression2 addend2)
        {
            this.op = op;
            this.addend1 = addend1;
            this.addend2 = addend2;
        }

        [Bind("op")]
        public BinaryOp Op => op;

        [Bind("addend1")]
        public Expression2 Addend1 => addend1;

        [Bind("addend2")]
        public Expression2 Addend2 => addend2;

        public override EnvSpec GetEnvSpec()
        {
            return addend1.GetEnvSpec() | addend2.GetEnvSpec();
        }

        private static readonly BinaryOp[] opArray = new BinaryOp[]
        {
            BinaryOp.Atan2, BinaryOp.IEEERemainder, BinaryOp.LogBase,
            BinaryOp.Max, BinaryOp.Min, BinaryOp.Pow
        };

        private static readonly string[] opName = new string[]
        {
            "Atan2", "IEEERemainder", "Log",
            "Max", "Min", "Pow"
        };

        private static readonly Type[] opTypes = new Type[]
        {
            typeof(byte), typeof(short), typeof(int), typeof(long), typeof(IntPtr),
            typeof(sbyte), typeof(ushort), typeof(uint), typeof(ulong), typeof(UIntPtr)
        };

        private static readonly BinaryOp[] opF = new BinaryOp[]
        {
            BinaryOp.Add, BinaryOp.Sub, BinaryOp.Mul, BinaryOp.Div
        };

        private static readonly Type[] opTypesF = new Type[]
        {
            typeof(byte), typeof(short), typeof(int), typeof(long), typeof(IntPtr),
            typeof(sbyte), typeof(ushort), typeof(uint), typeof(ulong), typeof(UIntPtr),
            typeof(float), typeof(double)
        };

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            TypeReference t1 = addend1.GetReturnType(s, envDesc);
            TypeReference t2 = addend2.GetReturnType(s, envDesc);

            if (!(t1 is ExistingTypeReference)) throw new PascalesqueException("Binary op on unsupported type");
            if (!(t2 is ExistingTypeReference)) throw new PascalesqueException("Binary op on unsupported type");

            ExistingTypeReference et1 = (ExistingTypeReference)t1;
            ExistingTypeReference et2 = (ExistingTypeReference)t2;

            if (op == BinaryOp.Shl || op == BinaryOp.Shr)
            {
                if (t2 != ExistingTypeReference.Int32 && t2 != ExistingTypeReference.IntPtr) throw new PascalesqueException("Shift amount must be int or IntPtr");
            }
            else if (opArray.Contains(op))
            {

                int index = Array.IndexOf<BinaryOp>(opArray, op);
                System.Diagnostics.Debug.Assert(index >= 0 && index < opArray.Length);
                MethodInfo mi = typeof(Math).GetMethod(opName[index], BindingFlags.Public | BindingFlags.Static, null, new Type[] { et1.ExistingType, et2.ExistingType }, null).AssertNotNull();
                if (mi == null) throw new PascalesqueException("Unknown binary op / types");
                return new ExistingTypeReference(mi.ReturnType);
            }
            else
            {
                if (t1 != t2) throw new PascalesqueException("Attempt to do binary op on two different types");

                Type[] opTypes1 = (opF.Any(x => x == op)) ? opTypesF : opTypes;

                if (!opTypes1.Any(x => x == et1.ExistingType)) throw new PascalesqueException("Attempt to do a binary op on an unsupported type");
            }

            return t1;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return addend1.GetCompileSteps(s, owner, envDesc).AddRange(addend2.GetCompileSteps(s, owner, envDesc));
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return addend1.GetReferences(s, owner, envDesc).Union(addend2.GetReferences(s, owner, envDesc));
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            EnvDescTypesOnly2 edto = envDesc.TypesOnly();
            TypeReference t1 = addend1.GetReturnType(s, edto);
            TypeReference t2 = addend2.GetReturnType(s, edto);

            if (!(t1 is ExistingTypeReference)) throw new PascalesqueException("Binary op on unsupported type");
            if (!(t2 is ExistingTypeReference)) throw new PascalesqueException("Binary op on unsupported type");

            ExistingTypeReference et1 = (ExistingTypeReference)t1;
            ExistingTypeReference et2 = (ExistingTypeReference)t2;

            bool isUnsigned = (et1.ExistingType == typeof(byte) || et1.ExistingType == typeof(ushort) || et1.ExistingType == typeof(uint) || et1.ExistingType == typeof(ulong));

            addend1.Compile(s, owner, cc, envDesc, references, false);
            addend2.Compile(s, owner, cc, envDesc, references, false);
            switch (op)
            {
                case BinaryOp.Add:
                    ilg.Add();
                    break;
                case BinaryOp.Sub:
                    ilg.Sub();
                    break;
                case BinaryOp.Mul:
                    ilg.Mul();
                    break;
                case BinaryOp.Div:
                    if (isUnsigned)
                    {
                        ilg.DivUn();
                    }
                    else
                    {
                        ilg.Div();
                    }
                    break;
                case BinaryOp.Rem:
                    if (isUnsigned)
                    {
                        ilg.RemUn();
                    }
                    else
                    {
                        ilg.Rem();
                    }
                    break;
                case BinaryOp.And:
                    ilg.And();
                    break;
                case BinaryOp.Or:
                    ilg.Or();
                    break;
                case BinaryOp.Xor:
                    ilg.Xor();
                    break;
                case BinaryOp.Shl:
                    ilg.Shl();
                    break;
                case BinaryOp.Shr:
                    if (isUnsigned)
                    {
                        ilg.ShrUn();
                    }
                    else
                    {
                        ilg.Shr();
                    }
                    break;
                default:
                    if (opArray.Contains(op))
                    {
                        int index = Array.IndexOf<BinaryOp>(opArray, op);
                        System.Diagnostics.Debug.Assert(index >= 0 && index < opArray.Length);
                        MethodInfo mi = typeof(Math).GetMethod(opName[index], BindingFlags.Public | BindingFlags.Static, null, new Type[] { et1.ExistingType, et2.ExistingType }, null).AssertNotNull();
                        if (mi == null) throw new PascalesqueException("Unknown binary op / types");
                        if (tail) ilg.Tail();
                        ilg.Call(mi);
                    }
                    else
                    {
                        throw new PascalesqueException("Unknown binary op / type combination");
                    }
                    break;
            }
            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class UnaryOpExpr2 : Expression2
    {
        private readonly UnaryOp op;
        private readonly Expression2 expr;

        public UnaryOpExpr2(UnaryOp op, Expression2 expr)
        {
            this.op = op;
            this.expr = expr;
        }

        [Bind("op")]
        public UnaryOp Op => op;

        [Bind("expr")]
        public Expression2 Expr => expr;

        public override EnvSpec GetEnvSpec()
        {
            return expr.GetEnvSpec();
        }

        private static readonly UnaryOp[] opArray = new UnaryOp[]
        {
            UnaryOp.Abs, UnaryOp.Acos, UnaryOp.Asin, UnaryOp.Atan, UnaryOp.Ceil,
            UnaryOp.Cos, UnaryOp.Cosh, UnaryOp.Exp, UnaryOp.Floor, UnaryOp.Log,
            UnaryOp.Log10, UnaryOp.Round, UnaryOp.Sign, UnaryOp.Sin, UnaryOp.Sinh,
            UnaryOp.Sqrt, UnaryOp.Tan, UnaryOp.Tanh, UnaryOp.Trunc
        };

        private static readonly string[] opName = new string[]
        {
            "Abs", "Acos", "Asin", "Atan", "Ceiling",
            "Cos", "Cosh", "Exp", "Floor", "Log",
            "Log10", "Round", "Sign", "Sin", "Sinh",
            "Sqrt", "Tan", "Tanh", "Truncate"
        };

#if NETSTANDARD2_0
        private static MethodInfo GetMathMethod(UnaryOp u, Type t)
#else
        private static MethodInfo? GetMathMethod(UnaryOp u, Type t)
#endif
        {
            int index = Array.IndexOf<UnaryOp>(opArray, u);
            if (index < 0 || index >= opArray.Length) return null;
            return typeof(Math).GetMethod(opName[index], BindingFlags.Public | BindingFlags.Static, null, new Type[] { t }, null).AssertNotNull();
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            TypeReference t1 = expr.GetReturnType(s, envDesc);
            if (!(t1 is ExistingTypeReference)) throw new PascalesqueException("Unsupported type for unary operation");
            ExistingTypeReference et1 = (ExistingTypeReference)t1;
            Type t = et1.ExistingType;

            if (op == UnaryOp.Invert)
            {
                Type[] supportedTypes = new Type[]
                {
                    typeof(byte), typeof(ushort), typeof(uint), typeof(ulong), typeof(UIntPtr),
                    typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(IntPtr)
                };
                if (!supportedTypes.Any(x => t == x)) throw new PascalesqueException("Unsupported type for invert");
            }
            else if (op == UnaryOp.Negate)
            {
                Type[] supportedTypes = new Type[]
                {
                    typeof(byte), typeof(ushort), typeof(uint), typeof(ulong), typeof(UIntPtr),
                    typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(IntPtr)
                };
                if (!supportedTypes.Any(x => t == x)) throw new PascalesqueException("Unsupported type for negate");
            }
            else if (op == UnaryOp.Not)
            {
                Type[] supportedTypes = new Type[]
                {
                    typeof(byte), typeof(ushort), typeof(uint), typeof(ulong), typeof(UIntPtr),
                    typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(IntPtr),
                    typeof(bool)
                };
                if (!supportedTypes.Any(x => t == x)) throw new PascalesqueException("Unsupported type for not");
                return ExistingTypeReference.Boolean;
            }
            else
            {
#if NETSTANDARD2_0
                MethodInfo m = GetMathMethod(op, t);
#else
                MethodInfo? m = GetMathMethod(op, t);
#endif
                if (m == null) throw new PascalesqueException("Unknown UnaryOp / type");
                return new ExistingTypeReference(m.ReturnType);
            }
            return t1;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return expr.GetCompileSteps(s, owner, envDesc);
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return expr.GetReferences(s, owner, envDesc);
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            Type exprType = expr.GetReturnType(s, envDesc.TypesOnly()).Resolve(references);

            expr.Compile(s, owner, cc, envDesc, references, false);
            switch (op)
            {
                case UnaryOp.Invert:
                    ilg.Invert();
                    break;
                case UnaryOp.Negate:
                    ilg.Negate();
                    break;
                case UnaryOp.Not:
                    ilg.Not();
                    break;
                default:
#if NETSTANDARD2_0
                    MethodInfo m = GetMathMethod(op, exprType);
#else
                    MethodInfo? m = GetMathMethod(op, exprType);
#endif
                    if (m == null) throw new PascalesqueException("Unknown UnaryOp / type (missed the first time)");
                    if (tail) ilg.Tail();
                    ilg.Call(m);
                    break;
            }
            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class ConvertExpr2 : Expression2
    {
        private readonly ConvertTo convertTo;
        private readonly Expression2 expression;

        public ConvertExpr2(ConvertTo convertTo, Expression2 expression)
        {
            this.convertTo = convertTo;
            this.expression = expression;
        }

        [Bind("convertTo")]
        public ConvertTo ConvertTo => convertTo;

        [Bind("expression")]
        public Expression2 Expression => expression;

        public override EnvSpec GetEnvSpec()
        {
            return expression.GetEnvSpec();
        }

        public static TypeReference GetReturnType(ConvertTo convertTo)
        {
            switch (convertTo)
            {
                case ConvertTo.Byte: return ExistingTypeReference.Byte;
                case ConvertTo.Short: return ExistingTypeReference.Int16;
                case ConvertTo.Int: return ExistingTypeReference.Int32;
                case ConvertTo.Long: return ExistingTypeReference.Int64;
                case ConvertTo.IntPtr: return ExistingTypeReference.IntPtr;
                case ConvertTo.SByte: return ExistingTypeReference.SByte;
                case ConvertTo.UShort: return ExistingTypeReference.UInt16;
                case ConvertTo.UInt: return ExistingTypeReference.UInt32;
                case ConvertTo.ULong: return ExistingTypeReference.UInt64;
                case ConvertTo.UIntPtr: return ExistingTypeReference.UIntPtr;
                case ConvertTo.Float: return ExistingTypeReference.Single;
                case ConvertTo.Double: return ExistingTypeReference.Double;
                default: throw new PascalesqueException("Unknown ConvertTo type");
            }
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            return GetReturnType(convertTo);
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return expression.GetCompileSteps(s, owner, envDesc);
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return expression.GetReferences(s, owner, envDesc);
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            TypeReference exprType = expression.GetReturnType(s, envDesc.TypesOnly());

            bool isUnsigned = (exprType == ExistingTypeReference.Byte || exprType == ExistingTypeReference.UInt16 || exprType == ExistingTypeReference.UInt32 || exprType == ExistingTypeReference.UInt64 || exprType == ExistingTypeReference.UIntPtr);

            expression.Compile(s, owner, cc, envDesc, references, false);
            switch (convertTo)
            {
                case ConvertTo.Byte: ilg.Conv_U1(); break;
                case ConvertTo.Short: ilg.Conv_I2(); break;
                case ConvertTo.Int: ilg.Conv_I4(); break;
                case ConvertTo.Long: ilg.Conv_I8(); break;
                case ConvertTo.IntPtr: ilg.Conv_I(); break;
                case ConvertTo.SByte: ilg.Conv_I1(); break;
                case ConvertTo.UShort: ilg.Conv_U2(); break;
                case ConvertTo.UInt: ilg.Conv_U4(); break;
                case ConvertTo.ULong: ilg.Conv_U8(); break;
                case ConvertTo.UIntPtr: ilg.Conv_U(); break;
                case ConvertTo.Float: if (isUnsigned) { ilg.Conv_R_Un(); } ilg.Conv_R4(); break;
                case ConvertTo.Double: if (isUnsigned) { ilg.Conv_R_Un(); } ilg.Conv_R8(); break;
            }
            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class RegardAsExpr2 : Expression2
    {
        private readonly ConvertTo regardAsWhat;
        private readonly Expression2 expression;

        public RegardAsExpr2(ConvertTo regardAsWhat, Expression2 expression)
        {
            this.regardAsWhat = regardAsWhat;
            this.expression = expression;
        }

        [Bind("regardAsWhat")]
        public ConvertTo RegardAsWhat => regardAsWhat;

        [Bind("expression")]
        public Expression2 Expression => expression;

        public static ActualStackType GetActualStackType(ConvertTo convertTo)
        {
            switch (convertTo)
            {
                case ConvertTo.Byte: return ActualStackType.Int32;
                case ConvertTo.SByte: return ActualStackType.Int32;
                case ConvertTo.Short: return ActualStackType.Int32;
                case ConvertTo.UShort: return ActualStackType.Int32;
                case ConvertTo.Int: return ActualStackType.Int32;
                case ConvertTo.UInt: return ActualStackType.Int32;

                case ConvertTo.Long: return ActualStackType.Int64;
                case ConvertTo.ULong: return ActualStackType.Int64;

                case ConvertTo.IntPtr: return ActualStackType.IntPtr;
                case ConvertTo.UIntPtr: return ActualStackType.IntPtr;

                case ConvertTo.Float: return ActualStackType.Float;
                case ConvertTo.Double: return ActualStackType.Float;

                default: throw new ArgumentException("Unknown ConvertTo type");
            }
        }

        public static ActualStackType GetActualStackType(TypeReference t)
        {
            if (t == ExistingTypeReference.Byte || t == ExistingTypeReference.SByte || t == ExistingTypeReference.Int16 || t == ExistingTypeReference.UInt16 || t == ExistingTypeReference.Int32 || t == ExistingTypeReference.UInt32 || t == ExistingTypeReference.Boolean || t == ExistingTypeReference.Char)
            {
                return ActualStackType.Int32;
            }
            else if (t == ExistingTypeReference.Int64 || t == ExistingTypeReference.UInt64)
            {
                return ActualStackType.Int64;
            }
            else if (t == ExistingTypeReference.IntPtr || t == ExistingTypeReference.UIntPtr)
            {
                return ActualStackType.IntPtr;
            }
            else if (t == ExistingTypeReference.Single || t == ExistingTypeReference.Double)
            {
                return ActualStackType.Float;
            }
            else throw new ArgumentException("Unknown actual stack type");
        }

        public override EnvSpec GetEnvSpec()
        {
            return expression.GetEnvSpec();
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            TypeReference eType = expression.GetReturnType(s, envDesc);

            if (GetActualStackType(eType) != GetActualStackType(regardAsWhat))
                throw new PascalesqueException("RegardAs doesn't work if types are physically different");

            return ConvertExpr2.GetReturnType(regardAsWhat);
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return expression.GetCompileSteps(s, owner, envDesc);
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return expression.GetReferences(s, owner, envDesc);
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            expression.Compile(s, owner, cc, envDesc, references, tail);
        }
    }

    public sealed class ComparisonExpr2 : Expression2
    {
        private readonly Comparison comp;
        private readonly Expression2 expr1;
        private readonly Expression2 expr2;

        public ComparisonExpr2(Comparison comp, Expression2 expr1, Expression2 expr2)
        {
            this.comp = comp;
            this.expr1 = expr1;
            this.expr2 = expr2;
        }

        public override EnvSpec GetEnvSpec()
        {
            return expr1.GetEnvSpec() | expr2.GetEnvSpec();
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            TypeReference t1 = expr1.GetReturnType(s, envDesc);
            TypeReference t2 = expr2.GetReturnType(s, envDesc);
            if (t1 != t2) throw new PascalesqueException("Comparison requires operands of same type");

            return ExistingTypeReference.Boolean;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return expr1.GetCompileSteps(s, owner, envDesc).AddRange(expr2.GetCompileSteps(s, owner, envDesc));
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return expr1.GetReferences(s, owner, envDesc).Union(expr2.GetReferences(s, owner, envDesc));
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            TypeReference t = expr1.GetReturnType(s, envDesc.TypesOnly());
            bool isUnsigned = (t == ExistingTypeReference.Byte || t == ExistingTypeReference.UInt16 || t == ExistingTypeReference.UInt32 || t == ExistingTypeReference.UInt64);

            expr1.Compile(s, owner, cc, envDesc, references, false);

            if (t == ExistingTypeReference.Byte) ilg.Conv_U1();
            else if (t == ExistingTypeReference.Int16) ilg.Conv_I2();
            else if (t == ExistingTypeReference.SByte) ilg.Conv_I1();
            else if (t == ExistingTypeReference.UInt16) ilg.Conv_U2();

            expr2.Compile(s, owner, cc, envDesc, references, false);

            if (t == ExistingTypeReference.Byte) ilg.Conv_U1();
            else if (t == ExistingTypeReference.Int16) ilg.Conv_I2();
            else if (t == ExistingTypeReference.SByte) ilg.Conv_I1();
            else if (t == ExistingTypeReference.UInt16) ilg.Conv_U2();

            switch (comp)
            {
                case Comparison.LessThan:
                    if (isUnsigned)
                    {
                        ilg.CltUn();
                    }
                    else
                    {
                        ilg.Clt();
                    }
                    break;
                case Comparison.GreaterThan:
                    if (isUnsigned)
                    {
                        ilg.CgtUn();
                    }
                    else
                    {
                        ilg.Cgt();
                    }
                    break;
                case Comparison.LessEqual:
                    if (isUnsigned)
                    {
                        ilg.CgtUn();
                    }
                    else
                    {
                        ilg.Cgt();
                    }
                    ilg.Not();
                    break;
                case Comparison.GreaterEqual:
                    if (isUnsigned)
                    {
                        ilg.CltUn();
                    }
                    else
                    {
                        ilg.Clt();
                    }
                    ilg.Not();
                    break;
                case Comparison.Equal:
                    ilg.Ceq();
                    break;
                case Comparison.NotEqual:
                    ilg.Ceq();
                    ilg.Not();
                    break;
            }

            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class ArrayLenExpr2 : Expression2
    {
        private readonly Expression2 array;

        public ArrayLenExpr2(Expression2 array)
        {
            this.array = array;
        }

        [Bind("array")]
        public Expression2 Array => array;

        public override EnvSpec GetEnvSpec()
        {
            return array.GetEnvSpec();
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            TypeReference tx = array.GetReturnType(s, envDesc);
            if (!(tx.IsArray)) throw new PascalesqueException("ArrayLen requires an array");

            return ExistingTypeReference.Int32;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return array.GetCompileSteps(s, owner, envDesc);
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return array.GetReferences(s, owner, envDesc);
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            TypeReference tx = array.GetReturnType(s, envDesc.TypesOnly());

            array.Compile(s, owner, cc, envDesc, references, false);

            if (tail) ilg.Tail();
            ilg.CallVirt(tx.Resolve(references).GetMethod("get_Length", Type.EmptyTypes).AssertNotNull());
            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class ArrayRefExpr2 : Expression2
    {
        private readonly Expression2 array;
        private readonly Expression2 index;

        public ArrayRefExpr2(Expression2 array, Expression2 index)
        {
            this.array = array;
            this.index = index;
        }

        [Bind("array")]
        public Expression2 Array => array;

        [Bind("index")]
        public Expression2 Index => index;

        public override EnvSpec GetEnvSpec()
        {
            return array.GetEnvSpec() | index.GetEnvSpec();
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            TypeReference t = array.GetReturnType(s, envDesc);
            if (!(t.IsArray)) throw new PascalesqueException("ArrayRef type mismatch; array required");

            TypeReference i = index.GetReturnType(s, envDesc);
            if (i != ExistingTypeReference.Int32 && i != ExistingTypeReference.IntPtr) throw new PascalesqueException("ArrayRef type mismatch; index must be int or IntPtr");

            return t.GetElementType();
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return array.GetCompileSteps(s, owner, envDesc).AddRange(index.GetCompileSteps(s, owner, envDesc));
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return array.GetReferences(s, owner, envDesc).Union(array.GetReturnType(s, envDesc).GetElementType().GetReferences()).Union(index.GetReferences(s, owner, envDesc));
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            TypeReference t = array.GetReturnType(s, envDesc.TypesOnly());
            if (!(t.IsArray)) throw new PascalesqueException("ArrayRef type mismatch; array required");

            TypeReference elType = t.GetElementType();

            array.Compile(s, owner, cc, envDesc, references, false);
            index.Compile(s, owner, cc, envDesc, references, false);
            ilg.LoadElement(elType.Resolve(references));
            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class ArraySetExpr2 : Expression2
    {
        private readonly Expression2 array;
        private readonly Expression2 index;
        private readonly Expression2 value;

        public ArraySetExpr2(Expression2 array, Expression2 index, Expression2 value)
        {
            this.array = array;
            this.index = index;
            this.value = value;
        }

        [Bind("array")]
        public Expression2 Array => array;

        [Bind("index")]
        public Expression2 Index => index;

        [Bind("value")]
        public Expression2 Value => value;

        public override EnvSpec GetEnvSpec()
        {
            return array.GetEnvSpec() | index.GetEnvSpec() | value.GetEnvSpec();
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            TypeReference t = array.GetReturnType(s, envDesc);
            if (!(t.IsArray)) throw new PascalesqueException("ArraySet type mismatch; array required");

            TypeReference i = index.GetReturnType(s, envDesc);
            if (i != ExistingTypeReference.Int32 && i != ExistingTypeReference.IntPtr) throw new PascalesqueException("ArraySet type mismatch; index must be int or IntPtr");

            TypeReference x = value.GetReturnType(s, envDesc);
            if (x != t.GetElementType()) throw new PascalesqueException("ArraySet type mismatch; value must match item type of array");

            return ExistingTypeReference.Void;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return array.GetCompileSteps(s, owner, envDesc)
                .AddRange(index.GetCompileSteps(s, owner, envDesc))
                .AddRange(value.GetCompileSteps(s, owner, envDesc));
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return array.GetReferences(s, owner, envDesc)
                .Union(array.GetReturnType(s, envDesc).GetElementType().GetReferences())
                .Union(index.GetReferences(s, owner, envDesc))
                .Union(value.GetReferences(s, owner, envDesc));
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            TypeReference t = array.GetReturnType(s, envDesc.TypesOnly());
            if (!(t.IsArray)) throw new PascalesqueException("ArrayRef type mismatch; array required");

            TypeReference elType = t.GetElementType();

            array.Compile(s, owner, cc, envDesc, references, false);
            index.Compile(s, owner, cc, envDesc, references, false);
            value.Compile(s, owner, cc, envDesc, references, false);
            ilg.StoreElement(elType.Resolve(references));
            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class NewArrayExpr2 : Expression2
    {
        private readonly TypeReference itemType;
        private readonly Expression2 size;

        public NewArrayExpr2(TypeReference itemType, Expression2 size)
        {
            this.itemType = itemType;
            this.size = size;
        }

        [Bind("itemType")]
        public TypeReference ItemType => itemType;

        [Bind("size")]
        public Expression2 Size => size;

        public override EnvSpec GetEnvSpec()
        {
            return size.GetEnvSpec();
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            TypeReference sizeType = size.GetReturnType(s, envDesc);
            if (sizeType != ExistingTypeReference.Int32 && sizeType != ExistingTypeReference.IntPtr) throw new PascalesqueException("Incorrect type for size of new array");
            return itemType.MakeArrayType();
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return size.GetCompileSteps(s, owner, envDesc);
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return size.GetReferences(s, owner, envDesc).Union(itemType.GetReferences());
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            size.Compile(s, owner, cc, envDesc, references, false);
            ilg.Emit(OpCodes.Newarr, itemType.Resolve(references));
            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class MethodCallExpr2 : Expression2
    {
        private readonly MethodReference methodToCall;
        private readonly bool allowVirtual;
        private readonly ImmutableList<Expression2> arguments;

        public MethodCallExpr2(MethodReference methodToCall, bool allowVirtual, ImmutableList<Expression2> arguments)
        {
            this.methodToCall = methodToCall;
            this.allowVirtual = allowVirtual;
            this.arguments = arguments;
        }

        [Bind("methodToCall")]
        public MethodReference MethodToCall => methodToCall;

        [Bind("allowVirtual")]
        public bool AllowVirtual => allowVirtual;

        [Bind("arguments")]
        public ImmutableList<Expression2> Arguments => arguments;

        public override EnvSpec GetEnvSpec()
        {
            EnvSpec e = EnvSpec.Empty();
            foreach (Expression2 arg in arguments)
            {
                e |= arg.GetEnvSpec();
            }
            return e;
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            int iEnd = methodToCall.ParameterCount;
            if (arguments.Count != iEnd) throw new PascalesqueException("Argument count doesn't match parameter count");

            for (int i = 0; i < iEnd; ++i)
            {
                TypeReference tParam = methodToCall.GetParameterType(i);
                //if (tParam.IsByRef) throw new PascalesqueException("ByRef parameters not supported");
                TypeReference tArg = arguments[i].GetReturnType(s, envDesc);
                if (tParam != tArg) throw new PascalesqueException("Argument type doesn't match parameter type");
            }

            return methodToCall.GetReturnType(s);
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return arguments.SelectMany(arg => arg.GetCompileSteps(s, owner, envDesc)).ToImmutableList();
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return arguments.Select(x => x.GetReferences(s, owner, envDesc)).UnionAll().Union(methodToCall.GetReferences());
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            foreach (Expression2 arg in arguments)
            {
                arg.Compile(s, owner, cc, envDesc, references, false);
            }

            MethodInfo mi = methodToCall.Resolve(references);
            if (mi.IsVirtual && allowVirtual)
            {
                if (tail) ilg.Tail();
                ilg.CallVirt(mi);
                if (tail) ilg.Return();
            }
            else
            {
                if (tail) ilg.Tail();
                ilg.Call(mi);
                if (tail) ilg.Return();
            }
        }
    }

    [Record]
    public sealed class ConstructorCallExpr2 : Expression2
    {
        private readonly ConstructorReference constructorToCall;
        private readonly Expression2 thisObj;
        private readonly ImmutableList<Expression2> arguments;

        public ConstructorCallExpr2(ConstructorReference constructorToCall, Expression2 thisObj, ImmutableList<Expression2> arguments)
        {
            this.constructorToCall = constructorToCall;
            this.thisObj = thisObj;
            this.arguments = arguments;
        }

        [Bind("constructorToCall")]
        public ConstructorReference ConstructorToCall => constructorToCall;

        [Bind("thisObj")]
        public Expression2 ThisObj => thisObj;

        [Bind("arguments")]
        public ImmutableList<Expression2> Arguments => arguments;

        public override EnvSpec GetEnvSpec()
        {
            EnvSpec e = thisObj.GetEnvSpec();
            foreach (Expression2 arg in arguments)
            {
                e |= arg.GetEnvSpec();
            }
            return e;
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            int iEnd = constructorToCall.ParameterCount;
            if (arguments.Count != iEnd) throw new PascalesqueException("Argument count doesn't match parameter count");

            // thisObj type not checked

            for (int i = 0; i < iEnd; ++i)
            {
                TypeReference tParam = constructorToCall.GetParameterType(i);
                //if (tParam.IsByRef) throw new PascalesqueException("ByRef parameters not supported");
                TypeReference tArg = arguments[i].GetReturnType(s, envDesc);
                if (tParam != tArg) throw new PascalesqueException("Argument type doesn't match parameter type");
            }

            return ExistingTypeReference.Void;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return thisObj.GetCompileSteps(s, owner, envDesc)
                .AddRange(arguments.SelectMany(arg => arg.GetCompileSteps(s, owner, envDesc)));
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return thisObj.GetReferences(s, owner, envDesc)
                .Union(arguments.Select(x => x.GetReferences(s, owner, envDesc)).UnionAll())
                .Union(constructorToCall.GetReferences());
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            thisObj.Compile(s, owner, cc, envDesc, references, false);

            foreach (Expression2 arg in arguments)
            {
                arg.Compile(s, owner, cc, envDesc, references, false);
            }

            ConstructorInfo ci = constructorToCall.Resolve(references);

            if (tail) ilg.Tail();
            ilg.Call(ci);
            if (tail) ilg.Return();
        }
    }

    public sealed class NewObjExpr2 : Expression2
    {
        private readonly ConstructorReference constructorToCall;
        private readonly ImmutableList<Expression2> arguments;

        public NewObjExpr2(ConstructorReference constructorToCall, ImmutableList<Expression2> arguments)
        {
            this.constructorToCall = constructorToCall;
            this.arguments = arguments;
        }

        [Bind("constructorToCall")]
        public ConstructorReference ConstructorToCall => constructorToCall;

        [Bind("arguments")]
        public ImmutableList<Expression2> Arguments => arguments;

        public override EnvSpec GetEnvSpec()
        {
            EnvSpec e = EnvSpec.Empty();
            foreach (Expression2 arg in arguments)
            {
                e |= arg.GetEnvSpec();
            }
            return e;
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            int iEnd = arguments.Count;
            for (int i = 0; i < iEnd; ++i)
            {
                TypeReference t1 = arguments[i].GetReturnType(s, envDesc);
                TypeReference t2 = constructorToCall.GetParameterType(i);
                if (t1 != t2) throw new PascalesqueException("Type mismatch in NewObj constructor call");
            }
            return constructorToCall.ConstructorOfWhat;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return arguments.SelectMany(arg => arg.GetCompileSteps(s, owner, envDesc)).ToImmutableList();
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return arguments.Select(x => x.GetReferences(s, owner, envDesc)).UnionAll();
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            foreach (Expression2 arg in arguments)
            {
                arg.Compile(s, owner, cc, envDesc, references, false);
            }

            ConstructorInfo ci = constructorToCall.Resolve(references);

            ilg.NewObj(ci);
            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class FieldRefExpr2 : Expression2
    {
        private readonly Expression2 fieldOfWhat;
        private readonly FieldReference fieldReference;

        public FieldRefExpr2(Expression2 fieldOfWhat, FieldReference fieldReference)
        {
            this.fieldOfWhat = fieldOfWhat;
            this.fieldReference = fieldReference;
        }

        [Bind("fieldOfWhat")]
        public Expression2 FieldOfWhat => fieldOfWhat;

        [Bind("fieldReference")]
        public FieldReference FieldReference => fieldReference;

        public override EnvSpec GetEnvSpec()
        {
            return fieldOfWhat.GetEnvSpec();
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            TypeReference t = fieldOfWhat.GetReturnType(s, envDesc);
            if (t != fieldReference.Owner) throw new PascalesqueException("Type Mismatch for Field Reference");

            return fieldReference.FieldType;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return fieldOfWhat.GetCompileSteps(s, owner, envDesc);
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return fieldOfWhat.GetReferences(s, owner, envDesc).Union(fieldReference.GetReferences());
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            fieldOfWhat.Compile(s, owner, cc, envDesc, references, false);
            ilg.LoadField(fieldReference.Resolve(references));
            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class FieldSetExpr2 : Expression2
    {
        private readonly Expression2 fieldOfWhat;
        private readonly FieldReference fieldReference;
        private readonly Expression2 val;

        public FieldSetExpr2(Expression2 fieldOfWhat, FieldReference fieldReference, Expression2 val)
        {
            this.fieldOfWhat = fieldOfWhat;
            this.fieldReference = fieldReference;
            this.val = val;
        }

        [Bind("fieldOfWhat")]
        public Expression2 FieldOfWhat => fieldOfWhat;

        [Bind("fieldReference")]
        public FieldReference FieldReference => fieldReference;

        [Bind("val")]
        public Expression2 Val => val;

        public override EnvSpec GetEnvSpec()
        {
            return fieldOfWhat.GetEnvSpec() | val.GetEnvSpec();
        }

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            TypeReference t1 = fieldOfWhat.GetReturnType(s, envDesc);
            TypeReference t2 = val.GetReturnType(s, envDesc);
            if (t1 != fieldReference.Owner || t2 != fieldReference.FieldType) throw new PascalesqueException("Type mismatch for Field Set");

            return ExistingTypeReference.Void;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return fieldOfWhat.GetCompileSteps(s, owner, envDesc)
                .AddRange(val.GetCompileSteps(s, owner, envDesc));
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return fieldOfWhat.GetReferences(s, owner, envDesc).Union(val.GetReferences(s, owner, envDesc)).Union(fieldReference.GetReferences());
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            fieldOfWhat.Compile(s, owner, cc, envDesc, references, false);
            val.Compile(s, owner, cc, envDesc, references, false);
            ilg.StoreField(fieldReference.Resolve(references));
            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class PokeExpr2 : Expression2
    {
        private readonly Expression2 ptr;
        private readonly Expression2 value;

        public PokeExpr2(Expression2 ptr, Expression2 value)
        {
            this.ptr = ptr;
            this.value = value;
        }

        [Bind("ptr")]
        public Expression2 Ptr => ptr;

        [Bind("value")]
        public Expression2 Value => value;

        public override EnvSpec GetEnvSpec()
        {
            return ptr.GetEnvSpec() | value.GetEnvSpec();
        }

        private static readonly Type[] okTypes = new Type[]
        {
            typeof(byte), typeof(short), typeof(int), typeof(long), typeof(IntPtr),
            typeof(sbyte), typeof(ushort), typeof(uint), typeof(ulong), typeof(UIntPtr),
            typeof(float), typeof(double), typeof(char), typeof(bool)
        };

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            TypeReference t1 = ptr.GetReturnType(s, envDesc);
            TypeReference t2 = value.GetReturnType(s, envDesc);

            if (!(t2 is ExistingTypeReference) || !(okTypes.Contains(((ExistingTypeReference)t2).ExistingType))) throw new PascalesqueException("Poke: argument type cannot be poked");

            if (t1 != ExistingTypeReference.IntPtr && t1 != ExistingTypeReference.UIntPtr) throw new PascalesqueException("Poke: pointer type is not IntPtr");

            return ExistingTypeReference.Void;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return ptr.GetCompileSteps(s, owner, envDesc)
                .AddRange(value.GetCompileSteps(s, owner, envDesc));
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return ptr.GetReferences(s, owner, envDesc).Union(value.GetReferences(s, owner, envDesc));
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;

            ptr.Compile(s, owner, cc, envDesc, references, false);
            value.Compile(s, owner, cc, envDesc, references, false);
            Type t2 = value.GetReturnType(s, envDesc.TypesOnly()).Resolve(references);
            ilg.Unaligned(Alignment.One);
            ilg.StoreObjIndirect(t2);
            if (tail) ilg.Return();
        }
    }

    [Record]
    public sealed class PeekExpr2 : Expression2
    {
        private readonly Expression2 ptr;
        private readonly TypeReference type;

        public PeekExpr2(Expression2 ptr, TypeReference type)
        {
            this.ptr = ptr;
            this.type = type;
        }

        [Bind("ptr")]
        public Expression2 Ptr => ptr;

        [Bind("type")]
        public TypeReference Type => type;

        public override EnvSpec GetEnvSpec()
        {
            return ptr.GetEnvSpec();
        }

        private static readonly Type[] okTypes = new Type[]
        {
            typeof(byte), typeof(short), typeof(int), typeof(long), typeof(IntPtr),
            typeof(sbyte), typeof(ushort), typeof(uint), typeof(ulong), typeof(UIntPtr),
            typeof(float), typeof(double), typeof(char), typeof(bool)
        };

        public override TypeReference GetReturnType(SymbolTable s, EnvDescTypesOnly2 envDesc)
        {
            if (!(type is ExistingTypeReference) || !(okTypes.Contains(((ExistingTypeReference)type).ExistingType))) throw new PascalesqueException("Peek: type can't be peeked");

            return type;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return ptr.GetCompileSteps(s, owner, envDesc);
        }

        public override ImmutableSortedSet<ItemKey> GetReferences(SymbolTable s, TypeKey owner, EnvDescTypesOnly2 envDesc)
        {
            return ptr.GetReferences(s, owner, envDesc);
        }

        public override void Compile(SymbolTable s, TypeKey owner, CompileContext2 cc, EnvDesc2 envDesc, ImmutableSortedDictionary<ItemKey, SaBox<object>> references, bool tail)
        {
            ILGenerator ilg = cc.ILGenerator;
            ptr.Compile(s, owner, cc, envDesc, references, false);
            ilg.Unaligned(Alignment.One);
            ilg.LoadObjIndirect(type.Resolve(references));
            if (tail) ilg.Return();
        }
    }
}
