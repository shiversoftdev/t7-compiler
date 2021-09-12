using T89CompilerLib.ScriptComponents;

namespace T89CompilerLib.OpCodes
{
    public class T89OP_AbstractCall : T89OpCode
    {
        private T89Import __import__;
        public T89Import Import
        {
            get => __import__;
            protected set
            {
                __import__?.References.Remove(this);
                __import__ = value;
                __import__?.References.Add(this);
            }
        }
    }
}
