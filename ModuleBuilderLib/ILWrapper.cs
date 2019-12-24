using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Sunlighter.ModuleBuilderLib
{
    public class ILContext
    {
        private readonly ImmutableDictionary<Symbol, Label> labels;
        private readonly ImmutableDictionary<Symbol, LocalBuilder> locals;
        private readonly ImmutableDictionary<Symbol, int> parameters;
        private readonly ImmutableDictionary<ItemKey, SaBox<object>> references;
        private readonly SymbolTable symbolTable;

        public ILContext
        (
            ImmutableDictionary<Symbol, Label> labels,
            ImmutableDictionary<Symbol, LocalBuilder> locals,
            ImmutableDictionary<Symbol, int> parameters,
            ImmutableDictionary<ItemKey, SaBox<object>> references,
            SymbolTable symbolTable
        )
        {
            this.labels = labels;
            this.locals = locals;
            this.parameters = parameters;
            this.references = references;
            this.symbolTable = symbolTable;
        }

        public ImmutableDictionary<Symbol, Label> Labels { get { return labels; } }
        public ImmutableDictionary<Symbol, LocalBuilder> Locals { get { return locals; } }
        public ImmutableDictionary<Symbol, int> Parameters { get { return parameters; } }
        public ImmutableDictionary<ItemKey, SaBox<object>> References { get { return references; } }
        public SymbolTable SymbolTable { get { return symbolTable; } }
    }

    public abstract class ILEmit
    {
        public virtual ImmutableHashSet<Symbol> LabelsDefined { get { return ImmutableHashSet<Symbol>.Empty; } }

        public virtual ImmutableHashSet<Symbol> LabelsUsed { get { return ImmutableHashSet<Symbol>.Empty; } }

        public virtual ImmutableHashSet<Symbol> LabelsWithoutAutoCreate { get { return ImmutableHashSet<Symbol>.Empty; } }

        public virtual ImmutableHashSet<ItemKey> References { get { return ImmutableHashSet<ItemKey>.Empty; } }

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

    public class ILEmitNoArg : ILEmit
    {
        private ILNoArg insn;

        public ILEmitNoArg(ILNoArg insn)
        {
            this.insn = insn;
        }

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

    public class ILEmitLabel : ILEmit
    {
        private Symbol name;

        public ILEmitLabel(Symbol name)
        {
            this.name = name;
        }

        public override ImmutableHashSet<Symbol> LabelsDefined
        {
            get
            {
                return ImmutableHashSet<Symbol>.Empty.Add(name);
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

    public class ILEmitBranch : ILEmit
    {
        private ILBranch branch;
        private Symbol target;

        public ILEmitBranch(ILBranch branch, Symbol target)
        {
            this.branch = branch;
            this.target = target;
        }

        public override ImmutableHashSet<Symbol> LabelsUsed
        {
            get
            {
                return ImmutableHashSet<Symbol>.Empty.Add(target);
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

    public class ILEmitSwitch : ILEmit
    {
        private readonly ImmutableList<Symbol> targets;

        public ILEmitSwitch(ImmutableList<Symbol> targets)
        {
            this.targets = targets;
        }

        public override ImmutableHashSet<Symbol> LabelsUsed
        {
            get
            {
                return ImmutableHashSet<Symbol>.Empty.Union(targets);
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.Emit(OpCodes.Switch, targets.Select(x => context.Labels[x]).ToArray());
        }
    }

    public class ILEmitLoadArg : ILEmit
    {
        private readonly Symbol name;

        public ILEmitLoadArg(Symbol name)
        {
            this.name = name;
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadArg(context.Parameters[name]);
        }
    }

    public class ILEmitLoadArgAddress : ILEmit
    {
        private readonly Symbol name;

        public ILEmitLoadArgAddress(Symbol name)
        {
            this.name = name;
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadArgAddress(context.Parameters[name]);
        }
    }

    public class ILEmitStoreArg : ILEmit
    {
        private readonly Symbol name;

        public ILEmitStoreArg(Symbol name)
        {
            this.name = name;
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.StoreArg(context.Parameters[name]);
        }
    }

    public class ILEmitLoadLocal : ILEmit
    {
        private readonly Symbol name;

        public ILEmitLoadLocal(Symbol name)
        {
            this.name = name;
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadLocal(context.Locals[name]);
        }
    }

    public class ILEmitLoadLocalAddress : ILEmit
    {
        private readonly Symbol name;

        public ILEmitLoadLocalAddress(Symbol name)
        {
            this.name = name;
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadLocalAddress(context.Locals[name]);
        }
    }

    public class ILEmitStoreLocal : ILEmit
    {
        private readonly Symbol name;

        public ILEmitStoreLocal(Symbol name)
        {
            this.name = name;
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.StoreLocal(context.Locals[name]);
        }
    }

    public class ILEmitLoadInt : ILEmit
    {
        private readonly int literal;

        public ILEmitLoadInt(int literal)
        {
            this.literal = literal;
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadInt(literal);
        }
    }

    public class ILEmitLoadLong : ILEmit
    {
        private readonly long literal;

        public ILEmitLoadLong(long literal)
        {
            this.literal = literal;
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadLong(literal);
        }
    }

    public class ILEmitLoadFloat : ILEmit
    {
        private readonly float literal;

        public ILEmitLoadFloat(float literal)
        {
            this.literal = literal;
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadFloat(literal);
        }
    }

    public class ILEmitLoadDouble : ILEmit
    {
        private readonly double literal;

        public ILEmitLoadDouble(double literal)
        {
            this.literal = literal;
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.LoadDouble(literal);
        }
    }

    public class ILEmitLoadString : ILEmit
    {
        private readonly string literal;

        public ILEmitLoadString(string literal)
        {
            this.literal = literal;
        }

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

    public class ILEmitFieldOp : ILEmit
    {
        private readonly ILFieldOp fieldOp;
        private readonly FieldReference fieldReference;

        public ILEmitFieldOp(ILFieldOp fieldOp, FieldReference fieldReference)
        {
            this.fieldOp = fieldOp;
            this.fieldReference = fieldReference;
        }

        public override ImmutableHashSet<ItemKey> References
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

    public class ILEmitLoadMethodToken : ILEmit
    {
        private readonly MethodReference methodReference;

        public ILEmitLoadMethodToken(MethodReference methodReference)
        {
            this.methodReference = methodReference;
        }

        public override ImmutableHashSet<ItemKey> References
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

    public class ILEmitLoadFieldToken : ILEmit
    {
        private readonly FieldReference fieldReference;

        public ILEmitLoadFieldToken(FieldReference fieldReference)
        {
            this.fieldReference = fieldReference;
        }

        public override ImmutableHashSet<ItemKey> References
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

    public class ILEmitLoadConstructorToken : ILEmit
    {
        private readonly ConstructorReference constructorReference;

        public ILEmitLoadConstructorToken(ConstructorReference constructorReference)
        {
            this.constructorReference = constructorReference;
        }

        public override ImmutableHashSet<ItemKey> References
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

    public class ILEmitLoadTypeToken : ILEmit
    {
        private readonly TypeReference typeReference;

        public ILEmitLoadTypeToken(TypeReference typeReference)
        {
            this.typeReference = typeReference;
        }

        public override ImmutableHashSet<ItemKey> References
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

    public class ILEmitNewObj : ILEmit
    {
        private readonly ConstructorReference constructorReference;

        public ILEmitNewObj(ConstructorReference constructorReference)
        {
            this.constructorReference = constructorReference;
        }

        public override ImmutableHashSet<ItemKey> References
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

    public class ILEmitCall : ILEmit
    {
        private readonly MethodReference methodReference;

        public ILEmitCall(MethodReference methodReference)
        {
            this.methodReference = methodReference;
        }

        public override ImmutableHashSet<ItemKey> References
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

    public class ILEmitConstructorCall : ILEmit
    {
        private readonly ConstructorReference constructorReference;

        public ILEmitConstructorCall(ConstructorReference constructorReference)
        {
            this.constructorReference = constructorReference;
        }

        public override ImmutableHashSet<ItemKey> References
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

    public class ILEmitCallVirt : ILEmit
    {
        private readonly MethodReference methodReference;

        public ILEmitCallVirt(MethodReference methodReference)
        {
            this.methodReference = methodReference;
        }

        public override ImmutableHashSet<ItemKey> References
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

    public class ILEmitTypeOp : ILEmit
    {
        private readonly ILTypeOp typeOp;
        private readonly TypeReference typeReference;

        public ILEmitTypeOp(ILTypeOp typeOp, TypeReference typeReference)
        {
            this.typeOp = typeOp;
            this.typeReference = typeReference;
        }

        public override ImmutableHashSet<ItemKey> References
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

    public class ILEmitUnaligned : ILEmit
    {
        private readonly Alignment a;

        public ILEmitUnaligned(Alignment a)
        {
            this.a = a;
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.Unaligned(a);
        }
    }

    public class ILEmitBeginExceptionBlock : ILEmit
    {
        private readonly Symbol endOfBlock;

        public ILEmitBeginExceptionBlock(Symbol endOfBlock)
        {
            this.endOfBlock = endOfBlock;
        }

        public override ImmutableHashSet<Symbol> LabelsDefined
        {
            get
            {
                return ImmutableHashSet<Symbol>.Empty.Add(endOfBlock);
            }
        }

        public override ImmutableHashSet<Symbol> LabelsWithoutAutoCreate
        {
            get
            {
                return ImmutableHashSet<Symbol>.Empty.Add(endOfBlock);
            }
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            Label endOfBlockLabel = ilg.BeginExceptionBlock();
            context.Labels.Add(endOfBlock, endOfBlockLabel);
        }
    }

    public class ILEmitBeginCatchBlock : ILEmit
    {
        private readonly TypeReference exceptionType;

        public ILEmitBeginCatchBlock(TypeReference exceptionType)
        {
            this.exceptionType = exceptionType;
        }

        public override ImmutableHashSet<ItemKey> References
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

    public class ILEmitBeginFinallyBlock : ILEmit
    {
        public ILEmitBeginFinallyBlock()
        {
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.BeginFinallyBlock();
        }
    }

    public class ILEmitEndExceptionBlock : ILEmit
    {
        public ILEmitEndExceptionBlock()
        {
        }

        public override void Emit(ILGenerator ilg, ILContext context)
        {
            ilg.EndExceptionBlock();
        }
    }

    public class LocalInfo
    {
        private readonly Symbol name;
        private readonly TypeReference localType;
        private readonly bool isPinned;

        public LocalInfo(Symbol name, TypeReference paramType, bool isPinned)
        {
            this.name = name;
            this.localType = paramType;
            this.isPinned = isPinned;
        }

        public Symbol Name { get { return name; } }

        public TypeReference LocalType { get { return localType; } }

        public bool IsPinned { get { return isPinned; } }
    }

    public static partial class Utils
    {
        public static void ILCompile(SymbolTable symbolTable, ImmutableList<ILEmit> instructions, ImmutableList<LocalInfo> localInfos, Option<Symbol> thisParameterName, ImmutableList<ParamInfo> paramInfos, ILGenerator ilg, ImmutableDictionary<ItemKey, SaBox<object>> vars)
        {
            ImmutableHashSet<Symbol> labelsDefined = instructions.Select(x => x.LabelsDefined).UnionAll();
            ImmutableHashSet<Symbol> labelsUsed = instructions.Select(x => x.LabelsUsed).UnionAll();
            ImmutableHashSet<Symbol> labelsWithoutAutoCreate = instructions.Select(x => x.LabelsWithoutAutoCreate).UnionAll();

            if (!((labelsUsed.Except(labelsDefined)).IsEmpty)) throw new Exception("Labels { " + ((labelsUsed.Except(labelsDefined)).Select(x => x.Name).Concatenate(" ")) + " } used without being defined");

            ImmutableDictionary<Symbol, Label> labels = ImmutableDictionary<Symbol, Label>.Empty;

            foreach (Symbol s in labelsUsed)
            {
                if (!labelsWithoutAutoCreate.Contains(s))
                {
                    labels = labels.Add(s, ilg.DefineLabel());
                }
            }

            ImmutableDictionary<Symbol, LocalBuilder> locals = ImmutableDictionary<Symbol, LocalBuilder>.Empty;

            foreach (LocalInfo localInfo in localInfos)
            {
                locals = locals.Add(localInfo.Name, ilg.DeclareLocal(localInfo.LocalType.Resolve(vars), localInfo.IsPinned));
            }

            ImmutableDictionary<Symbol, int> parameters = ImmutableDictionary<Symbol, int>.Empty;

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

    public class ILConstructorToBuild : ElementOfClass
    {
        private readonly MethodAttributes attributes;
        private readonly Symbol thisParameterName;
        private readonly ImmutableList<ParamInfo> parameters;
        private readonly ImmutableList<LocalInfo> locals;
        private readonly ImmutableList<ILEmit> opcodes;

        public ILConstructorToBuild
        (
            MethodAttributes attributes,
            Symbol thisParameterName,
            ImmutableList<ParamInfo> parameters,
            ValueTuple<ImmutableList<LocalInfo>, ImmutableList<ILEmit>> localsAndOpcodes
        )
        {
            this.attributes = attributes;
            this.thisParameterName = thisParameterName;
            this.parameters = parameters;
            this.locals = localsAndOpcodes.Item1;
            this.opcodes = localsAndOpcodes.Item2;
        }

        private ConstructorKey GetConstructorKey(TypeKey owner)
        {
            return new ConstructorKey(owner, parameters.Select(x => x.ParamType).ToImmutableList());
        }

        private class MakeILConstructor : ICompileStep
        {
            private readonly ILConstructorToBuild parent;
            private readonly SymbolTable symbolTable;
            private readonly TypeKey owner;
            private readonly ConstructorKey constructorKey;
            private readonly ImmutableList<LocalInfo> locals;

            public MakeILConstructor(ILConstructorToBuild parent, SymbolTable symbolTable, TypeKey owner, ConstructorKey constructorKey, ImmutableList<LocalInfo> locals)
            {
                this.parent = parent;
                this.symbolTable = symbolTable;
                this.owner = owner;
                this.constructorKey = constructorKey;
                this.locals = locals;
            }

            public int Phase { get { return 1; } }

            public ImmutableHashSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableHashSet<ItemKey>.Empty
                        .Add(owner)
                        .Union(parent.parameters.Select(x => x.ParamType.GetReferences()).UnionAll())
                        .Union(locals.Select(x => x.LocalType.GetReferences()).UnionAll());
                }
            }

            public ImmutableHashSet<ItemKey> Outputs
            {
                get
                {
                    return ImmutableHashSet<ItemKey>.Empty.Add(constructorKey);
                }
            }

            public void Compile(ModuleBuilder mb, ImmutableDictionary<ItemKey, SaBox<object>> vars)
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

            public ImmutableHashSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableHashSet<ItemKey>.Empty
                        .Add(owner)
                        .Union(parent.parameters.Select(x => x.ParamType.GetReferences()).UnionAll())
                        .Union(locals.Select(x => x.LocalType.GetReferences()).UnionAll())
                        .Union(instructions.Select(x => x.References).UnionAll())
                        .Add(constructorKey);
                }
            }

            public ImmutableHashSet<ItemKey> Outputs
            {
                get
                {
                    return ImmutableHashSet<ItemKey>.Empty;
                }
            }

            public void Compile(ModuleBuilder mb, ImmutableDictionary<ItemKey, SaBox<object>> vars)
            {
                TypeBuilder tb = (TypeBuilder)(vars[owner].Value);
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
                .Add(new MakeILConstructor(this, s, owner, ck, locals))
                .Add(new MakeILConstructorBody(this, s, owner, ck, locals, opcodes));
        }
    }

    public class ILMethodToBuild : ElementOfClass
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
            Symbol name,
            MethodAttributes attributes,
            TypeReference returnType,
            Option<Symbol> thisParameterName,
            ImmutableList<ParamInfo> parameters,
            ValueTuple<ImmutableList<LocalInfo>, ImmutableList<ILEmit>> localsAndOpcodes
        )
        {
            this.name = name;
            this.attributes = attributes;
            this.returnType = returnType;
            this.thisParameterName = thisParameterName;
            this.parameters = parameters;
            this.locals = localsAndOpcodes.Item1;
            this.opcodes = localsAndOpcodes.Item2;

            if (attributes.HasFlag(MethodAttributes.Static) && thisParameterName.HasValue)
            {
                throw new ArgumentException("A static method doesn't have a \"this\" parameter");
            }
            else if (!attributes.HasFlag(MethodAttributes.Static) && !thisParameterName.HasValue)
            {
                throw new ArgumentException("A non-static method requires a \"this\" parameter");
            }
        }

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

            public ImmutableHashSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableHashSet<ItemKey>.Empty
                        .Add(owner)
                        .Union(parent.parameters.Select(x => x.ParamType.GetReferences()).UnionAll())
                        .Union(locals.Select(x => x.LocalType.GetReferences()).UnionAll())
                        .Union(symbolTable[methodKey].ReturnType.GetReferences());
                }
            }

            public ImmutableHashSet<ItemKey> Outputs
            {
                get
                {
                    return ImmutableHashSet<ItemKey>.Empty.Add(methodKey);
                }
            }

            public void Compile(ModuleBuilder mb, ImmutableDictionary<ItemKey, SaBox<object>> vars)
            {
                TypeBuilder oType = (TypeBuilder)(vars[owner].Value);

                MethodBuilder meb = oType.DefineMethod
                (
                    parent.name.Name,
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

            public ImmutableHashSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableHashSet<ItemKey>.Empty
                        .Add(owner)
                        .Union(parent.parameters.Select(x => x.ParamType.GetReferences()).UnionAll())
                        .Union(locals.Select(x => x.LocalType.GetReferences()).UnionAll())
                        .Union(instructions.Select(x => x.References).UnionAll())
                        .Add(methodKey);
                }
            }

            public ImmutableHashSet<ItemKey> Outputs
            {
                get
                {
                    return ImmutableHashSet<ItemKey>.Empty;
                }
            }

            public void Compile(ModuleBuilder mb, ImmutableDictionary<ItemKey, SaBox<object>> vars)
            {
                TypeBuilder tb = (TypeBuilder)(vars[owner].Value);
                MethodBuilder meb = (MethodBuilder)(vars[methodKey].Value);
                ILGenerator ilg = meb.GetILGenerator();

                if (parent.attributes.HasFlag(MethodAttributes.Static))
                {
                    Utils.ILCompile(symbolTable, instructions, locals, Option<Symbol>.None(), parent.parameters, ilg, vars);
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

