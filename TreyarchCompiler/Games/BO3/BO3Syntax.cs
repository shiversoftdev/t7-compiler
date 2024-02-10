using Irony.Parsing;

namespace TreyarchCompiler.Games.BO3
{
    [Language("Game Script", "T4.3", "GSC Grammar For Call of Duty Made By AgreedBog381, modified by S.")]
    internal sealed class BO3Syntax : BaseSyntax
    {
        #region Threadsafe
        //public new static Parser Parser => ThreadSafeInstance.SyntaxParser;
        private static readonly object InstanceLock = new object();
        private static BO3Syntax instance;

        private static BO3Syntax ThreadSafeInstance
        {
            get
            {
                lock (InstanceLock)
                {
                    return instance ?? (instance = new BO3Syntax());
                }
            }
        }

        internal static ParseTree ParseCode(string code)
        {
            lock (InstanceLock)
            {
                return ThreadSafeInstance.SyntaxParser.Parse(code);
            }
        }
        #endregion

        #region Virtual
        private NonTerminal autoexec_priority => new NonTerminal("autoexec", ToTerm("autoexec", "autoexec_p") + "(" + NumberLiteral + ")" | ToTerm("autoexec", "autoexec_p"));
        protected override NonTerminal FunctionFrame => new NonTerminal("functionFrame", autoexec_priority + functions | functions | ToTerm("function") + autoexec_priority + functions | ToTerm("function") + functions);
        protected override NonTerminal NameSpaceDirective => new NonTerminal("namespace", "#namespace" + Identifier + ";");
        protected override NonTerminal verbatimString => new NonTerminal("verbatimString", Unsupported);
        protected override NonTerminal iString => new NonTerminal("iString", ToTerm("&") + StringLiteral);
        protected override NonTerminal hashedString => new NonTerminal("hashedString", ToTerm("#") + StringLiteral);
        protected override NonTerminal canonHashed => new NonTerminal("canonHashed", ToTerm("#") + Identifier);
        //protected override NonTerminal usingTree => new NonTerminal("usingTree", ToTerm("#using_animtree") + "(" + StringLiteral + ")" + ";");
        #endregion
    }
}