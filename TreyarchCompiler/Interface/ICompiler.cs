using TreyarchCompiler.Utilities;

namespace TreyarchCompiler.Interface
{
    interface ICompiler
    {
        CompiledCode Compile();

        CompiledCode Compile(string address);
    }
}