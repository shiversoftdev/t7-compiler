using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Quartz.QAsm.Instructions
{
    // root class for quartz meta instructions
    public abstract class QInstruction
    {
        // Doubly linked list internally to use some traversal information when emitting bytecode
        public QInstruction Next { get; private set; }
        public QInstruction Previous { get; private set; }

        // Allows us to get information about our context scope
        public QCodeBlock ParentBlock { get; set; }

        public void Link(QInstruction next)
        {
            if(Next != null)
            {
                throw new InvalidOperationException("Tried to link an instruction which already has been linked");
            }
            Next = next;
            next.Previous = this;
        }
    }
}
