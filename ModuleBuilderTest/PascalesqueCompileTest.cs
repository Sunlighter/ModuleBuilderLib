using Sunlighter.ModuleBuilderLib;
using Sunlighter.ModuleBuilderLib.Pascalesque;
using Sunlighter.TypeTraitsLib;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ModuleBuilderTest
{
    [TestClass]
    public class PascalesqueCompileTest
    {
        public static ModuleToBuild GetTestModule()
        {
            return new ModuleToBuild
            (
                ImmutableList<ElementOfModule>.Empty.Add
                (
                    new ClassToBuild
                    (
                        "MessagePrinter",
                        TypeAttributes.Public,
                        ExistingTypeReference.Object,
                        ImmutableList<TypeReference>.Empty,
                        ImmutableList<ElementOfClass>.Empty.Add
                        (
                            new FieldToBuild
                            (
                                FieldAttributes.Private | FieldAttributes.InitOnly,
                                ExistingTypeReference.String,
                                "message"
                            )
                        )
                        .Add
                        (
                            new ConstructorToBuild
                            (
                                MethodAttributes.Public,
                                new LambdaExpr2
                                (
                                    ImmutableList<ParamInfo>.Empty.Add
                                    (
                                        new ParamInfo
                                        (
                                            "this",
                                            new TypeKeyReference(new TypeKey("MessagePrinter"))
                                        )
                                    )
                                    .Add
                                    (
                                        new ParamInfo
                                        (
                                            "message",
                                            ExistingTypeReference.String
                                        )
                                    ),
                                    new BeginExpr2
                                    (
                                        ImmutableList<Expression2>.Empty.Add
                                        (
                                            new ConstructorCallExpr2
                                            (
                                                new ExistingConstructorReference
                                                (
                                                    typeof(object).GetConstructor(Type.EmptyTypes).AssertNotNull()
                                                ),
                                                new VarRefExpr2
                                                (
                                                    "this"
                                                ),
                                                ImmutableList<Expression2>.Empty
                                            )
                                        )
                                        .Add
                                        (
                                            new FieldSetExpr2
                                            (
                                                new VarRefExpr2("this"),
                                                new FieldKeyReference(new FieldKey(new TypeKey("MessagePrinter"), "message", ExistingTypeReference.String)),
                                                new VarRefExpr2("message")
                                            )
                                        )
                                    )
                                )
                            )
                        )
                        .Add
                        (
                            new MethodToBuild
                            (
                                "Print",
                                MethodAttributes.Public,
                                ExistingTypeReference.Void,
                                new LambdaExpr2
                                (
                                    ImmutableList<ParamInfo>.Empty.Add
                                    (
                                        new ParamInfo
                                        (
                                            "this",
                                            new TypeKeyReference(new TypeKey("MessagePrinter"))
                                        )
                                    ),
                                    new MethodCallExpr2
                                    (
                                        new ExistingMethodReference(typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }).AssertNotNull()),
                                        false,
                                        ImmutableList<Expression2>.Empty.Add
                                        (
                                            new FieldRefExpr2
                                            (
                                                new VarRefExpr2("this"),
                                                new FieldKeyReference(new FieldKey(new TypeKey("MessagePrinter"), "message", ExistingTypeReference.String))
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
                .Add
                (
                    new ClassToBuild
                    (
                        "Program",
                        TypeAttributes.Public,
                        ExistingTypeReference.Object,
                        ImmutableList<TypeReference>.Empty,
                        ImmutableList<ElementOfClass>.Empty.Add
                        (
                            new MethodToBuild
                            (
                                "Run",
                                MethodAttributes.Static | MethodAttributes.Public,
                                ExistingTypeReference.Void,
                                new LambdaExpr2
                                (
                                    ImmutableList<ParamInfo>.Empty,
                                    new LetExpr2
                                    (
                                        ImmutableList<LetClause2>.Empty.Add
                                        (
                                            new LetClause2
                                            (
                                                "a",
                                                new TypeKeyReference(new TypeKey("MessagePrinter")),
                                                new NewObjExpr2
                                                (
                                                    new ConstructorKeyReference
                                                    (
                                                        new ConstructorKey
                                                        (
                                                            new TypeKey("MessagePrinter"),
                                                            ImmutableList<TypeReference>.Empty.Add(ExistingTypeReference.String)
                                                        )
                                                    ),
                                                    ImmutableList<Expression2>.Empty.Add(new LiteralExpr2("Hello, world!"))
                                                )
                                            )
                                        )
                                        .Add
                                        (
                                            new LetClause2
                                            (
                                                "b",
                                                new TypeKeyReference(new TypeKey("MessagePrinter")),
                                                new NewObjExpr2
                                                (
                                                    new ConstructorKeyReference
                                                    (
                                                        new ConstructorKey
                                                        (
                                                            new TypeKey("MessagePrinter"),
                                                            ImmutableList<TypeReference>.Empty.Add(ExistingTypeReference.String)
                                                        )
                                                    ),
                                                    ImmutableList<Expression2>.Empty.Add(new LiteralExpr2("This is a test!"))
                                                )
                                            )
                                        ),
                                        new BeginExpr2
                                        (
                                            ImmutableList<Expression2>.Empty.Add
                                            (
                                                new MethodCallExpr2
                                                (
                                                    new MethodKeyReference
                                                    (
                                                        new MethodKey
                                                        (
                                                            new TypeKey("MessagePrinter"),
                                                            "Print",
                                                            true,
                                                            ImmutableList<TypeReference>.Empty
                                                        )
                                                    ),
                                                    true,
                                                    ImmutableList<Expression2>.Empty.Add(new VarRefExpr2("a"))
                                                )
                                            )
                                            .Add
                                            (
                                                new MethodCallExpr2
                                                (
                                                    new MethodKeyReference
                                                    (
                                                        new MethodKey
                                                        (
                                                            new TypeKey("MessagePrinter"),
                                                            "Print",
                                                            true,
                                                            ImmutableList<TypeReference>.Empty
                                                        )
                                                    ),
                                                    true,
                                                    ImmutableList<Expression2>.Empty.Add(new VarRefExpr2("b"))
                                                )
                                            )
                                        )
                                    )
                                )
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

            ImmutableSortedDictionary<ItemKey, Type> results = mb.Compile(GetTestModule());
            Type programClass = results[new CompletedTypeKey("Program")];
            MethodInfo runMethod = programClass.GetMethod("Run", Type.EmptyTypes).AssertNotNull();
            Assert.IsNotNull(runMethod, "Run method not found");
            runMethod.Invoke(null, null);
        }
    }
}
