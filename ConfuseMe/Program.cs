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
using System.Diagnostics;

namespace ConfuseMe
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("oh my god a string D:");
            Console.WriteLine("OH snap... im decrypted");
            Process.GetCurrentProcess().WaitForExit();
        }
    }
}