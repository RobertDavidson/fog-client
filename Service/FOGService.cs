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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using FOG.Handlers;
using FOG.Modules;

namespace FOG
{
    /// <summary>
    ///     Coordinate all system wide FOG modules
    /// </summary>
    public class FOGService : ServiceBase
    {

        private const string LOG_NAME = "Service";
        private readonly PipeServer notificationPipe;
        private readonly Thread notificationPipeThread;
        private readonly PipeServer servicePipe;
        private const int sleepDefaultTime = 60;
        //Define variables
        private readonly Thread threadManager;
        private List<AbstractModule> modules;

        public FOGService()
        {
            //Initialize everything
            if (!CommunicationHandler.GetAndSetServerAddress()) return;
            initializeModules();
            threadManager = new Thread(serviceLooper);

            //Setup the notification pipe server
            notificationPipeThread = new Thread(notificationPipeHandler);
            notificationPipe = new PipeServer("fog_pipe_notification");
            notificationPipe.MessageReceived += notificationPipeServer_MessageReceived;

            //Setup the user-service pipe server, this is only Server -- > Client communication so no need to setup listeners
            servicePipe = new PipeServer("fog_pipe_service");
            servicePipe.MessageReceived += servicePipeService_MessageReceived;

            //Unschedule any old updates
            ShutdownHandler.UpdatePending = false;
        }

        //This is run by the pipe thread, it will send out notifications to the tray
        private void notificationPipeHandler()
        {
            while (true)
            {
                if (!notificationPipe.isRunning())
                    notificationPipe.start();

                if (NotificationHandler.Notifications.Count > 0)
                {
                    //Split up the notification into 3 messages: Title, Message, and Duration
                    notificationPipe.sendMessage(string.Format("TLE:{0}", NotificationHandler.Notifications[0].Title));
                    Thread.Sleep(750);
                    notificationPipe.sendMessage(string.Format("MSG:{0}", NotificationHandler.Notifications[0].Message));
                    Thread.Sleep(750);
                    notificationPipe.sendMessage(string.Format("DUR:{0}", NotificationHandler.Notifications[0].Duration));
                    NotificationHandler.Notifications.RemoveAt(0);
                }

                Thread.Sleep(3000);
            }
        }

        //Handle recieving a message
        private static void notificationPipeServer_MessageReceived(Client client, string message)
        {
            LogHandler.Log("PipeServer", "Notification message recieved");
            LogHandler.Log("PipeServer", message);
        }

        //Handle recieving a message
        private static void servicePipeService_MessageReceived(Client client, string message)
        {
            LogHandler.Log("PipeServer", "Server-Pipe message recieved");
            LogHandler.Log("PipeServer", message);
        }

        //Called when the service starts
        protected override void OnStart(string[] args)
        {
            //Start the pipe server
            notificationPipeThread.Priority = ThreadPriority.Normal;
            notificationPipeThread.Start();

            servicePipe.start();

            //Start the main thread that handles all modules
            threadManager.Priority = ThreadPriority.Normal;
            threadManager.IsBackground = true;
            threadManager.Name = "FOGService";
            threadManager.Start();

            //Unschedule any old updates
            ShutdownHandler.UpdatePending = false;

            //Delete old temp files
            try
            {
                if (Directory.Exists(string.Format("{0}\\tmp", AppDomain.CurrentDomain.BaseDirectory)))
                {
                    Directory.Delete(string.Format("{0}\\tmp", AppDomain.CurrentDomain.BaseDirectory));
                }
            }
            catch (Exception ex)
            {
                LogHandler.Log(LOG_NAME, "Could not delete tmp dir");
                LogHandler.Log(LOG_NAME, "ERROR: " + ex.Message);
            }
        }

        //Load all of the modules
        private void initializeModules()
        {
            modules = new List<AbstractModule>
            {
                new ClientUpdater(),
                new TaskReboot(),
                new HostnameChanger(),
                new SnapinClient(),
                new DisplayManager(),
                new GreenFOG(),
                new UserTracker()
            };
        }

        //Called when the service stops
        protected override void OnStop()
        {
            foreach (var process in Process.GetProcessesByName("FOGUserService"))
                process.Kill();
            foreach (var process in Process.GetProcessesByName("FOGTray"))
                process.Kill();
            //Delete old temp files
            try
            {
                if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\tmp"))
                {
                    Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + @"\tmp");
                }
            }
            catch (Exception ex)
            {
            }
        }

        //Run each service
        private void serviceLooper()
        {
            LogHandler.NewLine();
            LogHandler.PaddedHeader("Authentication");
            LogHandler.Log("Client-Info", string.Format("Version: {0}", RegistryHandler.GetSystemSetting("Version")));
            CommunicationHandler.Authenticate();
            LogHandler.NewLine();

            //Only run the service if there wasn't a stop or shutdown request
            while (!ShutdownHandler.ShutdownPending && !ShutdownHandler.UpdatePending)
            {
                foreach (var module in modules.TakeWhile(module => !ShutdownHandler.ShutdownPending && !ShutdownHandler.UpdatePending))
                {
                    //Log file formatting
                    LogHandler.NewLine();
                    LogHandler.PaddedHeader(module.Name);
                    LogHandler.Log("Client-Info", string.Format("Version: {0}", RegistryHandler.GetSystemSetting("Version")));

                    try
                    {
                        module.Start();
                    }
                    catch (Exception ex)
                    {
                        LogHandler.Log(LOG_NAME, string.Format("Failed to Start {0}", module.Name));
                        LogHandler.Log(LOG_NAME, string.Format("ERROR: {0}", ex.Message));
                    }

                    //Log file formatting
                    LogHandler.Divider();
                    LogHandler.NewLine();
                }


                if (ShutdownHandler.ShutdownPending || ShutdownHandler.UpdatePending) continue;
                //Once all modules have been run, sleep for the set time
                var sleepTime = getSleepTime();
                RegistryHandler.SetSystemSetting("Sleep", sleepTime.ToString());
                LogHandler.Log(LOG_NAME, string.Format("Sleeping for {0} seconds", sleepTime));
                Thread.Sleep(sleepTime*1000);
            }

            if (ShutdownHandler.UpdatePending)
            {
                UpdateHandler.beginUpdate(servicePipe);
            }
        }

        //Get the time to sleep from the FOG server, if it cannot it will use the default time
        private static int getSleepTime()
        {
            LogHandler.Log(LOG_NAME, "Getting sleep duration...");

            var sleepResponse = CommunicationHandler.GetResponse("/management/index.php?node=client&sub=configure");

            try
            {
                if (!sleepResponse.Error && !sleepResponse.GetField("#sleep").Equals(""))
                {
                    var sleepTime = int.Parse(sleepResponse.GetField("#sleep"));
                    if (sleepTime >= sleepDefaultTime)
                        return sleepTime;
                    LogHandler.Log(LOG_NAME, string.Format("Sleep time set on the server is below the minimum of {0}", sleepDefaultTime));
                }
            }
            catch (Exception ex)
            {
                LogHandler.Log(LOG_NAME, "Failed to parse sleep time");
                LogHandler.Log(LOG_NAME, string.Format("ERROR: {0}", ex.Message));
            }

            LogHandler.Log(LOG_NAME, "Using default sleep time");

            return sleepDefaultTime;
        }
    }
}