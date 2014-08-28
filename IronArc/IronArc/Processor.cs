﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronArc
{
    /// <summary>
    /// An instance of an IronArc processor.
    /// </summary>
    public sealed class Processor
    {
        // Registers
        public ByteBlock EAX { get; set; }
        public ByteBlock EBX { get; set; }
        public ByteBlock ECX { get; set; }
        public ByteBlock EDX { get; set; }
        public ByteBlock EEX { get; set; }
        public ByteBlock EFX { get; set; }
        public ByteBlock EGX { get; set; }
        public ByteBlock EHX { get; set; }

        // System memory
        public ByteBlock Memory { get; set; }

        // System stack
        public Stack<long> Stack { get; set; }

        // Instruction pointer
        public uint InstructionPointer { get; set; }

        // Flags register

        public ByteBlock EFLAGS { get; set; }

        // Diskfile
        public Diskfile Diskfile { get; set; }

        // Call stack
        public Stack<int> CallStack { get; set; }

        public Processor(int memorySize)
        {
            this.EAX = ByteBlock.FromLength(8);
            this.EBX = ByteBlock.FromLength(8);
            this.ECX = ByteBlock.FromLength(8);
            this.EDX = ByteBlock.FromLength(8);
            this.EEX = ByteBlock.FromLength(8);
            this.EFX = ByteBlock.FromLength(8);
            this.EGX = ByteBlock.FromLength(8);
            this.EHX = ByteBlock.FromLength(8);

            this.EFLAGS = ByteBlock.FromLength(4);

            this.Memory = ByteBlock.FromLength(memorySize);
            this.Stack = new Stack<long>();
            this.CallStack = new Stack<int>();
        }

        #region Memory Read Methods
        public byte ReadByte()
        {
            return this.Memory[(int)this.InstructionPointer++]; // fix
        }

        public sbyte ReadSByte()
        {
            return (sbyte)this.ReadByte();
        }

        public short ReadShort()
        {
            short result = this.Memory.ReadShortAt(this.InstructionPointer);
            this.InstructionPointer += 2;
            return result;
        }

        public ushort ReadUShort()
        {
            return (ushort)this.ReadShort();
        }

        public int ReadInt()
        {
            int result = this.Memory.ReadIntAt(this.InstructionPointer);
            this.InstructionPointer += 4;
            return result;
        }

        public uint ReadUInt()
        {
            return (uint)this.ReadInt();
        }

        public long ReadLong()
        {
            long result = this.Memory.ReadLongAt(this.InstructionPointer);
            this.InstructionPointer += 8;
            return result;
        }

        public ulong ReadULong()
        {
            return (ulong)this.ReadLong();
        }

        public float ReadFloat()
        {
            float result = this.Memory.ReadFloatAt(this.InstructionPointer);
            this.InstructionPointer += 4;
            return result;
        }

        public double ReadDouble()
        {
            double result = this.Memory.ReadDoubleAt(this.InstructionPointer);
            this.InstructionPointer += 8;
            return result;
        }

        public char ReadChar()
        {
            return (char)this.ReadShort();
        }

        public ByteBlock Read(uint length)
        {
            ByteBlock result = this.Memory.ReadAt(length, this.InstructionPointer);
            this.InstructionPointer += length;
            return result;
        }
        #endregion

        #region Register Read/Write
        public ByteBlock ReadRegister(byte register)
        {
            switch (register)
            {
                case 0x00:
                    return this.EAX;
                case 0x01:
                    return this.EBX;
                case 0x02:
                    return this.ECX;
                case 0x03:
                    return this.EDX;
                case 0x04:
                    return this.EEX;
                case 0x05:
                    return this.EFX;
                case 0x06:
                    return this.EGX;
                case 0x07:
                    return this.EHX;
                case 0x08:
                    return new ByteBlock(this.InstructionPointer);
                case 0x09:
                    return new ByteBlock((byte)0);
                default:
                    new SystemError("InvalidProcessorRegister", string.Format("Tried to read from invalid processor register 0x{0:X2}."));
                    break;
            }

            return null;
        }

        public void WriteRegister(byte register, ByteBlock value)
        {
            if (register >= 0x00 && register <= 0x07)
            {
                if (value.Length > 8)
                {
                    new SystemError("InvalidWriteLength", string.Format("Cannot write a {0}-byte value to a standard 8-byte register.", value.Length)).WriteToError();
                }
            }
            else if (register == 0x08)
            {
                if (value.Length > 4)
                {
                    new SystemError("InvalidWriteLength", string.Format("Cannot write a {0}-byte value to the 4-byte instruction pointer.", value.Length)).WriteToError();
                }
            }

            switch (register)
            {
                case 0x00:
                    this.EAX = 0UL;
                    this.EAX.WriteAt(value, 8 - value.Length);
                    break;
                case 0x01:
                    this.EBX = 0UL;
                    this.EBX.WriteAt(value, 8 - value.Length);
                    break;
                case 0x02:
                    this.ECX = 0UL;
                    this.ECX.WriteAt(value, 8 - value.Length);
                    break;
                case 0x03:
                    this.EDX = 0UL;
                    this.EDX.WriteAt(value, 8 - value.Length);
                    break;
                case 0x04:
                    this.EEX = 0UL;
                    this.EEX.WriteAt(value, 8 - value.Length);
                    break;
                case 0x05:
                    this.EFX = 0UL;
                    this.EFX.WriteAt(value, 8 - value.Length);
                    break;
                case 0x06:
                    this.EGX = 0UL;
                    this.EGX.WriteAt(value, 8 - value.Length);
                    break;
                case 0x07:
                    this.EHX = 0UL;
                    this.EHX.WriteAt(value, 8 - value.Length);
                    break;
                case 0x08:
                    this.InstructionPointer = value.ToUInt();
                    break;
                case 0x09:
                    break;
            }
        }
        #endregion

        #region Opcodes
        // Each opcode has a value in one of these enums, which is the value of its second byte
        private enum CFOpcode : byte
        {
            NOP = 0x00,
            JMP = 0X01,
            JMPA = 0X02,
            JE = 0X03,
		    JNE = 0x04,
		    JLT = 0x05,
		    JGT = 0x06,
		    JLTE = 0x07,
		    JGTE = 0x08,
		    JZ = 0x09,
		    JNZ = 0x0A,
	    	CALL = 0x0B,
    		CALLA = 0x0C,
		    RET = 0x0D,
		    END = 0x0E
        };

        private enum DataOpcode : byte
        {
            MOV = 0x00,
            ADD = 0x01,
            SUB = 0x02,
            MULT = 0x03,
            DIV = 0x04,
            MOD = 0x05,
            INV = 0x06,
            EQ = 0x07,
            INEQ = 0x08,
            LT = 0x09,
            GT = 0x0A,
            LTEQ = 0x0B,
            GTEQ = 0x0C,
            AND = 0x0D,
            OR = 0x0E,
            NOT = 0x0F,
            BWNOT = 0x10,
            BWAND = 0x11,
            BWOR = 0x12,
            BWXOR = 0x13,
            BWLSHIFT = 0x14,
            BWRSHIFT = 0x15,
            ADDL = 0x40,
            SUBL = 0x41,
            MULTL = 0x42,
            DIVL = 0x43,
            MODL = 0x44,
            INVL = 0x45,
            EQL = 0x46,
            INEQL = 0x47,
            LTL = 0x48,
            GTL = 0x49,
            LTEQL = 0x4A,
            GTEQL = 0x4B,
            ANDL = 0x4C,
            ORL = 0x4D,
            NOTL = 0x4E,
            BWNOTL = 0x4F,
            BWANDL = 0x50,
            BWORL = 0x51,
            BWXORL = 0x52,
            BWLSHIFTL = 0x53,
            BWRSHIFTL = 0x54,
            PUSH = 0x80,
            POP = 0x81,
            PEEK = 0x82,
            STACKALLOC = 0x83,
            ARRAYALLOC = 0x84,
            DEREF = 0x85,
            ARRAYACCESS = 0x86,
            CBYTE = 0xA0,
            CSBYTE = 0xA1,
            CSHORT = 0xA2,
            CUSHORT = 0xA3,
            CINT = 0xA4,
            CUINT = 0xA5,
            CLONG = 0xA6,
            CULONG = 0xA7,
            CSING = 0xA8,
            CDOUBLE = 0xA9
        };

        private enum SysOpcode : byte
        {
            HWCALL = 0x00,
            SETFLAG = 0x10,
            CLEARFLAG = 0x11,
            TESTFLAG = 0x12,
            TOGGLEFLAG = 0x13
        };
        #endregion

        #region Execution
        public void Execute()
        {
            ushort fullopcode = this.ReadUShort();
            byte opcode = (byte)((fullopcode << 8) >> 8);
            ushort arity = 0;
            switch (fullopcode >> 8)
            {
                case 0x00: // Control flow instruction
                    CFOpcode cfopcode = (CFOpcode)opcode;
                    switch (cfopcode)
                    {
                        case CFOpcode.NOP:
                        case CFOpcode.RET:
                        case CFOpcode.END: // no operands
                            break;
                        case CFOpcode.JMP:
                        case CFOpcode.JMPA:
                        case CFOpcode.CALL:
                        case CFOpcode.CALLA: // one operand, etc
                            arity = 1;
                            break;
                        case CFOpcode.JZ:
                        case CFOpcode.JNZ:
                            arity = 2;
                            break;
                        case CFOpcode.JE:
                        case CFOpcode.JNE:
                        case CFOpcode.JLT:
                        case CFOpcode.JGT:
                        case CFOpcode.JLTE:
                        case CFOpcode.JGTE:
                            arity = 3;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(); // will likely change
                    }
                    break;
                case 0x01: // data instructions
                    DataOpcode dopcode = (DataOpcode)opcode;
                    switch (dopcode & (DataOpcode)0xE0) // seems only the top 3 bits are needed
                    {
                        case (DataOpcode)0x00: // stack arithmetic (or a move)
                            if (dopcode == DataOpcode.MOV) { arity = 2; }
                            break;
                        case (DataOpcode)0x40: // long arithmetic
                            switch (dopcode)
                            {
                                case DataOpcode.INVL:
                                case DataOpcode.NOTL:
                                case DataOpcode.BWNOTL:
                                    arity = 2;
                                    break;
                                default:
                                    if (dopcode > (DataOpcode)0x54) 
                                    {
                                        throw new ArgumentOutOfRangeException();
                                    }
                                    else
                                    {
                                        arity = 3;
                                        break;
                                    }
                            }
                            break;
                        case (DataOpcode)0x80: // stack operations
                            if (dopcode == DataOpcode.ARRAYALLOC)
                            {
                                arity = 2;
                            }
                            else
                            {
                                arity = 1;
                            }
                            break;
                        case (DataOpcode)0xA0: // conversions
                            arity = 2;
                            break;
                    }
                    break;
                case 0x02: // system instructions
                    SysOpcode sopcode = (SysOpcode)opcode;
                    arity = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (arity > 0)
            {
                Operand[] operands = {null, null, null};
                byte flags = this.ReadByte();
                int i;
                for (i = 1; i <= arity; i++)
                {
                    switch (flags >> 6)
                    {
                        case 0: // memory addr
                            operands[i] = new Operand(this, Operand.OperandType.AddressBlock); // addressblock code to be redone
                            break;
                        case 1: // register
                            operands[i] = new Operand(this, Operand.OperandType.Register);
                            break;
                        case 2: // stack index
                            operands[i] = new Operand(this, Operand.OperandType.StackIndex);
                            break;
                        case 3: // literal
                            byte type = this.ReadByte();
                            operands[i] = new Operand(this, (Operand.OperandType)(type - 3));
                            break;
                    }
                    flags <<= 2;
                }
                // check operand types
                
            }
        }

        private ByteBlock GetValue(Operand op)
        {
            ByteBlock result;
            switch (op.Type)
            {
                case Operand.OperandType.AddressBlock:
                    break;
                case Operand.OperandType.Register:
                    result = this.ReadRegister((byte)op.Value);
                    break;
                case Operand.OperandType.StackIndex:
                    break;
                case Operand.OperandType.NumericByte:
                    result = new ByteBlock((byte)op.Value);
                    break;
                case Operand.OperandType.NumericSByte:
                    result = new ByteBlock((sbyte)op.Value);
                    break;
                case Operand.OperandType.NumericShort:
                    result = new ByteBlock((short)op.Value);
                    break;
                case Operand.OperandType.NumericUShort:
                    result = new ByteBlock((ushort)op.Value);
                    break;
                case Operand.OperandType.NumericInt:
                    result = new ByteBlock((int)op.Value);
                    break;
                case Operand.OperandType.NumericUInt:
                    result = new ByteBlock((uint)op.Value);
                    break;
                case Operand.OperandType.NumericLong:
                    result = new ByteBlock((long)op.Value);
                    break;
                case Operand.OperandType.NumericULong:
                    result = new ByteBlock((ulong)op.Value);
                    break;
                case Operand.OperandType.NumericFloat:
                    result = new ByteBlock((float)op.Value);
                    break;
                case Operand.OperandType.NumericDouble:
                    result = new ByteBlock((double)op.Value);
                    break;
                case Operand.OperandType.LPString:
                    result = new ByteBlock((string)op.Value);
                    break;
                default:
                    throw new ArgumentException();
            }
            return result;
        }

        private void LongArithmeticInstruction(DataOpcode opcode, Operand left, Operand right, Operand dest)
        {
            if (left.Type == Operand.OperandType.LPString || right.Type == Operand.OperandType.LPString)
            {
                throw new ArgumentException("can't do arithmetic on strings");
            }
            if (dest.Type != Operand.OperandType.AddressBlock && dest.Type != Operand.OperandType.Register && dest.Type != Operand.OperandType.StackIndex)
            {
                throw new ArgumentException("can't assign to a literal");
            }

            ByteBlock op1 = GetValue(left);
            ByteBlock op2 = GetValue(right);


            ulong uresult;
            long sresult;
            double fresult;
            if ((left.Type == Operand.OperandType.NumericByte || left.Type == Operand.OperandType.NumericUShort ||
                 left.Type == Operand.OperandType.NumericUInt || left.Type == Operand.OperandType.NumericULong ||
                 left.Type == Operand.OperandType.Register || left.Type == Operand.OperandType.AddressBlock) &&
                (right.Type == Operand.OperandType.NumericByte || right.Type == Operand.OperandType.NumericUShort ||
                 right.Type == Operand.OperandType.NumericUInt || right.Type == Operand.OperandType.NumericULong ||
                 right.Type == Operand.OperandType.Register || right.Type == Operand.OperandType.AddressBlock)) // for now, register & memory values are only treated as ulongs.  Fix
            {
                uresult = UnsignedArithmetic(opcode, op1.ToULong(), op2.ToULong());
                if (dest.Type == Operand.OperandType.Register)
                {
                    // add
                }
                else
                {
                    // add
                }
            } else if ((left.Type == Operand.OperandType.NumericSByte || left.Type == Operand.OperandType.NumericShort ||
                        left.Type == Operand.OperandType.NumericInt || left.Type == Operand.OperandType.NumericLong) &&
                       (right.Type == Operand.OperandType.NumericSByte || right.Type == Operand.OperandType.NumericShort ||
                        right.Type == Operand.OperandType.NumericInt || right.Type == Operand.OperandType.NumericLong))
            {
                sresult = SignedArithmetic(opcode, op1.ToLong(), op2.ToLong());
                if (dest.Type == Operand.OperandType.Register)
                {
                    // add
                }
                else
                {
                    // add
                }
            }
            else
            {
                // add
                if (dest.Type == Operand.OperandType.Register)
                {
                    // add
                }
                else
                {
                    // add
                }
            }
        }

        private ulong UnsignedArithmetic(DataOpcode opcode, ulong left, ulong right)
        {
            ulong result;
            switch (opcode)
            {
                case DataOpcode.MOV:
                    break;
                case DataOpcode.ADD:
                case DataOpcode.ADDL:
                    result = left + right;
                    break;
                case DataOpcode.SUB:
                case DataOpcode.SUBL:
                    result = left - right;
                    break;
                case DataOpcode.MULT:
                case DataOpcode.MULTL:
                    result = left * right;
                    break;
                case DataOpcode.DIV:
                case DataOpcode.DIVL:
                    result = left / right;
                    break;
                case DataOpcode.MOD:
                case DataOpcode.MODL:
                    result = left % right;
                    break;
                case DataOpcode.INV:
                case DataOpcode.INVL:
                    throw new ArgumentException("can't invert an unsigned number"); //maybe use something other than ArgumentException
                case DataOpcode.EQ:
                case DataOpcode.EQL:
                    result = left == right ? 1UL : 0UL;
                    break;
                case DataOpcode.INEQ:
                case DataOpcode.INEQL:
                    result = left == right ? 0UL : 1UL;
                    break;
                case DataOpcode.LT:
                case DataOpcode.LTL:
                    result = left < right ? 1UL : 0UL;
                    break;
                case DataOpcode.GT:
                case DataOpcode.GTL:
                    result = left > right ? 1UL : 0UL;
                    break;
                case DataOpcode.LTEQ:
                case DataOpcode.LTEQL:
                    result = left > right ? 0UL : 1UL;
                    break;
                case DataOpcode.GTEQ:
                case DataOpcode.GTEQL:
                    result = left < right ? 0UL : 1UL;
                    break;
                case DataOpcode.AND:
                case DataOpcode.ANDL:
                    result = (left != 0UL && right != 0UL) ? 1UL : 0UL;
                    break;
                case DataOpcode.OR:
                case DataOpcode.ORL:
                    result = (left != 0UL && right != 0UL) ? 1UL : 0UL;
                    break;
                case DataOpcode.NOT:
                case DataOpcode.NOTL:
                    result = (left != 0UL) ? 0UL : 1UL;
                    break;
                case DataOpcode.BWNOT:
                case DataOpcode.BWNOTL:
                    result = ~left;
                    break;
                case DataOpcode.BWAND:
                case DataOpcode.BWANDL:
                    result = left & right;
                    break;
                case DataOpcode.BWOR:
                case DataOpcode.BWORL:
                    result = left | right;    
                    break;
                case DataOpcode.BWXOR:
                case DataOpcode.BWXORL:
                    result = left ^ right;
                    break;
                case DataOpcode.BWLSHIFT:
                case DataOpcode.BWLSHIFTL:
                    result = left << (int)right; // max 63 bits
                    break;
                case DataOpcode.BWRSHIFT:
                case DataOpcode.BWRSHIFTL:
                    result = left >> (int)right;
                    break;
                case DataOpcode.PUSH:
                    break;
                case DataOpcode.POP:
                    break;
                case DataOpcode.PEEK:
                    break;
                case DataOpcode.STACKALLOC:
                    break;
                case DataOpcode.ARRAYALLOC:
                    break;
                case DataOpcode.DEREF:
                    break;
                case DataOpcode.ARRAYACCESS:
                    break;
                case DataOpcode.CBYTE:
                    break;
                case DataOpcode.CSBYTE:
                    break;
                case DataOpcode.CSHORT:
                    break;
                case DataOpcode.CUSHORT:
                    break;
                case DataOpcode.CINT:
                    break;
                case DataOpcode.CUINT:
                    break;
                case DataOpcode.CLONG:
                    break;
                case DataOpcode.CULONG:
                    break;
                case DataOpcode.CSING:
                    break;
                case DataOpcode.CDOUBLE:
                    break;
                default:
                    break;
            }
            return result;
        }

        private long SignedArithmetic(DataOpcode opcode, long left, long right) // I don't like this repetition
        {
            long result;
            switch (opcode)
            {
                case DataOpcode.MOV:
                    break;
                case DataOpcode.ADD:
                case DataOpcode.ADDL:
                    result = left + right;
                    break;
                case DataOpcode.SUB:
                case DataOpcode.SUBL:
                    result = left - right;
                    break;
                case DataOpcode.MULT:
                case DataOpcode.MULTL:
                    result = left * right;
                    break;
                case DataOpcode.DIV:
                case DataOpcode.DIVL:
                    result = left / right;
                    break;
                case DataOpcode.MOD:
                case DataOpcode.MODL:
                    result = left % right;
                    break;
                case DataOpcode.INV:
                case DataOpcode.INVL:
                    result = -left;
                    break;
                case DataOpcode.EQ:
                case DataOpcode.EQL:
                    result = left == right ? 1L : 0L;
                    break;
                case DataOpcode.INEQ:
                case DataOpcode.INEQL:
                    result = left == right ? 0L : 1L;
                    break;
                case DataOpcode.LT:
                case DataOpcode.LTL:
                    result = left < right ? 1L : 0L;
                    break;
                case DataOpcode.GT:
                case DataOpcode.GTL:
                    result = left > right ? 1L : 0L;
                    break;
                case DataOpcode.LTEQ:
                case DataOpcode.LTEQL:
                    result = left > right ? 0L : 1L;
                    break;
                case DataOpcode.GTEQ:
                case DataOpcode.GTEQL:
                    result = left < right ? 0L : 1L;
                    break;
                case DataOpcode.AND:
                case DataOpcode.ANDL:
                    result = (left != 0L && right != 0L) ? 1L : 0L;
                    break;
                case DataOpcode.OR:
                case DataOpcode.ORL:
                    result = (left != 0L && right != 0L) ? 1L : 0L;
                    break;
                case DataOpcode.NOT:
                case DataOpcode.NOTL:
                    result = (left != 0L) ? 0L : 1L;
                    break;
                case DataOpcode.BWNOT:
                case DataOpcode.BWNOTL:
                    result = ~left;
                    break;
                case DataOpcode.BWAND:
                case DataOpcode.BWANDL:
                    result = left & right;
                    break;
                case DataOpcode.BWOR:
                case DataOpcode.BWORL:
                    result = left | right;
                    break;
                case DataOpcode.BWXOR:
                case DataOpcode.BWXORL:
                    result = left ^ right;
                    break;
                case DataOpcode.BWLSHIFT:
                case DataOpcode.BWLSHIFTL:
                    result = left << (int)right; // max 63 bits
                    break;
                case DataOpcode.BWRSHIFT:
                case DataOpcode.BWRSHIFTL:
                    result = left >> (int)right;
                    break;
                case DataOpcode.PUSH:
                    break;
                case DataOpcode.POP:
                    break;
                case DataOpcode.PEEK:
                    break;
                case DataOpcode.STACKALLOC:
                    break;
                case DataOpcode.ARRAYALLOC:
                    break;
                case DataOpcode.DEREF:
                    break;
                case DataOpcode.ARRAYACCESS:
                    break;
                case DataOpcode.CBYTE:
                    break;
                case DataOpcode.CSBYTE:
                    break;
                case DataOpcode.CSHORT:
                    break;
                case DataOpcode.CUSHORT:
                    break;
                case DataOpcode.CINT:
                    break;
                case DataOpcode.CUINT:
                    break;
                case DataOpcode.CLONG:
                    break;
                case DataOpcode.CULONG:
                    break;
                case DataOpcode.CSING:
                    break;
                case DataOpcode.CDOUBLE:
                    break;
                default:
                    break;
            }
            return result;
        }
        #endregion
    }

    private class Operand
    {
        public enum OperandType : byte
        {
            AddressBlock,
            Register,
            StackIndex,
            NumericByte,
            NumericSByte,
            NumericShort,
            NumericUShort,
            NumericInt,
            NumericUInt,
            NumericLong,
            NumericULong,
            NumericFloat,
            NumericDouble,
            LPString
        };
        public OperandType Type;
        public object Value; // maybe change
        public uint Length;

        public Operand(Processor cpu, OperandType optype)
        {
            Type = optype;
            switch (Type)
            {
                case OperandType.AddressBlock:
                    Length = 8;
                    // add later
                    break;
                case OperandType.Register:
                    Length = 1;
                    Value = cpu.ReadByte();
                    break;
                case OperandType.StackIndex:
                    Length = 4;
                    Value = cpu.ReadUInt();
                    break;
                case OperandType.NumericByte:
                    Length = 1;
                    Value = cpu.ReadByte();
                    break;
                case OperandType.NumericSByte:
                    Length = 1;
                    Value = cpu.ReadSByte();
                    break;
                case OperandType.NumericShort:
                    Length = 2;
                    Value = cpu.ReadShort();
                    break;
                case OperandType.NumericUShort:
                    Length = 2;
                    Value = cpu.ReadUShort();
                    break;
                case OperandType.NumericInt:
                    Length = 4;
                    Value = cpu.ReadInt();
                    break;
                case OperandType.NumericUInt:
                    Length = 4;
                    Value = cpu.ReadUInt();
                    break;
                case OperandType.NumericLong:
                    Length = 8;
                    Value = cpu.ReadLong();
                    break;
                case OperandType.NumericULong:
                    Length = 8;
                    Value = cpu.ReadULong();
                    break;
                case OperandType.NumericFloat:
                    Length = 4;
                    Value = cpu.ReadFloat();
                    break;
                case OperandType.NumericDouble:
                    Length = 8;
                    Value = cpu.ReadDouble();
                    break;
                case OperandType.LPString:
                    Length = cpu.ReadUInt();
                    ByteBlock bytes = cpu.Read(Length);
                    Value = System.Text.Encoding.UTF8.GetString(bytes.ToByteArray());
                    break;
                default:
                    break;
            }
        }
    }
}
