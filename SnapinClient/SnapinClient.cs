using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace FOG {
	/// <summary>
	/// Installs snapins on client computers
	/// </summary>
	public class SnapinClient : AbstractModule {
		
		public SnapinClient():base(){
			setName("SnapinClient");
			setDescription("Installs snapins on client computers");
			addTrigger(EventHandler.Events.Snapin);
		}
		
		public override void onEvent(EventHandler.Events trigger, Dictionary<String, String> data) {
			if(trigger == EventHandler.Events.Snapin) {
				processSnapin(data);
			}
		}
		
		private void processSnapin(Dictionary<String, String> data) {
			var taskID = int.Parse(data["TaskID"]);
			var reboot = Boolean.Parse(data["Reboot"]);
			 
			LogHandler.Log(getName(), "Snapin Found:");
			LogHandler.Log(getName(), "--> ID: "          + taskID);
			LogHandler.Log(getName(), "--> RunWith: "     + data["RunWith"]);
			LogHandler.Log(getName(), "--> RunWithArgs: " + data["RunWithArgs"]);
			LogHandler.Log(getName(), "--> Name: "        + data["Name"]);
			LogHandler.Log(getName(), "--> File: "        + data["FileName"]);					
			LogHandler.Log(getName(), "--> Created: "     + data["Creation"]);
			LogHandler.Log(getName(), "--> Args: "        + data["Args"]);
			LogHandler.Log(getName(), "--> Reboot: "      + reboot);
			
			data.Add("FilePath",AppDomain.CurrentDomain.BaseDirectory + @"tmp\" + data["FileName"]);
			
			Boolean downloaded = CommunicationHandler.DownloadFile("/service/snapins.file.php?mac=" + CommunicationHandler.GetMacAddresses() + "&taskid=" + taskID, data["FilePath"]);
			String exitCode = "-1";
			
			//If the file downloaded successfully then run the snapin and report to FOG what the exit code was
			if(downloaded) {
				exitCode = startSnapin(data);
				if(File.Exists(data["FilePath"]))
					File.Delete(data["FilePath"]);
				
				if (reboot)
					ShutdownHandler.Restart("Snapin requested shutdown", 45);
			}
			
			CommunicationHandler.EmitMessage(getName(), "{ \"TaskID\":" + taskID + ", \"ExitCode\":" + exitCode + "}");		
		}
		
		//Execute the snapin once it has been downloaded
		private String startSnapin(Dictionary<String, String> data) {
			NotificationHandler.CreateNotification(new Notification(data["Name"], "FOG is installing " + data["Name"], 10));
			
			var process = generateProcess(data);
			
			LogHandler.Log(getName(), "Starting snapin...");
			process.Start();
			process.WaitForExit();
			LogHandler.Log(getName(), "Snapin finished");
			LogHandler.Log(getName(), "Return Code: " + process.ExitCode);
			
			var notification = new Dictionary<String, String>();
			notification.Add("Title", "Finished " + data["Name"]);
			notification.Add("Body",  data["Name"] + " finished installing");
			notification.Add("Dur",   "10");
			EventHandler.Notify(EventHandler.Events.Notification, notification);
			
			return process.ExitCode.ToString();
			
		}
		
		//Create a proccess to run the snapin with
		private Process generateProcess(Dictionary<String, String> data) {
			var process = new Process();
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			
			//Check if the snapin run with field was specified
			if(!data["RunWith"].Equals("")) {
				process.StartInfo.FileName = Environment.ExpandEnvironmentVariables(data["RunWith"]);
				process.StartInfo.Arguments = Environment.ExpandEnvironmentVariables(data["RunWithArgs"]);
				process.StartInfo.Arguments = Environment.ExpandEnvironmentVariables(data["RunWithArgs"] + " \"" + data["FilePath"] + " \"" + Environment.ExpandEnvironmentVariables(data["Args"]));
			} else {
				process.StartInfo.FileName = Environment.ExpandEnvironmentVariables(data["FilePath"]);
				process.StartInfo.Arguments = Environment.ExpandEnvironmentVariables(data["Args"]);
			}
			
			return process;
		}
	}
}