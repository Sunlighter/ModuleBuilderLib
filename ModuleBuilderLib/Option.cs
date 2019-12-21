using System;
using System.Collections.Generic;
using System.Text;

namespace Sunlighter.ModuleBuilderLib
{
    public abstract class Option<T>
    {
        public static Option<T> Some(T value) { return new Some<T>(value); }

        public static Option<T> None() { return new None<T>(); }

        public abstract bool HasValue { get; }

        public abstract T Value { get; }
    }

    public class Some<T> : Option<T>
    {
        private T value;

        public Some(T value)
        {
            this.value = value;
        }

        public override bool HasValue
        {
            get { return true; }
        }

        public override T Value
        {
            get { return value; }
        }
    }

    public class None<T> : Option<T>
    {
        public None()
        {
        }

        public override bool HasValue
        {
            get { return false; }
        }

        public override T Value
        {
            get { throw new InvalidOperationException(); }
        }
    }

    public static partial class Extensions
    {
        public static Option<U> Map<T, U>(Func<T, U> func, Option<T> opValue)
        {
            if (opValue.HasValue)
            {
                return Option<U>.Some(func(opValue.Value));
            }
            else
            {
                return Option<U>.None();
            }
        }

        public static Option<U> Bind<T, U>(Func<T, Option<U>> func, Option<T> opValue)
        {
            if (opValue.HasValue)
            {
                return func(opValue.Value);
            }
            else
            {
                return Option<U>.None();
            }
        }

        public static T Coalesce<T>(Option<T> val1, T val2)
        {
            if (val1.HasValue)
            {
                return val1.Value;
            }
            else
            {
                return val2;
            }
        }
    }
}
