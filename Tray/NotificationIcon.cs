﻿
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace FOG
{
    public sealed class NotificationIcon
    {
		
        //Define variables
        private NotifyIcon notifyIcon;
        private ContextMenu notificationMenu;
        private PipeClient systemNotificationPipe;
        private PipeClient userNotificationPipe;
        private PipeClient servicePipe;
		
        private Notification notification;
        private Boolean isNotificationReady;
		
        #region Initialize icon and menu
        public NotificationIcon()
        {
			
            // Setup the pipe client

			
            this.userNotificationPipe = new PipeClient("fog_pipe_notification_user_" + UserHandler.GetCurrentUser());
            this.userNotificationPipe.MessageReceived += new PipeClient.MessageReceivedHandler(pipeNotificationClient_MessageReceived);
            this.userNotificationPipe.connect();				
			
            this.systemNotificationPipe = new PipeClient("fog_pipe_notification");
            this.systemNotificationPipe.MessageReceived += new PipeClient.MessageReceivedHandler(pipeNotificationClient_MessageReceived);
            this.systemNotificationPipe.connect();	
			
            this.servicePipe = new PipeClient("fog_pipe_service");
            this.servicePipe.MessageReceived += new PipeClient.MessageReceivedHandler(pipeNotificationClient_MessageReceived);
            this.servicePipe.connect();				
			
			
			
            notifyIcon = new NotifyIcon();
            notificationMenu = new ContextMenu(InitializeMenu());
			
            notifyIcon.DoubleClick += IconDoubleClick;
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(NotificationIcon));
            notifyIcon.Icon = (Icon)resources.GetObject("icon");
            notifyIcon.ContextMenu = notificationMenu;
			
            this.notification = new Notification();
            this.isNotificationReady = false;
        }
		
        //Called when a message is recieved from the pipe server
        private void pipeNotificationClient_MessageReceived(String message)
        {
			
            if (message.StartsWith("TLE:"))
            {
                message = message.Substring(4);
                this.notification.setTitle(message);
            }
            else if (message.StartsWith("MSG:"))
            {
                message = message.Substring(4);
                this.notification.setMessage(message);
            }
            else if (message.StartsWith("DUR:"))
            {
                message = message.Substring(4);
                try
                {
                    this.notification.setDuration(int.Parse(message));
                }
                catch
                {
                }
                this.isNotificationReady = true;
            }
            else if (message.Equals("UPD"))
            {
                Application.Exit();
            }
			
            if (this.isNotificationReady)
            {
                this.notifyIcon.BalloonTipTitle = this.notification.getTitle();
                this.notifyIcon.BalloonTipText = this.notification.getMessage();
                this.notifyIcon.ShowBalloonTip(this.notification.getDuration());
                this.isNotificationReady = false;
                this.notification = new Notification();
            }
        }
		
        private MenuItem[] InitializeMenu()
        {
            var menu = new MenuItem[] { };
            return menu;
        }
        #endregion
		
        #region Main - Program entry point
        /// <summary>Program entry point.</summary>
        /// <param name="args">Command Line Arguments</param>
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

			
            bool isFirstInstance;
            // Please use a unique name for the mutex to prevent conflicts with other programs
            using (var mtx = new Mutex(true, "Tray", out isFirstInstance))
            {
                if (isFirstInstance)
                {
                    var notificationIcon = new NotificationIcon();
                    notificationIcon.notifyIcon.Visible = true;
                    Application.Run();
                    notificationIcon.notifyIcon.Dispose();
                }
                else
                {
                    // The application is already running
                    // TODO: Display message box or change focus to existing application instance
                }
            } // releases the Mutex
        }
        #endregion
		
        #region Event Handlers

        private void IconDoubleClick(object sender, EventArgs e)
        {
        }
        #endregion
    }
}
