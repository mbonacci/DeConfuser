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
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using Mono.Cecil.Metadata;

namespace DeConfuser.Removers
{
    public class StringDecrypter
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

        };

        public StringDecrypter()
        {

        }

        public bool FindMethod(AssemblyDefinition asm, ref TypeDefinition DecryptType, ref MethodDefinition DecryptMethod)
        {
            foreach(ModuleDefinition module in asm.Modules)
            {
                for (int i = 0; i < module.Types.Count; i++)
                {
                    //well since Confuser only dumps his AntiDebug in <Module> we only check there
                    if (module.Types[i].Name != "<Module>")
                        continue;

                    foreach (MethodDefinition m in module.Types[i].Methods)
                    {
                        if (!m.HasBody)
                            continue;

                        if (Program.ScanSignature(m, Signature))
                        {
                            DecryptType = (TypeDefinition)m.DeclaringType;
                            DecryptMethod = m;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public byte[] GetStringResource(AssemblyDefinition asm, string FilePath, MethodDefinition method)
        {
            if (!method.HasBody)
            {
                return new byte[0];
            }

            //funny... the first string in the method is a resource name
            string name = "";
            for(int i = 0; i < method.Body.Instructions.Count; i++)
            {
                if(method.Body.Instructions[i].OpCode.Code == Mono.Cecil.Cil.Code.Ldstr)
                {
                    if(method.Body.Instructions[i].Operand == null)
                        continue;
                    name = method.Body.Instructions[i].Operand.ToString();
                    break;
                }
            }

            foreach (Resource res in asm.MainModule.Resources)
            {
                if(res.Name == name)
                {
                    if(res.GetType() == typeof(EmbeddedResource))
                    {
                        //lets read it the way confuser is doing...
                        MemoryStream stream = new MemoryStream();
                        using (DeflateStream stream2 = new DeflateStream(System.Reflection.Assembly.LoadFile(FilePath).GetManifestResourceStream(name), CompressionMode.Decompress))
                        {
                            byte[] buffer = new byte[0x1000];
                            int count = stream2.Read(buffer, 0, 0x1000);
                            do
                            {
                                stream.Write(buffer, 0, count);
                                count = stream2.Read(buffer, 0, 0x1000);
                            }while (count != 0);
                        }
                        return stream.ToArray();
                    }
                }
            }
            return new byte[0];
        }

        public void DecryptAllStrings(AssemblyDefinition asm, MethodDefinition DecryptMethod, byte[] StringData)
        {
            //this is for getting the key... just a signature would help us :3
            //this key is being generated randomly at while using the obfuscator
            OpCode[] KeySig = new OpCode[]
            {
                OpCodes.Nop,
                OpCodes.Nop,
                OpCodes.Ldc_I4_1,
                OpCodes.Newobj,
                OpCodes.Callvirt,
                OpCodes.Callvirt,
                OpCodes.Stloc_S,
                OpCodes.Ldloc_S,
                OpCodes.Ldarg_0,
                OpCodes.Xor
                //key
                //nop
            };
            OpCode[] NumKeySig = new OpCode[]
            {
                OpCodes.Callvirt,
                OpCodes.Ldloc_S,
                OpCodes.Conv_I8,
                OpCodes.Ldc_I4_0,
                OpCodes.Callvirt,
                OpCodes.Pop,
                OpCodes.Ldloc_S,
                OpCodes.Callvirt,
                OpCodes.Not
            };
            OpCode[] SeedKeySig = new OpCode[]
            {
                OpCodes.Ldc_I4,
                OpCodes.Xor,
                OpCodes.Stloc_S,
                OpCodes.Ldloc_S,
                OpCodes.Ldloc_S,
                OpCodes.Callvirt,
                OpCodes.Stloc_S
            };

            //yep my sucky signature seeker
            int key = 0;
            int NumKey = 0;
            int SeedKey = 0;
            if (!Program.ReadSigKey(DecryptMethod, KeySig, ref key) || !Program.ReadSigKey(DecryptMethod, NumKeySig, ref NumKey) ||
                !Program.ReadSigKey(DecryptMethod, SeedKeySig, ref SeedKey))
            {
                Console.WriteLine("One of the keys could not be found ;(!");
                return;
            }

            //time to decrypt everything ;)
            foreach (TypeDefinition t in asm.MainModule.Types)
            {
                foreach (MethodDefinition m in t.Methods)
                {
                    if (!m.HasBody)
                        continue;

                    //lets look where our decrypt method is called
                    for (int i = 0; i < m.Body.Instructions.Count; i++)
                    {
                        if (m.Body.Instructions[i].Operand == DecryptMethod)
                        {
                            //1 instruction before call is our ID-KEY
                            if (m.Body.Instructions[i].Previous != null)
                            {
                                int id = Convert.ToInt32(m.Body.Instructions[i].Previous.Operand);
                                int token = (int)m.MetadataToken.ToUInt();
                                int Offset = (token ^ id) - key;

                                //decrypt here
                                string str = "";
                                using (BinaryReader reader = new BinaryReader(new MemoryStream(StringData)))
                                {
                                    reader.BaseStream.Position = Offset;
                                    int num4 = ((int)~reader.ReadUInt32()) ^ NumKey;
                                    byte[] bytes = reader.ReadBytes(num4);
                                    string crypted = ASCIIEncoding.ASCII.GetString(bytes);

                                    Random random = new Random(SeedKey);
                                    int num5 = 0;
                                    for (int j = 0; j < bytes.Length; j++)
                                    {
                                        byte num7 = bytes[j];
                                        bytes[j] = (byte) (bytes[j] ^ (random.Next() & num5));
                                        num5 += num7;
                                    }
                                    str = ASCIIEncoding.ASCII.GetString(bytes);
                                    Console.WriteLine("[String Decryptor] \"" + crypted + "\" -->> \"" + str + "\"");
                                }

                                //ok when it's all decrypted lets put the original string back
                                m.Body.Instructions[i-1] = new Instruction(OpCodes.Ldstr, str);
                                m.Body.Instructions.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }
            }
        }

        public void RemoveDecryptMethod(AssemblyDefinition asm, TypeDefinition DecryptType, MethodDefinition DecryptMethod)
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
                            if (m.Body.Instructions[x].Operand == DecryptMethod)
                            {
                                m.Body.Instructions.Remove(m.Body.Instructions[x]);
                                x--;
                            }
                        }
                    }
                }
            }
            DecryptType.Methods.Remove(DecryptMethod);
        }
    }
}