using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T7CompilerLib.ScriptComponents
{
    public abstract class T7ScriptSection
    {
        public T7ScriptSection NextSection { get; private set; }
        public T7ScriptSection PreviousSection { get; private set; }

        public abstract uint Size();

        public abstract ushort Count();

        public abstract byte[] Serialize();

        public abstract void UpdateHeader(ref T7ScriptHeader Header);

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

        internal void Link(T7ScriptSection previous, T7ScriptSection next)
        {
            NextSection = next;
            PreviousSection = previous;
        }

        public virtual void Commit(ref byte[] RawData, ref T7ScriptHeader Header)
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
