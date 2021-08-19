using TreyarchCompiler.Enums;
using TreyarchCompiler.Games;
using TreyarchCompiler.Interface;
using TreyarchCompiler.Utilities;

namespace TreyarchCompiler
{
    //NOTE: this class system will no longer work as of bo3, because each platform has unique opcodes.
    public class Compiler
    {
        public static CompiledCode Compile(Platforms platform, Enums.Games game, Modes mode, bool uset8masking, string code, string path = "")
        {
            switch(platform)
            {
                case Platforms.PC:
                    return CompilePC(game, mode, code, path, uset8masking)?.Compile();

                case Platforms.Xbox:
                case Platforms.PS3:
                    return CompileConsole(game, mode, code, path)?.Compile();
            }
            return null;
        }

        public static CompiledCode CompileRCE(Platforms platform, Enums.Games game, Modes mode, string code, string address, string path = "")
        {
            switch (platform)
            {
                case Platforms.PC:
                    return CompilePC(game, mode, code, path, false)?.Compile(address);

                case Platforms.Xbox:
                case Platforms.PS3:
                    return CompileConsole(game, mode, code, path)?.Compile(address);
            }
            return null;
        }

        private static ICompiler CompilePC(Enums.Games game, Modes mode, string code, string path, bool uset8masking)
        {
            switch(game)
            {
                case Enums.Games.T7:
                    return new GSCCompiler(mode, code, path, Platforms.PC, game, false);
            }
            return null;
        }

        private static ICompiler CompilePS4(Enums.Games game, Modes mode, string code, string path, bool uset8masking)
        {
            return null;
        }

        private static ICompiler CompileConsole(Enums.Games game, Modes mode, string code, string path)
        {
            return null;
        }
    }
}