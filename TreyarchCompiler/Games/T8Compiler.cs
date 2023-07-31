using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T89CompilerLib;
using T89CompilerLib.OpCodes;
using T89CompilerLib.ScriptComponents;
using TreyarchCompiler.Enums;
using TreyarchCompiler.Interface;
using TreyarchCompiler.Utilities;

using static T89CompilerLib.VMREVISIONS;
using ExportFlags = T89CompilerLib.ScriptExportFlags;
using ScriptOpCode = T89CompilerLib.OpCodes.ScriptOpCode;
using ImportFlags = T89CompilerLib.ScriptComponents.T89Import.T89ImportFlags;

// todo<side> Class Method Call

namespace TreyarchCompiler.Games
{
    internal class T89Compiler : BLOPSCompilerBase, ICompiler
    {
        private const string CALL_PTR_TERMNAME = "baseCallPointer";

        private readonly Dictionary<string, ParseTreeNode> Macros = new Dictionary<string, ParseTreeNode>();
        private readonly Dictionary<string, ScriptFunctionMetaData> FunctionMetadata;
        private readonly Stack<QOperand> ScriptOperands = new Stack<QOperand>();

        private readonly Enums.Games Game;
        private T89ScriptObject Script;
        private uint ScriptNamespace = 0x30FCC2BF;

        public T89Compiler(Enums.Games game, string code)
        {
            Game = game;
            _tree = NewSyntax.ThreadSafeInstance.SyntaxParser.Parse(code);
            Script = new T89ScriptObject(VM_36);
            Script.Header.ScriptName = Script.T8s64Hash("scripts/core_common/clientids_shared.gsc");
            FunctionMetadata = new Dictionary<string, ScriptFunctionMetaData>();
        }

        public CompiledCode Compile()
        {
            var ticks = DateTime.Now.Ticks;
            var data = new CompiledCode();
            try { CompileTree(); }
            catch (Exception ex)
            {
                data.Error = ex.Message;
                return data;
            }
            var assemble_ticks = DateTime.Now.Ticks;
            try
            {
                data.RequiresGSI = Script.UsingGSI;
                data.CompiledScript = Script.Serialize();
                data.HashMap = Script.GetHashMap();
            } 
            catch (Exception ex) { data.Error = ex.Message; }
            var finalticks = DateTime.Now.Ticks;
            //Temporary debugging stats to keep track of compiler speed
            Console.WriteLine($"{ TimeSpan.FromTicks(finalticks - ticks).TotalMilliseconds } ms compile time (excluding irony)");
            Console.WriteLine($" -- { TimeSpan.FromTicks(assemble_ticks - ticks).TotalMilliseconds } ms to build the structure.");
            Console.WriteLine($" -- { TimeSpan.FromTicks(finalticks - assemble_ticks).TotalMilliseconds } ms to commit to binary.");
            //End of temp debugging stats
            return data;
        }

        public CompiledCode Compile(string address)
        {
            throw new NotImplementedException();
        }

        private byte GetAutoExecByVM()
        {
            return (byte)ExportFlags.AutoExec;
        }

        private byte GetPrivateByVM()
        {
            return (byte)ExportFlags.Private;
        }

        private void CompileTree()
        {
            if (_tree.HasErrors()) throw new Exception($"Syntax error in input script! [line={_tree.ParserMessages[0].Location.Line}]");
            if (_tree.Root.ChildNodes[0].ChildNodes.Count <= 0) return;
            var functionTree = new Dictionary<string, ParseTreeNode>();
            SetNamespace();
            foreach (var directive in _tree.Root.ChildNodes[0].ChildNodes[0].
                ChildNodes.OrderBy(x => x.ChildNodes[0].Term.Name.ToLower() == "functions"))
            {
                byte flags = GetPrivateByVM();
                var FunctionFrame = directive;
                switch (directive.ChildNodes[0].Term.Name.ToLower())
                {
                    case "includes":
                        var node = directive.ChildNodes[0];
                        var str = node.ChildNodes[0].Token.ValueString.ToLower().Replace("\\", "/");
                        if (!str.StartsWith("script_"))
                        {
                            if (node.ChildNodes[1].FindToken() is null)
                            {
                                str += ".gsc";
                            }
                            else
                            {
                                str += node.ChildNodes[1].FindToken().ValueString.ToLower();
                            }
                        }
                        Script.Includes.Add(Script.T8s64Hash(str));
                        break;

                    case "globals":
                        Macros[directive.ChildNodes[0].ChildNodes[0].FindTokenAndGetText().ToLower()] = directive.ChildNodes[0].ChildNodes[2];
                        break;

                    case "functionframe":
                        FunctionFrame = directive.ChildNodes[0];
                        if(FunctionFrame.ChildNodes[0].Term.Name == "autoexec") flags |= GetAutoExecByVM();
                        goto functionsLabel;

                    case "functions":
                    functionsLabel:
                        var function = FunctionFrame.ChildNodes[FunctionFrame.ChildNodes.Count - 1];
                        var functionName = function.ChildNodes[function.ChildNodes.FindIndex(e => e.Term.Name == "identifier")].Token.ValueString.ToLower();
                        var Parameters = function.ChildNodes[function.ChildNodes.FindIndex(e => e.Term.Name == "parameters")].ChildNodes[0].ChildNodes;
                        if (FunctionMetadata.ContainsKey(functionName)) throw new ArgumentException($"Function '{functionName}' has been defined more than once.");
                        functionTree.Add(functionName, function);
                        FunctionMetadata[functionName] = new ScriptFunctionMetaData()
                        {
                            FunctionHash = Script.T8Hash(functionName),
                            NamespaceHash = ScriptNamespace,
                            FunctionName = functionName,
                            NamespaceName = "treyarch",
                            NumParams = (byte)Parameters.Count,
                            Flags = flags
                        };
                        break;

                    case "functiondetour":
                        var detour = directive.ChildNodes[0];
                        var local_detour_target = "detour_" + Guid.NewGuid().ToString().ToLower();
                        var detour_parameters = detour.ChildNodes[detour.ChildNodes.FindIndex(e => e.Term.Name == "parameters")].ChildNodes[0].ChildNodes;
                        functionTree.Add(local_detour_target, detour);
                        FunctionMetadata[local_detour_target] = new ScriptFunctionMetaData()
                        {
                            FunctionHash = Script.T8Hash(local_detour_target),
                            NamespaceHash = ScriptNamespace,
                            FunctionName = local_detour_target,
                            NamespaceName = "ilcustom",
                            NumParams = (byte)detour_parameters.Count,
                            Flags = 0, // detours are not private
                            IsDetour = true
                        };

                        var detourPathIndex = detour.ChildNodes.FindIndex(e => e.Term.Name == "detourPath");
                        string detourFunc = detour.ChildNodes[detourPathIndex + 1].Token.ValueString.ToLower();
                        string detourNamespace = "";
                        string detourScript = null;
                        if (detour.ChildNodes[detourPathIndex].ChildNodes[0].Term.Name == "gscForFunction")
                        {
                            detourNamespace = detour.ChildNodes[detourPathIndex].ChildNodes[0].ChildNodes[0].Token.ValueString.ToLower();
                        }
                        else
                        {
                            detourNamespace = detour.ChildNodes[detourPathIndex].ChildNodes[0].Token.ValueString.ToLower();
                            detourScript = detour.ChildNodes[detourPathIndex].ChildNodes[2].Token.ValueString.ToLower();

                            string exten = detour.ChildNodes[detourPathIndex].ChildNodes[3].Token.ValueString.ToLower();
                            if(exten.Contains("."))
                            {
                                detourScript += exten;
                            }

                            detourScript = detourScript.Replace("\\", "/");
                        }

                        Script.AddScriptDetour(local_detour_target, detourNamespace, detourFunc, Script.T8s64Hash(detourScript));
                        break;
                }
            }
            //Iterate over all function declarations
            foreach (var item in functionTree)
            {
                _currentDeclaration = item.Key;
                EmitFunction(item.Value, item.Key);
            }
        }

