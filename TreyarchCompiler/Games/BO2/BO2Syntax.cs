using Irony.Parsing;

namespace TreyarchCompiler.Games.BO2
{
    [Language("Game Script", "T4.2", "GSC Grammar For Call of Duty Made By AgreedBog381, modified by S.")]
    internal sealed class BO2Syntax : BaseSyntax
    {
        #region Threadsafe
        //public new static Parser Parser => ThreadSafeInstance.SyntaxParser;
        private static readonly object InstanceLock = new object();
        private static BO2Syntax instance;

        private static BO2Syntax ThreadSafeInstance
        {
            get
            {
                lock (InstanceLock)
                {
                    return instance ?? (instance = new BO2Syntax());
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
        protected override NonTerminal FunctionFrame => new NonTerminal("functionFrame", functions);
        protected override NonTerminal Overrides => new NonTerminal("overrides", ToTerm("#overrides") + IncludeIdentifier + ";");
        protected override NonTerminal NameSpaceDirective => new NonTerminal("namespace", Unsupported);
        protected override NonTerminal verbatimString => new NonTerminal("verbatimString", Unsupported);
        protected override NonTerminal iString => new NonTerminal("iString", ToTerm("&") + StringLiteral);
        protected override NonTerminal hashedString => new NonTerminal("hashedString", Unsupported);
        protected override NonTerminal usingTree => new NonTerminal("usingTree", ToTerm("#using_animtree") + animTree + ";");
        protected override NonTerminal animTree => new NonTerminal("animTree", ToTerm("%") + Identifier);
        protected override NonTerminal getAnimation => new NonTerminal("getAnimation", ToTerm("->") + Identifier);
        protected override NonTerminal animRef => new NonTerminal("animRef", animTree + getAnimation);
        #endregion

    }
}