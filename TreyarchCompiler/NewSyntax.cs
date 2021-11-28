using Irony.Parsing;

//From Serious: Never again :)

namespace TreyarchCompiler
{
    [Language("GSC4", "37", "by serious")]
    public class NewSyntax : Grammar
    {
        #region BaseThreadsafe
        public readonly Parser SyntaxParser;

        //public static Parser Parser => ThreadSafeInstance.SyntaxParser;
        private static readonly object InstanceLock = new object();
        private static NewSyntax instance;

        public static NewSyntax ThreadSafeInstance
        {
            get
            {
                lock (InstanceLock)
                {
                    return instance ?? (instance = new NewSyntax());
                }
            }
        }
        #endregion BaseThreadsafe

        #region Private

        #region Terminals
        protected NumberLiteral NumberLiteral { private set; get; }
        protected StringLiteral StringLiteral { private set; get; }
        protected IdentifierTerminal Identifier { private set; get; }
        protected KeyTerm Unsupported => ToTerm("~@#$@#$@#$@#$~", "UNSUPPORTED SYNTAX");
        protected NonTerminal includeExtension { private set; get; }
        #endregion Terminals

        #region Directives
        protected NonTerminal directives { private set; get; }
        protected NonTerminal directive { private set; get; }
        protected NonTerminal functions { private set; get; }
        protected NonTerminal globals { private set; get; }
        protected NonTerminal includes { private set; get; }
        protected NonTerminal functionDetour { private set; get; }
        protected NonTerminal detourPath { private set; get; }
        #endregion

        #region Boolean
        protected NonTerminal booleanExpression { private set; get; }
        protected NonTerminal booleanAndExpression { private set; get; }
        protected NonTerminal booleanOrExpression { private set; get; }
        protected NonTerminal boolNot { private set; get; }
        protected NonTerminal parenBooleanExpression { private set; get; }
        protected NonTerminal parenBoolOpsExpr { private set; get; }
        protected NonTerminal boolExprOperand { private set; get; }
        protected NonTerminal boolNotOperand { private set; get; }
        protected NonTerminal blorOp { private set; get; }
        protected NonTerminal boolOperand { private set; get; }
        protected NonTerminal conditionalStatement { private set; get; }
        #endregion

        #region Expressions
        protected NonTerminal expr { private set; get; }
        protected NonTerminal mathExpr { private set; get; }
        protected NonTerminal variableExpr { private set; get; }
        protected NonTerminal parenExpr { private set; get; }
        protected NonTerminal parenMathExpr { private set; get; }
        protected NonTerminal parenVariableExpr { private set; get; }
        protected NonTerminal setVariableFieldExpr { private set; get; }
        protected NonTerminal directAccess { private set; get; }
        protected NonTerminal array { private set; get; }
        protected NonTerminal newArray { private set; get; }
        protected NonTerminal vector { private set; get; }
        protected NonTerminal size { private set; get; }
        protected NonTerminal shortHandArray { private set; get; }
        protected NonTerminal arrayAssignments { private set; get; }
        protected NonTerminal arrayAssignment { private set; get; }
        protected NonTerminal structAssignments { private set; get; }
        protected NonTerminal structAssignment { private set; get; }
        protected NonTerminal shortHandStruct { private set; get; }
        protected NonTerminal stackAccess { private set; get; }
        #endregion

        #region Calls
        protected NonTerminal classCall { private set; get; }
        protected NonTerminal call { private set; get; }
        protected NonTerminal callPrefix { private set; get; }
        protected NonTerminal callFrame { private set; get; }
        protected NonTerminal baseCall { private set; get; }
        protected NonTerminal baseCallPointer { private set; get; }
        protected NonTerminal gscForFunction { private set; get; }
        protected NonTerminal getFunction { private set; get; }
        protected NonTerminal callParameters { private set; get; }
        protected NonTerminal parenCallParameters { private set; get; }
        #endregion

        #region Math
        protected NonTerminal equalOperator { private set; get; }
        protected NonTerminal expression { private set; get; }
        protected NonTerminal relationalOperator { private set; get; }
        protected NonTerminal shortExprOperator { private set; get; }
        protected NonTerminal incDecOperator { private set; get; }
        protected NonTerminal equalityOperator { private set; get; }
        #endregion