        private void SetNamespace()
        {
            if (_tree.Root.ChildNodes[0].ChildNodes.Count <= 0) return;
            var directive = _tree.Root.ChildNodes[0].ChildNodes[0].ChildNodes.Find(x => x.ChildNodes[0].Term.Name.ToLower() == "namespace");
            if (directive != null) ScriptNamespace = Script.T8Hash(directive.ChildNodes[0].ChildNodes[1].FindTokenAndGetText());
        }

        private void EmitFunction(ParseTreeNode functionNode, string FunctionName)
        {
            var Parameters = functionNode.ChildNodes[functionNode.ChildNodes.FindIndex(e => e.Term.Name == "parameters")].ChildNodes[0].ChildNodes;
            var CurrentFunction = Script.Exports.Add(FunctionMetadata[FunctionName].FunctionHash, FunctionMetadata[FunctionName].NamespaceHash, FunctionMetadata[FunctionName].NumParams);
            CurrentFunction.Flags = FunctionMetadata[FunctionName].Flags;
            CurrentFunction.FriendlyName = FunctionName;
            foreach (var paramNode in Parameters) AddLocal(CurrentFunction, paramNode.FindTokenAndGetText());
            IEnumerable<string> locals = CollectLocalVariables(CurrentFunction, functionNode.ChildNodes[functionNode.ChildNodes.FindIndex(e => e.Term.Name == "block")], false);
            foreach(var variable in locals) AddLocal(CurrentFunction, variable);
            ScriptOperands.Clear();
            EmitOptionalParameters(CurrentFunction, Parameters);
            ScriptOperands.Clear();
            Push(CurrentFunction, functionNode.ChildNodes[functionNode.ChildNodes.FindIndex(e => e.Term.Name == "block")], 0);
            IterateStack();
            CurrentFunction.AddOp(ScriptOpCode.End);
        }

        private void AddEvalLocal(T89ScriptExport CurrentFunction, string pname, bool IsRef)
        {
            CurrentFunction.AddEvalLocal(pname, Script.T8Hash(pname), IsRef);
        }

        private void AddAssignLocal(T89ScriptExport CurrentFunction, string pname)
        {
            CurrentFunction.AddAssignLocal(pname, Script.T8Hash(pname));
        }

        private void AddEvalLocalDefined(T89ScriptExport CurrentFunction, string pname)
        {
            CurrentFunction.AddEvalLocalDefined(pname, Script.T8Hash(pname));
        }

        private void AddLocal(T89ScriptExport CurrentFunction, string LocalName)
        {
            CurrentFunction.Locals.AddLocal(Script.T8Hash(LocalName));
        }

        private void Push(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context)
        {
            ScriptOperands.Push(new QOperand(CurrentFunction, node, Context));
        }

        private void Push(QOperand op)
        {
            ScriptOperands.Push(op);
        }

        private void PushObject(object o)
        {
            ScriptOperands.Push(new QOperand(null, o, 0));
        }

