
using System;
using System.Collections.Generic;

namespace FOG {
	/// <summary>
	/// Reboot the computer if a task needs to
	/// </summary>
	public class TaskReboot : AbstractModule {
		
		private Boolean notifiedUser; //This variable is used to detect if the user has been told their is a pending shutdown
		
		public TaskReboot():base(){
			setName("TaskReboot");
			setDescription("Reboot if a task is scheduled");
			addTrigger(EventHandler.Events.TaskReboot);
			this.notifiedUser = false;
			
		}
		
		public override void onEvent(EventHandler.Events trigger, Dictionary<String, String> data) {
			if(trigger == EventHandler.Events.TaskReboot) {
				processTask(data);
			}
		}
		
		
		private void processTask(Dictionary<String, String> data) {

			//Shutdown if a task is avaible and the user is logged out or it is forced
			LogHandler.Log(getName(), "Restarting computer for task");
			if(!UserHandler.IsUserLoggedIn() || data["force"].Equals("1") ) {
				ShutdownHandler.Restart(getName(), 30);
			} else if(!this.notifiedUser) {
				LogHandler.Log(getName(), "User is currently logged in, will try again later");
				NotificationHandler.CreateNotification(new Notification("Please log off", NotificationHandler.GetCompanyName() + 
					                                                        " is attemping to service your computer, please log off at the soonest available time",
					                                                        60));
				this.notifiedUser = true;
			}
			
		}
		
	}
}