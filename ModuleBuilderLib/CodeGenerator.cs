using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Sunlighter.ModuleBuilderLib
{
    public class CodeGenerator2
    {
        private readonly SymbolTable symbolTable;

        private ImmutableList<ILEmit> opcodes;
        private ImmutableList<LocalInfo2> locals;

        private readonly string context;

        private ImmutableStack<Symbol> labelStack;

        public CodeGenerator2(SymbolTable symbolTable, string context)
        {
            this.symbolTable = symbolTable;
            this.opcodes = ImmutableList<ILEmit>.Empty;
            this.locals = ImmutableList<LocalInfo2>.Empty;
            this.context = context;
            this.labelStack = ImmutableStack<Symbol>.Empty;
        }

        public SymbolTable SymbolTable { get { return symbolTable; } }

        public string Context { get { return context; } }

        public ImmutableList<ILEmit> ResultInstructions { get { return opcodes; } }
        public ImmutableList<LocalInfo2> ResultLocals { get { return locals; } }

        public void Emit(ILEmit instruction)
        {
            opcodes = opcodes.Add(instruction);
        }

        #region Arithmetic / Logic

        public void Add() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Add)); }
        public void AddOvf() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.AddOvf)); }
        public void AddOvfUn() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.AddOvfUn)); }

        public void Sub() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Sub)); }
        public void SubOvf() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.SubOvf)); }
        public void SubOvfUn() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.SubOvfUn)); }

        public void Mul() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Mul)); }
        public void MulOvf() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.MulOvf)); }
        public void MulOvfUn() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.MulOvfUn)); }

        public void Div() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Div)); }
        public void DivUn() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.DivUn)); }

        public void Rem() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Rem)); }
        public void RemUn() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.RemUn)); }

        public void And() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.And)); }
        public void Or() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Or)); }
        public void Xor() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Xor)); }
        public void Invert() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Not)); }
        public void Negate() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Negate)); }

        public void Shl() /* ( value shiftamount -- value ) */ { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Shl)); }
        public void Shr() /* ( value shiftamount -- value ) */ { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Shr)); }
        public void ShrUn() /* ( value shiftamount -- value ) */ { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.ShrUn)); }

        #endregion

        public void Dup() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Dup)); }
        public void Drop() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Pop)); }

        public void LoadLocal(Symbol s)
        {
            opcodes = opcodes.Add(new ILEmitLoadLocal(s));
        }

        public void StoreLocal(Symbol s)
        {
            opcodes = opcodes.Add(new ILEmitStoreLocal(s));
        }

        public void LoadArg(Symbol s)
        {
            opcodes = opcodes.Add(new ILEmitLoadArg(s));
        }

        public void StoreArg(Symbol s)
        {
            opcodes = opcodes.Add(new ILEmitStoreArg(s));
        }

        public void LoadInt(int literal)
        {
            opcodes = opcodes.Add(new ILEmitLoadInt(literal));
        }

        public void LoadLong(long literal)
        {
            opcodes = opcodes.Add(new ILEmitLoadLong(literal));
        }

        public void LoadFloat(float literal)
        {
            opcodes = opcodes.Add(new ILEmitLoadFloat(literal));
        }

        public void LoadDouble(double literal)
        {
            opcodes = opcodes.Add(new ILEmitLoadDouble(literal));
        }

        public void LoadString(string literal)
        {
            opcodes = opcodes.Add(new ILEmitLoadString(literal));
        }

        public void LoadNullPtr()
        {
            opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.LoadNullPtr));
        }

        public void LoadField(FieldReference fi)
        {
            opcodes = opcodes.Add(new ILEmitFieldOp(ILFieldOp.Load, fi));
        }

        public void LoadFieldAddress(FieldReference fi)
        {
            opcodes = opcodes.Add(new ILEmitFieldOp(ILFieldOp.LoadAddress, fi));
        }

        public void StoreField(FieldReference fi)
        {
            opcodes = opcodes.Add(new ILEmitFieldOp(ILFieldOp.Store, fi));
        }

        public void LoadStaticField(FieldReference fi)
        {
            opcodes = opcodes.Add(new ILEmitFieldOp(ILFieldOp.LoadStatic, fi));
        }

        public void LoadStaticFieldAddress(FieldReference fi)
        {
            opcodes = opcodes.Add(new ILEmitFieldOp(ILFieldOp.LoadStaticAddress, fi));
        }

        public void StoreStaticField(FieldReference fi)
        {
            opcodes = opcodes.Add(new ILEmitFieldOp(ILFieldOp.Store, fi));
        }

        public void LoadToken(TypeReference t)
        {
            opcodes = opcodes.Add(new ILEmitLoadTypeToken(t));
        }

        public void LoadToken(MethodReference mi)
        {
            opcodes = opcodes.Add(new ILEmitLoadMethodToken(mi));
        }

        public void LoadToken(ConstructorReference ci)
        {
            opcodes = opcodes.Add(new ILEmitLoadConstructorToken(ci));
        }

        public void LoadToken(FieldReference fi)
        {
            opcodes = opcodes.Add(new ILEmitLoadFieldToken(fi));
        }

        public void NewObj(ConstructorReference ci)
        {
            opcodes = opcodes.Add(new ILEmitNewObj(ci));
        }

        public void Throw()
        {
            opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Throw));
        }

        public void Tail()
        {
            opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Tail));
        }

        public void Call(MethodReference mi)
        {
            opcodes = opcodes.Add(new ILEmitCall(mi));
        }

        public void CallVirt(MethodReference mi)
        {
            opcodes = opcodes.Add(new ILEmitCallVirt(mi));
        }

        public void Return()
        {
            opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Return));
        }

        public void IsInst(TypeReference t)
        {
            opcodes = opcodes.Add(new ILEmitTypeOp(ILTypeOp.IsInst, t));
        }

        public void CastClass(TypeReference t)
        {
            opcodes = opcodes.Add(new ILEmitTypeOp(ILTypeOp.CastClass, t));
        }

        private void SwapLabels()
        {
            labelStack = labelStack.Swap();
        }

        public void Ahead(bool useLongForm)
        {
            Symbol l = new Symbol();
            opcodes = opcodes.Add(new ILEmitBranch(useLongForm ? ILBranch.Br : ILBranch.Br_S, l));
            labelStack = labelStack.Push(l);
        }

        public void Then()
        {
            Symbol l = labelStack.Peek();
            labelStack = labelStack.Pop();
            opcodes = opcodes.Add(new ILEmitLabel(l));
        }

        public void IfNot(ILBranch branch)
        {
            Symbol l = new Symbol();
            opcodes = opcodes.Add(new ILEmitBranch(branch, l));
            labelStack = labelStack.Push(l);
        }

        public void Else(bool useLongForm)
        {
            Ahead(useLongForm);
            SwapLabels();
            Then();
        }

        public void Begin()
        {
            Symbol l = new Symbol();
            opcodes = opcodes.Add(new ILEmitLabel(l));
            labelStack = labelStack.Push(l);
        }

        public void Again(bool useLongForm)
        {
            Symbol l = labelStack.Peek();
            labelStack = labelStack.Pop();
            opcodes.Add(new ILEmitBranch(useLongForm ? ILBranch.Br : ILBranch.Br_S, l));
        }

        public void UntilNot(ILBranch branch)
        {
            Symbol l = labelStack.Peek();
            labelStack = labelStack.Pop();
            opcodes.Add(new ILEmitBranch(branch, l));
        }

        public void WhileNot(ILBranch branch)
        {
            IfNot(branch);
        }

        public void Repeat(bool useLongForm)
        {
            SwapLabels();
            Again(useLongForm);
            Then();
        }

        public Symbol DeclareLocal(TypeReference t)
        {
            Symbol s = new Symbol();
            locals = locals.Add(new LocalInfo2(s, t, false));
            return s;
        }

        public TypeReference GetLocalType(Symbol local)
        {
            Option<LocalInfo2> localInfo = locals.FindOption(x => x.Name == local);
            if (localInfo.HasValue)
            {
                return localInfo.Value.LocalType;
            }
            else
            {
                throw new InvalidOperationException("Attempt to get type of undefined local " + local.ToString());
            }
        }

        public void Ceq() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Ceq)); }
        public void Clt() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Clt)); }
        public void CltUn() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.CltUn)); }
        public void Cgt() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.Cgt)); }
        public void CgtUn() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.CgtUn)); }

        public void SizeOf(TypeReference t)
        {
            opcodes = opcodes.Add(new ILEmitTypeOp(ILTypeOp.SizeOf, t));
        }

        public void LoadObjRef() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.LoadObjRef)); }

        public void LoadObjIndirect(TypeReference t)
        {
            opcodes = opcodes.Add(new ILEmitTypeOp(ILTypeOp.LoadObjIndirect, t));
        }

        public void StoreObjRef() { opcodes = opcodes.Add(new ILEmitNoArg(ILNoArg.StoreObjRef)); }

        public void StoreObjIndirect(TypeReference t)
        {
            opcodes = opcodes.Add(new ILEmitTypeOp(ILTypeOp.StoreObjIndirect, t));
        }

        public void Box(TypeReference t)
        {
            opcodes = opcodes.Add(new ILEmitTypeOp(ILTypeOp.Box, t));
        }

        public void Unbox(TypeReference t)
        {
            opcodes = opcodes.Add(new ILEmitTypeOp(ILTypeOp.Unbox, t));
        }

        public void UnboxAny(TypeReference t)
        {
            opcodes = opcodes.Add(new ILEmitTypeOp(ILTypeOp.UnboxAny, t));
        }

        public void Try(Symbol endOfBlockLabel)
        {
            opcodes = opcodes.Add(new ILEmitBeginExceptionBlock(endOfBlockLabel));
        }

        public void Catch(TypeReference exceptionType)
        {
            opcodes = opcodes.Add(new ILEmitBeginCatchBlock(exceptionType));
        }

        public void Finally()
        {
            opcodes = opcodes.Add(new ILEmitBeginFinallyBlock());
        }

        public void EndTryCatchFinally()
        {
            opcodes = opcodes.Add(new ILEmitEndExceptionBlock());
        }

        public void Leave(bool useLongForm, Symbol endOfBlockLabel)
        {
            opcodes = opcodes.Add(new ILEmitBranch(useLongForm ? ILBranch.Leave : ILBranch.Leave_S, endOfBlockLabel));
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
            return Option<T>.None();
        }
    }
}
