using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T7CompilerLib.ScriptComponents;

namespace T7CompilerLib.OpCodes
{
    public class T7OP_AbstractCall : T7OpCode
    {
        private T7Import __import__;

        public T7OP_AbstractCall(EndianType endianess) : base(endianess) { }

        protected T7Import Import
        {
            get => __import__;
            set
            {
                __import__?.References.Remove(this);
                __import__ = value;
                __import__?.References.Add(this);
            }
        }
    }
}
