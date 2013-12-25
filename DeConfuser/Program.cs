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
using System.IO;
using Mono.Cecil;
using System.Diagnostics;
using DeConfuser.Removers;
using Mono.Cecil.Cil;

namespace DeConfuser
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "DeConfuser - The De-Obfuscator for confuser v1.6";
            Console.WriteLine("Copyright © DragonHunter - 2012");
            Console.WriteLine("This deobfuscator might not work at every confused assembly, still BETA");
            Console.WriteLine("Checkout this project at http://deconfuser.codeplex.com");
            Console.WriteLine("Thanks also to Mono.Cecil there was no DeConfuser without Mono.Cecil");
            Console.WriteLine("This version of Mono.Cecil is modded by DragonHunter to do some evil shit");

            //hardcoded path atm...
            string inputPath = @"H:\DeConfuser\ConfuseMe\bin\Debug\confused\ConfuseMe.exe";
            string outputPath = @"H:\DeConfuser\ConfuseMe\bin\Debug\confused\ConfuseMe_cleaned.exe";

            //load assembly
            AssemblyDefinition asm = AssemblyFactory.GetAssembly(inputPath);

            #region Anti-Debug remover
            AntiDebug debug = new AntiDebug();
            TypeDefinition AntiType = null;
            MethodDefinition AntiMethod = null;
            Console.WriteLine("-------------------------------------------------------");
            if (debug.FindAntiDebug(asm, ref AntiType, ref AntiMethod))
            {
                Console.WriteLine("[Anti-Debugger] Anti-Debugger detected, removing...");
                debug.RemoveAntiDebug(asm, AntiType, AntiMethod);
                Console.WriteLine("[Anti-Debugger] Removed anti-debugger");
            }
            else
            {
                Console.WriteLine("This assembly is not protected with anti-debugging");
            }
            Console.WriteLine("-------------------------------------------------------");
            #endregion
            #region String Decryptor
            StringDecrypter decrypter = new StringDecrypter();
            TypeDefinition DecryptType = null;
            MethodDefinition DecryptMethod = null;
            if (decrypter.FindMethod(asm, ref DecryptType, ref DecryptMethod))
            {
                Console.WriteLine("[String Decryptor] Found string decryptor, decrypting strings...");
                byte[] StringData = decrypter.GetStringResource(asm, inputPath, DecryptMethod);
                decrypter.DecryptAllStrings(asm, DecryptMethod, StringData);
                decrypter.RemoveDecryptMethod(asm, DecryptType, DecryptMethod);
                Console.WriteLine("[String Decryptor] Removed the decrypt method");
            }
            else
            {
                Console.WriteLine("This assembly is not protected with encrypted strings");
            }
            Console.WriteLine("-------------------------------------------------------");
            #endregion
            #region Anti-Dump remover
            AntiDump dump = new AntiDump();
            TypeDefinition AntiDumpType = null;
            MethodDefinition AntiDumpMethod = null;
            if (dump.FindAntiDump(asm, ref AntiDumpType, ref AntiDumpMethod))
            {
                Console.WriteLine("[Anti-Dump] Anti-Dump detected, removing...");
                dump.RemoveAntiDump(asm, AntiDumpType, AntiDumpMethod);
                Console.WriteLine("[Anti-Dump] Removed anti-dump");
            }
            else
            {
                Console.WriteLine("This assembly is not protected with anti-dump");
            }
            Console.WriteLine("-------------------------------------------------------");
            #endregion
            #region Resource Decryptor
            ResourceDecrypter resourceDecrypter = new ResourceDecrypter();
            TypeDefinition ResourceType = null;
            MethodDefinition ResourceMethod = null;
            if (resourceDecrypter.FindMethod(asm, ref ResourceType, ref ResourceMethod))
            {
                Console.WriteLine("[Resource-Decrypter] Resource-Decrypter, decrypting");
                resourceDecrypter.DecryptAllResources(asm, inputPath, ResourceType, ResourceMethod);
            }
            else
            {
                Console.WriteLine("This assembly is not protected with encrypted resources");
            }
            Console.WriteLine("-------------------------------------------------------");
            #endregion


            AssemblyFactory.SaveAssembly(asm, outputPath);
            Console.WriteLine("File dumped to \"" + outputPath + "\"");
            Console.WriteLine("Thanks for using DeConfuser :)");
            Process.GetCurrentProcess().WaitForExit();
        }
        
        public static bool ScanSignature(MethodDefinition m, OpCode[] Signature)
        {
            bool found = true;
            for (int j = 0; j < m.Body.Instructions.Count && j < Signature.Length; j++)
            {
                if (m.Body.Instructions[j].OpCode != Signature[j])
                {
                    found = false;
                    break;
                }
            }
            return found;
        }
        public static bool ReadSigKey(MethodDefinition DecryptMethod, OpCode[] KeySig, ref int val)
        {
            object value = null;
            bool ret = ReadSigKey(DecryptMethod, KeySig, ref value);
            if (value != null)
                val = Convert.ToInt32(value);
            return ret;
        }
        public static bool ReadSigKey(MethodDefinition DecryptMethod, OpCode[] KeySig, ref string val)
        {
            object value = null;
            bool ret = ReadSigKey(DecryptMethod, KeySig, ref value);
            if (value != null)
                val = value.ToString();
            return ret;
        }
        public static bool ReadSigKey(MethodDefinition DecryptMethod, OpCode[] KeySig, ref object val)
        {
            int score = 0;
            for (int i = 0; i < DecryptMethod.Body.Instructions.Count; i++)
            {
                if (DecryptMethod.Body.Instructions[i].OpCode == KeySig[score])
                {
                    score++;
                    if (score == KeySig.Length)
                    {
                        if (DecryptMethod.Body.Instructions[i].Next != null)
                        {
                            if (DecryptMethod.Body.Instructions[i].Next.Operand != null)
                            {
                                val = DecryptMethod.Body.Instructions[i].Next.Operand;
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    score = 0;
                }
            }
            return false;
        }
    }
}