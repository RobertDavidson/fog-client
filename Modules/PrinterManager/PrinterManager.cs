﻿/*
 * FOG Service : A computer management client for the FOG Project
 * Copyright (C) 2014-2015 FOG Project
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 3
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using FOG.Handlers;

namespace FOG.Modules
{
    /// <summary>
    ///     Manage printers
    /// </summary>
    class PrinterManager : AbstractModule
    {
        public PrinterManager()
        {
            Name = "PrinterManager";
            Description = "Manage printers";
        }

        protected override void doWork()
        {
            //Get printers
            var printerResponse = CommunicationHandler.GetResponse("/service/Printer.php", true);
            if (printerResponse.Error || printerResponse.GetField("mode").Equals("0")) return;

            var printerIDs = CommunicationHandler.ParseDataArray(printerResponse, "#printer", false);

            var printers = createPrinters(printerIDs);

            if (printerResponse.GetField("mode").Equals("ar"))
                removeExtraPrinters(printers);

            foreach (var printer in printers)
            {
                printer.Add();
                if(printer.Default)
                    printer.setDefault();
            }
        }

        private void removeExtraPrinters(IEnumerable<Printer> newPrinters)
        {
            var printerQuery = new ManagementObjectSearcher("SELECT * from Win32_Printer");
            foreach (var name in from ManagementBaseObject printer in printerQuery.Get() select printer.GetPropertyValue("Name").ToString() into name let safe = newPrinters.Any(newPrinter => newPrinter.Name.Equals(name)) where !safe select name)
            {
                Printer.Remove(name);
            }
        }

        private IEnumerable<Printer> createPrinters(IEnumerable<string> printerIDs)
        {
            try
            {
                return
                    printerIDs.Select(
                        id => CommunicationHandler.GetResponse(string.Format("/service/Printer.php?id={0}", id), true))
                        .Where(printerData => !printerData.Error).Select(printerFactory).ToList();
            }
            catch (Exception ex)
            {
                LogHandler.Log(Name, "ERROR:" + ex.Message);
                return new List<Printer>();
            }

        }

        private Printer printerFactory(Response printerData)
        {
            if(printerData.GetField("type").Equals("iPrint"))
                return new iPrintPrinter(printerData.GetField("name"), 
                    printerData.GetField("ip"), 
                    printerData.GetField("port"), 
                    bool.Parse(printerData.GetField("default")));
            if (printerData.GetField("type").Equals("Network"))
                return new NetworkPrinter(printerData.GetField("name"),
                    printerData.GetField("ip"),
                    printerData.GetField("port"),
                    bool.Parse(printerData.GetField("default")));
            if (printerData.GetField("type").Equals("Local"))
                return new LocalPrinter(printerData.GetField("name"),
                    printerData.GetField("file"),
                    printerData.GetField("port"),
                    printerData.GetField("model"),
                    bool.Parse(printerData.GetField("default")));

            return null;
        }


    }
}