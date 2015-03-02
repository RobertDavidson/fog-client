
using System;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;

namespace FOG {
	/// <summary>
	/// Rename a host, register with AD, and activate the windows key
	/// </summary>
	public class HostnameChanger:AbstractModule {
		
		//Import dll methods
		[DllImport("netapi32.dll", CharSet=CharSet.Unicode)] 
		private static extern int NetJoinDomain( string lpServer, string lpDomain, string lpAccountOU, 
		                                        string lpAccount, string lpPassword, JoinOptions NameType);
		
		[DllImport("netapi32.dll", CharSet=CharSet.Unicode)]
		private static extern int NetUnjoinDomain(string lpServer, string lpAccount, string lpPassword, UnJoinOptions fUnjoinOptions);

		[Flags]
		private enum UnJoinOptions {
			NONE = 0x00000000,
			NETSETUP_ACCOUNT_DELETE = 0x00000004
		}
		
		[Flags]
		private enum JoinOptions {
			NETSETUP_JOIN_DOMAIN = 0x00000001,
			NETSETUP_ACCT_CREATE = 0x00000002,
			NETSETUP_ACCT_DELETE = 0x00000004,
			NETSETUP_WIN9X_UPGRADE = 0x00000010,
			NETSETUP_DOMAIN_JOIN_IF_JOINED = 0x00000020,
			NETSETUP_JOIN_UNSECURE = 0x00000040,
			NETSETUP_MACHINE_PWD_PASSED = 0x00000080,
			NETSETUP_DEFER_SPN_SET = 0x10000000
		}
		
		private Dictionary<int, String> adErrors;
		private int successIndex;
		private Boolean notifiedUser; //This variable is used to detect if the user has been told their is a pending shutdown

		
		public HostnameChanger():base() {
			setName("HostnameChanger");
			setDescription("Rename a host, register with AD, and activate the windows key");		
			
			addTrigger(EventHandler.Events.Hostname);
			addTrigger(EventHandler.Events.Start);
			setADErrors();
			this.notifiedUser = false;
			
		}
		
		public override void onEvent(EventHandler.Events trigger, Dictionary<String, String> data) {
			if(trigger == EventHandler.Events.Hostname) {
				applyHostname(data);
				RegistryHandler.SetModuleSetting(getName(), "hostname", data["hostname"]);
				RegistryHandler.SetModuleSetting(getName(), "force", data["force"]);
			} else if(trigger == EventHandler.Events.Start) {
				applyHostname();
			}
		}
	 
	    
		private void setADErrors() {
	      	this.adErrors = new Dictionary<int, String>();
	      	this.successIndex = 0;
	      	
	      	this.adErrors.Add(this.successIndex,"Success");
	      	this.adErrors.Add(5, "Access Denied");
	      	
		}
		
		private void applyHostname() {
			var data = new Dictionary<String, String>();
			data["hostname"] = RegistryHandler.GetModuleSetting(getName(), "hostname");
			data["force"] = RegistryHandler.GetModuleSetting(getName(), "force");
			renameComputer(data);
		}
		
		private void applyHostname(Dictionary<String, String> data) {
			
			renameComputer(data);
			if(!ShutdownHandler.IsShutdownPending())
				registerComputer(data);
			if(!ShutdownHandler.IsShutdownPending())
				activateComputer(data);
		}
		
		//Rename the computer and remove it from active directory
		private void renameComputer(Dictionary<String, String> data) {
			if(!data["hostname"].Equals("")) {
				if(!System.Environment.MachineName.ToLower().Equals(data["hostname"].ToLower())) {
				
					LogHandler.Log(getName(), "Renaming host to " + data["hostname"]);
					if(!UserHandler.IsUserLoggedIn() || data["force"].Equals("1")) {
					
						//First unjoin it from active directory
			      		unRegisterComputer(data);		
		
			      		LogHandler.Log(getName(), "Updating registry");
						RegistryKey regKey;
			
						regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true);
						regKey.SetValue("NV Hostname", data["hostname"]);
						regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\ComputerName\ActiveComputerName", true);
						regKey.SetValue("ComputerName", data["hostname"]);
						regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName", true);
						regKey.SetValue("ComputerName", data["hostname"]);
						
						ShutdownHandler.Restart(NotificationHandler.GetCompanyName() + " needs to rename your computer", 10);
					} else if(!this.notifiedUser) {
						LogHandler.Log(getName(), "User is currently logged in, will try again later");
						//Notify the user they should log off if it is not forced
						NotificationHandler.CreateNotification(new Notification("Please log off", NotificationHandler.GetCompanyName() +
					                                                        " is attemping to service your computer, please log off at the soonest available time",
					                                                        120));
						
						this.notifiedUser = true;
					}
				} else {
					LogHandler.Log(getName(), "Hostname is correct");
				}
			} 
	      	
		}
		
