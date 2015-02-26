using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading;

using FOG;

namespace FOG{
	/// <summary>
	/// Coordinate all system wide FOG modules
	/// </summary>
	public partial class FOGService  : ServiceBase {
		//Define variables
		private Thread threadManager;
		private Thread notificationPipeThread;
		
		private List<AbstractModule> modules;
		private PipeServer notificationPipe;
		private PipeServer servicePipe;
		
		private const String LOG_NAME = "Service";
		
		public FOGService() {
			//Initialize everything
			if(CommunicationHandler.GetAndSetServerAddress()) {

				initializeModules();
				this.threadManager = new Thread(new ThreadStart(serviceThread));
				
				//Setup the notification pipe server
				this.notificationPipeThread = new Thread(new ThreadStart(notificationPipeHandler));
				this.notificationPipe = new PipeServer("fog_pipe_notification");
				this.notificationPipe.MessageReceived += new PipeServer.MessageReceivedHandler(notificationPipeServer_MessageReceived);
				
				//Setup the user-service pipe server, this is only Server -- > Client communication so no need to setup listeners
				this.servicePipe = new PipeServer("fog_pipe_service");
				this.servicePipe.MessageReceived += new PipeServer.MessageReceivedHandler(servicePipeService_MessageReceived);
				
				//Unschedule any old updates
				ShutdownHandler.UnScheduleUpdate();
			}
		}
		
		//This is run by the pipe thread, it will send out notifications to the tray
		private void notificationPipeHandler() {
			while (true) {
				if(!this.notificationPipe.isRunning()) 
					this.notificationPipe.start();			
				
				if(NotificationHandler.GetNotifications().Count > 0) {
					//Split up the notification into 3 messages: Title, Message, and Duration
					this.notificationPipe.sendMessage("TLE:" + NotificationHandler.GetNotifications()[0].getTitle());
					Thread.Sleep(750);
					this.notificationPipe.sendMessage("MSG:" + NotificationHandler.GetNotifications()[0].getMessage());
					Thread.Sleep(750);
					this.notificationPipe.sendMessage("DUR:" + NotificationHandler.GetNotifications()[0].getDuration().ToString());
					NotificationHandler.RemoveNotification(0);
				} 
				
				Thread.Sleep(3000);
			}
		}		
		
		//Handle recieving a message
		private void notificationPipeServer_MessageReceived(Client client, String message) {
			LogHandler.Log("PipeServer", "Notification message recieved");
			LogHandler.Log("PipeServer",message);
		}

		//Handle recieving a message
		private void servicePipeService_MessageReceived(Client client, String message) {
			LogHandler.Log("PipeServer", "Server-Pipe message recieved");
			LogHandler.Log("PipeServer",message);
		}		

		//Called when the service starts
		protected override void OnStart(string[] args) {
			
			//Start the pipe server
			this.notificationPipeThread.Priority = ThreadPriority.Normal;
			this.notificationPipeThread.Start();
			
			this.servicePipe.start();
			
			//Start the main thread that handles all modules
			this.threadManager.Priority = ThreadPriority.Normal;
			this.threadManager.IsBackground = true;
			this.threadManager.Name = "FOGService";
			this.threadManager.Start();
			
			//Unschedule any old updates
			ShutdownHandler.UnScheduleUpdate();
        }
		
		//Load all of the modules
		private void initializeModules() {
			this.modules = new List<AbstractModule>();
			this.modules.Add(new SnapinClient());
			
			foreach(AbstractModule module in this.modules) {
				foreach(EventHandler.Events trigger in module.getTriggers()) {
					EventHandler.Subscribe(trigger, module);
				}
			}

		}
		
		//Called when the service stops
		protected override void OnStop() {

		}
		
		//Run each service
		private void serviceThread() {
			CommunicationHandler.OpenSocketIO();
			while(CommunicationHandler.IsSocketOpen()) { }

			
			if(ShutdownHandler.IsUpdatePending()) {
				UpdateHandler.beginUpdate(servicePipe);
			}
		}


	}
}
