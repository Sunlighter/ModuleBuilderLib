using System;

namespace Sunlighter.ModuleBuilderLib
{
    public class SaBox<T>
    {
        private T theValue;
        private bool hasValue;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SaBox()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
#pragma warning disable CS8601 // Possible null reference assignment.
            theValue = default(T);
#pragma warning restore CS8601 // Possible null reference assignment.
            hasValue = false;
        }

        public SaBox(T initValue)
        {
            theValue = initValue;
            hasValue = true;
        }

        public T Value
        {
            get
            {
                if (hasValue) return theValue;
                else throw new InvalidOperationException("Uninitialized Box");
            }
            set
            {
                if (hasValue) throw new InvalidOperationException("Single assignment violation");
                theValue = value;
                hasValue = true;
            }
        }

        public bool HasValue { get { return hasValue; } }
    }
}
