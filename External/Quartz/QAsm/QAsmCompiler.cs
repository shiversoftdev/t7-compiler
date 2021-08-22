using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Quartz.QAsm.QAsmGrammar;

namespace System.Quartz.QAsm
{
    public class QAsmCompiler
    {
        public const string ROOT_CONTEXT_NAME = "root";
        private readonly QAsmCompilerOptions CompilerOptions;
        private QObj Env;
        private readonly Stack<QOperand> TaskStack = new Stack<QOperand>();
        private readonly Stack<QOperand> ValueStack = new Stack<QOperand>();
        private Dictionary<string, InstructionEmitter> NodeHandlers;
        public QAsmCompiler(QAsmCompilerOptions options)
        {
            CompilerOptions = options;
            RegisterNodeHandlers();
        }

        private delegate IEnumerable<QOperand> InstructionEmitter(QOperand currentTask);

        private void RegisterNodeHandlers()
        {
            NodeHandlers = new Dictionary<string, InstructionEmitter>();
            NodeHandlers[INSTRUCTION_CONST] = Emit_IConst;
            NodeHandlers[VARIABLE_QUALIFIER] = Emit_VarQualifier;
            NodeHandlers[CONST_EXPR] = Emit_ConstValue;
            NodeHandlers[INSTRUCTION_EXTERN] = Emit_IExtern;
            NodeHandlers[NAMED_CODEBLOCK] = Emit_NamedBlock;
            NodeHandlers[INSTRUCTION_LET] = Emit_ILet;
        }

        public QAsmCompilerResult Compile(string sourceBuffer)
        {
            QAsmCompilerResult result = new QAsmCompilerResult();
            var ParseResult = QAsmGrammar.Parser.Parse(sourceBuffer);
            if(ParseResult.HasErrors())
            {
                throw new Exception(ResolveParserError(ParseResult));
            }
            Env = new QObj();
            IterateParseTree(ParseResult);
            return result;
        }

        public struct QAsmCompilerOptions
        { }

        public struct QAsmCompilerResult
        {
            public byte[] CompiledBuffer;
        }

        private static string ResolveParserError(ParseTree parseTree)
        {
            var msg = parseTree.ParserMessages[0];
            switch (msg.ParserState.Name)
            {
                default:
                    return $"[Syntax(Line {msg.Location.Line})] {msg.Message}";
            }
        }

        private void IterateParseTree(ParseTree tree)
        {
            // setup root context for this script
            QCodeBlock rootBlock = new QCodeBlock(ROOT_CONTEXT_NAME);
            QContextEntry rootContext = new QContextEntry(rootBlock, rootBlock.Context, null);
            Env.PutContext(null, rootContext);
            PushTask(rootBlock, tree.Root);
            ExecuteTaskStack();
        }

        private void ExecuteTaskStack()
        {
            while (TaskStack.Count > 0)
            {
                var currentTask = TaskStack.Pop();
                if (!currentTask.IsParseNode)
                {
                    throw new InvalidOperationException("Compiler detected leftover value operands in the task stack."); //Stack misalignment
                }
                var node = currentTask.ObjectNode;
                var currentBlock = currentTask.CurrentBlock;
                if (currentTask.GetOperands != null)
                {
                    if (!currentTask.GetOperands.MoveNext()) continue;
                    PushTask(currentTask);
                    PushTask(currentTask.GetOperands.Current);
                    continue;
                }
                if(NodeHandlers.ContainsKey(node.Term.Name))
                {
                    currentTask.SetOperands = NodeHandlers[node.Term.Name](currentTask);
                    PushTask(currentTask);
                }
                else
                {
                    foreach (var child in node.ChildNodes.AsEnumerable().Reverse())
                    {
                        PushTask(currentBlock, child);
                    }
                }
            }
        }

        #region Instruction Handlers
        private IEnumerable<QOperand> Emit_IConst(QOperand currentTask)
        {
            // node {const} {variableQualifier} {constExpr}
            var node = currentTask.ObjectNode;

            // Yield<Storage>: Variable name and type specifier, which should push the result to the stack
            yield return new QOperand(currentTask.CurrentBlock, node.ChildNodes[1]);
            QVariable variable = ValueStack.Pop().ObjectValue as QVariable;

            if(variable is null)
            {
                throw new InvalidOperationException($"Stack expected to be retrieving a QVariable, but instead retrieved '{variable.GetType()}'");
            }

            // Yield<Value>: Const operand value
            yield return new QOperand(currentTask.CurrentBlock, node.ChildNodes[2]);
            object value = ValueStack.Pop().ObjectValue;
            variable.SetValue(value);

            // Register the variable with the environment
            Env.PutReadonly(variable);

        }
        private IEnumerable<QOperand> Emit_IExtern(QOperand currentTask)
        {
            // node {extern} {variableQualifier}
            var node = currentTask.ObjectNode;

            // Yield<Storage>: Variable name and type specifier, which should push the result to the stack
            yield return new QOperand(currentTask.CurrentBlock, node.ChildNodes[1]);
            QVariable variable = ValueStack.Pop().ObjectValue as QVariable;

            // Externs are simply context registered with no value.
            // These values must be validated though. Variables with size >= 8 that are not pointers will be rejected
            if(!variable.TypeInfo.IsReferenceType && variable.TypeInfo.Size > sizeof(ulong))
            {
                throw new InvalidDataException("Externs must be a reference type, or of size less than 8.");
            }

            // Register the variable as an external with the environment
            Env.PutExtern(variable);
        }
        private IEnumerable<QOperand> Emit_ILet(QOperand currentTask)
        {
            // node {let} {variableQualifier}
            var node = currentTask.ObjectNode;

            // Yield<Storage>: Variable name and type specifier, which should push the result to the stack
            yield return new QOperand(currentTask.CurrentBlock, node.ChildNodes[1]);
            QVariable variable = ValueStack.Pop().ObjectValue as QVariable;
            variable.IsConst = false;

            // Push storage into the current scope
            currentTask.CurrentBlock.AddScopedParameter(variable);
        }
        #endregion

