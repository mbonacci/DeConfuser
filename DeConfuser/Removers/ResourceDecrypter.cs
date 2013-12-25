using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.IO.Compression;
using System.IO;

namespace DeConfuser.Removers
{
    public class ResourceDecrypter
    {
        public OpCode[] Signature = new OpCode[]
        {
            OpCodes.Nop,
            OpCodes.Call,
            OpCodes.Ldstr,
            OpCodes.Callvirt,
            OpCodes.Isinst,
            OpCodes.Dup,
            OpCodes.Stloc_0,
            OpCodes.Ldnull,
            OpCodes.Ceq,
            OpCodes.Ldc_I4_0,
            OpCodes.Ceq,
            OpCodes.Stloc_S,
        };

        public ResourceDecrypter()
        {
        }

        public bool FindMethod(AssemblyDefinition asm, ref TypeDefinition DecryptType, ref MethodDefinition DecryptMethod)
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

                    //lets go through every CALL and see if it's our anti-dump
                    for (int x = 0; x < m.Body.Instructions.Count; x++)
                    {
                        if (m.Body.Instructions[x].OpCode.Code == Code.Ldftn)
                        {
                            //lets check it out
                            if (m.Body.Instructions[x].Operand == null)
                                continue;

                            if (m.Body.Instructions[x].Operand.GetType() == typeof(MethodDefinition))
                            {
                                MethodDefinition method = (MethodDefinition)m.Body.Instructions[x].Operand;
                                if (method.HasBody)
                                {
                                    //lets scan signature
                                    if (Program.ScanSignature(method, Signature))
                                    {
                                        DecryptType = asm.MainModule.Types[i];
                                        DecryptMethod = method;
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

        public void DecryptAllResources(AssemblyDefinition asm, string FilePath, TypeDefinition type, MethodDefinition method)
        {
            string resourceName = "";
            int key = 0;
            if(!Program.ReadSigKey(method, new OpCode[] { OpCodes.Nop, OpCodes.Call }, ref resourceName))
            {
                Console.WriteLine("[Resource-Decrypter] Unable to get the resource name, Stopped decrypting resources");
                return;
            }

            //if we have 1 key... we have all 3 of them
            if(!Program.ReadSigKey(method, new OpCode[] {
                                                            OpCodes.Div,
                                                            OpCodes.Ldloc_3,
                                                            OpCodes.Ldloc_S,
                                                            OpCodes.Ldc_I4_1,
                                                            OpCodes.Add,
                                                            OpCodes.Ldelem_U1 }, ref key))
            {
                Console.WriteLine("[Resource-Decrypter] Unable to get the key, Stopped decrypting resources");
                return;
            }

            using (BinaryReader reader = new BinaryReader(new DeflateStream(System.Reflection.Assembly.LoadFile(FilePath).GetManifestResourceStream(resourceName), CompressionMode.Decompress)))
            {
                byte[] buffer = reader.ReadBytes(reader.ReadInt32());
                byte[] buffer2 = new byte[buffer.Length / 2];
                for (int i = 0; i < buffer.Length; i += 2)
                {
                    buffer2[i / 2] = (byte) (((buffer[i + 1] ^ key) * key) + (buffer[i] ^ key));
                }
                using (BinaryReader reader2 = new BinaryReader(new DeflateStream(new MemoryStream(buffer2), CompressionMode.Decompress)))
                {
                    //remove all the resources in the assembly we want to clean first
                    asm.MainModule.Resources.Clear();

                    AssemblyDefinition assembly = AssemblyFactory.GetAssembly(reader2.ReadBytes(reader2.ReadInt32()));
                    Console.WriteLine("[Resource-Decrypter] Decrypted the resources file");
                    foreach(Resource res in assembly.MainModule.Resources)
                    {
                        asm.MainModule.Resources.Add(res);
                        Console.WriteLine("[Resource-Decrypter] Injected resource \"" + res.Name + "\"");
                    }
                    Console.WriteLine("[Resource-Decrypter] Decrypted+Injected all resources");
                }
            }
            RemoveResourceStuff(asm, type, method);
            Console.WriteLine("[Resource-Decrypter] Removed Resources methods");
        }


        public void RemoveResourceStuff(AssemblyDefinition asm, TypeDefinition ResourceType, MethodDefinition ResourceMethod)
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
                            if(m.Body.Instructions[x].Operand == null)
                                continue;

                            Type adsa = m.Body.Instructions[x].Operand.GetType();
                            if(m.Body.Instructions[x].Operand.GetType() == typeof(MethodReference))
                            {
                                if(((MethodReference)m.Body.Instructions[x].Operand).Name.ToLower().Contains("get_currentdomain"))
                                {
                                    //ok lets now clean up the mess in the static constructor of <Module>
                                    m.Body.Instructions.Remove(m.Body.Instructions[x]);
                                    m.Body.Instructions.Remove(m.Body.Instructions[x].Next);
                                    m.Body.Instructions.Remove(m.Body.Instructions[x].Next.Next);
                                    m.Body.Instructions.Remove(m.Body.Instructions[x].Next.Next.Next);
                                    m.Body.Instructions.Remove(m.Body.Instructions[x].Next.Next.Next.Next);
                                }
                            }
                        }
                    }
                }
            }
            asm.MainModule.Types.Remove(ResourceType);
        }
    }
}