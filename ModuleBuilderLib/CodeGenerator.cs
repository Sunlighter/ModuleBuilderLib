using Sunlighter.OptionLib;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Sunlighter.ModuleBuilderLib
{
    public class CodeGenerator
    {
        private readonly ImmutableList<ILEmit> opcodes;
        private readonly ImmutableList<LocalInfo> locals;
        private readonly ImmutableStack<Symbol> labelStack;

        private CodeGenerator
        (
            ImmutableList<ILEmit> opcodes,
            ImmutableList<LocalInfo> locals,
            ImmutableStack<Symbol> labelStack
        )
        {
            this.opcodes = opcodes;
            this.locals = locals;
            this.labelStack = labelStack;
        }

        private CodeGenerator()
        {
            this.opcodes = ImmutableList<ILEmit>.Empty;
            this.locals = ImmutableList<LocalInfo>.Empty;
            this.labelStack = ImmutableStack<Symbol>.Empty;
        }

        private static CodeGenerator _empty = new CodeGenerator();

        public static CodeGenerator Empty { get { return _empty; } }

        public ImmutableList<ILEmit> ResultInstructions { get { return opcodes; } }
        public ImmutableList<LocalInfo> ResultLocals { get { return locals; } }

        public bool IsLabelStackEmpty { get { return labelStack.IsEmpty; } }

        public ValueTuple<ImmutableList<LocalInfo>, ImmutableList<ILEmit>> Results
        {
            get
            {
                if (!IsLabelStackEmpty) throw new InvalidOperationException("Label stack is not empty");
                return (locals, opcodes);
            }
        }

        private CodeGenerator WithOpcodes(ImmutableList<ILEmit> opcodes2)
        {
            return new CodeGenerator(opcodes2, locals, labelStack);
        }

        public CodeGenerator Emit(ILEmit instruction)
        {
            return WithOpcodes(opcodes.Add(instruction));
        }

        #region Arithmetic / Logic

        public CodeGenerator Add() { return Emit(new ILEmitNoArg(ILNoArg.Add)); }
        public CodeGenerator AddOvf() { return Emit(new ILEmitNoArg(ILNoArg.AddOvf)); }
        public CodeGenerator AddOvfUn() { return Emit(new ILEmitNoArg(ILNoArg.AddOvfUn)); }

        public CodeGenerator Sub() { return Emit(new ILEmitNoArg(ILNoArg.Sub)); }
        public CodeGenerator SubOvf() { return Emit(new ILEmitNoArg(ILNoArg.SubOvf)); }
        public CodeGenerator SubOvfUn() { return Emit(new ILEmitNoArg(ILNoArg.SubOvfUn)); }

        public CodeGenerator Mul() { return Emit(new ILEmitNoArg(ILNoArg.Mul)); }
        public CodeGenerator MulOvf() { return Emit(new ILEmitNoArg(ILNoArg.MulOvf)); }
        public CodeGenerator MulOvfUn() { return Emit(new ILEmitNoArg(ILNoArg.MulOvfUn)); }

        public CodeGenerator Div() { return Emit(new ILEmitNoArg(ILNoArg.Div)); }
        public CodeGenerator DivUn() { return Emit(new ILEmitNoArg(ILNoArg.DivUn)); }

        public CodeGenerator Rem() { return Emit(new ILEmitNoArg(ILNoArg.Rem)); }
        public CodeGenerator RemUn() { return Emit(new ILEmitNoArg(ILNoArg.RemUn)); }

        public CodeGenerator And() { return Emit(new ILEmitNoArg(ILNoArg.And)); }
        public CodeGenerator Or() { return Emit(new ILEmitNoArg(ILNoArg.Or)); }
        public CodeGenerator Xor() { return Emit(new ILEmitNoArg(ILNoArg.Xor)); }
        public CodeGenerator Invert() { return Emit(new ILEmitNoArg(ILNoArg.Not)); }
        public CodeGenerator Negate() { return Emit(new ILEmitNoArg(ILNoArg.Negate)); }

        public CodeGenerator Shl() /* ( value shiftamount -- value ) */ { return Emit(new ILEmitNoArg(ILNoArg.Shl)); }
        public CodeGenerator Shr() /* ( value shiftamount -- value ) */ { return Emit(new ILEmitNoArg(ILNoArg.Shr)); }
        public CodeGenerator ShrUn() /* ( value shiftamount -- value ) */ { return Emit(new ILEmitNoArg(ILNoArg.ShrUn)); }

        #endregion

        public CodeGenerator Dup() { return Emit(new ILEmitNoArg(ILNoArg.Dup)); }
        public CodeGenerator Drop() { return Emit(new ILEmitNoArg(ILNoArg.Pop)); }

        public CodeGenerator LoadLocal(Symbol s)
        {
            return Emit(new ILEmitLoadLocal(s));
        }

        public CodeGenerator StoreLocal(Symbol s)
        {
            return Emit(new ILEmitStoreLocal(s));
        }

        public CodeGenerator LoadArg(Symbol s)
        {
            return Emit(new ILEmitLoadArg(s));
        }

        public CodeGenerator StoreArg(Symbol s)
        {
            return Emit(new ILEmitStoreArg(s));
        }

        public CodeGenerator LoadInt(int literal)
        {
            return Emit(new ILEmitLoadInt(literal));
        }

        public CodeGenerator LoadLong(long literal)
        {
            return Emit(new ILEmitLoadLong(literal));
        }

        public CodeGenerator LoadFloat(float literal)
        {
            return Emit(new ILEmitLoadFloat(literal));
        }

        public CodeGenerator LoadDouble(double literal)
        {
            return Emit(new ILEmitLoadDouble(literal));
        }

        public CodeGenerator LoadString(string literal)
        {
            return Emit(new ILEmitLoadString(literal));
        }

        public CodeGenerator LoadNullPtr()
        {
            return Emit(new ILEmitNoArg(ILNoArg.LoadNullPtr));
        }

        public CodeGenerator LoadField(FieldReference fi)
        {
            return Emit(new ILEmitFieldOp(ILFieldOp.Load, fi));
        }

        public CodeGenerator LoadFieldAddress(FieldReference fi)
        {
            return Emit(new ILEmitFieldOp(ILFieldOp.LoadAddress, fi));
        }

        public CodeGenerator StoreField(FieldReference fi)
        {
            return Emit(new ILEmitFieldOp(ILFieldOp.Store, fi));
        }

        public CodeGenerator LoadStaticField(FieldReference fi)
        {
            return Emit(new ILEmitFieldOp(ILFieldOp.LoadStatic, fi));
        }

        public CodeGenerator LoadStaticFieldAddress(FieldReference fi)
        {
            return Emit(new ILEmitFieldOp(ILFieldOp.LoadStaticAddress, fi));
        }

        public CodeGenerator StoreStaticField(FieldReference fi)
        {
            return Emit(new ILEmitFieldOp(ILFieldOp.Store, fi));
        }

        public CodeGenerator LoadToken(TypeReference t)
        {
            return Emit(new ILEmitLoadTypeToken(t));
        }

        public CodeGenerator LoadToken(MethodReference mi)
        {
            return Emit(new ILEmitLoadMethodToken(mi));
        }

        public CodeGenerator LoadToken(ConstructorReference ci)
        {
            return Emit(new ILEmitLoadConstructorToken(ci));
        }

        public CodeGenerator LoadToken(FieldReference fi)
        {
            return Emit(new ILEmitLoadFieldToken(fi));
        }

        public CodeGenerator NewObj(ConstructorReference ci)
        {
            return Emit(new ILEmitNewObj(ci));
        }

        public CodeGenerator Throw()
        {
            return Emit(new ILEmitNoArg(ILNoArg.Throw));
        }

        public CodeGenerator Tail()
        {
            return Emit(new ILEmitNoArg(ILNoArg.Tail));
        }

        public CodeGenerator Call(MethodReference mi)
        {
            return Emit(new ILEmitCall(mi));
        }

        public CodeGenerator CallVirt(MethodReference mi)
        {
            return Emit(new ILEmitCallVirt(mi));
        }

        public CodeGenerator Return()
        {
            return Emit(new ILEmitNoArg(ILNoArg.Return));
        }

        public CodeGenerator IsInst(TypeReference t)
        {
            return Emit(new ILEmitTypeOp(ILTypeOp.IsInst, t));
        }

        public CodeGenerator CastClass(TypeReference t)
        {
            return Emit(new ILEmitTypeOp(ILTypeOp.CastClass, t));
        }

        private CodeGenerator SwapLabels()
        {
            return new CodeGenerator(opcodes, locals, labelStack.Swap());
        }

        public CodeGenerator Ahead(bool useLongForm)
        {
            Symbol l = new Symbol();
            ImmutableList<ILEmit> opcodes2 = opcodes.Add(new ILEmitBranch(useLongForm ? ILBranch.Br : ILBranch.Br_S, l));
            ImmutableStack<Symbol> labelStack2 = labelStack.Push(l);
            return new CodeGenerator(opcodes2, locals, labelStack2);
        }

        public CodeGenerator Then()
        {
            Symbol l = labelStack.Peek();
            ImmutableStack<Symbol> labelStack2 = labelStack.Pop();
            ImmutableList<ILEmit> opcodes2 = opcodes.Add(new ILEmitLabel(l));
            return new CodeGenerator(opcodes2, locals, labelStack2);
        }

        public CodeGenerator IfNot(ILBranch branch)
        {
            Symbol l = new Symbol();
            ImmutableList<ILEmit> opcodes2 = opcodes.Add(new ILEmitBranch(branch, l));
            ImmutableStack<Symbol> labelStack2 = labelStack.Push(l);
            return new CodeGenerator(opcodes2, locals, labelStack2);
        }

        public CodeGenerator Else(bool useLongForm)
        {
            CodeGenerator cg2 = Ahead(useLongForm);
            cg2 = cg2.SwapLabels();
            cg2 = cg2.Then();
            return cg2;
        }

        public CodeGenerator Begin()
        {
            Symbol l = new Symbol();
            ImmutableList<ILEmit> opcodes2 = opcodes.Add(new ILEmitLabel(l));
            ImmutableStack<Symbol> labelStack2 = labelStack.Push(l);
            return new CodeGenerator(opcodes2, locals, labelStack2);
        }

        public CodeGenerator Again(bool useLongForm)
        {
            Symbol l = labelStack.Peek();
            ImmutableStack<Symbol> labelStack2 = labelStack.Pop();
            ImmutableList<ILEmit> opcodes2 = opcodes.Add(new ILEmitBranch(useLongForm ? ILBranch.Br : ILBranch.Br_S, l));
            return new CodeGenerator(opcodes2, locals, labelStack2);
        }

        public CodeGenerator UntilNot(ILBranch branch)
        {
            Symbol l = labelStack.Peek();
            ImmutableStack<Symbol> labelStack2 = labelStack.Pop();
            ImmutableList<ILEmit> opcodes2 = opcodes.Add(new ILEmitBranch(branch, l));
            return new CodeGenerator(opcodes2, locals, labelStack2);
        }

        public CodeGenerator WhileNot(ILBranch branch)
        {
            return IfNot(branch);
        }

        public CodeGenerator Repeat(bool useLongForm)
        {
            CodeGenerator cg2 = SwapLabels();
            cg2 = cg2.Again(useLongForm);
            cg2 = cg2.Then();
            return cg2;
        }

        public ValueTuple<CodeGenerator, Symbol> DeclareLocal(TypeReference t)
        {
            Symbol s = new Symbol();
            CodeGenerator cg2 = new CodeGenerator(opcodes, locals.Add(new LocalInfo(s, t, false)), labelStack);
            return (cg2, s);
        }

        public TypeReference GetLocalType(Symbol local)
        {
            Option<LocalInfo> localInfo = locals.FindOption(x => x.Name == local);
            if (localInfo.HasValue)
            {
                return localInfo.Value.LocalType;
            }
            else
            {
                throw new InvalidOperationException("Attempt to get type of undefined local " + local.ToString());
            }
        }

        public CodeGenerator Ceq() { return Emit(new ILEmitNoArg(ILNoArg.Ceq)); }
        public CodeGenerator Clt() { return Emit(new ILEmitNoArg(ILNoArg.Clt)); }
        public CodeGenerator CltUn() { return Emit(new ILEmitNoArg(ILNoArg.CltUn)); }
        public CodeGenerator Cgt() { return Emit(new ILEmitNoArg(ILNoArg.Cgt)); }
        public CodeGenerator CgtUn() { return Emit(new ILEmitNoArg(ILNoArg.CgtUn)); }

        public CodeGenerator SizeOf(TypeReference t)
        {
            return Emit(new ILEmitTypeOp(ILTypeOp.SizeOf, t));
        }

        public CodeGenerator LoadObjRef() { return Emit(new ILEmitNoArg(ILNoArg.LoadObjRef)); }

        public CodeGenerator LoadObjIndirect(TypeReference t)
        {
            return Emit(new ILEmitTypeOp(ILTypeOp.LoadObjIndirect, t));
        }

        public CodeGenerator StoreObjRef() { return Emit(new ILEmitNoArg(ILNoArg.StoreObjRef)); }

        public CodeGenerator StoreObjIndirect(TypeReference t)
        {
            return Emit(new ILEmitTypeOp(ILTypeOp.StoreObjIndirect, t));
        }

        public CodeGenerator Box(TypeReference t)
        {
            return Emit(new ILEmitTypeOp(ILTypeOp.Box, t));
        }

        public CodeGenerator Unbox(TypeReference t)
        {
            return Emit(new ILEmitTypeOp(ILTypeOp.Unbox, t));
        }

        public CodeGenerator UnboxAny(TypeReference t)
        {
            return Emit(new ILEmitTypeOp(ILTypeOp.UnboxAny, t));
        }

        public CodeGenerator Try(Symbol endOfBlockLabel)
        {
            return Emit(new ILEmitBeginExceptionBlock(endOfBlockLabel));
        }

        public CodeGenerator Catch(TypeReference exceptionType)
        {
            return Emit(new ILEmitBeginCatchBlock(exceptionType));
        }

        public CodeGenerator Finally()
        {
            return Emit(new ILEmitBeginFinallyBlock());
        }

        public CodeGenerator EndTryCatchFinally()
        {
            return Emit(new ILEmitEndExceptionBlock());
        }

        public CodeGenerator Leave(bool useLongForm, Symbol endOfBlockLabel)
        {
            return Emit(new ILEmitBranch(useLongForm ? ILBranch.Leave : ILBranch.Leave_S, endOfBlockLabel));
        }
    }

    public static partial class Extensions
    {
        public static ImmutableStack<T> Swap<T>(this ImmutableStack<T> stack)
        {
            T l = stack.Peek();
            stack = stack.Pop();
            T m = stack.Peek();
            stack = stack.Pop();
            return stack.Push(l).Push(m);
        }

        public static Option<T> FindOption<T>(this IEnumerable<T> collection, Predicate<T> predicate)
        {
            foreach (T item in collection)
            {
                if (predicate(item)) return Option<T>.Some(item);
            }
            return Option<T>.None;
        }
    }
}
