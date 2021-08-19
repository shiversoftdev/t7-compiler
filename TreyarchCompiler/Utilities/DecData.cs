namespace TreyarchCompiler.Utilities
{
    public class DecData
    {
        private readonly string _declaration;
        private readonly string _call;

        public DecData(string declaration, string call)
        {
            _declaration = declaration;
            _call = call;
        }

        public string GetDeclaration()
        {
            return _declaration;
        }

        public string GetCall()
        {
            return _call;
        }
    }
}