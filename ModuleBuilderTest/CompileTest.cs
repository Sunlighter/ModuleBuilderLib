using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sunlighter.ModuleBuilderLib;
using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;

namespace ModuleBuilderTest
{
    public interface IInt32Operation
    {
        int Operate(int i, int j);
    }

    [TestClass]
    public class CompileTest
    {
        public static ModuleToBuild GetTestModule()
        {
            return new ModuleToBuild
            (
                ImmutableList<ElementOfModule>.Empty.Add
                (
                    new ClassToBuild
                    (
                        "MyClass",
                        TypeAttributes.Public | TypeAttributes.Sealed,
                        ExistingTypeReference.Object,
                        ImmutableList<TypeReference>.Empty.Add(new ExistingTypeReference(typeof(IInt32Operation))),
                        ImmutableList<ElementOfClass>.Empty.Add
                        (
                            new FieldToBuild(FieldAttributes.Private, ExistingTypeReference.Int32, "iScale")
                        )
                        .Add
                        (
                            new FieldToBuild(FieldAttributes.Private, ExistingTypeReference.Int32, "jScale")
                        )
                        .Add
                        (
                            new ILConstructorToBuild
                            (
                                MethodAttributes.Public,
                                "this",
                                ImmutableList<ParamInfo2>.Empty
                                .Add(new ParamInfo2("iScale", ExistingTypeReference.Int32))
                                .Add(new ParamInfo2("jScale", ExistingTypeReference.Int32)),
                                "MyClass constructor",
                                cg2 =>
                                {
                                    cg2.LoadArg("this");
                                    cg2.LoadArg("iScale");
                                    cg2.StoreField(new FieldKeyReference(new FieldKey(new TypeKey("MyClass"), "iScale", ExistingTypeReference.Int32)));
                                    cg2.LoadArg("this");
                                    cg2.LoadArg("jScale");
                                    cg2.StoreField(new FieldKeyReference(new FieldKey(new TypeKey("MyClass"), "jScale", ExistingTypeReference.Int32)));
                                    cg2.Return();
                                }
                            )
                        )
                        .Add
                        (
                            new ILMethodToBuild
                            (
                                "Operate",
                                MethodAttributes.Public | MethodAttributes.Virtual,
                                ExistingTypeReference.Int32,
                                new Some<Symbol>("this"),
                                ImmutableList<ParamInfo2>.Empty
                                .Add(new ParamInfo2("i", ExistingTypeReference.Int32))
                                .Add(new ParamInfo2("j", ExistingTypeReference.Int32)),
                                "MyClass.Operate",
                                cg2 =>
                                {
                                    cg2.LoadArg("i");
                                    cg2.LoadArg("this");
                                    cg2.LoadField(new FieldKeyReference(new FieldKey(new TypeKey("MyClass"), "iScale", ExistingTypeReference.Int32)));
                                    cg2.Mul();
                                    cg2.LoadArg("j");
                                    cg2.LoadArg("this");
                                    cg2.LoadField(new FieldKeyReference(new FieldKey(new TypeKey("MyClass"), "jScale", ExistingTypeReference.Int32)));
                                    cg2.Mul();
                                    cg2.Add();
                                    cg2.Return();
                                }
                            )
                        )
                    )
                )
            );
        }

        [TestMethod]
        public void TestCompile()
        {
            string name = $"S_{Guid.NewGuid():N}";
            AssemblyName aName = new AssemblyName(name);
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder mb = ab.DefineDynamicModule(name);

            ImmutableDictionary<ItemKey, Type> results = mb.Compile(GetTestModule());
            Type myClass = results[new CompletedTypeKey("MyClass")];
            IInt32Operation myInstance = (IInt32Operation)myClass.GetConstructor(new Type[] { typeof(int), typeof(int) }).Invoke(new object[] { 100, 10 });
            
            int value = myInstance.Operate(3, 8);
            Assert.AreEqual(380, value);
        }
    }
}
