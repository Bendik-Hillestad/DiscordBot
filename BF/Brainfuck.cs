using System;
using System.Collections.Generic;

namespace DiscordBot.BF
{
    public sealed class Brainfuck
    {
        public enum BFToken : int
        {
            [Token(@"\<")]
            MOVL,

            [Token(@"\>")]
            MOVR,

            [Token(@"\-")]
            DEC,

            [Token(@"\+")]
            INC,

            [Token(@"\.")]
            PRNT,

            [Token(@"\,")]
            READ,

            [Token(@"\[")]
            LSTART,

            [Token(@"\]")]
            LEND
        }

        public struct OP
        {
            public enum Code
            {
                ENTRY,  EXIT,
                DECPTR, SUBPTR,
                INCPTR, ADDPTR,
                DEC,    SUB,
                INC,    ADD,
                PRNT,   READ,
                LSTART, LCNT, LEND
            }

            public Code opCode;
            public int? operand;
        }

        public static BFToken[] TokenizeBF(string bfCode)
        {
            //Get token list
            var tokenList = Token.GetTokens<BFToken>();

            //Tokenize string
            return Tokenizer.Tokenize<BFToken>(bfCode, tokenList);
        }

        public static OP[] Parse(BFToken[] tokenList)
        {
            var ops        = new List<OP>();
            var labelStack = new Stack<Tuple<int, int>>();
            var curLabel   = new Tuple<int, int>(0, 0);
            int labelCount = 0;
            var prevToken  = (BFToken?)null;
            int tokenCount = 0;

            //Push entry op
            ops.Add(new OP { opCode = OP.Code.ENTRY });

            //Iterate over tokens
            for (int i = 0; i < tokenList.Length + 1; i++)
            {
                //Get token
                var token = (i < tokenList.Length) ? tokenList[i] : (BFToken?)null;

                //Check if previous token is null
                if (prevToken == null)
                {
                    //Just save this one
                    prevToken  = token;
                    tokenCount = 1;

                    //Get next token
                    continue;
                }

                //Check if this token differs from the previous one
                if (token != prevToken)
                {
                    //Switch on previous token
                    switch (prevToken)
                    {
                        case BFToken.MOVL:
                        {
                            //Check if there's more than 1 of this token
                            if (tokenCount > 1)
                            {
                                //Emit OP for multiple moves
                                ops.Add(new OP { opCode = OP.Code.SUBPTR, operand = tokenCount });
                            }
                            else
                            {
                                //Emit OP for single move
                                ops.Add(new OP { opCode = OP.Code.DECPTR });
                            }
                        } break;

                        case BFToken.MOVR:
                        {
                            //Check if there's more than 1 of this token
                            if (tokenCount > 1)
                            {
                                //Emit OP for multiple moves
                                ops.Add(new OP { opCode = OP.Code.ADDPTR, operand = tokenCount });
                            }
                            else
                            {
                                //Emit OP for single move
                                ops.Add(new OP { opCode = OP.Code.INCPTR });
                            }
                        } break;

                        case BFToken.DEC:
                        {
                            //Check if there's more than 1 of this token
                            if (tokenCount > 1)
                            {
                                //Emit OP for multiple decrements
                                ops.Add(new OP { opCode = OP.Code.SUB, operand = tokenCount });
                            }
                            else
                            {
                                //Emit OP for single decrement
                                ops.Add(new OP { opCode = OP.Code.DEC });
                            }
                        } break;

                        case BFToken.INC:
                        {
                            //Check if there's more than 1 of this token
                            if (tokenCount > 1)
                            {
                                //Emit OP for multiple increments
                                ops.Add(new OP { opCode = OP.Code.ADD, operand = tokenCount });
                            }
                            else
                            {
                                //Emit OP for single increment
                                ops.Add(new OP { opCode = OP.Code.INC });
                            }
                        } break;

                        case BFToken.PRNT:
                        {
                            //Emit OP for n prints
                            ops.Add(new OP { opCode = OP.Code.PRNT, operand = tokenCount });
                        } break;

                        case BFToken.READ:
                        {
                            //Emit OP for n reads
                            ops.Add(new OP { opCode = OP.Code.READ, operand = tokenCount });
                        } break;

                        case BFToken.LSTART:
                        {
                            //Emit OP for loop start
                            ops.Add(new OP { opCode = OP.Code.LSTART, operand = labelCount++ });

                            //Check if we simply need to replace the current label
                            if (curLabel.Item2 == 0) curLabel = new Tuple<int, int>(ops.Count - 1, tokenCount);
                            else
                            {
                                //Push current label onto the stack
                                labelStack.Push(curLabel);

                                //Update current label
                                curLabel = new Tuple<int, int>(ops.Count - 1, tokenCount);
                            }
                        } break;

                        case BFToken.LEND:
                        {
                            //For each terminator
                            for (int j = 0; j < tokenCount; j++)
                            {
                                //Check if we need to pop the next label
                                if (curLabel.Item2 == 0)
                                {
                                    //Try to pop next label
                                    if (labelStack.Count > 0) curLabel = labelStack.Pop();
                                    else throw new Exception("mismatched brackets");
                                }
                                
                                //Check if we're at the start of the loop
                                if (j == 0)
                                {
                                    //Check if we will need to emit an end label
                                    if (curLabel.Item2 <= tokenCount)
                                    {
                                        //Increment label count
                                        labelCount++;

                                        //Emit OP for loop end
                                        ops.Add(new OP { opCode = OP.Code.LEND, operand = ops[curLabel.Item1].operand });
                                    }
                                    else
                                    {
                                        //Emit OP for loop continue
                                        ops.Add(new OP { opCode = OP.Code.LCNT, operand = ops[curLabel.Item1].operand });
                                    }
                                }

                                //Decrement remaining loop terminators
                                curLabel = new Tuple<int, int>(curLabel.Item1, curLabel.Item2 - 1);

                                //Check if this is the end of the loop
                                if (curLabel.Item2 == 0)
                                {
                                    //Adjust loop start to refer to the loop end emitted earlier
                                    ops[curLabel.Item1] = new OP { opCode = OP.Code.LSTART, operand = labelCount - 1 };
                                }
                            }
                        } break;

                        default: break; //Unknown token
                    }

                    //Update previous token
                    prevToken  = token;
                    tokenCount = 1;
                }
                else
                {
                    //Increment token count
                    tokenCount++;
                }
            }

            //Check that the label stack is empty and the current label doesn't have any more matches
            if (labelStack.Count != 0 || curLabel.Item2 != 0)
                throw new Exception("Mismatched brackets");

            //Push exit op
            ops.Add(new OP { opCode = OP.Code.EXIT });

            //Return operations
            return ops.ToArray();
        }

