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
}
