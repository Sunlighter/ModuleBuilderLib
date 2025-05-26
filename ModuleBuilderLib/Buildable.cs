using Sunlighter.OptionLib;
using Sunlighter.TypeTraitsLib.Building;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Sunlighter.ModuleBuilderLib
{
    public interface ICompileStep
    {
        int Phase { get; }
        ImmutableSortedSet<ItemKey> Inputs { get; }
        ImmutableSortedSet<ItemKey> Outputs { get; }
        void Compile(ModuleBuilder mb, ImmutableSortedDictionary<ItemKey, SaBox<object>> vars);
    }

    [Record]
    public sealed class ParamInfo
    {
        private readonly Symbol name;
        private readonly TypeReference paramType;

        public ParamInfo(Symbol name, TypeReference paramType)
        {
            this.name = name;
            this.paramType = paramType;
        }

        [Bind("name")]
        public Symbol Name { get { return name; } }

        [Bind("paramType")]
        public TypeReference ParamType { get { return paramType; } }
    }

    [UnionOfDescendants]
    public abstract class ElementOfClass
    {
        public abstract SymbolTable DefineSymbols(SymbolTable s, TypeKey owner);
        public abstract ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner);
    }

    [Record]
    public sealed class FieldToBuild : ElementOfClass
    {
        private readonly FieldAttributes attributes;
        private readonly TypeReference fieldType;
        private readonly Symbol name;

        public FieldToBuild(FieldAttributes attributes, TypeReference fieldType, Symbol name)
        {
            this.attributes = attributes;
            this.fieldType = fieldType;
            this.name = name;
        }

        [Bind("attributes")]
        public FieldAttributes Attributes => attributes;

        [Bind("fieldType")]
        public TypeReference FieldType => fieldType;

        [Bind("name")]
        public Symbol Name => name;

        public override SymbolTable DefineSymbols(SymbolTable s, TypeKey owner)
        {
            FieldKey fk = new FieldKey(owner, name, fieldType);
            FieldAux fa = new FieldAux();
            return s.SetItem(fk, fa);
        }

        private class MakeField : ICompileStep
        {
            private readonly TypeKey owner;
            private readonly FieldToBuild parent;
            private readonly FieldKey fieldKey;

            public MakeField(TypeKey owner, FieldToBuild parent)
            {
                this.owner = owner;
                this.parent = parent;
                this.fieldKey = new FieldKey(owner, parent.name, parent.fieldType);
            }

            #region ICompileStep Members

            public int Phase
            {
                get { return 1; }
            }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get { return ImmutableSortedSet<ItemKey>.Empty.Add(owner).Union(parent.fieldType.GetReferences()); }
            }

            public ImmutableSortedSet<ItemKey> Outputs
            {
                get { return ImmutableSortedSet<ItemKey>.Empty.Add(fieldKey); }
            }

            public void Compile(System.Reflection.Emit.ModuleBuilder mb, ImmutableSortedDictionary<ItemKey, SaBox<object>> vars)
            {
                TypeBuilder t = (TypeBuilder)(vars[owner].Value);
                FieldBuilder fb = t.DefineField(parent.name.SymbolName(), parent.fieldType.Resolve(vars), parent.attributes);
                vars[fieldKey].Value = fb;
            }

            #endregion
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner)
        {
            return ImmutableList<ICompileStep>.Empty.Add(new MakeField(owner, this));
        }
    }

    [UnionOfDescendants]
    public abstract class ElementOfModule
    {
        public abstract SymbolTable DefineSymbols(SymbolTable symbolTable);
        public abstract ImmutableList<ICompileStep> GetCompileSteps(SymbolTable symbolTable);
    }

    [Record]
    public class ClassToBuild : ElementOfModule
    {
        private readonly Symbol name;
        private readonly TypeAttributes attributes;
        private readonly TypeReference ancestor;
        private readonly ImmutableList<TypeReference> interfaces;
        private readonly ImmutableList<ElementOfClass> elements;

        public ClassToBuild
        (
            Symbol name,
            TypeAttributes attributes,
            TypeReference ancestor,
            ImmutableList<TypeReference> interfaces,
            ImmutableList<ElementOfClass> elements
        )
        {
            this.name = name;
            this.attributes = attributes;
            this.ancestor = ancestor;
            this.interfaces = interfaces;
            this.elements = elements;
        }

        [Bind("name")]
        public Symbol Name => name;

        [Bind("attributes")]
        public TypeAttributes Attributes => attributes;

        [Bind("ancestor")]
        public TypeReference BaseClass => ancestor;

        [Bind("interfaces")]
        public ImmutableList<TypeReference> Interfaces => interfaces;

        [Bind("elements")]
        public ImmutableList<ElementOfClass> Elements => elements;

        private class MakeClass : ICompileStep
        {
            private readonly ClassToBuild parent;
            private readonly TypeKey classKey;

            public MakeClass(ClassToBuild parent)
            {
                this.parent = parent;
                this.classKey = new TypeKey(parent.name);
            }

            #region ICompileStep Members

            public int Phase
            {
                get { return 1; }
            }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get { return parent.ancestor.GetReferences().UnionAll(parent.interfaces.Select(x => x.GetReferences())); }
            }

            public ImmutableSortedSet<ItemKey> Outputs
            {
                get { return ImmutableSortedSet<ItemKey>.Empty.Add(classKey); }
            }

            public void Compile(ModuleBuilder mb, ImmutableSortedDictionary<ItemKey, SaBox<object>> vars)
            {
                if (parent.ancestor == null)
                {
                    Type[] interfaces = parent.interfaces.Select(x => x.Resolve(vars)).ToArray();
                    TypeBuilder tb = mb.DefineType(parent.name.SymbolName(), parent.attributes, null, interfaces);
                }
                else
                {
                    Type ancestor = parent.ancestor.Resolve(vars);
                    Type[] interfaces = parent.interfaces.Select(x => x.Resolve(vars)).ToArray();
                    TypeBuilder tb = mb.DefineType(parent.name.SymbolName(), parent.attributes, ancestor, interfaces);
                    vars[classKey].Value = tb;
                }
            }

            #endregion
        }

        private class BakeClass : ICompileStep
        {
            private readonly TypeKey classKey;
            private readonly CompletedTypeKey completedClassKey;

            public BakeClass(ClassToBuild parent)
            {
                classKey = new TypeKey(parent.name);
                completedClassKey = new CompletedTypeKey(parent.name);
            }

            #region ICompileStep Members

            public int Phase
            {
                get { return 2; }
            }

            public ImmutableSortedSet<ItemKey> Inputs
            {
                get { return ImmutableSortedSet<ItemKey>.Empty.Add(classKey); }
            }

            public ImmutableSortedSet<ItemKey> Outputs
            {
                get { return ImmutableSortedSet<ItemKey>.Empty.Add(completedClassKey); }
            }

            public void Compile(ModuleBuilder mb, ImmutableSortedDictionary<ItemKey, SaBox<object>> vars)
            {
                TypeBuilder tb = (TypeBuilder)(vars[classKey].Value);
#if NETSTANDARD2_0
                Type t = (Type)tb.CreateTypeInfo();
#else
                Type t = tb.CreateType();
#endif
                vars[completedClassKey].Value = t;
            }

            #endregion
        }

        public override SymbolTable DefineSymbols(SymbolTable s)
        {
            TypeKey typeKey = new TypeKey(name);
            TypeAux a = new TypeAux(false, false, Option<TypeReference>.Some(ancestor), interfaces);
            s = s.SetItem(typeKey, a);
            foreach (ElementOfClass element in elements)
            {
                s = element.DefineSymbols(s, typeKey);
            }
            return s;
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s)
        {
            return ImmutableList<ICompileStep>.Empty
                .Add(new MakeClass(this))
                .Add(new BakeClass(this))
                .AddRange(elements.SelectMany(e => e.GetCompileSteps(s, new TypeKey(name))));
        }
    }

    [Record]
    public sealed class ModuleToBuild
    {
        private readonly ImmutableList<ElementOfModule> elements;

        public ModuleToBuild(ImmutableList<ElementOfModule> elements)
        {
            this.elements = elements;
        }

        [Bind("elements")]
        public ImmutableList<ElementOfModule> Elements => elements;

        public SymbolTable DefineSymbols(SymbolTable symbolTable)
        {
            foreach (ElementOfModule element in elements)
            {
                symbolTable = element.DefineSymbols(symbolTable);
            }
            return symbolTable;
        }

        public ImmutableList<ICompileStep> GetCompileSteps(SymbolTable symbolTable)
        {
            return elements.SelectMany(e => e.GetCompileSteps(symbolTable)).ToImmutableList();
        }
    }
}
