using Sunlighter.ModuleBuilderLib;
using Sunlighter.OptionLib;
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
                [
                    new ClassToBuild
                    (
                        "MyClass",
                        TypeAttributes.Public | TypeAttributes.Sealed,
                        ExistingTypeReference.Object,
                        [
                            new ExistingTypeReference(typeof(IInt32Operation))
                        ],
                        [
                            new FieldToBuild(FieldAttributes.Private, ExistingTypeReference.Int32, "iScale"),
                            new FieldToBuild(FieldAttributes.Private, ExistingTypeReference.Int32, "jScale"),
                            new ILConstructorToBuild
                            (
                                MethodAttributes.Public,
                                "this",
                                [
                                    new ParamInfo("iScale", ExistingTypeReference.Int32),
                                    new ParamInfo("jScale", ExistingTypeReference.Int32)
                                ],
                                CodeGenerator.Empty
                                    .LoadArg("this")
                                    .LoadArg("iScale")
                                    .StoreField(new FieldKeyReference(new FieldKey(new TypeKey("MyClass"), "iScale", ExistingTypeReference.Int32)))
                                    .LoadArg("this")
                                    .LoadArg("jScale")
                                    .StoreField(new FieldKeyReference(new FieldKey(new TypeKey("MyClass"), "jScale", ExistingTypeReference.Int32)))
                                    .Return()
                                    .Results
                            ),
                            new ILMethodToBuild
                            (
                                "Operate",
                                MethodAttributes.Public | MethodAttributes.Virtual,
                                ExistingTypeReference.Int32,
                                Option<Symbol>.Some("this"),
                                [
                                    new ParamInfo("i", ExistingTypeReference.Int32),
                                    new ParamInfo("j", ExistingTypeReference.Int32)
                                ],
                                CodeGenerator.Empty
                                    .LoadArg("i")
                                    .LoadArg("this")
                                    .LoadField(new FieldKeyReference(new FieldKey(new TypeKey("MyClass"), "iScale", ExistingTypeReference.Int32)))
                                    .Mul()
                                    .LoadArg("j")
                                    .LoadArg("this")
                                    .LoadField(new FieldKeyReference(new FieldKey(new TypeKey("MyClass"), "jScale", ExistingTypeReference.Int32)))
                                    .Mul()
                                    .Add()
                                    .Return()
                                    .Results
                            ),
                        ]
                    )
                ]
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
            ConstructorInfo? ci = myClass.GetConstructor(new Type[] { typeof(int), typeof(int) });
            Assert.IsNotNull(ci, "Constructor not found");
            IInt32Operation myInstance = (IInt32Operation)ci.Invoke(new object[] { 100, 10 });
            
            int value = myInstance.Operate(3, 8);
            Assert.AreEqual(380, value);
        }
    }
}
