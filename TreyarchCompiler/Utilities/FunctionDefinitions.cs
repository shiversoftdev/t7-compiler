namespace TreyarchCompiler.Utilities
{
    public class FunctionDefinitions
    {
        public int start;
        public int size;
        public byte[] code;

        public FunctionDefinitions()
        {
        }

        public FunctionDefinitions(int start)
        {
            this.start = start;
        }
    }
}