		//Add a host to active directory
		private void registerComputer(Dictionary<String, String> data) {
			if(data["AD"].Equals("1")) {
				LogHandler.Log(getName(), "Adding host to active directory");
				if(!(data["ADDom"].Equals("")) && !(data["ADUser"].Equals("")) && !(data["ADPass"].Equals(""))) {

					int returnCode = NetJoinDomain(null, data["ADDom"], data["ADOU"], data["ADUser"], data["ADPass"], (JoinOptions.NETSETUP_JOIN_DOMAIN | JoinOptions.NETSETUP_ACCT_CREATE));
					if(returnCode == 2224) {
						returnCode = NetJoinDomain(null, data["ADDom"], data["ADOU"], data["ADUser"], data["ADPass"], JoinOptions.NETSETUP_JOIN_DOMAIN);
					}
					
					//Log the response
					if(this.adErrors.ContainsKey(returnCode)) {
						LogHandler.Log(getName(), this.adErrors[returnCode] + " Return code: " + returnCode.ToString());
					} else {
						LogHandler.Log(getName(), "Unknown return code: " + returnCode.ToString());
					}	
					
					if(returnCode.Equals(this.successIndex))
						ShutdownHandler.Restart("Host joined to active directory, restart needed", 20);
					
				} else {
					LogHandler.Log(getName(), "Unable to remove host from active directory");
					LogHandler.Log(getName(), "ERROR: Not all active directory fields are set");
				}
			} else {
				LogHandler.Log(getName(), "Active directory is disabled");
			}
		}
		
		//Remove the host from active directory
		private void unRegisterComputer(Dictionary<String, String> data) {
			LogHandler.Log(getName(), "Removing host from active directory");
			if(!data["ADUser"].Equals("") && !data["ADPass"].Equals("")) {
				
				int returnCode = NetUnjoinDomain(null, data["ADUser"], data["ADPass"], UnJoinOptions.NETSETUP_ACCOUNT_DELETE);
				
				//Log the response
				if(this.adErrors.ContainsKey(returnCode)) {
					LogHandler.Log(getName(), this.adErrors[returnCode] + " Return code: " + returnCode.ToString());
				} else {
					LogHandler.Log(getName(), "Unknown return code: " + returnCode.ToString());
				}
				
				if(returnCode.Equals(this.successIndex))
					ShutdownHandler.Restart("Host joined to active directory, restart needed", 20);
			} else {
				LogHandler.Log(getName(), "Unable to remove host from active directory, some settings are empty");
			}
		}
		
		//Active a computer with a product key
		private void activateComputer(Dictionary<String, String> data) {
			if(data.ContainsKey("Key")) {
				LogHandler.Log(getName(), "Activing host with product key");
				
				//The standard windows key is 29 characters long -- 5 sections of 5 characters with 4 dashes (5*5+4)
				if(data["Key"].Length == 29) {
					Process process = new Process();
					
					//Give windows the new key
					process.StartInfo.FileName = @"cscript";
					process.StartInfo.Arguments ="//B //Nologo "  + Environment.SystemDirectory + @"\slmgr.vbs /ipk " + data["Key"];
					process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					process.Start();
					process.WaitForExit();
					process.Close();
					
					//Try and activate the new key
					process.StartInfo.Arguments ="//B //Nologo " + Environment.SystemDirectory + @"\slmgr.vbs /ato";
					process.Start();
					process.WaitForExit();
					process.Close();
				} else {
					LogHandler.Log(getName(), "Unable to activate windows");
					LogHandler.Log(getName(), "ERROR: Invalid product key");
				}
			} else {
				LogHandler.Log(getName(), "Windows activation disabled");				
			}
		}
		
	}
}