        #region Functions
        protected NonTerminal parameters { private set; get; }
        protected NonTerminal parameterExpr { private set; get; }
        protected NonTerminal optionalParameters { private set; get; }
        protected NonTerminal setOptionalParam { private set; get; }
        protected NonTerminal optionalExpr { private set; get; }
        protected NonTerminal parenOptionalExpr { private set; get; }
        protected NonTerminal parenVariableFieldExpr { private set; get; }
        protected NonTerminal block { private set; get; }
        protected NonTerminal blockContent { private set; get; }
        #endregion

        #region Declarations
        protected NonTerminal declarations { private set; get; }
        protected NonTerminal declaration { private set; get; }
        protected NonTerminal _return { private set; get; }
        protected NonTerminal simpleCall { private set; get; }
        protected NonTerminal statement { private set; get; }
        protected NonTerminal statementBlock { private set; get; }
        protected NonTerminal wait { private set; get; }
        protected NonTerminal waitExpr { private set; get; }
        protected NonTerminal parenWaitExpr { private set; get; }
        protected NonTerminal setVariableField { private set; get; }
        protected NonTerminal waittillframeend { private set; get; }
        protected NonTerminal waitFrame { private set; get; }
        #endregion

        #region Control Flow
        protected NonTerminal ifStatement { private set; get; }
        protected NonTerminal elseStatement { private set; get; }
        protected NonTerminal whileStatement { private set; get; }
        protected NonTerminal forStatement { private set; get; }
        protected NonTerminal forBody { private set; get; }
        protected NonTerminal forIterate { private set; get; }
        protected NonTerminal switchStatement { private set; get; }
        protected NonTerminal switchContents { private set; get; }
        protected NonTerminal switchContent { private set; get; }
        protected NonTerminal switchLabel { private set; get; }
        protected NonTerminal foreachStatement { private set; get; }
        protected NonTerminal foreachSingle { private set; get; }
        protected NonTerminal foreachDouble { private set; get; }
        protected NonTerminal jumpStatement { private set; get; }
        #endregion

        #endregion

