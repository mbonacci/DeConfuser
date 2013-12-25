//
// Instruction.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
namespace Mono.Cecil.Cil {

	public sealed class Instruction : ICodeVisitable {

		int m_offset;
		OpCode m_opCode;
		object m_operand;

		Instruction m_previous;
		Instruction m_next;

		SequencePoint m_sequencePoint;

		public int Offset {
			get { return m_offset; }
			set { m_offset = value; }
		}

		public OpCode OpCode {
			get { return m_opCode; }
			set { m_opCode = value; }
		}

		public object Operand {
			get { return m_operand; }
			set { m_operand = value; }
		}

		public Instruction Previous {
			get { return m_previous; }
			set { m_previous = value; }
		}

		public Instruction Next {
			get { return m_next; }
			set { m_next = value; }
		}

		public SequencePoint SequencePoint {
			get { return m_sequencePoint; }
			set { m_sequencePoint = value; }
		}

		internal Instruction (int offset, OpCode opCode, object operand) : this (offset, opCode)
		{
			m_operand = operand;
		}

		internal Instruction (int offset, OpCode opCode)
		{
			m_offset = offset;
			m_opCode = opCode;
		}

		internal Instruction (OpCode opCode, object operand) : this (0, opCode, operand)
		{
		}

		internal Instruction (OpCode opCode) : this (0, opCode)
		{
		}

		public void Accept (ICodeVisitor visitor)
		{
			visitor.VisitInstruction (this);
		}

        public static Instruction Create(OpCode opcode)
        {
            if (opcode.OperandType != OperandType.InlineNone)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, null);
        }

        public static Instruction Create(OpCode opcode, TypeReference type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (opcode.OperandType != OperandType.InlineType &&
                opcode.OperandType != OperandType.InlineTok)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, type);
        }

        public static Instruction Create(OpCode opcode, CallSite site)
        {
            if (site == null)
                throw new ArgumentNullException("site");
            if (opcode.Code != Code.Calli)
                throw new ArgumentException("code");

            return new Instruction(opcode, site);
        }

        public static Instruction Create(OpCode opcode, MethodReference method)
        {
            if (method == null)
                throw new ArgumentNullException("method");
            if (opcode.OperandType != OperandType.InlineMethod &&
                opcode.OperandType != OperandType.InlineTok)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, method);
        }

        public static Instruction Create(OpCode opcode, FieldReference field)
        {
            if (field == null)
                throw new ArgumentNullException("field");
            if (opcode.OperandType != OperandType.InlineField &&
                opcode.OperandType != OperandType.InlineTok)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, field);
        }

        public static Instruction Create(OpCode opcode, string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (opcode.OperandType != OperandType.InlineString)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, value);
        }

        public static Instruction Create(OpCode opcode, sbyte value)
        {
            if (opcode.OperandType != OperandType.ShortInlineI &&
                opcode != OpCodes.Ldc_I4_S)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, value);
        }

        public static Instruction Create(OpCode opcode, byte value)
        {
            if (opcode.OperandType != OperandType.ShortInlineI ||
                opcode == OpCodes.Ldc_I4_S)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, value);
        }

        public static Instruction Create(OpCode opcode, int value)
        {
            if (opcode.OperandType != OperandType.InlineI)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, value);
        }

        public static Instruction Create(OpCode opcode, long value)
        {
            if (opcode.OperandType != OperandType.InlineI8)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, value);
        }

        public static Instruction Create(OpCode opcode, float value)
        {
            if (opcode.OperandType != OperandType.ShortInlineR)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, value);
        }

        public static Instruction Create(OpCode opcode, double value)
        {
            if (opcode.OperandType != OperandType.InlineR)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, value);
        }

        public static Instruction Create(OpCode opcode, Instruction target)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            if (opcode.OperandType != OperandType.InlineBrTarget &&
                opcode.OperandType != OperandType.ShortInlineBrTarget)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, target);
        }

        public static Instruction Create(OpCode opcode, Instruction[] targets)
        {
            if (targets == null)
                throw new ArgumentNullException("targets");
            if (opcode.OperandType != OperandType.InlineSwitch)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, targets);
        }

        public static Instruction Create(OpCode opcode, VariableDefinition variable)
        {
            if (variable == null)
                throw new ArgumentNullException("variable");
            if (opcode.OperandType != OperandType.ShortInlineVar &&
                opcode.OperandType != OperandType.InlineVar)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, variable);
        }

        public static Instruction Create(OpCode opcode, ParameterDefinition parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException("parameter");
            if (opcode.OperandType != OperandType.ShortInlineArg &&
                opcode.OperandType != OperandType.InlineArg)
                throw new ArgumentException("opcode");

            return new Instruction(opcode, parameter);
        }

        public static Instruction CreateJunkCode(ushort val)
        {
            return new Instruction(new OpCode(0x5000000 | val, 0x13000505), null);
        }
	}
}
