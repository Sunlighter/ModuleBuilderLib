using System;
using System.Collections.Generic;
using System.Text;

namespace Sunlighter.ModuleBuilderLib
{
    public class SaBox<T>
    {
        private T theValue;
        private bool hasValue;

        public SaBox()
        {
            theValue = default(T);
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
