using Sunlighter.TypeTraitsLib.Building;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Sunlighter.ModuleBuilderLib.Pascalesque
{
    [Record]
    public sealed class ConstructorToBuild : ElementOfClass
    {
        private readonly MethodAttributes attributes;
        private readonly LambdaExpr2 body;

        public ConstructorToBuild(MethodAttributes attributes, LambdaExpr2 body)
        {
            this.attributes = attributes;
            this.body = body;
        }

        [Bind("attributes")]
        public MethodAttributes Attributes => attributes;

        [Bind("body")]
        public LambdaExpr2 Body => body;

        public override SymbolTable DefineSymbols(SymbolTable s, TypeKey owner)
        {
            ConstructorKey ck = new ConstructorKey(owner, body.Parameters.Skip(1).Select(x => x.ParamType).ToImmutableList());
            ConstructorAux a = new ConstructorAux(attributes);
            return s.SetItem(ck, a);
        }

        private sealed class MakeConstructor : ICompileStep
        {
            private readonly ConstructorToBuild parent;
            private readonly TypeKey owner;
            private readonly ConstructorKey constructorKey;

            public MakeConstructor(ConstructorToBuild parent, TypeKey owner, ConstructorKey constructorKey)
            {
                this.parent = parent;
                this.owner = owner;
                this.constructorKey = constructorKey;
            }

            public int Phase { get { return 1; } }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty
                        .Add(owner)
                        .Union(parent.body.Parameters.Select(x => x.ParamType.GetReferences()).UnionAll());
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

                ConstructorBuilder cb = oType.DefineConstructor(parent.attributes, CallingConventions.Standard, parent.body.Parameters.Skip(1).Select(x => (Type)(x.ParamType.Resolve(vars))).ToArray());

                vars[constructorKey].Value = cb;
            }
        }

        private sealed class MakeConstructorBody : ICompileStep
        {
            private readonly ConstructorToBuild parent;
            private readonly SymbolTable symbolTable;
            private readonly TypeKey owner;
            private readonly ConstructorKey constructorKey;
            private readonly EnvDesc2 envDesc;

            public MakeConstructorBody(ConstructorToBuild parent, SymbolTable symbolTable, TypeKey owner, ConstructorKey constructorKey, EnvDesc2 envDesc)
            {
                this.parent = parent;
                this.symbolTable = symbolTable;
                this.owner = owner;
                this.constructorKey = constructorKey;
                this.envDesc = envDesc;
            }

            public int Phase { get { return 1; } }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty
                        .Add(owner)
                        .Union(parent.body.Parameters.Select(x => x.ParamType.GetReferences()).UnionAll())
                        .Add(constructorKey)
                        .Union(parent.body.Body.GetReferences(symbolTable, owner, envDesc.TypesOnly()));
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
                ConstructorBuilder cb = (ConstructorBuilder)(vars[constructorKey].Value);
                ILGenerator ilg = cb.GetILGenerator();
                CompileContext2 cc = new CompileContext2(ilg, true);
                parent.body.Body.Compile(symbolTable, owner, cc, envDesc, vars, true);
            }
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner)
        {
            EnvSpec es = body.Body.GetEnvSpec();

            List<ParamInfo> paramInfos = body.Parameters.ToList();
            List<Tuple<Symbol, IVarDesc2>> vars = new List<Tuple<Symbol, IVarDesc2>>();

            if (paramInfos.Count == 0) throw new PascalesqueException("Constructor must at least take a \"this\" parameter");

            if (paramInfos[0].ParamType != new TypeKeyReference(owner)) throw new PascalesqueException("A constructor's \"this\" parameter is not of the correct type");

            int iEnd = paramInfos.Count;
            for (int i = 0; i < iEnd; ++i)
            {
                ParamInfo x = paramInfos[i];
                vars.Add(new Tuple<Symbol, IVarDesc2>(x.Name, new ArgVarDesc2(x.ParamType, es[x.Name].IsCaptured, i)));
            }

            EnvDesc2 e = EnvDesc2.FromSequence(vars);

            ConstructorKey ck = new ConstructorKey(owner, paramInfos.Skip(1).Select(x => x.ParamType).ToImmutableList());

            return body.Body.GetCompileSteps(s, owner, e.TypesOnly())
                .Add(new MakeConstructor(this, owner, ck))
                .Add(new MakeConstructorBody(this, s, owner, ck, e));
        }
    }

    [Record]
    public sealed class MethodToBuild : ElementOfClass
    {
        private readonly Symbol name;
        private readonly MethodAttributes attributes;
        private readonly TypeReference returnType;
        private readonly LambdaExpr2 body;

        public MethodToBuild(Symbol name, MethodAttributes attributes, TypeReference returnType, LambdaExpr2 body)
        {
            this.name = name;
            this.attributes = attributes;
            this.returnType = returnType;
            this.body = body;
        }

        [Bind("name")]
        public Symbol Name => name;

        [Bind("attributes")]
        public MethodAttributes Attributes => attributes;

        [Bind("returnType")]
        public TypeReference ReturnType => returnType;

        [Bind("body")]
        public LambdaExpr2 Body => body;

        public override SymbolTable DefineSymbols(SymbolTable s, TypeKey owner)
        {
            bool isInstance = !attributes.HasFlag(MethodAttributes.Static);
            MethodKey mk = new MethodKey(owner, name, isInstance, body.Parameters.Select(x => x.ParamType).Skip(isInstance ? 1 : 0).ToImmutableList());
            MethodAux ma = new MethodAux(attributes, returnType);
            return s.SetItem(mk, ma);
        }

        private sealed class MakeMethod : ICompileStep
        {
            private readonly MethodToBuild parent;
            private readonly SymbolTable symbolTable;
            private readonly TypeKey owner;
            private readonly MethodKey methodKey;

            public MakeMethod(MethodToBuild parent, SymbolTable symbolTable, TypeKey owner, MethodKey methodKey)
            {
                this.parent = parent;
                this.symbolTable = symbolTable;
                this.owner = owner;
                this.methodKey = methodKey;
            }

            public int Phase { get { return 1; } }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty
                        .Add(owner)
                        .Union(parent.body.Parameters.Select(x => x.ParamType.GetReferences()).UnionAll())
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

                MethodBuilder meb = oType.DefineMethod(parent.name.SymbolName(), parent.attributes, symbolTable[methodKey].ReturnType.Resolve(vars), methodKey.Parameters.Select(x => x.Resolve(vars)).ToArray());

                vars[methodKey].Value = meb;
            }
        }

        private sealed class MakeMethodBody : ICompileStep
        {
            private readonly MethodToBuild parent;
            private readonly SymbolTable symbolTable;
            private readonly TypeKey owner;
            private readonly MethodKey methodKey;
            private readonly EnvDesc2 envDesc;

            public MakeMethodBody(MethodToBuild parent, SymbolTable symbolTable, TypeKey owner, MethodKey methodKey, EnvDesc2 envDesc)
            {
                this.parent = parent;
                this.symbolTable = symbolTable;
                this.owner = owner;
                this.methodKey = methodKey;
                this.envDesc = envDesc;
            }

            public int Phase { get { return 1; } }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get
                {
                    return ImmutableSortedSet<ItemKey>.Empty
                        .Add(owner)
                        .Union(parent.body.Parameters.Select(x => x.ParamType.GetReferences()).UnionAll())
                        .Add(methodKey)
                        .Union(parent.body.Body.GetReferences(symbolTable, owner, envDesc.TypesOnly()));
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
                MethodBuilder meb = (MethodBuilder)(vars[methodKey].Value);
                ILGenerator ilg = meb.GetILGenerator();
                CompileContext2 cc = new CompileContext2(ilg, false);
                parent.body.Body.Compile(symbolTable, owner, cc, envDesc, vars, true);
            }
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner)
        {
            EnvSpec es = body.Body.GetEnvSpec();

            ImmutableList<ParamInfo> paramInfos = body.Parameters;
            ImmutableList<Tuple<Symbol, IVarDesc2>> vars = ImmutableList<Tuple<Symbol, IVarDesc2>>.Empty;

            if (!attributes.HasFlag(MethodAttributes.Static))
            {
                if (paramInfos.Count == 0) throw new PascalesqueException("An instance method must at least take a \"this\" parameter");

                if (paramInfos[0].ParamType != new TypeKeyReference(owner)) throw new PascalesqueException("An instance method's \"this\" parameter is not of the correct type");
            }

            int iEnd = paramInfos.Count;
            for (int i = 0; i < iEnd; ++i)
            {
                ParamInfo x = paramInfos[i];
                vars = vars.Add(new Tuple<Symbol, IVarDesc2>(x.Name, new ArgVarDesc2(x.ParamType, es[x.Name].IsCaptured, i)));
            }

            EnvDesc2 e = EnvDesc2.FromSequence(vars);

            TypeReference returnType2 = body.Body.GetReturnType(s, e.TypesOnly());

            if (returnType != returnType2) throw new PascalesqueException("Return type of method does not match");

            bool isInstance = !attributes.HasFlag(MethodAttributes.Static);
            MethodKey mk = new MethodKey(owner, name, isInstance, paramInfos.Select(x => x.ParamType).Skip(isInstance ? 1 : 0).ToImmutableList());

            return body.Body.GetCompileSteps(s, owner, e.TypesOnly())
                .Add(new MakeMethod(this, s, owner, mk))
                .Add(new MakeMethodBody(this, s, owner, mk, e));
        }
    }

    public sealed class SimpleMethodOverrideToBuild : ElementOfClass
    {
        private readonly MethodReference methodToOverride;
        private readonly MethodKey methodTarget;

        public SimpleMethodOverrideToBuild(MethodReference methodToOverride, MethodKey methodTarget)
        {
            this.methodToOverride = methodToOverride;
            this.methodTarget = methodTarget;
        }

        [Bind("methodToOverride")]
        public MethodReference MethodToOverride => methodToOverride;

        [Bind("methodTarget")]
        public MethodKey MethodTarget => methodTarget;

        public override SymbolTable DefineSymbols(SymbolTable s, TypeKey owner)
        {
            return s;
        }

        private class DefineMethodOverride : ICompileStep
        {
            private readonly SimpleMethodOverrideToBuild parent;
            private readonly SymbolTable symbolTable;
            private readonly TypeKey owner;

            public DefineMethodOverride(SimpleMethodOverrideToBuild parent, SymbolTable symbolTable, TypeKey owner)
            {
                this.parent = parent;
                this.symbolTable = symbolTable;
                this.owner = owner;
            }

            public int Phase
            {
                get { return 1; }
            }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get
                {
                    return parent.methodToOverride.GetReferences().Add(parent.methodTarget).Add(owner);
                }
            }

            public ImmutableSortedSet<ItemKey> Outputs
            {
                get { return ImmutableSortedSet<ItemKey>.Empty; }
            }

            public void Compile(ModuleBuilder mb, ImmutableSortedDictionary<ItemKey, SaBox<object>> vars)
            {
                ((TypeBuilder)vars[owner].Value).DefineMethodOverride
                (
                    ((MethodInfo)vars[parent.methodTarget].Value),
                    parent.methodToOverride.Resolve(vars)
                );
            }
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner)
        {
            return ImmutableList<ICompileStep>.Empty.Add(new DefineMethodOverride(this, s, owner));
        }
    }

}
