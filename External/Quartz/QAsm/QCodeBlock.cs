using System;
using System.Collections.Generic;
using System.Linq;
using System.Quartz.QAsm.Instructions;
using System.Text;
using System.Threading.Tasks;

namespace System.Quartz.QAsm
{
    public class QCodeBlock : QInstruction
    {
        private List<QInstruction> Instructions;
        private QInstruction LastInstruction;
        private QInstruction FirstInstruction;
        private List<QVariable> ScopedVariables;
        private QVariable[] Parameters;

        /// <summary>
        /// Context string is the full path context identifier for this block
        /// </summary>
        public readonly string Context;
        public QCodeBlock(string context)
        {
            Context = context;
            Instructions = new List<QInstruction>();
            ScopedVariables = new List<QVariable>();
        }

        public void SetParameters(QVariable[] parameters)
        {
            if (parameters != null)
            {
                ScopedVariables.AddRange(parameters);
                Parameters = new QVariable[parameters.Length];
                parameters.CopyTo(Parameters, 0);
            }
        }

        public void Add(QInstruction instruction)
        {
            if(instruction is null)
            {
                throw new ArgumentException($"{nameof(instruction)} cannot be null");
            }
            if(FirstInstruction is null)
            {
                FirstInstruction = instruction;
                LastInstruction = instruction;
            }
            else
            {
                LastInstruction.Link(instruction);
                LastInstruction = instruction;
            }
            instruction.ParentBlock = this;
        }

        public void AddScopedParameter(QVariable variable)
        {
            ScopedVariables.Add(variable);
        }
    }
}