        public static string GenerateASM(OP[] ops)
        {
            //Initialise code
            var code       = "";

            //Track labels
            int labelCount = 0;

            //Track whether we can omit the cmp operation
            bool zf        = false;

            //Iterate over operations
            for (int i = 0; i < ops.Length; i++)
            {
                //Grab operation
                var op = ops[i];

                //Switch on opcode
                switch (op.opCode)
                {
                    case OP.Code.ENTRY:
                    {
                        //Emit .main label
                        code += $".main:\n";
                        zf    = false;
                    } break;

                    case OP.Code.EXIT:
                    {
                        //Emit code for returning pointer and exiting
                        code += $"\tmov  eax, ecx\n";
                        code += $"\tret";
                        zf    = false;
                    } break;

                    case OP.Code.DECPTR:
                    {
                        //Emit pointer decrement
                        code += $"\tdec  ecx\n";
                        zf    = false;
                    } break;

                    case OP.Code.SUBPTR:
                    {
                        //Emit pointer subtraction
                        code += $"\tsub  ecx, {op.operand}\n";
                        zf    = false;
                    } break;

                    case OP.Code.INCPTR:
                    {
                        //Emit pointer increment
                        code += $"\tinc  ecx\n";
                        zf    = false;
                    } break;

                    case OP.Code.ADDPTR:
                    {
                        //Emit pointer addition
                        code += $"\tadd  ecx, {op.operand}\n";
                        zf    = false;
                    } break;

                    case OP.Code.DEC:
                    {
                        //Emit decrement
                        code += $"\tdec  BYTE PTR [ecx]\n";
                        zf    = true;
                    } break;

                    case OP.Code.SUB:
                    {
                        //Emit subtraction
                        code += $"\tsub  BYTE PTR [ecx], {op.operand}\n";
                        zf    = true;
                    } break;

                    case OP.Code.INC:
                    {
                        //Emit increment
                        code += $"\tinc  BYTE PTR [ecx]\n";
                        zf    = true;
                    } break;

                    case OP.Code.ADD:
                    {
                        //Emit addition
                        code += $"\tadd  BYTE PTR [ecx], {op.operand}\n";
                        zf    = true;
                    } break;

                    case OP.Code.PRNT:
                    {
                        //Emit code for printing value at pointer n times
                        code += $"\tpush ecx\n";
                        code += $"\tmov  ecx, BYTE PTR [ecx]\n";
                        for (int j = 0; j < op.operand; j++)
                            code += $"\tcall putc\n";
                        code += $"\tpop  ecx\n";
                        zf    = false;
                    } break;

                    case OP.Code.READ:
                    {
                        //Emit code for reading from input n times, only storing the last value
                        for (int j = 0; j < op.operand; j++)
                            code += $"\tcall getc\n";
                        code += $"\tmov  BYTE PTR [ecx], eax\n";
                        zf    = false;
                    } break;

                    case OP.Code.LSTART:
                    {
                        //Emit code for branch start
                        if (!zf) code += $"\tcmp  BYTE PTR [ecx], 0\n";
                        code += $"\tje  .LB_{op.operand}\n";
                        code += $".LB_{labelCount++}:\n";
                        zf    = false;
                    } break;

                    case OP.Code.LCNT:
                    {
                        //Emit code for continue
                        if (!zf) code += $"\tcmp  BYTE PTR [ecx], 0\n";
                        code += $"\tjne .LB_{op.operand}\n";
                        zf    = false;
                    } break;

                    case OP.Code.LEND:
                    {
                        //Emit code for branch end
                        if (!zf) code += $"\tcmp  BYTE PTR [ecx], 0\n";
                        code += $"\tjne .LB_{op.operand}\n";
                        code += $".LB_{labelCount++}:\n";
                        zf    = false;
                    } break;

                    default: break;
                }
            }

            //Return code
            return code;
        }
    }
}