        #region Misc Handlers
        private IEnumerable<QOperand> Emit_VarQualifier(QOperand currentTask)
        {
            // node {identifier} {typeAccessor}?
            var node = currentTask.ObjectNode;

            // Variable name
            string var_name = node.ChildNodes[0].Token.ValueString;
            string typeName = "qword";
            if(node.ChildNodes.Count > 1)
            {
                typeName = node.ChildNodes[1].ChildNodes[0].Token.ValueString;
            }

            // Allocate a constant and push it to the stack. This is the default action for variables. The type of variable can be changed by the compiler to fit its needs.
            // This default readonly behavior will help to prevent invalid assignments to constant variables.
            var variable = QVariable.AllocConst(typeName, Env, currentTask.CurrentBlock.Context, var_name);
            ValueStack.Push(new QOperand(currentTask.CurrentBlock, variable));
            yield break;
        }
        private IEnumerable<QOperand> Emit_ConstValue(QOperand currentTask)
        {
            // node stringLiteral | boolLiteral | numberLiteral
            var node = currentTask.ObjectNode;

            switch(node.ChildNodes[0].Term.Name)
            {
                case NUMBER_LITERAL:
                    ValueStack.Push(new QOperand(currentTask.CurrentBlock, node.ChildNodes[0].Token.Value));
                    break;

                case STRING_LITERAL:
                    ValueStack.Push(new QOperand(currentTask.CurrentBlock, node.ChildNodes[0].Token.ValueString));
                    break;

                case BOOL_LITERAL:
                    ValueStack.Push(new QOperand(currentTask.CurrentBlock, node.ChildNodes[0].Token.ValueString == "true"));
                    break;

                default:
                    throw new Exception("Unknown or unhandled type inside constexpr node");
            }

            yield break;
        }
        #endregion

        #region Control Flow
        private IEnumerable<QOperand> Emit_NamedBlock(QOperand currentTask)
        {
            // node {name} {paramsList} {codeBlock}
            var node = currentTask.ObjectNode;

            // Build context information
            string functionName = node.ChildNodes[0].Token.ValueString;
            string newContext = $"{currentTask.CurrentBlock.Context}.{functionName}";

            // Create a new export
            QCodeBlock exportBlock = new QCodeBlock(newContext);
            QContextEntry contextEntry = new QContextEntry(exportBlock, functionName, currentTask.CurrentBlock.Context);
            Env.PutContext(newContext, contextEntry);

            // collect the parameter references
            foreach(var pnode in node.ChildNodes[1].ChildNodes)
            {
                yield return new QOperand(exportBlock, pnode);
            }

            // populate a params array
            var numParams = node.ChildNodes[1].ChildNodes.Count;
            QVariable[] parameters = new QVariable[numParams];
            for(int i = parameters.Length - 1; i > -1; i--)
            {
                parameters[i] = ValueStack.Pop().ObjectValue as QVariable;
                if(parameters[i] is null)
                {
                    throw new InvalidCastException($"Failed to create a qvariable from a parameter index {i}");
                }
            }

            // Put params in the target export context
            exportBlock.SetParameters(parameters);

            // Link the export block into the current context so it is emitted
            currentTask.CurrentBlock.Add(exportBlock);

            // function.codeblock.directives
            yield return new QOperand(exportBlock, node.ChildNodes[2].ChildNodes[0]);
        }
        #endregion

        #region Stack Operations
        private void PushTask(QCodeBlock currentBlock, ParseTreeNode node)
        {
            TaskStack.Push(new QOperand(currentBlock, node));
        }
        private void PushTask(QOperand op)
        {
            TaskStack.Push(op);
        }
        private void PushValue(QCodeBlock currentBlock, object value)
        {
            ValueStack.Push(new QOperand(currentBlock, value));
        }
        private void PushValue(QOperand op)
        {
            ValueStack.Push(op);
        }

        #endregion

        #region TypeDef
        private class QOperand
        {
            public readonly bool IsParseNode;
            public object ObjectValue { private set; get; }
            public ParseTreeNode ObjectNode
            {
                get
                {
                    return ObjectValue as ParseTreeNode;
                }
            }

            private IEnumerable<QOperand> __operandsList;
            public IEnumerable<QOperand> SetOperands
            {
                set
                {
                    __operandsList = value;
                    GetOperands = __operandsList.GetEnumerator();
                }
            }

            public IEnumerator<QOperand> GetOperands { get; private set; }
            public readonly QCodeBlock CurrentBlock;

            public QOperand(QCodeBlock export, object value)
            {
                if (value is ParseTreeNode) IsParseNode = true;
                ObjectValue = value;
                CurrentBlock = export;
            }

            public QOperand Replace(int index)
            {
                ObjectValue = ObjectNode.ChildNodes[index];
                return this;
            }
        }
        #endregion
    }
}