        private IEnumerable<string> CollectLocalVariables(T89ScriptExport CurrentFunction, ParseTreeNode node, bool AllowIDCollection)
        {
            Stack<ParseTreeNode> Remaining = new Stack<ParseTreeNode>();
            Remaining.Push(node);
            while (Remaining.Count > 0)
            {
                node = Remaining.Pop();
                foreach (var childNode in node.ChildNodes)
                {
                    switch (childNode.Term.Name)
                    {
                        case "identifier":
                            if (AllowIDCollection) yield return childNode.FindTokenAndGetText().ToLower();
                            break;

                        case "setVariableField":
                            if (childNode.ChildNodes[0].ChildNodes[0].Term.Name.Contains("identifier"))
                                yield return childNode.FindTokenAndGetText().ToLower();
                            break;

                        case "foreachSingle":
                        case "foreachDouble":
                            var array = Guid.NewGuid().ToString().ToLower(); //iterator
                            var iterator = Guid.NewGuid().ToString().ToLower(); //iterator
                            var key = Guid.NewGuid().ToString().ToLower(); //key
                            var NextKey = Guid.NewGuid().ToString().ToLower(); //NextKey
                            CurrentFunction.PushFEKeys(array, iterator, key, NextKey);
                            yield return array;
                            yield return iterator;
                            yield return key;
                            yield return NextKey;
                            if (childNode.Term.Name == "foreachDouble")
                                yield return childNode.ChildNodes[childNode.ChildNodes.FindIndex(e => e.Term.Name.ToLower() == "key")].FindTokenAndGetText().ToLower();
                            yield return childNode.ChildNodes[childNode.ChildNodes.FindIndex(e => e.Term.Name.ToLower() == "value")].FindTokenAndGetText().ToLower();
                            break;

                        case "switchStatement":
                            key = Guid.NewGuid().ToString().ToLower();
                            CurrentFunction.PushSwitchKey(key);
                            yield return key;
                            break;
                    }
                    Remaining.Push(childNode);
                }
            }
            yield break;
        }

        private void EmitOptionalParameters(T89ScriptExport CurrentFunction, ParseTreeNodeList Params)
        {
            foreach (var node in Params)
            {
                if (node.ChildNodes[0].Term.Name != "setOptionalParam") continue;
                var optional = node.ChildNodes[0];
                string pname = optional.ChildNodes[0].FindTokenAndGetText().ToLower();
                AddEvalLocalDefined(CurrentFunction, pname);
                var __jmp = CurrentFunction.AddJump(ScriptOpCode.JumpOnTrue);
                Push(CurrentFunction, optional.ChildNodes[2], 0);
                IterateStack();
                AddAssignLocal(CurrentFunction, pname);
                __jmp.After = CurrentFunction.Locals.GetEndOfChain();
            }
        }

