/*
Copyright (C) 2012 DragonHunter

This file is part of DeConfuser.

DeConfuser is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.

DeConfuser is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with DeConfuser. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil.Cil;
using Mono.Cecil;

namespace DeConfuser.Removers
{
    public class AntiDebug
    {
        public OpCode[] Signature = new OpCode[]
        {
            OpCodes.Nop,
            OpCodes.Ldstr,
            OpCodes.Call,
            OpCodes.Brtrue_S,
            OpCodes.Ldstr,
            OpCodes.Call,
            OpCodes.Ldnull,
            OpCodes.Ceq,
            OpCodes.Br_S
        };

        public AntiDebug()
        {

        }

        public bool FindAntiDebug(AssemblyDefinition asm, ref TypeDefinition AntiType, ref MethodDefinition AntiMethod)
        {
            //lets scan the whole assembly for anti-debugging
            Console.WriteLine("[Anti-Debugger] Searching for Anti-Debugger");

            for (int i = 0; i < asm.MainModule.Types.Count; i++)
            {
                //well since Confuser only dumps his AntiDebug in <Module> we only check there
                if (asm.MainModule.Types[i].Name != "<Module>")
                    continue;

                foreach (MethodDefinition m in asm.MainModule.Types[i].Constructors)
                {
                    if (!m.HasBody)
                        continue;

                    //lets go through every CALL and see if it's our anti-debug
                    for (int x = 0; x < m.Body.Instructions.Count; x++)
                    {
                        if (m.Body.Instructions[x].OpCode.Code == Code.Call)
                        {
                            //lets check it out
                            if (m.Body.Instructions[x].Operand == null)
                                continue;

                            if (m.Body.Instructions[x].Operand.GetType() == typeof(MethodDefinition))
                            {
                                MethodDefinition method = (MethodDefinition)m.Body.Instructions[x].Operand;
                                if (method.HasBody)
                                {
                                    if (Program.ScanSignature(method, Signature))
                                    {
                                        AntiType = asm.MainModule.Types[i];
                                        AntiMethod = method;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public void RemoveAntiDebug(AssemblyDefinition asm, TypeDefinition AntiType, MethodDefinition AntiMethod)
        {
            for (int i = 0; i < asm.MainModule.Types.Count; i++)
            {
                //well since Confuser only dumps his AntiDebug in <Module> we only check there
                if (asm.MainModule.Types[i].Name != "<Module>")
                    continue;

                foreach (MethodDefinition m in asm.MainModule.Types[i].Constructors)
                {
                    if (!m.HasBody)
                        continue;

                    for (int x = 0; x < m.Body.Instructions.Count; x++)
                    {
                        if (m.Body.Instructions[x].OpCode.Code == Code.Call)
                        {
                            if (m.Body.Instructions[x].Operand == AntiMethod)
                            {
                                m.Body.Instructions.Remove(m.Body.Instructions[x]);
                                x--;
                            }
                        }
                    }
                }
            }
            asm.MainModule.Types.Remove(AntiType);
        }
    }
}