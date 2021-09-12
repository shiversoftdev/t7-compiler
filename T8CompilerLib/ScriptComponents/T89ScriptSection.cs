namespace T89CompilerLib.ScriptComponents
{
    public abstract class T89ScriptSection
    {
        public T89ScriptSection NextSection { get; private set; }
        public T89ScriptSection PreviousSection { get; private set; }

        public abstract uint Size();

        public abstract ushort Count();

        public abstract byte[] Serialize();

        public abstract void UpdateHeader(ref T89ScriptHeader Header);

        public uint GetScriptEnd()
        {
            return Size() + (NextSection?.GetScriptEnd() ?? 0u);
        }

        public uint GetBaseAddress()
        {
            if (PreviousSection == null)
                return 0;

            return PreviousSection.GetSectionEnd();
        }

        public uint GetSectionEnd()
        {
            if (PreviousSection == null)
                return Size();

            return Size() + PreviousSection.GetSectionEnd();
        }

        internal void Link(T89ScriptSection previous, T89ScriptSection next)
        {
            NextSection = next;
            PreviousSection = previous;
        }

        public virtual void Commit(ref byte[] RawData, ref T89ScriptHeader Header)
        {
            byte[] LocalData = Serialize();

            byte[] NewBuffer = new byte[LocalData.Length + RawData.Length];

            RawData.CopyTo(NewBuffer, 0);
            LocalData.CopyTo(NewBuffer, RawData.Length);

            RawData = NewBuffer;

            UpdateHeader(ref Header);
            NextSection?.Commit(ref RawData, ref Header);
        }
    }
}
