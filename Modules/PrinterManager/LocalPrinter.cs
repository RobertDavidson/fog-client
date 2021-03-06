﻿using System.Diagnostics;
using FOG.Handlers;

namespace FOG.Modules.PrinterManager
{
    class LocalPrinter : Printer
    {
        public LocalPrinter(string name, string file, string port, string model, bool defaulted)
        {
            Name = name;
            Port = port;
            File = file;
            Model = model;
            Default = defaulted;
            LogName = "LocalPrinter";
        }

        public override void Add()
        {
            Log.Entry(LogName, "Attempting to add printer:");
            Log.Entry(LogName, string.Format("--> Name = {0}", Name));
            Log.Entry(LogName, string.Format("--> Port = {0}", Port));
            Log.Entry(LogName, string.Format("--> File = {0}", File));
            Log.Entry(LogName, string.Format("--> Model = {0}", Model));

            var proc = Process.Start("rundll32.exe", 
                string.Format(" printui.dll,PrintUIEntry /if /q /b \"{0}\" /f \"{1}\" /r \"{2}\" /m \"{3}\"", Name, File, Port, Model));
            if (proc != null) proc.WaitForExit(120000);
        }
    }
}
