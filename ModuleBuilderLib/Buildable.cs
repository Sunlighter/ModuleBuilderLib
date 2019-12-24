using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

namespace Sunlighter.ModuleBuilderLib
{
    public interface ICompileStep
    {
        int Phase { get; }
        ImmutableHashSet<ItemKey> Inputs { get; }
        ImmutableHashSet<ItemKey> Outputs { get; }
        void Compile(System.Reflection.Emit.ModuleBuilder mb, ImmutableDictionary<ItemKey, SaBox<object>> vars);
    }

    public class ParamInfo
    {
        private readonly Symbol name;
        private readonly TypeReference paramType;

        public ParamInfo(Symbol name, TypeReference paramType)
        {
            this.name = name;
            this.paramType = paramType;
        }

        public Symbol Name { get { return name; } }

        public TypeReference ParamType { get { return paramType; } }
    }

    public abstract class ElementOfClass
    {
        public abstract SymbolTable DefineSymbols(SymbolTable s, TypeKey owner);
        public abstract ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner);
    }

    public class FieldToBuild : ElementOfClass
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

            public ImmutableHashSet<ItemKey> Inputs
            {
                get { return ImmutableHashSet<ItemKey>.Empty.Add(owner).Union(parent.fieldType.GetReferences()); }
            }

            public ImmutableHashSet<ItemKey> Outputs
            {
                get { return ImmutableHashSet<ItemKey>.Empty.Add(fieldKey); }
            }

            public void Compile(System.Reflection.Emit.ModuleBuilder mb, ImmutableDictionary<ItemKey, SaBox<object>> vars)
            {
                TypeBuilder t = (TypeBuilder)(vars[owner].Value);
                FieldBuilder fb = t.DefineField(parent.name.Name, parent.fieldType.Resolve(vars), parent.attributes);
                vars[fieldKey].Value = fb;
            }

            #endregion
        }

        public override ImmutableList<ICompileStep> GetCompileSteps(SymbolTable s, TypeKey owner)
        {
            return ImmutableList<ICompileStep>.Empty.Add(new MakeField(owner, this));
        }
    }

    public abstract class ElementOfModule
    {
        public abstract SymbolTable DefineSymbols(SymbolTable symbolTable);
        public abstract ImmutableList<ICompileStep> GetCompileSteps(SymbolTable symbolTable);
    }

    public class ClassToBuild : ElementOfModule
    {
        private readonly Symbol name;
        private readonly TypeAttributes attributes;
        private readonly TypeReference ancestor;
        private readonly ImmutableList<TypeReference> interfaces;
        private readonly ImmutableList<ElementOfClass> elements;

        public ClassToBuild(Symbol name, TypeAttributes attributes, TypeReference ancestor, ImmutableList<TypeReference> interfaces, ImmutableList<ElementOfClass> elements)
        {
            this.name = name;
            this.attributes = attributes;
            this.ancestor = ancestor;
            this.interfaces = interfaces;
            this.elements = elements;
        }

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

            public ImmutableHashSet<ItemKey> Inputs
            {
                get { return parent.ancestor.GetReferences().Union(parent.interfaces.Select(x => x.GetReferences()).UnionAll()); }
            }

            public ImmutableHashSet<ItemKey> Outputs
            {
                get { return ImmutableHashSet<ItemKey>.Empty.Add(classKey); }
            }

            public void Compile(ModuleBuilder mb, ImmutableDictionary<ItemKey, SaBox<object>> vars)
            {
                if (parent.ancestor == null)
                {
                    Type[] interfaces = parent.interfaces.Select(x => x.Resolve(vars)).ToArray();
                    TypeBuilder tb = mb.DefineType(parent.name.Name, parent.attributes, null, interfaces);
                }
                else
                {
                    Type ancestor = parent.ancestor.Resolve(vars);
                    Type[] interfaces = parent.interfaces.Select(x => x.Resolve(vars)).ToArray();
                    TypeBuilder tb = mb.DefineType(parent.name.Name, parent.attributes, ancestor, interfaces);
                    vars[classKey].Value = tb;
                }
            }

            #endregion
        }

        private class BakeClass : ICompileStep
        {
            private readonly ClassToBuild parent;
            private readonly TypeKey classKey;
            private readonly CompletedTypeKey completedClassKey;

            public BakeClass(ClassToBuild parent)
            {
                this.parent = parent;
                this.classKey = new TypeKey(parent.name);
                this.completedClassKey = new CompletedTypeKey(parent.name);
            }

            #region ICompileStep Members

            public int Phase
            {
                get { return 2; }
            }

            public ImmutableHashSet<ItemKey> Inputs
            {
                get { return ImmutableHashSet<ItemKey>.Empty.Add(classKey); }
            }

            public ImmutableHashSet<ItemKey> Outputs
            {
                get { return ImmutableHashSet<ItemKey>.Empty.Add(completedClassKey); }
            }

            public void Compile(System.Reflection.Emit.ModuleBuilder mb, ImmutableDictionary<ItemKey, SaBox<object>> vars)
            {
                TypeBuilder tb = (TypeBuilder)(vars[classKey].Value);
                Type t = tb.CreateType();
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

    public class ModuleToBuild
    {
        private readonly ImmutableList<ElementOfModule> elements;

        public ModuleToBuild(ImmutableList<ElementOfModule> elements)
        {
            this.elements = elements;
        }

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
