namespace TreyarchCompiler.Utilities
{
    public class WarningsData
    {
        public readonly string Declaration;
        public readonly string Warning;
        public int Refs;

        public WarningsData(string declaration, string warning)
        {
            Declaration = declaration;
            Warning = warning;
            Refs = 1;
        }

        public string GetDeclaration()
        {
            return Declaration;
        }

        public string GetWarning()
        {
            return Warning;
        }

        public int GetRef()
        {
            return Refs;
        }

        public void OneUpRefs()
        {
            Refs++;
        }
    }
}