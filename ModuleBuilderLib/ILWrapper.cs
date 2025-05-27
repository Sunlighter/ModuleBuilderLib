using Sunlighter.OptionLib;
using Sunlighter.TypeTraitsLib.Building;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Sunlighter.ModuleBuilderLib
{
    public class ILContext
    {
        private readonly ImmutableSortedDictionary<Symbol, Label> labels;
        private readonly ImmutableSortedDictionary<Symbol, LocalBuilder> locals;
        private readonly ImmutableSortedDictionary<Symbol, int> parameters;
        private readonly ImmutableSortedDictionary<ItemKey, SaBox<object>> references;
        private readonly SymbolTable symbolTable;

        public ILContext
        (
            ImmutableSortedDictionary<Symbol, Label> labels,
            ImmutableSortedDictionary<Symbol, LocalBuilder> locals,
            ImmutableSortedDictionary<Symbol, int> parameters,
            ImmutableSortedDictionary<ItemKey, SaBox<object>> references,
            SymbolTable symbolTable
        )
        {
            this.labels = labels;
            this.locals = locals;
            this.parameters = parameters;
            this.references = references;
            this.symbolTable = symbolTable;
        }

        public ImmutableSortedDictionary<Symbol, Label> Labels { get { return labels; } }
        public ImmutableSortedDictionary<Symbol, LocalBuilder> Locals { get { return locals; } }
        public ImmutableSortedDictionary<Symbol, int> Parameters { get { return parameters; } }
        public ImmutableSortedDictionary<ItemKey, SaBox<object>> References { get { return references; } }
        public SymbolTable SymbolTable { get { return symbolTable; } }
    }

    [UnionOfDescendants]
    public abstract class ILEmit
    {
        public virtual ImmutableSortedSet<Symbol> LabelsDefined { get { return ImmutableSortedSet<Symbol>.Empty; } }

        public virtual ImmutableSortedSet<Symbol> LabelsUsed { get { return ImmutableSortedSet<Symbol>.Empty; } }

        public virtual ImmutableSortedSet<Symbol> LabelsWithoutAutoCreate { get { return ImmutableSortedSet<Symbol>.Empty; } }

        public virtual ImmutableSortedSet<ItemKey> References { get { return ImmutableSortedSet<ItemKey>.Empty; } }

        public abstract void Emit(ILGenerator ilg, ILContext context);
    }

    public enum ILNoArg
    {
        Add, AddOvf, AddOvfUn, Sub, SubOvf, SubOvfUn, Mul, MulOvf, MulOvfUn, Div, DivUn, Rem, RemUn,
        And, Or, Xor, Invert, Negate, Shl, Shr, ShrUn, Dup, Pop, Not,
        LoadNullPtr,
        Throw, Tail, Return, Ceq, Clt, CltUn, Cgt, CgtUn,
        Conv_I, Conv_I1, Conv_I2, Conv_I4, Conv_I8,
        Conv_Ovf_I, Conv_Ovf_I1, Conv_Ovf_I2, Conv_Ovf_I4, Conv_Ovf_I8,
        Conv_Ovf_I_Un, Conv_Ovf_I1_Un, Conv_Ovf_I2_Un, Conv_Ovf_I4_Un, Conv_Ovf_I8_Un,
        Conv_Ovf_U, Conv_Ovf_U1, Conv_Ovf_U2, Conv_Ovf_U4, Conv_Ovf_U8,
        Conv_Ovf_U_Un, Conv_Ovf_U1_Un, Conv_Ovf_U2_Un, Conv_Ovf_U4_Un, Conv_Ovf_U8_Un,
        Conv_R_Un, Conv_R4, Conv_R8,
        Conv_U, Conv_U1, Conv_U2, Conv_U4, Conv_U8,
        LoadObjRef, StoreObjRef
    }

    [Record]
    public sealed class ILEmitNoArg : ILEmit
    {
        private readonly ILNoArg insn;

        public ILEmitNoArg(ILNoArg insn)
        {
            this.insn = insn;
        }

        [Bind("insn")]
        public ILNoArg Instruction => insn;

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            switch (insn)
            {
                case ILNoArg.Add:
                    ilg.Add();
                    break;
                case ILNoArg.AddOvf:
                    ilg.AddOvf();
                    break;
                case ILNoArg.AddOvfUn:
                    ilg.AddOvfUn();
                    break;
                case ILNoArg.Sub:
                    ilg.Sub();
                    break;
                case ILNoArg.SubOvf:
                    ilg.SubOvf();
                    break;
                case ILNoArg.SubOvfUn:
                    ilg.SubOvfUn();
                    break;
                case ILNoArg.Mul:
                    ilg.Mul();
                    break;
                case ILNoArg.MulOvf:
                    ilg.MulOvf();
                    break;
                case ILNoArg.MulOvfUn:
                    ilg.MulOvfUn();
                    break;
                case ILNoArg.Div:
                    ilg.Div();
                    break;
                case ILNoArg.DivUn:
                    ilg.DivUn();
                    break;
                case ILNoArg.Rem:
                    ilg.Rem();
                    break;
                case ILNoArg.RemUn:
                    ilg.RemUn();
                    break;
                case ILNoArg.And:
                    ilg.And();
                    break;
                case ILNoArg.Or:
                    ilg.Or();
                    break;
                case ILNoArg.Xor:
                    ilg.Xor();
                    break;
                case ILNoArg.Invert:
                    ilg.Invert();
                    break;
                case ILNoArg.Negate:
                    ilg.Negate();
                    break;
                case ILNoArg.Shl:
                    ilg.Shl();
                    break;
                case ILNoArg.Shr:
                    ilg.Shr();
                    break;
                case ILNoArg.ShrUn:
                    ilg.ShrUn();
                    break;
                case ILNoArg.Dup:
                    ilg.Dup();
                    break;
                case ILNoArg.Pop:
                    ilg.Pop();
                    break;
                case ILNoArg.Not:
                    ilg.Not();
                    break;
                case ILNoArg.LoadNullPtr:
                    ilg.LoadNullPtr();
                    break;
                case ILNoArg.Throw:
                    ilg.Throw();
                    break;
                case ILNoArg.Tail:
                    ilg.Tail();
                    break;
                case ILNoArg.Return:
                    ilg.Return();
                    break;
                case ILNoArg.Ceq:
                    ilg.Ceq();
                    break;
                case ILNoArg.Clt:
                    ilg.Clt();
                    break;
                case ILNoArg.CltUn:
                    ilg.CltUn();
                    break;
                case ILNoArg.Cgt:
                    ilg.Cgt();
                    break;
                case ILNoArg.CgtUn:
                    ilg.CgtUn();
                    break;
                case ILNoArg.Conv_I:
                    ilg.Conv_I();
                    break;
                case ILNoArg.Conv_I1:
                    ilg.Conv_I1();
                    break;
                case ILNoArg.Conv_I2:
                    ilg.Conv_I2();
                    break;
                case ILNoArg.Conv_I4:
                    ilg.Conv_I4();
                    break;
                case ILNoArg.Conv_I8:
                    ilg.Conv_I8();
                    break;
                case ILNoArg.Conv_Ovf_I:
                    ilg.Conv_Ovf_I();
                    break;
                case ILNoArg.Conv_Ovf_I1:
                    ilg.Conv_Ovf_I1();
                    break;
                case ILNoArg.Conv_Ovf_I2:
                    ilg.Conv_Ovf_I2();
                    break;
                case ILNoArg.Conv_Ovf_I4:
                    ilg.Conv_Ovf_I4();
                    break;
                case ILNoArg.Conv_Ovf_I8:
                    ilg.Conv_Ovf_I8();
                    break;
                case ILNoArg.Conv_Ovf_I_Un:
                    ilg.Conv_Ovf_I_Un();
                    break;
                case ILNoArg.Conv_Ovf_I1_Un:
                    ilg.Conv_Ovf_I1_Un();
                    break;
                case ILNoArg.Conv_Ovf_I2_Un:
                    ilg.Conv_Ovf_I2_Un();
                    break;
                case ILNoArg.Conv_Ovf_I4_Un:
                    ilg.Conv_Ovf_I4_Un();
                    break;
                case ILNoArg.Conv_Ovf_I8_Un:
                    ilg.Conv_Ovf_I8_Un();
                    break;
                case ILNoArg.Conv_Ovf_U:
                    ilg.Conv_Ovf_U();
                    break;
                case ILNoArg.Conv_Ovf_U1:
                    ilg.Conv_Ovf_U1();
                    break;
                case ILNoArg.Conv_Ovf_U2:
                    ilg.Conv_Ovf_U2();
                    break;
                case ILNoArg.Conv_Ovf_U4:
                    ilg.Conv_Ovf_U4();
                    break;
                case ILNoArg.Conv_Ovf_U8:
                    ilg.Conv_Ovf_U8();
                    break;
                case ILNoArg.Conv_Ovf_U_Un:
                    ilg.Conv_Ovf_U_Un();
                    break;
                case ILNoArg.Conv_Ovf_U1_Un:
                    ilg.Conv_Ovf_U1_Un();
                    break;
                case ILNoArg.Conv_Ovf_U2_Un:
                    ilg.Conv_Ovf_U2_Un();
                    break;
                case ILNoArg.Conv_Ovf_U4_Un:
                    ilg.Conv_Ovf_U4_Un();
                    break;
                case ILNoArg.Conv_Ovf_U8_Un:
                    ilg.Conv_Ovf_U8_Un();
                    break;
                case ILNoArg.Conv_R_Un:
                    ilg.Conv_R_Un();
                    break;
                case ILNoArg.Conv_R4:
                    ilg.Conv_R4();
                    break;
                case ILNoArg.Conv_R8:
                    ilg.Conv_R8();
                    break;
                case ILNoArg.Conv_U:
                    ilg.Conv_U();
                    break;
                case ILNoArg.Conv_U1:
                    ilg.Conv_U1();
                    break;
                case ILNoArg.Conv_U2:
                    ilg.Conv_U2();
                    break;
                case ILNoArg.Conv_U4:
                    ilg.Conv_U4();
                    break;
                case ILNoArg.Conv_U8:
                    ilg.Conv_U8();
                    break;
                case ILNoArg.LoadObjRef:
                    ilg.LoadObjRef();
                    break;
                case ILNoArg.StoreObjRef:
                    ilg.StoreObjRef();
                    break;
                default:
                    throw new InvalidOperationException("Unknown no-argument opcode");
            }
        }
    }

    [Record]
    public sealed class ILEmitLabel : ILEmit
    {
        private readonly Symbol name;

        public ILEmitLabel(Symbol name)
        {
            this.name = name;
        }

        [Bind("name")]
        public Symbol Name => name;

        public override ImmutableSortedSet<Symbol> LabelsDefined
        {
            get
            {
                return ImmutableSortedSet<Symbol>.Empty.Add(name);
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.MarkLabel(context.Labels[name]);
        }
    }

    public enum ILBranch
    {
        Beq, Beq_S,
        Bge, Bge_S, Bge_Un, Bge_Un_S,
        Bgt, Bgt_S, Bgt_Un, Bgt_Un_S,
        Ble, Ble_S, Ble_Un, Ble_Un_S,
        Blt, Blt_S, Blt_Un, Blt_Un_S,
        Bne_Un, Bne_Un_S,
        Br, Br_S,
        Brfalse, Brfalse_S,
        Brtrue, Brtrue_S,
        Leave, Leave_S,
    }

    [Record]
    public sealed class ILEmitBranch : ILEmit
    {
        private readonly ILBranch branch;
        private readonly Symbol target;

        public ILEmitBranch(ILBranch branch, Symbol target)
        {
            this.branch = branch;
            this.target = target;
        }

        [Bind("branch")]
        public ILBranch BranchType => branch;

        [Bind("target")]
        public Symbol Target => target;

        public override ImmutableSortedSet<Symbol> LabelsUsed
        {
            get
            {
                return ImmutableSortedSet<Symbol>.Empty.Add(target);
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.Emit(GetOpCode(branch), context.Labels[target]);
        }

        private static OpCode GetOpCode(ILBranch branch)
        {
            switch (branch)
            {
                case ILBranch.Beq:
                    return OpCodes.Beq;
                case ILBranch.Beq_S:
                    return OpCodes.Beq_S;
                case ILBranch.Bge:
                    return OpCodes.Bge;
                case ILBranch.Bge_S:
                    return OpCodes.Bge_S;
                case ILBranch.Bge_Un:
                    return OpCodes.Bge_Un;
                case ILBranch.Bge_Un_S:
                    return OpCodes.Bge_Un_S;
                case ILBranch.Bgt:
                    return OpCodes.Bgt;
                case ILBranch.Bgt_S:
                    return OpCodes.Bgt_S;
                case ILBranch.Bgt_Un:
                    return OpCodes.Bgt_Un;
                case ILBranch.Bgt_Un_S:
                    return OpCodes.Bgt_Un_S;
                case ILBranch.Ble:
                    return OpCodes.Ble;
                case ILBranch.Ble_S:
                    return OpCodes.Ble_S;
                case ILBranch.Ble_Un:
                    return OpCodes.Ble_Un;
                case ILBranch.Ble_Un_S:
                    return OpCodes.Ble_Un_S;
                case ILBranch.Blt:
                    return OpCodes.Blt;
                case ILBranch.Blt_S:
                    return OpCodes.Blt_S;
                case ILBranch.Blt_Un:
                    return OpCodes.Blt_Un;
                case ILBranch.Blt_Un_S:
                    return OpCodes.Blt_Un_S;
                case ILBranch.Bne_Un:
                    return OpCodes.Bne_Un;
                case ILBranch.Bne_Un_S:
                    return OpCodes.Bne_Un_S;
                case ILBranch.Br:
                    return OpCodes.Br;
                case ILBranch.Br_S:
                    return OpCodes.Br_S;
                case ILBranch.Brfalse:
                    return OpCodes.Brfalse;
                case ILBranch.Brfalse_S:
                    return OpCodes.Brfalse_S;
                case ILBranch.Brtrue:
                    return OpCodes.Brtrue;
                case ILBranch.Brtrue_S:
                    return OpCodes.Brtrue_S;
                case ILBranch.Leave:
                    return OpCodes.Leave;
                case ILBranch.Leave_S:
                    return OpCodes.Leave_S;
                default:
                    throw new ArgumentException("Unknown branch opcode");
            }
        }
    }

    [Record]
    public sealed class ILEmitSwitch : ILEmit
    {
        private readonly ImmutableList<Symbol> targets;

        public ILEmitSwitch(ImmutableList<Symbol> targets)
        {
            this.targets = targets;
        }

        [Bind("targets")]
        public ImmutableList<Symbol> Targets => targets;

        public override ImmutableSortedSet<Symbol> LabelsUsed
        {
            get
            {
                return ImmutableSortedSet<Symbol>.Empty.Union(targets);
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.Emit(OpCodes.Switch, targets.Select(x => context.Labels[x]).ToArray());
        }
    }

    [Record]
    public sealed class ILEmitLoadArg : ILEmit
    {
        private readonly Symbol name;

        public ILEmitLoadArg(Symbol name)
        {
            this.name = name;
        }

        [Bind("name")]
        public Symbol Name => name;

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadArg(context.Parameters[name]);
        }
    }

    [Record]
    public sealed class ILEmitLoadArgAddress : ILEmit
    {
        private readonly Symbol name;

        public ILEmitLoadArgAddress(Symbol name)
        {
            this.name = name;
        }

        [Bind("name")]
        public Symbol Name => name;

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadArgAddress(context.Parameters[name]);
        }
    }

    [Record]
    public sealed class ILEmitStoreArg : ILEmit
    {
        private readonly Symbol name;

        public ILEmitStoreArg(Symbol name)
        {
            this.name = name;
        }

        [Bind("name")]
        public Symbol Name => name;

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.StoreArg(context.Parameters[name]);
        }
    }

    [Record]
    public sealed class ILEmitLoadLocal : ILEmit
    {
        private readonly Symbol name;

        public ILEmitLoadLocal(Symbol name)
        {
            this.name = name;
        }

        [Bind("name")]
        public Symbol Name => name;

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadLocal(context.Locals[name]);
        }
    }

    [Record]
    public sealed class ILEmitLoadLocalAddress : ILEmit
    {
        private readonly Symbol name;

        public ILEmitLoadLocalAddress(Symbol name)
        {
            this.name = name;
        }

        [Bind("name")]
        public Symbol Name => name;

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadLocalAddress(context.Locals[name]);
        }
    }

    [Record]
    public sealed class ILEmitStoreLocal : ILEmit
    {
        private readonly Symbol name;

        public ILEmitStoreLocal(Symbol name)
        {
            this.name = name;
        }

        [Bind("name")]
        public Symbol Name => name;

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.StoreLocal(context.Locals[name]);
        }
    }

    [Record]
    public sealed class ILEmitLoadInt : ILEmit
    {
        private readonly int literal;

        public ILEmitLoadInt(int literal)
        {
            this.literal = literal;
        }

        [Bind("literal")]
        public int Value => literal;

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadInt(literal);
        }
    }

    [Record]
    public sealed class ILEmitLoadLong : ILEmit
    {
        private readonly long literal;

        public ILEmitLoadLong(long literal)
        {
            this.literal = literal;
        }

        [Bind("literal")]
        public long Value => literal;

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadLong(literal);
        }
    }

    [Record]
    public sealed class ILEmitLoadFloat : ILEmit
    {
        private readonly float literal;

        public ILEmitLoadFloat(float literal)
        {
            this.literal = literal;
        }

        [Bind("literal")]
        public float Value => literal;

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadFloat(literal);
        }
    }

    [Record]
    public sealed class ILEmitLoadDouble : ILEmit
    {
        private readonly double literal;

        public ILEmitLoadDouble(double literal)
        {
            this.literal = literal;
        }

        [Bind("literal")]
        public double Value => literal;

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadDouble(literal);
        }
    }

    [Record]
    public sealed class ILEmitLoadString : ILEmit
    {
        private readonly string literal;

        public ILEmitLoadString(string literal)
        {
            this.literal = literal;
        }

        [Bind("literal")]
        public string Value => literal;

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadString(literal);
        }
    }

    public enum ILFieldOp
    {
        Load,
        LoadAddress,
        Store,
        LoadStatic,
        LoadStaticAddress,
        StoreStatic
    }

    [Record]
    public sealed class ILEmitFieldOp : ILEmit
    {
        private readonly ILFieldOp fieldOp;
        private readonly FieldReference fieldReference;

        public ILEmitFieldOp(ILFieldOp fieldOp, FieldReference fieldReference)
        {
            this.fieldOp = fieldOp;
            this.fieldReference = fieldReference;
        }

        [Bind("fieldOp")]
        public ILFieldOp FieldOp => fieldOp;

        [Bind("fieldReference")]
        public FieldReference FieldReference => fieldReference;

        public override ImmutableSortedSet<ItemKey> References
        {
            get
            {
                return fieldReference.GetReferences();
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            switch (fieldOp)
            {
                case ILFieldOp.Load:
                    ilg.LoadField(fieldReference.Resolve(context.References));
                    break;
                case ILFieldOp.LoadAddress:
                    ilg.LoadFieldAddress(fieldReference.Resolve(context.References));
                    break;
                case ILFieldOp.Store:
                    ilg.StoreField(fieldReference.Resolve(context.References));
                    break;
                case ILFieldOp.LoadStatic:
                    ilg.LoadStaticField(fieldReference.Resolve(context.References));
                    break;
                case ILFieldOp.LoadStaticAddress:
                    ilg.LoadStaticFieldAddress(fieldReference.Resolve(context.References));
                    break;
                case ILFieldOp.StoreStatic:
                    ilg.StoreStaticField(fieldReference.Resolve(context.References));
                    break;
                default:
                    throw new InvalidOperationException("Unknown field operation");
            }
        }
    }

    [Record]
    public sealed class ILEmitLoadMethodToken : ILEmit
    {
        private readonly MethodReference methodReference;

        public ILEmitLoadMethodToken(MethodReference methodReference)
        {
            this.methodReference = methodReference;
        }

        [Bind("methodReference")]
        public MethodReference Method => methodReference;

        public override ImmutableSortedSet<ItemKey> References
        {
            get
            {
                return methodReference.GetReferences();
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadToken(methodReference.Resolve(context.References));
        }
    }

    [Record]
    public sealed class ILEmitLoadFieldToken : ILEmit
    {
        private readonly FieldReference fieldReference;

        public ILEmitLoadFieldToken(FieldReference fieldReference)
        {
            this.fieldReference = fieldReference;
        }

        [Bind("fieldReference")]
        public FieldReference Field => fieldReference;

        public override ImmutableSortedSet<ItemKey> References
        {
            get
            {
                return fieldReference.GetReferences();
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadToken(fieldReference.Resolve(context.References));
        }
    }

    [Record]
    public sealed class ILEmitLoadConstructorToken : ILEmit
    {
        private readonly ConstructorReference constructorReference;

        public ILEmitLoadConstructorToken(ConstructorReference constructorReference)
        {
            this.constructorReference = constructorReference;
        }

        [Bind("constructorReference")]
        public ConstructorReference Constructor => constructorReference;

        public override ImmutableSortedSet<ItemKey> References
        {
            get
            {
                return constructorReference.GetReferences();
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadToken(constructorReference.Resolve(context.References));
        }
    }

    [Record]
    public sealed class ILEmitLoadTypeToken : ILEmit
    {
        private readonly TypeReference typeReference;

        public ILEmitLoadTypeToken(TypeReference typeReference)
        {
            this.typeReference = typeReference;
        }

        [Bind("typeReference")]
        public TypeReference TypeArg => typeReference;

        public override ImmutableSortedSet<ItemKey> References
        {
            get
            {
                return typeReference.GetReferences();
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadToken(typeReference.Resolve(context.References));
        }
    }

    [Record]
    public sealed class ILEmitNewObj : ILEmit
    {
        private readonly ConstructorReference constructorReference;

        public ILEmitNewObj(ConstructorReference constructorReference)
        {
            this.constructorReference = constructorReference;
        }

        [Bind("constructorReference")]
        public ConstructorReference Constructor => constructorReference;

        public override ImmutableSortedSet<ItemKey> References
        {
            get
            {
                return constructorReference.GetReferences();
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.NewObj(constructorReference.Resolve(context.References));
        }
    }

    [Record]
    public sealed class ILEmitCall : ILEmit
    {
        private readonly MethodReference methodReference;

        public ILEmitCall(MethodReference methodReference)
        {
            this.methodReference = methodReference;
        }

        [Bind("methodReference")]
        public MethodReference Method => methodReference;

        public override ImmutableSortedSet<ItemKey> References
        {
            get
            {
                return methodReference.GetReferences();
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.Call(methodReference.Resolve(context.References));
        }
    }

    [Record]
    public sealed class ILEmitConstructorCall : ILEmit
    {
        private readonly ConstructorReference constructorReference;

        public ILEmitConstructorCall(ConstructorReference constructorReference)
        {
            this.constructorReference = constructorReference;
        }

        [Bind("constructorReference")]
        public ConstructorReference Constructor => constructorReference;

        public override ImmutableSortedSet<ItemKey> References
        {
            get
            {
                return constructorReference.GetReferences();
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.Call(constructorReference.Resolve(context.References));
        }
    }

    [Record]
    public sealed class ILEmitCallVirt : ILEmit
    {
        private readonly MethodReference methodReference;

        public ILEmitCallVirt(MethodReference methodReference)
        {
            this.methodReference = methodReference;
        }

        [Bind("methodReference")]
        public MethodReference Method => methodReference;

        public override ImmutableSortedSet<ItemKey> References
        {
            get
            {
                return methodReference.GetReferences();
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.CallVirt(methodReference.Resolve(context.References));
        }
    }

    public enum ILTypeOp
    {
        IsInst,
        CastClass,
        SizeOf,
        LoadObjIndirect,
        StoreObjIndirect,
        Box,
        Unbox,
        UnboxAny,
        LoadElement,
        LoadElementAddress,
        StoreElement
    }

    [Record]
    public sealed class ILEmitTypeOp : ILEmit
    {
        private readonly ILTypeOp typeOp;
        private readonly TypeReference typeReference;

        public ILEmitTypeOp(ILTypeOp typeOp, TypeReference typeReference)
        {
            this.typeOp = typeOp;
            this.typeReference = typeReference;
        }

        [Bind("typeOp")]
        public ILTypeOp TypeOp => typeOp;

        [Bind("typeReference")]
        public TypeReference TypeArg => typeReference;

        public override ImmutableSortedSet<ItemKey> References
        {
            get
            {
                return typeReference.GetReferences();
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            switch (typeOp)
            {
                case ILTypeOp.IsInst:
                    ilg.IsInst(typeReference.Resolve(context.References));
                    break;
                case ILTypeOp.CastClass:
                    ilg.CastClass(typeReference.Resolve(context.References));
                    break;
                case ILTypeOp.SizeOf:
                    ilg.SizeOf(typeReference.Resolve(context.References));
                    break;
                case ILTypeOp.LoadObjIndirect:
                    ilg.LoadObjIndirect(typeReference.Resolve(context.References));
                    break;
                case ILTypeOp.StoreObjIndirect:
                    ilg.StoreObjIndirect(typeReference.Resolve(context.References));
                    break;
                case ILTypeOp.Box:
                    ilg.Box(typeReference.Resolve(context.References));
                    break;
                case ILTypeOp.Unbox:
                    ilg.Unbox(typeReference.Resolve(context.References));
                    break;
                case ILTypeOp.UnboxAny:
                    ilg.UnboxAny(typeReference.Resolve(context.References));
                    break;
                case ILTypeOp.LoadElement:
                    ilg.LoadElement(typeReference.Resolve(context.References));
                    break;
                case ILTypeOp.LoadElementAddress:
                    ilg.LoadElementAddress(typeReference.Resolve(context.References));
                    break;
                case ILTypeOp.StoreElement:
                    ilg.StoreElement(typeReference.Resolve(context.References));
                    break;
                default:
                    throw new InvalidOperationException("Unsupported type operation");
            }
        }

    }

    [Record]
    public sealed class ILEmitUnaligned : ILEmit
    {
        private readonly Alignment a;

        public ILEmitUnaligned(Alignment a)
        {
            this.a = a;
        }

        [Bind("a")]
        public Alignment Alignment => a;

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.Unaligned(a);
        }
    }

    [Record]
    public sealed class ILEmitBeginExceptionBlock : ILEmit
    {
        private readonly Symbol endOfBlock;

        public ILEmitBeginExceptionBlock(Symbol endOfBlock)
        {
            this.endOfBlock = endOfBlock;
        }

        [Bind("endOfBlock")]
        public Symbol EndOfBlock => endOfBlock;

        public override ImmutableSortedSet<Symbol> LabelsDefined
        {
            get
            {
                return ImmutableSortedSet<Symbol>.Empty.Add(endOfBlock);
            }
        }

        public override ImmutableSortedSet<Symbol> LabelsWithoutAutoCreate
        {
            get
            {
                return ImmutableSortedSet<Symbol>.Empty.Add(endOfBlock);
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            Label endOfBlockLabel = ilg.BeginExceptionBlock();
            context.Labels.Add(endOfBlock, endOfBlockLabel);
        }
    }

    [Record]
    public sealed class ILEmitBeginCatchBlock : ILEmit
    {
        private readonly TypeReference exceptionType;

        public ILEmitBeginCatchBlock(TypeReference exceptionType)
        {
            this.exceptionType = exceptionType;
        }

        [Bind("exceptionType")]
        public TypeReference ExceptionType => exceptionType;

        public override ImmutableSortedSet<ItemKey> References
        {
            get
            {
                return exceptionType.GetReferences();
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.BeginCatchBlock(exceptionType.Resolve(context.References));
        }
    }

    [Record]
    public sealed class ILEmitBeginFinallyBlock : ILEmit
    {
        public ILEmitBeginFinallyBlock()
        {
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.BeginFinallyBlock();
        }
    }

    [Record]
    public sealed class ILEmitEndExceptionBlock : ILEmit
    {
        public ILEmitEndExceptionBlock()
        {
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.EndExceptionBlock();
        }
    }

    [Record]
    public sealed class LocalInfo
    {
        private readonly Symbol name;
        private readonly TypeReference localType;
        private readonly bool isPinned;

        public LocalInfo(Symbol name, TypeReference localType, bool isPinned)
        {
            this.name = name;
            this.localType = localType;
            this.isPinned = isPinned;
        }

        [Bind("name")]
        public Symbol Name => name;

        [Bind("localType")]
        public TypeReference LocalType => localType;

        [Bind("isPinned")]
        public bool IsPinned => isPinned;
    }

    public static partial class Utils
    {
        public static void ILCompile(SymbolTable symbolTable, ImmutableList<ILEmit> instructions, ImmutableList<LocalInfo> localInfos, Option<Symbol> thisParameterName, ImmutableList<ParamInfo> paramInfos, ILGenerator ilg, ImmutableSortedDictionary<ItemKey, SaBox<object>> vars)
        {
            ImmutableSortedSet<Symbol> labelsDefined = ImmutableSortedSet<Symbol>.Empty.UnionAll(instructions.Select(x => x.LabelsDefined));
            ImmutableSortedSet<Symbol> labelsUsed = ImmutableSortedSet<Symbol>.Empty.UnionAll(instructions.Select(x => x.LabelsUsed));
            ImmutableSortedSet<Symbol> labelsWithoutAutoCreate = ImmutableSortedSet<Symbol>.Empty.UnionAll(instructions.Select(x => x.LabelsWithoutAutoCreate));

            if (!labelsUsed.Except(labelsDefined).IsEmpty) throw new Exception("Labels { " + string.Join(" ", labelsUsed.Except(labelsDefined).Select(x => x.SymbolName())) + " } used without being defined");

            ImmutableSortedDictionary<Symbol, Label> labels = ImmutableSortedDictionary<Symbol, Label>.Empty;

            foreach (Symbol s in labelsUsed)
            {
                if (!labelsWithoutAutoCreate.Contains(s))
                {
                    labels = labels.Add(s, ilg.DefineLabel());
                }
            }

            ImmutableSortedDictionary<Symbol, LocalBuilder> locals = ImmutableSortedDictionary<Symbol, LocalBuilder>.Empty;

            foreach (LocalInfo localInfo in localInfos)
            {
                locals = locals.Add(localInfo.Name, ilg.DeclareLocal(localInfo.LocalType.Resolve(vars), localInfo.IsPinned));
            }

            ImmutableSortedDictionary<Symbol, int> parameters = ImmutableSortedDictionary<Symbol, int>.Empty;

            if (thisParameterName.HasValue)
            {
                parameters = parameters.Add(thisParameterName.Value, 0);
            }

            for (int i = 0; i < paramInfos.Count; ++i)
            {
                parameters = parameters.Add(paramInfos[i].Name, i + (thisParameterName.HasValue ? 1 : 0));
            }

            ILContext c = new ILContext(labels, locals, parameters, vars, symbolTable);

            foreach (ILEmit instruction in instructions)
            {
                instruction.Emit(ilg, c);
            }
        }
    }

    [Record]
    public sealed class ILConstructorToBuild : ElementOfClass
    {
        private readonly MethodAttributes attributes;
        private readonly Symbol thisParameterName;
        private readonly ImmutableList<ParamInfo> parameters;
        private readonly ImmutableList<LocalInfo> locals;
        private readonly ImmutableList<ILEmit> opcodes;

        public ILConstructorToBuild
        (
            [Bind("attributes")] MethodAttributes attributes,
            Symbol thisParameterName,
            ImmutableList<ParamInfo> parameters,
            ImmutableList<LocalInfo> locals,
            ImmutableList<ILEmit> opcodes
        )
        {
            this.attributes = attributes;
            this.thisParameterName = thisParameterName;
            this.parameters = parameters;
            this.locals = locals;
            this.opcodes = opcodes;
        }

        public ILConstructorToBuild
        (
            MethodAttributes attributes,
            Symbol thisParameterName,
            ImmutableList<ParamInfo> parameters,
            GeneratedCode localsAndOpcodes
        )
            : this(attributes, thisParameterName, parameters, localsAndOpcodes.Locals, localsAndOpcodes.Opcodes)
        {
        }

        [Bind("attributes")]
        public MethodAttributes Attributes => attributes;

        [Bind("thisParameterName")]
        public Symbol ThisParameterName => thisParameterName;

        [Bind("parameters")]
        public ImmutableList<ParamInfo> Parameters => parameters;

        [Bind("locals")]
        public ImmutableList<LocalInfo> Locals => locals;

        [Bind("opcodes")]
        public ImmutableList<ILEmit> Opcodes => opcodes;

        private ConstructorKey GetConstructorKey(TypeKey owner)
        {
            return new ConstructorKey(owner, parameters.Select(x => x.ParamType).ToImmutableList());
        }

        private class MakeILConstructor : ICompileStep
        {
            private readonly ILConstructorToBuild parent;
            private readonly TypeKey owner;
            private readonly ConstructorKey constructorKey;
            private readonly ImmutableList<LocalInfo> locals;

            public MakeILConstructor(ILConstructorToBuild parent, TypeKey owner, ConstructorKey constructorKey, ImmutableList<LocalInfo> locals)
            {
                this.parent = parent;
                this.owner = owner;
                this.constructorKey = constructorKey;
                this.locals = locals;
            }

            public int Phase { get { return 1; } }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty
                        .Add(owner)
                        .UnionAll(parent.parameters.Select(x => x.ParamType.GetReferences()))
                        .UnionAll(locals.Select(x => x.LocalType.GetReferences()));
                }
            }

            public ImmutableSortedSet<ItemKey> Outputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty.Add(constructorKey);
                }
            }

            public void Compile(ModuleBuilder mb, ImmutableSortedDictionary<ItemKey, SaBox<object>> vars)
            {
                TypeBuilder oType = (TypeBuilder)(vars[owner].Value);

                ConstructorBuilder cb = oType.DefineConstructor(parent.attributes, CallingConventions.Standard, constructorKey.Parameters.Select(x => x.Resolve(vars)).ToArray());

                vars[constructorKey].Value = cb;
            }
        }

        private class MakeILConstructorBody : ICompileStep
        {
            private readonly ILConstructorToBuild parent;
            private readonly SymbolTable symbolTable;
            private readonly TypeKey owner;
            private readonly ConstructorKey constructorKey;
            private readonly ImmutableList<LocalInfo> locals;
            private readonly ImmutableList<ILEmit> instructions;

            public MakeILConstructorBody(ILConstructorToBuild parent, SymbolTable symbolTable, TypeKey owner, ConstructorKey constructorKey, ImmutableList<LocalInfo> locals, ImmutableList<ILEmit> instructions)
            {
                this.parent = parent;
                this.symbolTable = symbolTable;
                this.owner = owner;
                this.constructorKey = constructorKey;
                this.locals = locals;
                this.instructions = instructions;
            }

            public int Phase { get { return 1; } }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty
                        .Add(owner)
                        .UnionAll(parent.parameters.Select(x => x.ParamType.GetReferences()))
                        .UnionAll(locals.Select(x => x.LocalType.GetReferences()))
                        .UnionAll(instructions.Select(x => x.References))
                        .Add(constructorKey);
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
                //TypeBuilder tb = (TypeBuilder)(vars[owner].Value);
                ConstructorBuilder ceb = (ConstructorBuilder)(vars[constructorKey].Value);
                ILGenerator ilg = ceb.GetILGenerator();

                Utils.ILCompile(symbolTable, instructions, locals, Option<Symbol>.Some(parent.thisParameterName), parent.parameters, ilg, vars);
            }
        }

        public override SymbolTable DefineSymbols(SymbolTable s, TypeKey owner)
        {
            ConstructorKey ck = GetConstructorKey(owner);
            return s.SetItem(ck, new ConstructorAux(attributes));
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner)
        {
            ConstructorKey ck = GetConstructorKey(owner);

            return ImmutableList<ICompileStep>.Empty
                .Add(new MakeILConstructor(this, owner, ck, locals))
                .Add(new MakeILConstructorBody(this, s, owner, ck, locals, opcodes));
        }
    }

    [Record]
    public sealed class ILMethodToBuild : ElementOfClass
    {
        private readonly Symbol name;
        private readonly MethodAttributes attributes;
        private readonly TypeReference returnType;
        private readonly Option<Symbol> thisParameterName;
        private readonly ImmutableList<ParamInfo> parameters;
        private readonly ImmutableList<LocalInfo> locals;
        private readonly ImmutableList<ILEmit> opcodes;

        public ILMethodToBuild
        (
            [Bind("name")] Symbol name,
            MethodAttributes attributes,
            TypeReference returnType,
            Option<Symbol> thisParameterName,
            ImmutableList<ParamInfo> parameters,
            ImmutableList<LocalInfo> locals,
            ImmutableList<ILEmit> opcodes
        )
        {
            this.name = name;
            this.attributes = attributes;
            this.returnType = returnType;
            this.thisParameterName = thisParameterName;
            this.parameters = parameters;
            this.locals = locals;
            this.opcodes = opcodes;

            if (attributes.HasFlag(MethodAttributes.Static) && thisParameterName.HasValue)
            {
                throw new ArgumentException("A static method doesn't have a \"this\" parameter");
            }
            else if (!attributes.HasFlag(MethodAttributes.Static) && !thisParameterName.HasValue)
            {
                throw new ArgumentException("A non-static method requires a \"this\" parameter");
            }
        }

        public ILMethodToBuild
        (
            Symbol name,
            MethodAttributes attributes,
            TypeReference returnType,
            Option<Symbol> thisParameterName,
            ImmutableList<ParamInfo> parameters,
            GeneratedCode localsAndOpcodes
        )
            : this(name, attributes, returnType, thisParameterName, parameters, localsAndOpcodes.Locals, localsAndOpcodes.Opcodes)
        {
        }

        [Bind("name")]
        public Symbol Name => name;

        [Bind("attributes")]
        public MethodAttributes Attributes => attributes;

        [Bind("returnType")]
        public TypeReference ReturnType => returnType;

        [Bind("thisParameterName")]
        public Option<Symbol> ThisParameterName => thisParameterName;

        [Bind("parameters")]
        public ImmutableList<ParamInfo> Parameters => parameters;

        [Bind("locals")]
        public ImmutableList<LocalInfo> Locals => locals;

        [Bind("opcodes")]
        public ImmutableList<ILEmit> Opcodes => opcodes;

        private MethodKey GetMethodKey(TypeKey owner)
        {
            return new MethodKey(owner, name, !(attributes.HasFlag(MethodAttributes.Static)), parameters.Select(x => x.ParamType).ToImmutableList());
        }

        private class MakeILMethod : ICompileStep
        {
            private readonly ILMethodToBuild parent;
            private readonly SymbolTable symbolTable;
            private readonly TypeKey owner;
            private readonly MethodKey methodKey;
            private readonly ImmutableList<LocalInfo> locals;

            public MakeILMethod(ILMethodToBuild parent, SymbolTable symbolTable, TypeKey owner, MethodKey methodKey, ImmutableList<LocalInfo> locals)
            {
                this.parent = parent;
                this.symbolTable = symbolTable;
                this.owner = owner;
                this.methodKey = methodKey;
                this.locals = locals;
            }

            public int Phase { get { return 1; } }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty
                        .Add(owner)
                        .UnionAll(parent.parameters.Select(x => x.ParamType.GetReferences()))
                        .UnionAll(locals.Select(x => x.LocalType.GetReferences()))
                        .Union(symbolTable[methodKey].ReturnType.GetReferences());
                }
            }

            public ImmutableSortedSet<ItemKey> Outputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty.Add(methodKey);
                }
            }

            public void Compile(ModuleBuilder mb, ImmutableSortedDictionary<ItemKey, SaBox<object>> vars)
            {
                TypeBuilder oType = (TypeBuilder)(vars[owner].Value);

                MethodBuilder meb = oType.DefineMethod
                (
                    parent.name.SymbolName(),
                    parent.attributes,
                    symbolTable[methodKey].ReturnType.Resolve(vars),
                    methodKey.Parameters.Select(x => x.Resolve(vars)).ToArray()
                );

                vars[methodKey].Value = meb;
            }
        }

        private class MakeILMethodBody : ICompileStep
        {
            private readonly ILMethodToBuild parent;
            private readonly SymbolTable symbolTable;
            private readonly TypeKey owner;
            private readonly MethodKey methodKey;
            private readonly ImmutableList<LocalInfo> locals;
            private readonly ImmutableList<ILEmit> instructions;

            public MakeILMethodBody(ILMethodToBuild parent, SymbolTable symbolTable, TypeKey owner, MethodKey methodKey, ImmutableList<LocalInfo> locals, ImmutableList<ILEmit> instructions)
            {
                this.parent = parent;
                this.symbolTable = symbolTable;
                this.owner = owner;
                this.methodKey = methodKey;
                this.locals = locals;
                this.instructions = instructions;
            }

            public int Phase { get { return 1; } }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty
                        .Add(owner)
                        .UnionAll(parent.parameters.Select(x => x.ParamType.GetReferences()))
                        .UnionAll(locals.Select(x => x.LocalType.GetReferences()))
                        .UnionAll(instructions.Select(x => x.References))
                        .Add(methodKey);
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
                //TypeBuilder tb = (TypeBuilder)(vars[owner].Value);
                MethodBuilder meb = (MethodBuilder)(vars[methodKey].Value);
                ILGenerator ilg = meb.GetILGenerator();

                if (parent.attributes.HasFlag(MethodAttributes.Static))
                {
                    Utils.ILCompile(symbolTable, instructions, locals, Option<Symbol>.None, parent.parameters, ilg, vars);
                }
                else
                {
                    Utils.ILCompile(symbolTable, instructions, locals, parent.thisParameterName, parent.parameters, ilg, vars);
                }
            }
        }

        public override SymbolTable DefineSymbols(SymbolTable s, TypeKey owner)
        {
            MethodKey mk = GetMethodKey(owner);
            return s.SetItem(mk, new MethodAux(attributes, returnType));
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner)
        {
            MethodKey mk = GetMethodKey(owner);

            return ImmutableList<ICompileStep>.Empty
                .Add(new MakeILMethod(this, s, owner, mk, locals))
                .Add(new MakeILMethodBody(this, s, owner, mk, locals, opcodes));
        }
    }
}

