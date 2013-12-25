using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil.Cil;
using Mono.Cecil;

namespace DeConfuser.Removers
{
    public class AntiDump
    {
        public OpCode[] Signature = new OpCode[]
        {
            OpCodes.Nop,
            OpCodes.Ldtoken,
            OpCodes.Call,
            OpCodes.Callvirt,
            OpCodes.Call,
            OpCodes.Call,
            OpCodes.Stloc_1,
            OpCodes.Ldloc_1,
            OpCodes.Ldc_I4_S,
            OpCodes.Conv_I,
            OpCodes.Add,
            OpCodes.Stloc_2,
            OpCodes.Ldloc_1,
            OpCodes.Ldloc_2,
            OpCodes.Ldind_U4,
            OpCodes.Add,
            OpCodes.Dup
        };

        public AntiDump()
        {

        }

        public bool FindAntiDump(AssemblyDefinition asm, ref TypeDefinition AntiType, ref MethodDefinition AntiMethod)
        {
            //lets scan the whole assembly for anti-debugging
            Console.WriteLine("[Anti-Dump] Searching for Anti-Dump");

            for (int i = 0; i < asm.MainModule.Types.Count; i++)
            {
                //well since Confuser only dumps his AntiDump in <Module> we only check there
                if (asm.MainModule.Types[i].Name != "<Module>")
                    continue;

                foreach (MethodDefinition m in asm.MainModule.Types[i].Constructors)
                {
                    if (!m.HasBody)
                        continue;

                    //lets go through every CALL and see if it's our anti-dump
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

        public void RemoveAntiDump(AssemblyDefinition asm, TypeDefinition AntiType, MethodDefinition AntiMethod)
        {
            for (int i = 0; i < asm.MainModule.Types.Count; i++)
            {
                //well since Confuser only dumps his AntiDump in <Module> we only check there
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