        #region Virtual
        protected virtual IdentifierTerminal IncludeIdentifier => new IdentifierTerminal("include_identifier", @"_/\", "_");
        protected virtual NonTerminal FunctionFrame => new NonTerminal("functionFrame", ToTerm("autoexec", "autoexec") + functions | functions);
        protected virtual NonTerminal Overrides => new NonTerminal("overrides", Unsupported);
        protected virtual NonTerminal NameSpaceDirective => new NonTerminal("namespace", "#namespace" + Identifier + ";");
        protected virtual NonTerminal verbatimString => new NonTerminal("Unsupported", Unsupported);
        protected virtual NonTerminal hashedString => new NonTerminal("hashedString", ToTerm("#") + StringLiteral);
        protected virtual NonTerminal hashedVariable => new NonTerminal("hashedVariable", ToTerm("#") + Identifier);
        protected virtual NonTerminal iString => new NonTerminal("Unsupported", Unsupported);
        protected virtual NonTerminal usingTree => new NonTerminal("usingTree", Unsupported);
        protected virtual NonTerminal animTree => new NonTerminal("animTree", Unsupported);
        protected virtual NonTerminal animRef => new NonTerminal("animRef", Unsupported);
        protected virtual NonTerminal getAnimation => new NonTerminal("getAnimation", Unsupported);
        #endregion

#if DEBUG
        public NewSyntax()
#else
        protected NewSyntax()
#endif
        {
            #region Terminals
            //Literals
            NumberLiteral = TerminalFactory.CreateCSharpNumber("numberLiteral");
            NumberLiteral.Options = NumberOptions.AllowSign;

            Identifier = new IdentifierTerminal("identifier", @"_\", "_");
            StringLiteral = new StringLiteral("stringLiteral", "\"", StringOptions.AllowsAllEscapes);

            //Comments
            NonGrammarTerminals.Add(new CommentTerminal("dev-comment", "/#", "#/"));
            NonGrammarTerminals.Add(new CommentTerminal("block-comment", "/*", "*/"));
            NonGrammarTerminals.Add(new CommentTerminal("line-comment", "//", "\r", "\n", "\u2085", "\u2028", "\u2029"));

            //Region Support
            NonGrammarTerminals.Add(new CommentTerminal("regionInstruction", "#region", "\n"));
            NonGrammarTerminals.Add(new CommentTerminal("endregionInstruction", "#endregion", "\n"));
            #endregion

            #region Operators
            //Punctuation
            MarkPunctuation("(", ")", "{", "}", "[", "]", ",", ".", ".(", ";", "::", "[[", "]]", "#define", "#include", "#using", "#using_animtree", "]]->");

            //Operators
            RegisterOperators(1, "||");
            RegisterOperators(2, "&&");
            RegisterOperators(3, "|");
            RegisterOperators(4, "^");
            RegisterOperators(5, "&");
            RegisterOperators(6, "==", "!=");
            RegisterOperators(7, "<", ">", "<=", ">=");
            RegisterOperators(8, "+", "-");
            RegisterOperators(9, "*", "/", "%");

            #endregion

            CreateNonTerminals();

            #region Directives
            //Master Directive Rules
            directives.Rule = MakeStarRule(directives, null, directive);
            directive.Rule = Empty | Overrides | includes | globals | FunctionFrame | NameSpaceDirective | usingTree | functionDetour;

            //Includes
            includes.Rule = ToTerm("#include") + IncludeIdentifier + includeExtension.Q() + ";" | 
                            ToTerm("#using") + IncludeIdentifier + includeExtension.Q() + ";";
            includeExtension.Rule = ToTerm(".csc") | ToTerm(".gsc");

            //Globals
            globals.Rule = ToTerm("#define") + Identifier + equalOperator + new NonTerminal("expr", (NumberLiteral | vector | verbatimString | iString | StringLiteral | booleanExpression | newArray)) + ";";
            
            //Functions
            functions.Rule = Identifier + parameters + block |
                             Identifier + equalOperator + parameters + "=>" + new NonTerminal("block", declaration) |
                             Identifier + equalOperator + parameters + "=>" + block + ";";

            detourPath.Rule = gscForFunction | Identifier + "<" + Identifier + ".gsc" + ">" + "::" | Identifier + "<" + Identifier + ".csc" + ">" + "::" | Identifier + "<" + Identifier + ">" + "::";
            functionDetour.Rule = ToTerm("detour") + detourPath + Identifier + parameters + block;
            #endregion

            #region Boolean
            //Master Boolean Rules
            booleanExpression.Rule = boolExprOperand | booleanAndExpression | booleanOrExpression;
            booleanAndExpression.Rule = booleanExpression + ToTerm("&&") + booleanExpression;
            booleanOrExpression.Rule = blorOp + ToTerm("||") + blorOp | booleanExpression + ToTerm("||") + booleanExpression;
            boolNot.Rule = ToTerm("!") + boolNotOperand;
            

            //Parenthesis
            parenBooleanExpression.Rule = "(" + booleanExpression + ")";
            parenBoolOpsExpr.Rule = "(" + booleanAndExpression + ")" | "(" + booleanOrExpression + ")";

            //Boolean Operands
            boolExprOperand.Rule = expr | boolOperand | parenBooleanExpression | parenBoolOpsExpr;
            boolNotOperand.Rule = expr | parenBooleanExpression | parenBoolOpsExpr;
            blorOp.Rule = booleanAndExpression | boolExprOperand;

            //Conditional
            conditionalStatement.Rule = booleanExpression + ToTerm("?") + booleanExpression + ToTerm(":") + booleanExpression;
            
            //Boolean Operand
            boolOperand.Rule =  new NonTerminal("pemdas", expression) |
                                new NonTerminal("relationalExpression", boolExprOperand + relationalOperator + boolExprOperand) |
                                new NonTerminal("relationalExpression", boolExprOperand + equalityOperator + boolExprOperand) |
                                conditionalStatement;
            #endregion

            #region Expressions
            //Master Expresssion Rules
            expr.Rule = parenExpr | mathExpr | animRef | animTree | newArray | shortHandArray | shortHandStruct | boolNot;
            mathExpr.Rule = parenMathExpr | variableExpr | StringLiteral | NumberLiteral | verbatimString | size | iString | hashedString | hashedVariable | vector;
            variableExpr.Rule = parenVariableExpr | directAccess | stackAccess | call | classCall | Identifier | getFunction | array;

            //Parenthesis
            parenExpr.Rule = "(" + expr + ")";
            parenMathExpr.Rule = "(" + mathExpr + ")";
            parenVariableExpr.Rule = "(" + variableExpr + ")";

            //Misc
            stackAccess.Rule = variableExpr + ".(" + booleanExpression + ")";
            directAccess.Rule = variableExpr + "." + Identifier;
            setVariableFieldExpr.Rule = parenVariableFieldExpr | booleanExpression;
            array.Rule = variableExpr + "[" + booleanExpression + "]" | StringLiteral + "[" + booleanExpression + "]";
            size.Rule = variableExpr + ".size" | StringLiteral + ".size";
            vector.Rule = "(" + booleanExpression + "," + booleanExpression + "," + booleanExpression + ")";

            arrayAssignment.Rule = NumberLiteral + ":" + booleanExpression | StringLiteral + ":" + booleanExpression | 
                hashedString + ":" + booleanExpression | hashedVariable + ":" + booleanExpression;
            arrayAssignments.Rule = MakePlusRule(arrayAssignments, ToTerm(","), arrayAssignment) | arrayAssignment;
            shortHandArray.Rule = "[" + arrayAssignments + "]";

            structAssignment.Rule = hashedVariable + ":" + booleanExpression;
            structAssignments.Rule = MakePlusRule(structAssignments, ToTerm(","), structAssignment) | structAssignment;
            shortHandStruct.Rule = "{" + structAssignments + "}";
            #endregion

            #region Calls
            //Master Call Rule
            call.Rule = callPrefix + callFrame | callFrame;

            //Call Components
            callPrefix.Rule = variableExpr + ToTerm("thread") | variableExpr | ToTerm("thread");
            callFrame.Rule = baseCall | baseCallPointer;
            classCall.Rule = ToTerm("thread") + ToTerm("[" + "[") + variableExpr + "]]->" + Identifier + parenCallParameters |
                             ToTerm("[" + "[") + variableExpr + "]]->" + Identifier + parenCallParameters;

            //Script Reference Components
            gscForFunction.Rule = ToTerm("&") + Identifier + "::";
            getFunction.Rule = ToTerm("&") + new NonTerminal("expr", Identifier) | gscForFunction + variableExpr;

            //Base Call Rules
            baseCall.Rule = Identifier + "::" + Identifier + parenCallParameters | Identifier + parenCallParameters;
            baseCallPointer.Rule = ToTerm("[" + "[") + variableExpr + "]" + "]" + parenCallParameters;

            //Parameters
            callParameters.Rule = MakeStarRule(callParameters, ToTerm(","), booleanExpression) | booleanExpression;
            parenCallParameters.Rule = "(" + callParameters + ")" | "(" + ")";
            #endregion

            #region Math
            //Math Expression
            expression.Rule = boolExprOperand + ToTerm("*") + boolExprOperand |
                              boolExprOperand + ToTerm("/") + boolExprOperand |
                              boolExprOperand + ToTerm("%") + boolExprOperand |
                              boolExprOperand + ToTerm("+") + boolExprOperand |
                              boolExprOperand + ToTerm("-") + boolExprOperand |
                              boolExprOperand + ToTerm("&") + boolExprOperand |
                              boolExprOperand + ToTerm("^") + boolExprOperand |
                              boolExprOperand + ToTerm("|") + boolExprOperand |
                              boolExprOperand + ToTerm("<<") + boolExprOperand |
                              boolExprOperand + ToTerm(">>") + boolExprOperand;

            #endregion

            #region Functions
            //Master Parameter Rules
            parameters.Rule = "(" + optionalParameters + ")" | "(" + ")";
            optionalParameters.Rule = MakeStarRule(optionalParameters, ToTerm(","), parameterExpr) | parameterExpr;
            
            //Parameter Rules
            parameterExpr.Rule = Identifier | setOptionalParam;
            setOptionalParam.Rule = Identifier + equalOperator + optionalExpr;

            //Parenthesis
            parenOptionalExpr.Rule = "(" + optionalExpr + ")";
            parenVariableFieldExpr.Rule = "(" + setVariableFieldExpr + ")";

            //Optional Expression
            optionalExpr.Rule = parenOptionalExpr | setVariableFieldExpr | ToTerm("true", "identifier") | ToTerm("false", "identifier");

            //Block Content
            block.Rule = ToTerm("{") + blockContent + "}" | ToTerm("{") + "}";
            blockContent.Rule = declarations;
            #endregion

            #region Declarations
            //Declaration Master Rules
            declarations.Rule = MakePlusRule(declarations, declaration);
            declaration.Rule = _return | simpleCall | statement | setVariableField | wait | waitFrame | waittillframeend | jumpStatement;

            //Statement Master Rule
            statement.Rule = ifStatement | whileStatement | forStatement | switchStatement | foreachStatement;
            statementBlock.Rule = block | declaration;

            //Declarations
            simpleCall.Rule = call + ";" | classCall + ";";
            _return.Rule = ToTerm("return") + booleanExpression + ";" | ToTerm("return") + newArray + ";" | ToTerm("return") + ";";

            //Parenthesis
            parenWaitExpr.Rule = "(" + waitExpr + ")";

            //Wait Expressions
            wait.Rule = ToTerm("wait") + waitExpr + ";";
            waitFrame.Rule = ToTerm("waitframe") + waitExpr + ";";
            waitExpr.Rule = parenWaitExpr | NumberLiteral | booleanExpression | size | variableExpr;
            waittillframeend.Rule = ToTerm("waittillframeend") + ";";

            //Set Variable Field
            setVariableField.Rule = variableExpr + shortExprOperator + booleanExpression + ";" | 
                                    variableExpr + equalOperator + setVariableFieldExpr + ";"  | 
                                    variableExpr + incDecOperator + ";";
            #endregion

            #region Control Flow
            //Control Flow Master Rules
            ifStatement.Rule = ToTerm("if") + "(" + booleanExpression + ")" + statementBlock | ToTerm("if") + "(" + booleanExpression + ")" + statementBlock + elseStatement;
            elseStatement.Rule = ToTerm("else") + statementBlock;
            whileStatement.Rule = ToTerm("while") + "(" + booleanExpression + ")" + statementBlock;
            forStatement.Rule = ToTerm("for") + "(" + forBody + ")" + statementBlock;
            switchStatement.Rule = ToTerm("switch") + parenExpr + "{" + switchContents + "}";
            foreachStatement.Rule = foreachSingle | foreachDouble;

            //Loop Control
            jumpStatement.Rule = ToTerm("break") + ";" | ToTerm("break") + NumberLiteral + ";" | ToTerm("continue") + ";" | ToTerm("continue") + NumberLiteral + ";";

            //Switch Contents
            switchContents.Rule = MakeStarRule(switchContents, switchContent);
            switchContent.Rule = switchLabel + blockContent | switchLabel + block | switchLabel;
            switchLabel.Rule = ToTerm("case") + expr + ":" | ToTerm("default") + ":";

            //Foreach
            foreachSingle.Rule = ToTerm("foreach") + "(" + new NonTerminal("value", Identifier) + "in" + expr + ")" + statementBlock;
            foreachDouble.Rule = ToTerm("foreach") + "(" + new NonTerminal("key", Identifier) + "," + new NonTerminal("value", Identifier) + "in" + expr + ")" + statementBlock;

            //For Loop
            forIterate.Rule = expr + shortExprOperator + expr | expr + incDecOperator;
            forBody.Rule = setVariableField + booleanExpression + ";" + forIterate
                           | ToTerm(";") + booleanExpression + ";" + forIterate
                           | ToTerm(";") + ";" + forIterate
                           | ToTerm(";") + ";" | setVariableField + ";" + forIterate
                           | setVariableField + ";"
                           | ToTerm(";") + booleanExpression + ";" | setVariableField + booleanExpression + ";";
            #endregion

            MarkTransient
                (
                    boolNotOperand, 
                    boolOperand,
                    parenExpr,
                    parenMathExpr,
                    parenVariableExpr,
                    includeExtension
                );

            SyntaxParser = new Parser(this);
        }

        protected virtual void CreateNonTerminals()
        {
            stackAccess = new NonTerminal("stackAccess");
            shortHandStruct = new NonTerminal("shortHandStruct");
            structAssignment = new NonTerminal("structAssignment");
            structAssignments = new NonTerminal("structAssignments");
            arrayAssignments = new NonTerminal("arrayAssignments");
            arrayAssignment = new NonTerminal("arrayAssignment");
            waitFrame = new NonTerminal("waitframe");
            directives = new NonTerminal("directives");
            directive = new NonTerminal("directive");
            functions = new NonTerminal("functions");
            globals = new NonTerminal("globals");
            includes = new NonTerminal("includes");
            equalOperator = new NonTerminal("equalOperator", ToTerm("="));
            vector = new NonTerminal("vector");
            newArray = new NonTerminal("newArray", ToTerm("[]"));
            booleanExpression = new NonTerminal("booleanExpression");
            boolExprOperand = new NonTerminal("boolNotOperand");
            expr = new NonTerminal("expr");
            parenExpr = new NonTerminal("parenExpr");
            mathExpr = new NonTerminal("expr");
            parenMathExpr = new NonTerminal("parenMathExpr");
            variableExpr = new NonTerminal("expr");
            parenVariableExpr = new NonTerminal("parenVariableExpr");
            directAccess = new NonTerminal("directAccess");
            classCall = new NonTerminal("classCall");
            call = new NonTerminal("call");
            callPrefix = new NonTerminal("callPrefix");
            callFrame = new NonTerminal("callFrame");
            baseCall = new NonTerminal("baseCall");
            gscForFunction = new NonTerminal("gscForFunction");
            parenCallParameters = new NonTerminal("parenCallParameters");
            callParameters = new NonTerminal("callParameters");
            baseCallPointer = new NonTerminal("baseCallPointer");
            getFunction = new NonTerminal("getFunction");
            array = new NonTerminal("array");
            size = new NonTerminal("size");
            boolNot = new NonTerminal("boolNot");
            boolNotOperand = new NonTerminal("boolNotOperand");
            parenBooleanExpression = new NonTerminal("parenBooleanExpression");
            parenBoolOpsExpr = new NonTerminal("parenBoolOpsExpr");
            boolOperand = new NonTerminal("boolOperand");
            expression = new NonTerminal("expression");
            relationalOperator = new NonTerminal("relationalOperator", ToTerm(">") | ">=" | "<" | "<=");
            equalityOperator = new NonTerminal("relationalOperator", ToTerm("==") | "!=" | "===" | "!==");
            conditionalStatement = new NonTerminal("conditionalStatement");
            booleanAndExpression = new NonTerminal("booleanExpression");
            booleanOrExpression = new NonTerminal("booleanExpression");
            blorOp = new NonTerminal("blor");
            parameters = new NonTerminal("parameters");
            optionalParameters = new NonTerminal("optionalParameters");
            parameterExpr = new NonTerminal("parameterExpr");
            setOptionalParam = new NonTerminal("setOptionalParam");
            parenOptionalExpr = new NonTerminal("parenOptionalExpr");
            setVariableFieldExpr = new NonTerminal("setVariableFieldExpr");
            parenVariableFieldExpr = new NonTerminal("parenVariableFieldExpr");
            shortHandArray = new NonTerminal("shortHandArray");
            block = new NonTerminal("block");
            blockContent = new NonTerminal("blockContent");
            declarations = new NonTerminal("declarations");
            declaration = new NonTerminal("declaration");
            _return = new NonTerminal("return");
            simpleCall = new NonTerminal("simpleCall");
            statement = new NonTerminal("statement");
            ifStatement = new NonTerminal("ifStatement");
            statementBlock = new NonTerminal("statementBlock");
            elseStatement = new NonTerminal("elseStatement");
            whileStatement = new NonTerminal("whileStatement");
            forStatement = new NonTerminal("forStatement");
            forBody = new NonTerminal("forBody");
            forIterate = new NonTerminal("forIterate");
            shortExprOperator = new NonTerminal("shortExprOperator", ToTerm("+=") | "-=" | "*=" | "/=" | "%=" | "&=" | "|=" | "<<=" | ">>=");
            incDecOperator = new NonTerminal("incDecOperator", ToTerm("++") | "--");
            switchStatement = new NonTerminal("switchStatement");
            switchContents = new NonTerminal("switchContents");
            switchContent = new NonTerminal("switchContent");
            switchLabel = new NonTerminal("switchLabel");
            foreachStatement = new NonTerminal("foreachStatement");
            foreachSingle = new NonTerminal("foreachSingle");
            foreachDouble = new NonTerminal("foreachDouble");
            optionalExpr = new NonTerminal("optionalExpr");
            wait = new NonTerminal("wait");
            waitExpr = new NonTerminal("expr");
            parenWaitExpr = new NonTerminal("parenWaitExpr");
            setVariableField = new NonTerminal("setVariableField");
            waittillframeend = new NonTerminal("waitTillFrameEnd");
            jumpStatement = new NonTerminal("jumpStatement");
            includeExtension = new NonTerminal("includeExtension");
            functionDetour = new NonTerminal("functionDetour");
            detourPath = new NonTerminal("detourPath");
            Root = new NonTerminal("program") { Rule = directives };
        }
    }
}