﻿#region License
/*
Klei Studio is licensed under the MIT license.
Copyright © 2013 Matt Stevens

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion License

using System;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;

namespace TEXTool
{
    static class Program
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [STAThread]
        static void Main(string[] args)
        {
            bool ConsoleMode = args.Length > 1;

            if (ConsoleMode) {
                AllocConsole();

                var inputFile = args[0];
                var outputFile = args[1];

                var tool = new TEXTool();
                tool.OpenFile(inputFile, new FileStream(inputFile, FileMode.Open, FileAccess.Read));
                tool.FileOpened += (sender, ev) => {
                    tool.SaveFile(outputFile);
                };
            } else {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                MainForm form = new MainForm();

                // Open With..
                //if (args.Length > 0)
                //    form.Tool.OpenFile(args[0], new FileStream(args[0], FileMode.Open, FileAccess.Read));

                Task t = new Task(() =>
                {
                    Thread.Sleep(1000);
                    if (args.Length > 0)
                    {
                        form.OpenExternalFile(args[0]);
                    }
                });
                t.Start();

                Application.Run(form);
            }
        }
    }
}