        private void IterateStack()
        {
            while (ScriptOperands.Count > 0)
            {
                var CurrentOp = ScriptOperands.Pop();
                if (!CurrentOp.IsParseNode) continue; //Stack misalignment
                var node = CurrentOp.ObjectNode;
                var CurrentFunction = CurrentOp.CurrentFunction;
                var Context = CurrentOp.Context;
                if (CurrentOp.GetOperands != null)
                {
                    if (!CurrentOp.GetOperands.MoveNext()) continue;
                    Push(CurrentOp);
                    Push(CurrentOp.GetOperands.Current);
                    continue;
                }

                switch (node.Term.Name)
                {
                    case "statement":
                    case "statementBlock":
                    case "declaration":
                    case "parenExpr":
                    case "block":
                        if (node.ChildNodes.Count > 0) Push(CurrentOp.Replace(0));
                        break;

                    case "blockContent":
                        foreach (var child in node.ChildNodes[0].ChildNodes.AsEnumerable().Reverse()) Push(CurrentFunction, child, Context);
                        break;

                    case "jumpStatement":
                        int offset = 1;
                        if (node.ChildNodes.Count > 1) offset = (int)node.ChildNodes[1].Token.Value;
                        offset = Math.Max(1, offset);
                        offset--;
                        if (node.ChildNodes[0].Term.Name == "continue") CurrentFunction.PushLCF(true, offset);
                        else CurrentFunction.PushLCF(false, offset);
                        break;

                    case "newArray":
                        CurrentFunction.AddOp(ScriptOpCode.GetEmptyArray);
                        break;

                    case "array":
                        CurrentOp.SetOperands = EmitArray(CurrentOp);
                        Push(CurrentOp);
                        break;

                    case "ifStatement":
                        int count = node.ChildNodes.Count;
                        CurrentOp.SetOperands = EmitConditionalJump(CurrentFunction, node.ChildNodes[1], node.ChildNodes[2], count == 4 ? node.ChildNodes[3].ChildNodes[1] : null);
                        Push(CurrentOp);
                        break;

                    case "whileStatement":
                        CurrentOp.SetOperands = EmitWhile(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "forStatement":
                        CurrentOp.SetOperands = EmitForLoop(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "conditionalStatement":
                        CurrentOp.SetOperands = EmitConditionalJump(CurrentFunction, node.ChildNodes[0], node.ChildNodes[2], node.ChildNodes[4]);
                        Push(CurrentOp);
                        break;

                    case "switchStatement":
                        CurrentOp.SetOperands = EmitSwitchStatement(CurrentFunction, node);
                        Push(CurrentOp);
                        break;

                    case "simpleCall":
                        Push(CurrentFunction, node.ChildNodes[0], (uint)ScriptContext.DecTop);
                        break;

                    case "call":
                        CurrentOp.SetOperands = EmitCall(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "wait":
                        CurrentOp.SetOperands = EmitWaitOfType(CurrentFunction, node, Context, ScriptOpCode.Wait);
                        Push(CurrentOp);
                        break;

                    case "waitframe":
                        CurrentOp.SetOperands = EmitWaitOfType(CurrentFunction, node, Context, ScriptOpCode.WaitFrame);
                        Push(CurrentOp);
                        break;

                    case "return":
                        CurrentOp.SetOperands = EmitReturn(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "waitTillFrameEnd":
                        if (node.ChildNodes[0].Token.ValueString == "waittillframeend") CurrentFunction.AddOp(ScriptOpCode.WaitTillFrameEnd);
                        break;

                    case "setVariableField":
                        CurrentOp.SetOperands = EmitSetVariableField(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "directAccess":
                        CurrentOp.SetOperands = EmitEvalFieldVariable(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "stackAccess":
                        CurrentOp.SetOperands = EmitEvalStackVariable(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "foreachSingle":
                    case "foreachDouble":
                        CurrentOp.SetOperands = EmitForeach(CurrentFunction, node);
                        Push(CurrentOp);
                        break;

                    case "booleanExpression":
                        CurrentOp.SetOperands = EmitBoolExpr(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "boolNot":
                        CurrentOp.SetOperands = EmitBoolNot(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "size":
                        CurrentOp.SetOperands = EmitSizeof(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "relationalExpression":
                        CurrentOp.SetOperands = EmitRelationalExpression(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "include_identifier":
                    case "identifier":
                        string LocalToLower = node.Token.ValueString.ToLower();
                        if (Macros.TryGetValue(LocalToLower, out ParseTreeNode MacroNode)) Push(CurrentFunction, MacroNode, Context);
                        else AddEvalLocal(CurrentFunction, LocalToLower, HasContext(Context, ScriptContext.IsRef));
                        break;

                    case "stringLiteral":
                        CurrentFunction.AddGetString(Script.Strings.AddString(node.Token.ValueString));
                        break;

                    case "hashedString":
                        CurrentFunction.AddGetHash(Script.T8s64Hash(node.ChildNodes[1].Token.ValueString));
                        break;

                    case "hashedVariable":
                        CurrentFunction.AddGetHash(Script.T8Hash(node.ChildNodes[1].FindTokenAndGetText()));
                        break;

                    case "numberLiteral":
                        CurrentFunction.AddGetNumber(node.Token.Value);
                        break;

                    case "expression+":
                    case "expression":
                        CurrentOp.SetOperands = EmitExpression(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "getFunction":
                        EmitFunctionPtr(CurrentFunction, node, 0);
                        break;

                    case "vector":
                        CurrentOp.SetOperands = EmitVector(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;
                    case "lazyFunction":
                        EmitLazyFunctionPtr(CurrentFunction, node);
                        break;

                    case "shortHandArray":
                        CurrentOp.SetOperands = EmitArraySH(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "shortHandStruct":
                        CurrentOp.SetOperands = EmitStructSH(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    default:
                        foreach (var child in node.ChildNodes.AsEnumerable().Reverse()) Push(CurrentFunction, child, Context);
                        break;
                }
            }
        }

        private IEnumerable<QOperand> EmitArraySH(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context)
        {
            var array_assignments = node.ChildNodes[0].ChildNodes;
            CurrentFunction.AddOp(ScriptOpCode.GetEmptyArray);
            foreach(var ass in array_assignments)
            {
                yield return new QOperand(CurrentFunction, ass.ChildNodes[2], 0);
                yield return new QOperand(CurrentFunction, ass.ChildNodes[0], 0);
                CurrentFunction.AddOp(ScriptOpCode.AddToArray);
            }
        }

        private IEnumerable<QOperand> EmitStructSH(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context)
        {
            var array_assignments = node.ChildNodes[0].ChildNodes;
            CurrentFunction.AddOp(ScriptOpCode.CreateStruct);
            foreach (var ass in array_assignments)
            {
                yield return new QOperand(CurrentFunction, ass.ChildNodes[2], 0);
                yield return new QOperand(CurrentFunction, ass.ChildNodes[0], 0);
                CurrentFunction.AddOp(ScriptOpCode.AddToStruct);
            }
        }

        private IEnumerable<QOperand> EmitConditionalJump(T89ScriptExport CurrentFunction, ParseTreeNode BoolExpr, ParseTreeNode BlockContent, ParseTreeNode SecondBlock = null)
        {
            foreach (var entry in EmitBoolExpr(CurrentFunction, BoolExpr, 0)) yield return entry;
            var __if_jmp = CurrentFunction.AddJump(ScriptOpCode.JumpOnFalse);
            yield return new QOperand(CurrentFunction, BlockContent, 0);
            if (SecondBlock != null)
            {
                var __else_jmp = CurrentFunction.AddJump(ScriptOpCode.Jump);
                __if_jmp.After = CurrentFunction.Locals.GetEndOfChain();

                yield return new QOperand(CurrentFunction, SecondBlock, 0);
                __else_jmp.After = CurrentFunction.Locals.GetEndOfChain();
            }
            else
            {
                __if_jmp.After = CurrentFunction.Locals.GetEndOfChain();
            }
        }

        private IEnumerable<QOperand> EmitBoolExpr(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context)
        {
            switch (node.ChildNodes.Count)
            {
                case 0:
                    yield break;

                case 1:
                    yield return new QOperand(CurrentFunction, node.ChildNodes[0], Context);
                    yield break;

                case 3:
                    yield return new QOperand(CurrentFunction, node.ChildNodes[0], 0);
                    dynamic target = node.ChildNodes[1].Term.Name == "&&" ? ScriptOpCode.JumpOnFalseExpr : ScriptOpCode.JumpOnTrueExpr;
                    dynamic __jmp = CurrentFunction.AddJump(target);
                    yield return new QOperand(CurrentFunction, node.ChildNodes[2], 0);
                    CurrentFunction.AddOp(ScriptOpCode.CastBool); // used to clean the stack after an expr
                    __jmp.After = CurrentFunction.Locals.GetEndOfChain();
                    yield break;

                default: throw new NotImplementedException($"Boolean expression contained an unhandled number of childnodes ({node.ChildNodes.Count})");
            }
        }
        private void EnterLoop(T89ScriptExport CurrentFunction)
        {
            CurrentFunction.IncLCFContext();
        }

        private void ExitLoop(T89ScriptExport CurrentFunction, T89OpCode Header, T89OpCode Footer)
        {
            while (CurrentFunction.TryPopLCF(out T89OP_Jump __lcf)) __lcf.After = __lcf.RefHead ? Header : Footer;
            CurrentFunction.DecLCFContext();
        }

        private IEnumerable<QOperand> EmitWhile(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context)
        {
            EnterLoop(CurrentFunction);
            var __backref = CurrentFunction.Locals.GetEndOfChain();
            foreach (var v in EmitBoolExpr(CurrentFunction, node.ChildNodes[1], 0)) yield return v;
            var __while_jmp = CurrentFunction.AddJump(ScriptOpCode.JumpOnFalse);
            yield return new QOperand(CurrentFunction, node.ChildNodes[2], 0);
            var __while_jmp_back = CurrentFunction.AddJump(ScriptOpCode.Jump);
            __while_jmp.After = __while_jmp_back;
            __while_jmp_back.After = __backref;
            ExitLoop(CurrentFunction, __backref, __while_jmp_back);
        }

        private IEnumerable<QOperand> EmitForLoop(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context)
        {
            EnterLoop(CurrentFunction);
            ParseTreeNode Header = node.ChildNodes[1];
            int SetVarIndex = Header.ChildNodes.FindIndex(e => e.Term.Name == "setVariableField");
            int BoolExprIndex = Header.ChildNodes.FindIndex(e => e.Term.Name == "booleanExpression");
            int IterateIndex = Header.ChildNodes.FindIndex(e => e.Term.Name == "forIterate");
            if (SetVarIndex != -1) yield return new QOperand(CurrentFunction, Header.ChildNodes[SetVarIndex], 0);
            var __header = CurrentFunction.Locals.GetEndOfChain();
            T89OP_Jump __jmp = null;
            if (BoolExprIndex != -1)
            {
                foreach (var val in EmitBoolExpr(CurrentFunction, Header.ChildNodes[BoolExprIndex], 0)) yield return val;
                __jmp = CurrentFunction.AddJump(ScriptOpCode.JumpOnFalse);
            }
            yield return new QOperand(CurrentFunction, node.ChildNodes[2], 0);
            var __ctheader = CurrentFunction.Locals.GetEndOfChain();
            if (IterateIndex != -1) 
                foreach(var val in EmitSetVariableField(CurrentFunction, Header.ChildNodes[IterateIndex], Context)) yield return val;
            var __bottomjump = CurrentFunction.AddJump(ScriptOpCode.Jump);
            __bottomjump.After = __header;
            if (__jmp != null) __jmp.After = __bottomjump;
            ExitLoop(CurrentFunction, __ctheader, __bottomjump);
        }

        private IEnumerable<QOperand> EmitSwitchStatement(T89ScriptExport CurrentFunction, ParseTreeNode node)
        {
            if (!CurrentFunction.TryPopSwitchKey(out string SWKey)) throw new InvalidOperationException("Tried to compile more switch statements than were expected");
            ParseTreeNodeList SwitchContentsArray = node.ChildNodes[2].ChildNodes;
            EnterLoop(CurrentFunction);
            yield return new QOperand(CurrentFunction, node.ChildNodes[1], 0);
            AddAssignLocal(CurrentFunction, SWKey);
            ParseTreeNode DefaultNode = null;
            foreach (var _node in SwitchContentsArray)
            {
                if (_node.ChildNodes[0].ChildNodes[0].Term.Name.ToLower() == "default")
                {
                    DefaultNode = _node;
                    break;
                }
            }
            if (DefaultNode != null)
            {
                SwitchContentsArray.Remove(DefaultNode);
                SwitchContentsArray.Add(DefaultNode);
            }
            List<dynamic> __orjumps = new List<dynamic>();
            foreach (var _node in SwitchContentsArray)
            {
                if (_node == DefaultNode)
                {
                    foreach (var jmp in __orjumps) jmp.After = CurrentFunction.Locals.GetEndOfChain();
                    __orjumps.Clear();
                    if (DefaultNode.ChildNodes.Count > 1) yield return new QOperand(CurrentFunction, DefaultNode.ChildNodes[1], 0);
                    break;
                }
                yield return new QOperand(CurrentFunction, _node.ChildNodes[0].ChildNodes[1], 0);
                AddEvalLocal(CurrentFunction, SWKey, false);
                CurrentFunction.AddCompareOp("==");
                if (_node.ChildNodes.Count > 1)
                {
                    var __jmp = CurrentFunction.AddJump(ScriptOpCode.JumpOnFalse);
                    foreach (var jmp in __orjumps) jmp.After = CurrentFunction.Locals.GetEndOfChain();
                    __orjumps.Clear();
                    yield return new QOperand(CurrentFunction, _node.ChildNodes[1], 0);
                    __jmp.After = CurrentFunction.Locals.GetEndOfChain();
                }
                else
                {
                    var __jmp = CurrentFunction.AddJump(ScriptOpCode.JumpOnTrue);
                    __orjumps.Add(__jmp);
                }
            }
            ExitLoop(CurrentFunction, null, CurrentFunction.Locals.GetEndOfChain());
        }

        private IEnumerable<QOperand> EmitCall(T89ScriptExport CurrentFunction, ParseTreeNode callNode, uint Context)
        {
            ParseTreeNode CallFrame = callNode.ChildNodes[callNode.ChildNodes.Count - 1];
            ParseTreeNode BaseCall = CallFrame.ChildNodes[0];
            ParseTreeNode CallPrefix = callNode.ChildNodes.Count > 1 ? callNode.ChildNodes[0] : null;
            ParseTreeNode Caller = null;
            string function_name = BaseCall.ChildNodes[BaseCall.ChildNodes.Count - 2].FindTokenAndGetText().ToLower();
            string NS_String = null;
            uint fhash = Script.T8Hash(function_name);
            if (BaseCall.ChildNodes.Count == 3) NS_String = BaseCall.ChildNodes[0].FindTokenAndGetText();
            ParseTreeNode CallParameters = BaseCall.ChildNodes[BaseCall.ChildNodes.Count - 1].ChildNodes[0];
            ParseTreeNodeList parameters = CallParameters.ChildNodes;

            //Our context should update if we have a prefix
            if (CallPrefix != null)
            {
                if (CallPrefix.ChildNodes[0].Term.Name == "expr")
                {
                    Context |= (uint)ScriptContext.HasCaller;
                    Caller = CallPrefix.ChildNodes[0];
                }
                if (CallPrefix.ChildNodes[CallPrefix.ChildNodes.Count - 1].Term.Name == "thread") Context |= (uint)ScriptContext.Threaded;
            }

            //Update the context if we are using a call pointer term
            if (CallFrame.ChildNodes[0].Term.Name == CALL_PTR_TERMNAME) Context |= (uint)ScriptContext.IsPointer;
            if (!HasContext(Context, ScriptContext.IsPointer) && !HasContext(Context, ScriptContext.Threaded) && NS_String == null)
            {
                if(T89ScriptExport.IsBuiltinMethod(function_name))
                {
                    object result;
                    if (HasContext(Context, ScriptContext.HasCaller))
                    {
                        foreach (var val in EmitNotifierCall(CurrentFunction, CallPrefix, BaseCall, HasContext(Context, ScriptContext.DecTop)))
                        {
                            yield return val;
                        }
                        result = ScriptOperands.Pop().ObjectValue;
                    }
                    else
                    {
                        parameters.Reverse();
                        foreach (ParseTreeNode parameter in parameters)
                        {
                            yield return new QOperand(CurrentFunction, parameter, 0);
                        }
                        result = CurrentFunction.TryAddBuiltInCall(BaseCall.ChildNodes[0].Token.ValueString.ToLower());
                    }
                    if (result != null) yield break;
                    throw new NotImplementedException($"Call to builtin method '{BaseCall.ChildNodes[0].Token.ValueString}' has not been handled!");
                }
            }

            CurrentFunction.AddOp(ScriptOpCode.PreScriptCall);

            parameters.Reverse();
            foreach (ParseTreeNode parameter in parameters)
            {
                yield return new QOperand(CurrentFunction, parameter, 0);
            }

            uint t8_ns = NS_String != null ? Script.T8Hash(NS_String) : ScriptNamespace;
            bool isCustomBuiltin = Script.T8Hash("compiler") == t8_ns;
            int paramCount = parameters.Count;

            if (isCustomBuiltin)
            {
                CurrentFunction.AddGetNumber((int)fhash);
                t8_ns = ScriptNamespace;
                fhash = Script.T8Hash("isprofilebuild");
                paramCount++;
            }

            if (HasContext(Context, ScriptContext.HasCaller))
            {
                yield return new QOperand(CurrentFunction, Caller, 0);
            }

            if (HasContext(Context, ScriptContext.IsPointer))
            {
                yield return new QOperand(CurrentFunction, CallFrame.ChildNodes[0].ChildNodes[0], 0);
                CurrentFunction.AddCallPtr(Context, (byte)paramCount);
            }
            else
            {
                byte Flags = 0;
                
                if (t8_ns == ScriptNamespace) Flags |= (byte)ImportFlags.NeedsResolver;

                if(HasContext(Context, ScriptContext.HasCaller))
                {
                    if (HasContext(Context, ScriptContext.Threaded))
                    {
                        Flags |= 6;
                    }
                    else
                    {
                        Flags |= 5;
                    }
                }
                else
                {
                    if(HasContext(Context, ScriptContext.Threaded))
                    {
                        Flags |= 3;
                    }
                    else
                    {
                        Flags |= 2; //script function call
                    }
                }                
                var import = Script.Imports.AddImport(fhash, t8_ns, (byte)paramCount, Flags);
                CurrentFunction.AddCall(import, Context);
            }
            if (HasContext(Context, ScriptContext.DecTop)) CurrentFunction.AddOp(ScriptOpCode.DecTop);
        }

        private IEnumerable<QOperand> EmitNotifierCall(T89ScriptExport CurrentFunction, ParseTreeNode CallPrefix, ParseTreeNode BaseCall, bool HasDectop)
        {
            ParseTreeNode CallParameters = BaseCall.ChildNodes[1].ChildNodes[0];
            ParseTreeNodeList parameters = CallParameters.ChildNodes;
            string strnotify = BaseCall.ChildNodes[0].Token.ValueString.ToLower();
            switch (strnotify)
            {
                case "notify":
                    parameters.Reverse();
                    CurrentFunction.AddOp(ScriptOpCode.PreScriptCall);
                    foreach (ParseTreeNode parameter in parameters) yield return new QOperand(CurrentFunction, parameter, 0);
                    foreach (var val in EmitObject(CurrentFunction, CallPrefix, 0)) yield return val;
                    PushObject(CurrentFunction.AddOp(ScriptOpCode.Notify));
                    yield break;
                case "endon_callback_a":
                case "endoncallback":
                case "endon":
                    parameters.Reverse();
                    foreach (ParseTreeNode parameter in parameters) yield return new QOperand(CurrentFunction, parameter, 0);
                    foreach (var val in EmitObject(CurrentFunction, CallPrefix, 0)) yield return val;
                    PushObject(CurrentFunction.AddNotification(strnotify, (byte)parameters.Count));
                    yield break;
                case "waittill_timeout_s":
                case "waittilltimeout":
                case "waittill":
                case "waittillmatch":
                    parameters.Reverse();
                    foreach (ParseTreeNode parameter in parameters) yield return new QOperand(CurrentFunction, parameter, 0);
                    foreach (var val in EmitObject(CurrentFunction, CallPrefix, 0)) yield return val;
                    PushObject(CurrentFunction.AddNotification(strnotify, (byte)parameters.Count));
                    if(HasDectop) CurrentFunction.AddOp(ScriptOpCode.DecTop);
                    yield break;
            }
            throw new ArgumentException($"{BaseCall.ChildNodes[0].Token.ValueString.ToLower()} was passed to EmitNotifierCall, but isnt a valid notifier");
        }

        private IEnumerable<QOperand> EmitObject(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context)
        {
            if (node.Token != null)
            {
                var op = CurrentFunction.TryAddBuiltIn(node.Token.ValueString, HasContext(Context, ScriptContext.IsRef));
                if (op != null) yield break;
            }
            yield return new QOperand(CurrentFunction, node, 0);// HasContext(Context, ScriptContext.IsRef) ? (byte)ScriptContext.IsRef : (byte)0);
            if (HasContext(Context, ScriptContext.IsRef)) CurrentFunction.AddOp(ScriptOpCode.CastFieldObject);
        }

        private IEnumerable<QOperand> EmitWaitOfType(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context, ScriptOpCode code)
        {
            yield return new QOperand(CurrentFunction, node.ChildNodes[1], 0);
            CurrentFunction.AddOp(code);
        }

        private IEnumerable<QOperand> EmitReturn(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context)
        {
            if (node.ChildNodes.Count > 1)
            {
                yield return new QOperand(CurrentFunction, node.ChildNodes[1], 0);
                CurrentFunction.AddOp(ScriptOpCode.Return);
            }
            else
            {
                CurrentFunction.AddOp(ScriptOpCode.End);
            }
        }

        private IEnumerable<QOperand> EmitEvalFieldVariable(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context)
        {
            foreach (var val in EmitObject(CurrentFunction, node.ChildNodes[0].ChildNodes[0], Context)) yield return val;
            AddFieldVariable(CurrentFunction, node.ChildNodes[1].FindTokenAndGetText(), Context);
        }

        private IEnumerable<QOperand> EmitEvalStackVariable(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context)
        {
            foreach (var val in EmitObject(CurrentFunction, node.ChildNodes[0].ChildNodes[0], Context | (byte)ScriptContext.IsRef)) yield return val;
            foreach (var val in EmitObject(CurrentFunction, node.ChildNodes[1], Context & ~(uint)ScriptContext.IsRef)) yield return val;
            AddStackVariable(CurrentFunction, Context);
        }

        private void AddFieldVariable(T89ScriptExport CurrentFunction, string FVIdentifier, uint Context)
        {
            CurrentFunction.AddFieldVariable(Script.T8Hash(FVIdentifier), Context);
        }

        private void AddStackVariable(T89ScriptExport CurrentFunction, uint Context)
        {
            CurrentFunction.AddStackVariable(Context);
        }

        private IEnumerable<QOperand> EmitBoolNot(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context)
        {
            yield return new QOperand(CurrentFunction, node.ChildNodes[1], 0);
            CurrentFunction.AddOp(ScriptOpCode.BoolNot);
        }

        private IEnumerable<QOperand> EmitSizeof(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context)
        {
            yield return new QOperand(CurrentFunction, node.ChildNodes[0], 0);
            CurrentFunction.AddOp(ScriptOpCode.SizeOf);
        }

        private IEnumerable<QOperand> EmitRelationalExpression(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context)
        {
            yield return new QOperand(CurrentFunction, node.ChildNodes[0], 0);
            yield return new QOperand(CurrentFunction, node.ChildNodes[2], 0);
            CurrentFunction.AddCompareOp(node.ChildNodes[1].ChildNodes[0].Term.Name);
        }

        private IEnumerable<QOperand> EmitExpression(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context)
        {
            yield return new QOperand(CurrentFunction, node.ChildNodes[0], 0);
            yield return new QOperand(CurrentFunction, node.ChildNodes[2], 0);
            CurrentFunction.AddMathToken(node.ChildNodes[1].Term.Name);
        }

        private IEnumerable<QOperand> EmitVector(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context)
        {
            yield return new QOperand(CurrentFunction, node.ChildNodes[2], 0);
            yield return new QOperand(CurrentFunction, node.ChildNodes[1], 0);
            yield return new QOperand(CurrentFunction, node.ChildNodes[0], 0);
            CurrentFunction.AddOp(ScriptOpCode.Vector);
        }

        private void EmitLazyFunctionPtr(dynamic CurrentFunction, ParseTreeNode node)
        {
            var ns = node.ChildNodes[1].Token.ValueString;
            var func = node.ChildNodes[node.ChildNodes.Count - 1].Token.ValueString;
            var script = node.ChildNodes[3].Token.ValueString.ToLower().Replace("\\", "/");
            if (!script.StartsWith("script_"))
            {
                // add the .csc/.gsc only if we aren't using the hashed value
                script += node.ChildNodes[4].Token.ValueString.ToLower();
            }
            CurrentFunction.AddLazyGetFunction(Script.T8s64Hash(script), Script.T8Hash(ns), Script.T8Hash(func));
        }

        private void EmitFunctionPtr(T89ScriptExport CurrentFunction, ParseTreeNode node, byte Numparams)
        {
            ParseTreeNode FuncNameNode = node.ChildNodes[node.ChildNodes.Count - 1];
            ParseTreeNode NSNode = null;
            if (node.ChildNodes.Count > 1 && node.ChildNodes[0].Term.Name == "gscForFunction") NSNode = node.ChildNodes[0].ChildNodes[1];
            uint t8_ns = ScriptNamespace;
            if (NSNode != null) t8_ns = Script.T8Hash(NSNode.FindTokenAndGetText());
            byte Flags = (byte)ImportFlags.IsRef;
            if(t8_ns == ScriptNamespace) Flags |= (byte)ImportFlags.NeedsResolver;
            string fname = FuncNameNode.ChildNodes[0].FindTokenAndGetText().ToLower();
            uint FunctionID = Script.T8Hash(fname);
            CurrentFunction.AddFunctionPtr(Script.Imports.AddImport(FunctionID, t8_ns, Numparams, Flags));
        }

        private IEnumerable<QOperand> EmitSetVariableField(T89ScriptExport CurrentFunction, ParseTreeNode node, uint Context)
        {
            if (node.ChildNodes[1].ChildNodes[0].Term.Name != "=" && node.ChildNodes.Count > 2)
            {
                yield return new QOperand(CurrentFunction, node.ChildNodes[0].ChildNodes[0], 0);
                yield return new QOperand(CurrentFunction, node.ChildNodes[2].ChildNodes[0], 0);
            }
            switch (node.ChildNodes[1].ChildNodes[0].Term.Name)
            {
                case "++":
                    yield return new QOperand(CurrentFunction, node.ChildNodes[0], Context | (uint)ScriptContext.IsRef);
                    CurrentFunction.AddOp(ScriptOpCode.Inc);
                    yield break;

                case "--":
                    yield return new QOperand(CurrentFunction, node.ChildNodes[0], Context | (uint)ScriptContext.IsRef);
                    CurrentFunction.AddOp(ScriptOpCode.Dec);
                    yield break;

                case "=":
                    yield return new QOperand(CurrentFunction, node.ChildNodes[2].ChildNodes[0], 0);
                    break;

                default:
                    CurrentFunction.AddMathToken(node.ChildNodes[1].ChildNodes[0].Term.Name[0].ToString());
                    break;
            }
            yield return new QOperand(CurrentFunction, node.ChildNodes[0].ChildNodes[0], Context | (uint)ScriptContext.IsRef);
            CurrentFunction.AddOp(ScriptOpCode.SetVariableField);
        }

        private IEnumerable<QOperand> EmitForeach(T89ScriptExport CurrentFunction, ParseTreeNode node)
        {
            return EmitForeach_36(CurrentFunction, node);
        }

        private IEnumerable<QOperand> EmitForeach_36(T89ScriptExport CurrentFunction, ParseTreeNode node)
        {
            if(!CurrentFunction.TryPopFEKeys(out string[] keys, 4)) throw new InvalidOperationException("Tried to compile more foreach statements than were expected");
            int KeyIndex = node.ChildNodes.FindIndex(e => e.Term.Name == "key");
            string _Key = KeyIndex != -1 ? node.ChildNodes[KeyIndex].FindTokenAndGetText().ToLower() : keys[2];
            string _Value = node.ChildNodes[node.ChildNodes.FindIndex(e => e.Term.Name == "value")].FindTokenAndGetText().ToLower();
            var _Array = keys[0];
            var _Iterator = keys[1];
            var _NextArrayKey = keys[3];
            // Allocate Array
            yield return new QOperand(CurrentFunction, node.ChildNodes[node.ChildNodes.FindIndex(e => e.Term.Name == "expr")], 0);
            AddAssignLocal(CurrentFunction, _Array);
            //Assign first array key
            CurrentFunction.AddAssignArrayKey(_Array, Script.T8Hash(_Array), true);
            AddAssignLocal(CurrentFunction, _Key);
            //Verify that the iterator is defined and start the loop
            EnterLoop(CurrentFunction);
            var __header = CurrentFunction.Locals.GetEndOfChain();
            AddEvalLocalDefined(CurrentFunction, _Key);
            var __jmp = CurrentFunction.AddJump(ScriptOpCode.JumpOnFalse);
            //Assign the value
            AddEvalLocal(CurrentFunction, _Key, false);
            AddEvalLocal(CurrentFunction, _Array, false);
            CurrentFunction.AddOp(ScriptOpCode.EvalArray);
            AddAssignLocal(CurrentFunction, _Value);
            //Assign next array key
            AddEvalLocal(CurrentFunction, _Key, false);
            AddEvalLocal(CurrentFunction, _Array, false);
            CurrentFunction.AddAssignArrayKey(_NextArrayKey, Script.T8Hash(_NextArrayKey), false);
            //Compile the body
            yield return new QOperand(CurrentFunction, node.ChildNodes[node.ChildNodes.Count - 1], 0);
            var __foreach_header = CurrentFunction.Locals.GetEndOfChain();
            //Assign iterator
            AddEvalLocal(CurrentFunction, _NextArrayKey, false);
            AddAssignLocal(CurrentFunction, _Key);
            //Exit the loop
            var __footer = CurrentFunction.AddJump(ScriptOpCode.Jump);
            __footer.After = __header;
            __jmp.After = __footer;
            ExitLoop(CurrentFunction, __foreach_header, __footer);
        }

        private IEnumerable<QOperand> EmitArray(QOperand CurrentOp)
        {
            var node = CurrentOp.ObjectNode;
            var CurrentFunction = CurrentOp.CurrentFunction;
            var Context = CurrentOp.Context;
            yield return new QOperand(CurrentFunction, node.ChildNodes[1], 0);
            yield return new QOperand(CurrentFunction, node.ChildNodes[0], Context);
            CurrentFunction.AddOp(HasContext(Context, ScriptContext.IsRef) ? ScriptOpCode.EvalArrayRef : ScriptOpCode.EvalArray);
        }

        private struct ScriptFunctionMetaData
        {
            public uint FunctionHash;
            public uint NamespaceHash;
            public string FunctionName;
            public string NamespaceName;
            public byte NumParams;
            public byte Flags;

            public bool IsDetour;
        }

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
            public readonly T89ScriptExport CurrentFunction;
            public readonly uint Context;

            public QOperand(T89ScriptExport export, object Value, uint context)
            {
                if (Value is ParseTreeNode) IsParseNode = true;
                ObjectValue = Value;
                CurrentFunction = export;
                Context = context;
            }

            public QOperand Replace(int index)
            {
                ObjectValue = ObjectNode.ChildNodes[index];
                return this;
            }
        }
        private bool HasContext(uint context, ScriptContext desired)
        {
            return (context & (uint)desired) > 0;
        }
    }
}
