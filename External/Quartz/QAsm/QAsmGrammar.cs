using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Quartz.QAsm
{
    [Language("Quartz Assembly", "0.0", "Quartz bridged x86 assembly language")]
    public class QAsmGrammar : Grammar
    {
        protected NumberLiteral NumberLiteral { private set; get; }
        protected StringLiteral StringLiteral { private set; get; }
        protected IdentifierTerminal Identifier { private set; get; }
        protected NonTerminal directives { private set; get; }
        protected NonTerminal directive { private set; get; }
        protected NonTerminal instruction { private set; get; }
        protected NonTerminal iConst { private set; get; }
        protected NonTerminal iExtern { private set; get; }
        protected NonTerminal variableQualifier { private set; get; }
        protected NonTerminal typeAccessor { private set; get; }
        protected NonTerminal constExpr { private set; get; }
        protected NonTerminal boolLiteral { private set; get; }
        protected NonTerminal builtinType { private set; get; }
        protected NonTerminal paramsList { private set; get; }
        protected NonTerminal declareFunction { private set; get; }
        protected NonTerminal codeBlock { private set; get; }
        protected NonTerminal iLet { private set; get; }
        protected NonTerminal iIf { private set; get; }
        protected NonTerminal iBool { private set; get; }
        protected NonTerminal iRet { private set; get; }
        protected NonTerminal iMov { private set; get; }
        protected NonTerminal iAdd { private set; get; }
        protected NonTerminal valueAccessor { private set; get; }
        protected NonTerminal memoryAccessor { private set; get; }
        protected NonTerminal destinationOperand { private set; get; }
        protected NonTerminal functionPointer { private set; get; }
        protected NonTerminal binOp { private set; get; }
        protected NonTerminal valueDisplacementOptional { private set; get; }
        protected NonTerminal iCalld { private set; get; }
        protected NonTerminal callparamsList { private set; get; }
        protected NonTerminal variableHandle { private set; get; }

        private List<KeyTerm> RegisteredTerms;

        public static Parser Parser => ThreadSafeInstance.SyntaxParser;
        protected readonly Parser SyntaxParser;
        private static readonly object InstanceLock = new object();
        private static QAsmGrammar __instance;
        protected static QAsmGrammar ThreadSafeInstance
        {
            get
            {
                lock (InstanceLock)
                {
                    return __instance ?? (__instance = new QAsmGrammar());
                }
            }
        }

        public QAsmGrammar()
        {
            NumberLiteral = TerminalFactory.CreateCSharpNumber(NUMBER_LITERAL);
            NumberLiteral.Options = NumberOptions.AllowSign;

            Identifier = new IdentifierTerminal("identifier", @"_", "_");
            StringLiteral = new StringLiteral(STRING_LITERAL, "\"", StringOptions.AllowsAllEscapes);

            NonGrammarTerminals.Add(new CommentTerminal("line-comment", "//", "\r", "\n", "\u2085", "\u2028", "\u2029"));

            MarkPunctuation("[", "]", ";", "::", ":", "&", ".", "(", ")", ",", "{", "}", "@");
            RegisterOperators(1, "+", "-");
            RegisterOperators(2, "*", "/");
            RegisterBracePair("[", "]");
            RegisterBracePair("{", "}");
            RegisterKeyTerms();
            CreateNonTerminals();

            directives.Rule = MakeStarRule(directives, null, directive);
            directive.Rule = instruction | declareFunction;
            instruction.Rule = iConst | iExtern | iLet | iIf | iBool | iRet | iMov | iAdd | iCalld;

            #region Instructions
            iConst.Rule = kConst + variableQualifier + "," + constExpr + ";";
            iExtern.Rule = kExtern + variableQualifier + ";";
            iLet.Rule = kLet + variableQualifier + ";";
            iBool.Rule = kBool + valueAccessor + ";";
            iRet.Rule = kRet + ";" | kRet + valueAccessor + ";";
            iMov.Rule = kMov + destinationOperand + "," + valueAccessor + ";";
            iAdd.Rule = kAdd + destinationOperand + "," + valueAccessor + ";";
            iCalld.Rule = kCall + valueAccessor + "," + callparamsList + ";" |
                          kCall + valueAccessor + ";";
            #endregion

            #region CodeBlocks
            declareFunction.Rule = Identifier + "(" + paramsList + ")" + codeBlock;
            paramsList.Rule = MakeStarRule(paramsList, ToTerm(","), variableQualifier);
            codeBlock.Rule = ToTerm("[") + directives + ToTerm("]");
            iIf.Rule = kIf + codeBlock + codeBlock + ToTerm(";");
            #endregion

            variableQualifier.Rule = Identifier + typeAccessor | Identifier;
            typeAccessor.Rule = ToTerm(":") + builtinType | ToTerm(":") + Identifier;
            builtinType.Rule = kByte | kWord | kDword | kQword | kString;
            constExpr.Rule = NumberLiteral | StringLiteral | boolLiteral;
            boolLiteral.Rule = kTrue | kFalse;
            functionPointer.Rule = Identifier + "::" + Identifier;
            variableHandle.Rule = ToTerm("&") + Identifier;
            valueAccessor.Rule = memoryAccessor | variableHandle | Identifier | NumberLiteral | boolLiteral | functionPointer;
            memoryAccessor.Rule = ToTerm("@") + "(" + valueDisplacementOptional + ")" + typeAccessor |
                                  ToTerm("@") + "(" + valueDisplacementOptional + ")";
            valueDisplacementOptional.Rule = valueAccessor + binOp + NumberLiteral | valueAccessor;
            destinationOperand.Rule = memoryAccessor | variableQualifier;
            binOp.Rule = ToTerm("+") | ToTerm("-");
            callparamsList.Rule = MakePlusRule(callparamsList, ToTerm(","), valueAccessor);

            // Marks all the rules that are able to be reduced, reducing the depth of the parsetree
            MarkTransient(directive, builtinType, instruction, valueAccessor, boolLiteral, destinationOperand, binOp);

            SyntaxParser = new Parser(this);
        }

        protected KeyTerm 
            kConst, kExtern, kLet, kIf, kBool, kRet, kMov, kAdd,
            kCall,
            kByte, kWord, kDword, kQword, kString,
            kTrue, kFalse;
        private void RegisterKeyTerms()
        {
            RegisteredTerms = new List<KeyTerm>();
            kConst = RegisterTerm("const");
            kExtern = RegisterTerm("extern");
            kLet = RegisterTerm("let");
            kIf = RegisterTerm("if");
            kBool = RegisterTerm("bool");
            kRet = RegisterTerm("ret");
            kMov = RegisterTerm("mov");
            kAdd = RegisterTerm("add");
            kCall = RegisterTerm("call");
            kByte = RegisterTerm("byte");
            kWord = RegisterTerm("word");
            kDword = RegisterTerm("dword");
            kQword = RegisterTerm("qword");
            kString = RegisterTerm("string");
            kTrue = RegisterTerm("true");
            kFalse = RegisterTerm("false");
        }

        private KeyTerm RegisterTerm(string text, string name = null)
        {
            KeyTerm kt = ToTerm(text);
            kt.SetFlag(TermFlags.IsReservedWord);
            RegisteredTerms.Add(kt);
            return kt;
        }

        public const string
            NUMBER_LITERAL = "numberLiteral",
            STRING_LITERAL = "stringLiteral",
            DIRECTIVES = "directives",
            DIRECTIVE = "directive",
            INSTRUCTION = "instruction",
            VARIABLE_QUALIFIER = "variableQualifier",
            TYPE_ACCESSOR = "typeAccessor",
            VALUE_ACCESSOR = "valueAccessor",
            VALUE_DISPLACEMENT_OPT = "valueDisplacementOptional",
            MEMORY_ACCESSOR = "memoryAccessor",
            DESTINATION_OPERAND = "destinationOperand",
            FUNCTION_POINTER = "functionPointer",
            CONST_EXPR = "constExpr",
            BOOL_LITERAL = "boolLiteral",
            BUILTIN_TYPE = "builtinType",
            PARAMS_LIST = "paramsList",
            CALLPARAMS_LIST = "callparamsList",
            CODEBLOCK = "codeBlock",
            INSTRUCTION_EXTERN = "iExtern",
            INSTRUCTION_CONST = "iConst",
            INSTRUCTION_LET = "iLet",
            INSTRUCTION_IF = "iIf",
            INSTRUCTION_BOOL = "iBool",
            INSTRUCTION_RET = "iRet",
            INSTRUCTION_MOV = "iMov",
            INSTRUCTION_ADD = "iAdd",
            INSTRUCTION_CALLD = "iCalld",
            NAMED_CODEBLOCK = "declareFunction",
            BIN_OP = "binOp",
            VARIABLE_HANDLE = "variableHandle";

        private void CreateNonTerminals()
        {
            directives = new NonTerminal(DIRECTIVES);
            directive = new NonTerminal(DIRECTIVE);
            instruction = new NonTerminal(INSTRUCTION);
            variableQualifier = new NonTerminal(VARIABLE_QUALIFIER);
            typeAccessor = new NonTerminal(TYPE_ACCESSOR);
            valueAccessor = new NonTerminal(VALUE_ACCESSOR);
            valueDisplacementOptional = new NonTerminal(VALUE_DISPLACEMENT_OPT);
            memoryAccessor = new NonTerminal(MEMORY_ACCESSOR);
            destinationOperand = new NonTerminal(DESTINATION_OPERAND);
            functionPointer = new NonTerminal(FUNCTION_POINTER);
            constExpr = new NonTerminal(CONST_EXPR);
            boolLiteral = new NonTerminal(BOOL_LITERAL);
            builtinType = new NonTerminal(BUILTIN_TYPE);
            paramsList = new NonTerminal(PARAMS_LIST);
            callparamsList = new NonTerminal(CALLPARAMS_LIST);
            codeBlock = new NonTerminal(CODEBLOCK);
            iExtern = new NonTerminal(INSTRUCTION_EXTERN);
            iConst = new NonTerminal(INSTRUCTION_CONST);
            iLet = new NonTerminal(INSTRUCTION_LET);
            iIf = new NonTerminal(INSTRUCTION_IF);
            iBool = new NonTerminal(INSTRUCTION_BOOL);
            iRet = new NonTerminal(INSTRUCTION_RET);
            iMov = new NonTerminal(INSTRUCTION_MOV);
            iAdd = new NonTerminal(INSTRUCTION_ADD);
            iCalld = new NonTerminal(INSTRUCTION_CALLD);
            declareFunction = new NonTerminal(NAMED_CODEBLOCK);
            binOp = new NonTerminal(BIN_OP);
            variableHandle = new NonTerminal(VARIABLE_HANDLE);
            Root = new NonTerminal("program") { Rule = directives };
        }
    }
}
