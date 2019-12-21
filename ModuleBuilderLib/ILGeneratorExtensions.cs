using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Sunlighter.ModuleBuilderLib
{
    public enum Alignment
    {
        One,
        Two,
        Four
    }

    public static partial class Extensions
    {
        #region ILGenerator Extension Methods

        public static void Add(this ILGenerator ilg) { ilg.Emit(OpCodes.Add); }
        public static void AddOvf(this ILGenerator ilg) { ilg.Emit(OpCodes.Add_Ovf); }
        public static void AddOvfUn(this ILGenerator ilg) { ilg.Emit(OpCodes.Add_Ovf_Un); }

        public static void Sub(this ILGenerator ilg) { ilg.Emit(OpCodes.Sub); }
        public static void SubOvf(this ILGenerator ilg) { ilg.Emit(OpCodes.Sub_Ovf); }
        public static void SubOvfUn(this ILGenerator ilg) { ilg.Emit(OpCodes.Sub_Ovf_Un); }

        public static void Mul(this ILGenerator ilg) { ilg.Emit(OpCodes.Mul); }
        public static void MulOvf(this ILGenerator ilg) { ilg.Emit(OpCodes.Mul_Ovf); }
        public static void MulOvfUn(this ILGenerator ilg) { ilg.Emit(OpCodes.Mul_Ovf_Un); }

        public static void Div(this ILGenerator ilg) { ilg.Emit(OpCodes.Div); }
        public static void DivUn(this ILGenerator ilg) { ilg.Emit(OpCodes.Div_Un); }

        public static void Rem(this ILGenerator ilg) { ilg.Emit(OpCodes.Rem); }
        public static void RemUn(this ILGenerator ilg) { ilg.Emit(OpCodes.Rem_Un); }

        public static void And(this ILGenerator ilg) { ilg.Emit(OpCodes.And); }
        public static void Or(this ILGenerator ilg) { ilg.Emit(OpCodes.Or); }
        public static void Xor(this ILGenerator ilg) { ilg.Emit(OpCodes.Xor); }
        public static void Invert(this ILGenerator ilg) { ilg.Emit(OpCodes.Not); }
        public static void Negate(this ILGenerator ilg) { ilg.Emit(OpCodes.Neg); }

        public static void Shl(this ILGenerator ilg) { ilg.Emit(OpCodes.Shl); }
        public static void Shr(this ILGenerator ilg) { ilg.Emit(OpCodes.Shr); }
        public static void ShrUn(this ILGenerator ilg) { ilg.Emit(OpCodes.Shr_Un); }

        public static void Dup(this ILGenerator ilg)
        {
            ilg.Emit(OpCodes.Dup);
        }

        public static void Pop(this ILGenerator ilg)
        {
            ilg.Emit(OpCodes.Pop);
        }

        public static void Not(this ILGenerator ilg)
        {
            Label l1 = ilg.DefineLabel();
            Label l2 = ilg.DefineLabel();
            ilg.Emit(OpCodes.Brfalse_S, l1);
            ilg.LoadInt(0);
            ilg.Emit(OpCodes.Br_S, l2);
            ilg.MarkLabel(l1);
            ilg.LoadInt(1);
            ilg.MarkLabel(l2);
        }

        public static void LoadLocal(this ILGenerator ilg, int index)
        {
            if (index == 0)
            {
                ilg.Emit(OpCodes.Ldloc_0);
            }
            else if (index == 1)
            {
                ilg.Emit(OpCodes.Ldloc_1);
            }
            else if (index == 2)
            {
                ilg.Emit(OpCodes.Ldloc_2);
            }
            else if (index == 3)
            {
                ilg.Emit(OpCodes.Ldloc_3);
            }
            else if (index < 256)
            {
                ilg.Emit(OpCodes.Ldloc_S, (byte)index);
            }
            else
            {
                ilg.Emit(OpCodes.Ldloc, index);
            }
        }

        public static void LoadLocal(this ILGenerator ilg, LocalBuilder lb)
        {
            ilg.LoadLocal(lb.LocalIndex);
        }

        public static void LoadLocalAddress(this ILGenerator ilg, int index)
        {
            if (index < 256)
            {
                ilg.Emit(OpCodes.Ldloca_S, (byte)index);
            }
            else
            {
                ilg.Emit(OpCodes.Ldloca, index);
            }
        }

        public static void LoadLocalAddress(this ILGenerator ilg, LocalBuilder lb)
        {
            ilg.LoadLocalAddress(lb.LocalIndex);
        }

        public static void StoreLocal(this ILGenerator ilg, int index)
        {
            if (index == 0)
            {
                ilg.Emit(OpCodes.Stloc_0);
            }
            else if (index == 1)
            {
                ilg.Emit(OpCodes.Stloc_1);
            }
            else if (index == 2)
            {
                ilg.Emit(OpCodes.Stloc_2);
            }
            else if (index == 3)
            {
                ilg.Emit(OpCodes.Stloc_3);
            }
            else if (index < 256)
            {
                ilg.Emit(OpCodes.Stloc_S, (byte)index);
            }
            else
            {
                ilg.Emit(OpCodes.Stloc, index);
            }
        }

        public static void StoreLocal(this ILGenerator ilg, LocalBuilder lb)
        {
            ilg.StoreLocal(lb.LocalIndex);
        }

        public static void LoadArg(this ILGenerator ilg, int index)
        {
            if (index == 0)
            {
                ilg.Emit(OpCodes.Ldarg_0);
            }
            else if (index == 1)
            {
                ilg.Emit(OpCodes.Ldarg_1);
            }
            else if (index == 2)
            {
                ilg.Emit(OpCodes.Ldarg_2);
            }
            else if (index == 3)
            {
                ilg.Emit(OpCodes.Ldarg_3);
            }
            else if (index < 256)
            {
                ilg.Emit(OpCodes.Ldarg_S, (byte)index);
            }
            else
            {
                ilg.Emit(OpCodes.Ldarg, index);
            }
        }

        public static void LoadArgAddress(this ILGenerator ilg, int index)
        {
            if (index < 256)
            {
                ilg.Emit(OpCodes.Ldarga_S, (byte)index);
            }
            else
            {
                ilg.Emit(OpCodes.Ldarga, index);
            }
        }

        public static void StoreArg(this ILGenerator ilg, int index)
        {
            if (index < 256)
            {
                ilg.Emit(OpCodes.Starg_S, (byte)index);
            }
            else
            {
                ilg.Emit(OpCodes.Starg, index);
            }
        }

        public static void LoadInt(this ILGenerator ilg, int literal)
        {
            if (literal == 0)
            {
                ilg.Emit(OpCodes.Ldc_I4_0);
            }
            else if (literal == 1)
            {
                ilg.Emit(OpCodes.Ldc_I4_1);
            }
            else if (literal == 2)
            {
                ilg.Emit(OpCodes.Ldc_I4_2);
            }
            else if (literal == 3)
            {
                ilg.Emit(OpCodes.Ldc_I4_3);
            }
            else if (literal == 4)
            {
                ilg.Emit(OpCodes.Ldc_I4_4);
            }
            else if (literal == 5)
            {
                ilg.Emit(OpCodes.Ldc_I4_5);
            }
            else if (literal == 6)
            {
                ilg.Emit(OpCodes.Ldc_I4_6);
            }
            else if (literal == 7)
            {
                ilg.Emit(OpCodes.Ldc_I4_7);
            }
            else if (literal == 8)
            {
                ilg.Emit(OpCodes.Ldc_I4_8);
            }
            else if (literal == -1)
            {
                ilg.Emit(OpCodes.Ldc_I4_M1);
            }
            else if (literal >= -128 && literal <= 127)
            {
                ilg.Emit(OpCodes.Ldc_I4_S, unchecked((byte)literal));
            }
            else
            {
                ilg.Emit(OpCodes.Ldc_I4, literal);
            }
        }

        public static void LoadLong(this ILGenerator ilg, long literal)
        {
            ilg.Emit(OpCodes.Ldc_I8, literal);
        }

        public static void LoadFloat(this ILGenerator ilg, float literal)
        {
            ilg.Emit(OpCodes.Ldc_R4, literal);
        }

        public static void LoadDouble(this ILGenerator ilg, double literal)
        {
            ilg.Emit(OpCodes.Ldc_R8, literal);
        }

        public static void LoadString(this ILGenerator ilg, string literal)
        {
            ilg.Emit(OpCodes.Ldstr, literal);
        }

        public static void LoadNullPtr(this ILGenerator ilg)
        {
            ilg.Emit(OpCodes.Ldnull);
        }

        public static void LoadField(this ILGenerator ilg, FieldInfo fi) // ( objref -- value )
        {
            ilg.Emit(OpCodes.Ldfld, fi);
        }

        public static void LoadFieldAddress(this ILGenerator ilg, FieldInfo fi) // ( objref -- ptr )
        {
            ilg.Emit(OpCodes.Ldflda, fi);
        }

        public static void StoreField(this ILGenerator ilg, FieldInfo fi) // ( objref value -- )
        {
            ilg.Emit(OpCodes.Stfld, fi);
        }

        public static void LoadStaticField(this ILGenerator ilg, FieldInfo fi) // ( -- value )
        {
            ilg.Emit(OpCodes.Ldsfld, fi);
        }

        public static void LoadStaticFieldAddress(this ILGenerator ilg, FieldInfo fi) // ( -- ptr )
        {
            ilg.Emit(OpCodes.Ldsflda, fi);
        }

        public static void StoreStaticField(this ILGenerator ilg, FieldInfo fi) // ( value -- )
        {
            ilg.Emit(OpCodes.Stsfld, fi);
        }

        public static void LoadFunction(this ILGenerator ilg, MethodInfo mi)
        {
            ilg.Emit(OpCodes.Ldftn, mi);
        }

        public static void LoadToken(this ILGenerator ilg, Type t) // ( -- token )
        {
            ilg.Emit(OpCodes.Ldtoken, t);
        }

        public static void LoadToken(this ILGenerator ilg, MethodInfo mi) // ( -- token )
        {
            ilg.Emit(OpCodes.Ldtoken, mi);
        }

        public static void LoadToken(this ILGenerator ilg, ConstructorInfo ci) // ( -- token )
        {
            ilg.Emit(OpCodes.Ldtoken, ci);
        }

        public static void LoadToken(this ILGenerator ilg, FieldInfo fi) // ( -- token )
        {
            ilg.Emit(OpCodes.Ldtoken, fi);
        }

        public static void NewObj(this ILGenerator ilg, ConstructorInfo ci) { ilg.Emit(OpCodes.Newobj, ci); }

        public static void Throw(this ILGenerator ilg) { ilg.Emit(OpCodes.Throw); }

        public static void Tail(this ILGenerator ilg) { ilg.Emit(OpCodes.Tailcall); }

        public static void Call(this ILGenerator ilg, MethodInfo mi) { ilg.Emit(OpCodes.Call, mi); }

        public static void Call(this ILGenerator ilg, ConstructorInfo ci) { ilg.Emit(OpCodes.Call, ci); }

        public static void CallVirt(this ILGenerator ilg, MethodInfo mi) { ilg.Emit(OpCodes.Callvirt, mi); }

        public static void Return(this ILGenerator ilg) { ilg.Emit(OpCodes.Ret); }

        public static void IsInst(this ILGenerator ilg, Type t) { ilg.Emit(OpCodes.Isinst, t); }

        public static void CastClass(this ILGenerator ilg, Type t) { ilg.Emit(OpCodes.Castclass, t); }

        public static void Ceq(this ILGenerator ilg) { ilg.Emit(OpCodes.Ceq); }
        public static void Clt(this ILGenerator ilg) { ilg.Emit(OpCodes.Clt); }
        public static void CltUn(this ILGenerator ilg) { ilg.Emit(OpCodes.Clt_Un); }
        public static void Cgt(this ILGenerator ilg) { ilg.Emit(OpCodes.Cgt); }
        public static void CgtUn(this ILGenerator ilg) { ilg.Emit(OpCodes.Cgt_Un); }

        public static void Conv_I(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_I); }
        public static void Conv_I1(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_I1); }
        public static void Conv_I2(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_I2); }
        public static void Conv_I4(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_I4); }
        public static void Conv_I8(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_I8); }

        public static void Conv_Ovf_I(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_I); }
        public static void Conv_Ovf_I1(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_I1); }
        public static void Conv_Ovf_I2(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_I2); }
        public static void Conv_Ovf_I4(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_I4); }
        public static void Conv_Ovf_I8(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_I8); }

        public static void Conv_Ovf_I_Un(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_I_Un); }
        public static void Conv_Ovf_I1_Un(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_I1_Un); }
        public static void Conv_Ovf_I2_Un(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_I2_Un); }
        public static void Conv_Ovf_I4_Un(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_I4_Un); }
        public static void Conv_Ovf_I8_Un(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_I8_Un); }

        public static void Conv_Ovf_U(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_U); }
        public static void Conv_Ovf_U1(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_U1); }
        public static void Conv_Ovf_U2(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_U2); }
        public static void Conv_Ovf_U4(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_U4); }
        public static void Conv_Ovf_U8(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_U8); }

        public static void Conv_Ovf_U_Un(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_U_Un); }
        public static void Conv_Ovf_U1_Un(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_U1_Un); }
        public static void Conv_Ovf_U2_Un(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_U2_Un); }
        public static void Conv_Ovf_U4_Un(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_U4_Un); }
        public static void Conv_Ovf_U8_Un(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_Ovf_U8_Un); }

        public static void Conv_R_Un(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_R_Un); }
        public static void Conv_R4(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_R4); }
        public static void Conv_R8(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_R8); }

        public static void Conv_U(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_U); }
        public static void Conv_U1(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_U1); }
        public static void Conv_U2(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_U2); }
        public static void Conv_U4(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_U4); }
        public static void Conv_U8(this ILGenerator ilg) { ilg.Emit(OpCodes.Conv_U8); }

        public static void SizeOf(this ILGenerator ilg, Type t) { ilg.Emit(OpCodes.Sizeof, t); }

        public static void LoadObjRef(this ILGenerator ilg) { ilg.Emit(OpCodes.Ldind_Ref); }
        public static void StoreObjRef(this ILGenerator ilg) { ilg.Emit(OpCodes.Stind_Ref); }

        public static void Unaligned(this ILGenerator ilg, Alignment a)
        {
            byte b;
            switch (a)
            {
                case Alignment.One: b = 1; break;
                case Alignment.Two: b = 2; break;
                case Alignment.Four: b = 4; break;
                default: throw new ArgumentException("Unknown alignment");
            }
            ilg.Emit(OpCodes.Unaligned, b);
        }

        public static void LoadObjIndirect(this ILGenerator ilg, Type t)
        {
            if (t == typeof(byte))
            {
                ilg.Emit(OpCodes.Ldind_U1);
            }
            else if (t == typeof(sbyte))
            {
                ilg.Emit(OpCodes.Ldind_I1);
            }
            else if (t == typeof(ushort))
            {
                ilg.Emit(OpCodes.Ldind_U2);
            }
            else if (t == typeof(short))
            {
                ilg.Emit(OpCodes.Ldind_I2);
            }
            else if (t == typeof(uint))
            {
                ilg.Emit(OpCodes.Ldind_U4);
            }
            else if (t == typeof(int))
            {
                ilg.Emit(OpCodes.Ldind_I4);
            }
            else if (t == typeof(long) || t == typeof(ulong))
            {
                ilg.Emit(OpCodes.Ldind_I8);
            }
            else if (t == typeof(IntPtr) || t == typeof(UIntPtr))
            {
                ilg.Emit(OpCodes.Ldind_I);
            }
            else if (t == typeof(float))
            {
                ilg.Emit(OpCodes.Ldind_R4);
            }
            else if (t == typeof(double))
            {
                ilg.Emit(OpCodes.Ldind_R8);
            }
            else if (t.IsValueType)
            {
                ilg.Emit(OpCodes.Ldobj, t);
            }
            else
            {
                ilg.Emit(OpCodes.Ldind_Ref);
            }
        }

        public static void StoreObjIndirect(this ILGenerator ilg, Type t)
        {
            if (t == typeof(sbyte) || t == typeof(byte))
            {
                ilg.Emit(OpCodes.Stind_I1);
            }
            else if (t == typeof(short) || t == typeof(ushort))
            {
                ilg.Emit(OpCodes.Stind_I2);
            }
            else if (t == typeof(int) || t == typeof(uint))
            {
                ilg.Emit(OpCodes.Stind_I4);
            }
            else if (t == typeof(long) || t == typeof(ulong))
            {
                ilg.Emit(OpCodes.Stind_I8);
            }
            else if (t == typeof(System.IntPtr) || t == typeof(System.UIntPtr))
            {
                ilg.Emit(OpCodes.Stind_I);
            }
            else if (t == typeof(float))
            {
                ilg.Emit(OpCodes.Stind_R4);
            }
            else if (t == typeof(double))
            {
                ilg.Emit(OpCodes.Stind_R8);
            }
            else if (t.IsValueType)
            {
                ilg.Emit(OpCodes.Stobj, t);
            }
            else
            {
                ilg.Emit(OpCodes.Stind_Ref);
            }
        }

        public static void LoadElement(this ILGenerator ilg, Type t)
        {
            if (t == typeof(sbyte))
            {
                ilg.Emit(OpCodes.Ldelem_I1);
            }
            else if (t == typeof(byte))
            {
                ilg.Emit(OpCodes.Ldelem_U1);
            }
            else if (t == typeof(short))
            {
                ilg.Emit(OpCodes.Ldelem_I2);
            }
            else if (t == typeof(ushort))
            {
                ilg.Emit(OpCodes.Ldelem_U2);
            }
            else if (t == typeof(int))
            {
                ilg.Emit(OpCodes.Ldelem_I4);
            }
            else if (t == typeof(uint))
            {
                ilg.Emit(OpCodes.Ldelem_U4);
            }
            else if (t == typeof(long) || t == typeof(ulong))
            {
                ilg.Emit(OpCodes.Ldelem_I8);
            }
            else if (t == typeof(IntPtr) || t == typeof(UIntPtr))
            {
                ilg.Emit(OpCodes.Ldelem_I);
            }
            else if (t == typeof(float))
            {
                ilg.Emit(OpCodes.Ldelem_R4);
            }
            else if (t == typeof(double))
            {
                ilg.Emit(OpCodes.Ldelem_R8);
            }
            else if (!(t.IsValueType))
            {
                ilg.Emit(OpCodes.Ldelem_Ref);
            }
            else
            {
                ilg.Emit(OpCodes.Ldelem, t);
            }
        }

        public static void LoadElementAddress(this ILGenerator ilg, Type t)
        {
            ilg.Emit(OpCodes.Ldelema, t);
        }

        public static void StoreElement(this ILGenerator ilg, Type t)
        {
            if (t == typeof(sbyte) || t == typeof(byte))
            {
                ilg.Emit(OpCodes.Stelem_I1);
            }
            else if (t == typeof(short) || t == typeof(ushort))
            {
                ilg.Emit(OpCodes.Stelem_I2);
            }
            else if (t == typeof(int) || t == typeof(uint))
            {
                ilg.Emit(OpCodes.Stelem_I4);
            }
            else if (t == typeof(long) || t == typeof(ulong))
            {
                ilg.Emit(OpCodes.Stelem_I8);
            }
            else if (t == typeof(IntPtr) || t == typeof(UIntPtr))
            {
                ilg.Emit(OpCodes.Stelem_I);
            }
            else if (t == typeof(float))
            {
                ilg.Emit(OpCodes.Stelem_R4);
            }
            else if (t == typeof(double))
            {
                ilg.Emit(OpCodes.Stelem_R8);
            }
            else if (!(t.IsValueType))
            {
                ilg.Emit(OpCodes.Stelem_Ref);
            }
            else
            {
                ilg.Emit(OpCodes.Stelem, t);
            }
        }

        public static void Box(this ILGenerator ilg, Type t) { ilg.Emit(OpCodes.Box, t); }

        public static void Unbox(this ILGenerator ilg, Type t) { ilg.Emit(OpCodes.Unbox, t); }
        public static void UnboxAny(this ILGenerator ilg, Type t) { ilg.Emit(OpCodes.Unbox_Any, t); }

        public static void Leave(this ILGenerator ilg, Label l) { ilg.Emit(OpCodes.Leave, l); }

        #endregion
    }
}
