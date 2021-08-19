using System;
using System.Collections.Generic;
using System.IO;
using Irony.Parsing;
using TreyarchCompiler.Enums;
using TreyarchCompiler.Utilities;

namespace TreyarchCompiler.Games
{
    internal abstract class BLOPSCompilerBase
    {
        protected struct Helpers
        {
            internal static readonly string UndefinedCall = "0undefinedFunction";

            //internal variables
            internal static readonly string BooleanExpression = "0booleanExpression";
            internal static readonly string BogsArrayKeys = "0bogsArrayKeys";
            internal static readonly string BogsArrayIndex = "0bogsIndex";

            //Preprocessor Global Variables
            internal static readonly string PreProcessGlobal = "0preProcessGlobal_";
        }

        //------------- Syntax & Parser -------------\\
        protected ParseTree _tree;

        //------------- Bytecode & Buffer -------------\\
        protected readonly List<byte> _byteCode;
        protected readonly List<byte> _buffer;

        //------------- Foreach Statement Support -------------\\
        protected readonly Random _random;
        protected readonly List<Keys> _forEachKeys; //for interop, not removed, but deprecated due to async

        //------------- Optional Parameters Support -------------\\
        protected readonly List<ParseTreeNode> _localParameterNodes;

        //THIS WILL HOLD THE BEGIN POSITIONS OF ALL DEFINITIONS FOR JUMPS
        //HOLDS ALL CALL POSITIONS WITH CALL NAME && DEC NAME
        protected readonly Dictionary<string, int> _functionDefIndex;
        protected string _currentDeclaration;//Used to help with debug info

        //------------- Local Variables -------------\\
        protected readonly List<string> _localVariables;

        //------------- Dubug Support -------------\\
        protected readonly List<WarningsData> _warnings;
        protected readonly List<string> _undefinedVariables;
        protected string _path;
        protected Modes _mode;

        protected BLOPSCompilerBase()
        {
            _byteCode = new List<byte>();
            _buffer = new List<byte>();

            _random = new Random();
            _functionDefIndex = new Dictionary<string, int>();

            _warnings = new List<WarningsData>();
            _undefinedVariables = new List<string>();
        }

        protected void WriteFile(string location, string fileName, byte[] data)
        {
            var stream = new FileStream(Path.Combine(location, fileName), FileMode.Create);
            stream.Write(data, 0, data.Length);
            stream.Close();
            stream.Dispose();
        }
    }
}