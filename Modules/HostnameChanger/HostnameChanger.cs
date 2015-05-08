/*
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
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using FOG.Handlers;
namespace FOG.Modules {
    /// <summary>
    /// Rename a host, register with AD, and activate the windows key
    /// </summary>
    public class HostnameChanger:AbstractModule {
        //Import dll methods
        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)] 
        private static extern int NetJoinDomain(string lpServer, string lpDomain, string lpAccountOU, string lpAccount, string lpPassword, JoinOptions NameType);
        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
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
            NETSETUP_WIN9X_UPGRADE = 0x00000010,
            NETSETUP_DOMAIN_JOIN_IF_JOINED = 0x00000020,
            NETSETUP_JOIN_UNSECURE = 0x00000040,
            NETSETUP_MACHINE_PWD_PASSED = 0x00000080,
			NETSETUP_DEFER_SPN_SET = 0x00000100,
			NETSETUP_JOIN_DC_ACCOUNT = 0x00000200,
			NETSETUP_AMBIGUOUS_DC = 0x00001000,
			NETSETUP_NO_NETLOGON_CACHE = 0x00002000,
			NETSETUP_DONT_CONTROL_SERVICES = 0x00004000,
			NETSETUP_SET_MACHINE_NAME = 0x00008000,
			NETSETUP_FORCE_SPN_SET = 0x00010000,
			NETSETUP_NO_ACCT_REUSE = 0x00020000,
			NETSETUP_IGNORE_UNSUPPORTED_FLAGS = 0x10000000,
            NETSETUP_ACCT_DELETE = 0x00000004
        }
        private Dictionary<int, String> adErrors;
        private int successIndex;
        private Boolean notifiedUser;
        public HostnameChanger() {
            Name = "HostnameChanger";
            Description = "Rename a host, register with AD, and activate the windows key";
            setADErrors();
            this.notifiedUser = false;
        }
		private void setADErrors() {
			this.adErrors = new Dictionary<int, String>();
			this.successIndex = 0;
			this.adErrors.Add(this.successIndex, "Success");
			this.adErrors.Add(2, "The OU parameter is not set properly or not working with this current setup");
			this.adErrors.Add(5, "Access is denied");
			this.adErrors.Add(87, "The parameter is incorrect");
			this.adErrors.Add(110, "The system cannot open the specified object");
			this.adErrors.Add(1323, "Unable to update the password");
			this.adErrors.Add(1326, "Logon failure: unknown username or bad password");
			this.adErrors.Add(1355, "The specified domain either does not exist or could not be contacted");
			this.adErrors.Add(2224, "The account already exists");
			this.adErrors.Add(2691, "The machine is already joined to the domain");
			this.adErrors.Add(2692, "The machine is not currently joined to a domain");
        }
        protected override void doWork() {
            //Get task info
            var taskResponse = CommunicationHandler.GetResponse("/service/hostname.php?moduleid=" + Name.ToLower(), true);
            if (!taskResponse.Error) {
                renameComputer(taskResponse);
                if (!ShutdownHandler.ShutdownPending)
                    registerComputer(taskResponse);
                if (!ShutdownHandler.ShutdownPending)
                    activateComputer(taskResponse);
            }
        }
        //Rename the computer and remove it from active directory
        private void renameComputer(Response taskResponse) {
			try {
	            LogHandler.Log(Name, taskResponse.getField("#hostname") + ":" + System.Environment.MachineName);
				if taskResponse.getField("#hostname").Equals("")) throw new Exception("Hostname is not specified");
				if (Environment.MachineName.ToLower().Equals(taskResponse.getField("#hostname").ToLower())) throw new Exception("Hostname is correct");
				LogHandler.Log(Name, string.Format("Renaming host to {0}",taskResponse.getField("#hostname")));
				if (!UserHandler.isUserLoggedIn() || taskResponse.getField("#force").Equals("1")) {
					LogHandler.Log(Name, "Unregistering computer");
					unRegisterComputer(taskResponse);
					LogHandler.Log(Name, "Updating registry");
					RegistryKey regKey;
					regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true);
					regKey.SetValue("NV Hostname", taskResponse.getField("#hostname"));
					regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\ComputerName\ActiveComputerName", true);
					regKey.SetValue("ComputerName", taskResponse.getField("#hostname"));
					regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName", true);
					regKey.SetValue("ComputerName", taskResponse.getField("#hostname"));
					ShutdownHandler.Restart(string.Format("{0} needs to rename your computer",NotificationHandler.Company),10);
				} else if (!this.notifiedUser) {
					LogHandler.Log(Name, "User is currently logged in, will try again later");
					NotificationHandler.Notifications.Add(new Notification("Please log off", string.Format("{0} is attempting to service yoru computer, please log off at the soonest available time",NotificationHandler.Company), 120));
					this.notifiedUser = true;
				}
			} catch (Exception ex) {
				LogHandler.Log(Name, ex.Message);
			}
		}
        //Add a host to active directory
        private void registerComputer(Response taskResponse) {
			try {
				if (!taskResponse.getField("#AD").Equals("1")) throw new Exception("Active directory joining disabled for this host");
				if (taskResponse.getField("#ADDom").Equals("") || taskResponse.getField("#ADUser").Equals("") || taskResponse.getField("#ADPass").Equals("")) throw new Exception("Required Domain Joining information is missing\r\nPlease check your Active Directory settings in the FOG GUI");
				LogHandler.Log(Name, "Adding host to active directory");
				String userDomain = taskResponse.getField("#ADDom");
				String userOU = taskResponse.getField("#ADOU");
				String userUsername = taskResponse.getField("#ADUser");
				String userPassword = taskResponse.getField("#ADPass");
				int returnCode +NetJoinDomain(null, userDomain.Trim(), userOU.Trim(), userUsername.Trim(), userPassword.Trim(), (JoinOptions.NETSETUP_JOIN_DOMAIN | JoinOptions.NETSETUP_ACCT_CREATE));
				if (returnCode.Equals("2224")) {
					returnCode = NetJoinDomain(null,userDomain.Trim(),userOU.Trim(),userUsername.Trim(),userPassword.Trim(), (JoinOptions.NETSETUP_JOIN_DOMAIN));
				} else if (returnCode.Equals("2")) {
					returnCode = NetJoinDomain(null,userDomain.Trim(),null,userUsername.Trim(),userPassword.Trim(), (JoinOptions.NETSETUP_JOIN_DOMAIN | JoinOptions.NETSETUP_ACCT_CREATE));
				}
				LogHandler.Log(Name, string.Format("{0} {1} {2}",this.adErrors[returnCode],(this.adErrors.ContainsKey(returnCode) ? "Return Code: " : "Unknown Return Code: "), returnCode.ToString()));
				if (returnCode.Equals(this.successIndex)) {
					ShutdownHandler.Restart("Host joined to Active Directory, restart required");
				}
			} catch (Exception ex) {
				LogHandler.Log(Name, ex.Message);
			}
		}
		//Remove the host from active directory
		private void unRegisterComputer(Response taskResponse) {
			try {
				LogHandler.Log(Name, "Removing host from active directory");
				if (taskResponse.getField("#ADUser").Equals("") || taskResponse.getField("#ADPass").Equals(""))	throw new Exception("Required Domain information is missing\r\n\tPlease check your Active Directory settings in the FOG GUI");
				String userUsername = taskResponse.getField("#ADUser");
				String userPassword = taskResponse.getField("#ADPass");
				int returnCode = NetUnjoinDomain(null,userUsername.Trim(),userPassword.Trim(),UnJoinOptions.NETSETUP_ACCOUNT_DELETE);
				LogHandler.Log(Name, string.Format("{0} {1} {2}",this.adErrors[returnCode],(this.adErrors.ContainsKey(returnCode) ? "Return Code: " : "Unknown Return Code: "), returnCode.ToString()));
                if (returnCode.Equals(this.successIndex)) {
                    ShutdownHandler.Restart("Host joined to active directory, restart needed", 20);
				}
			} catch (Exception ex) {
				LogHandler.Log(Name, ex.Message);
			}
		}
        //Active a computer with a product key
		private void activateComputer(Response taskResponse) {
			try {
				if (!taskResponse.Data.ContainsKey("#Key")) throw new Exception("Windows activation disabled");
				String ProdKey = taskResponse.getField("#Key");
				if (ProdKey.Trim().Length != 29) throw new Exception("Invalid Product Key\r\n\tMake sure it is in format XXXXX-XXXXX-XXXXX-XXXXX-XXXXX\r\n\tOn the host entry in the FOG GUI");
				LogHandler.Log(Name, "Activating host with product key");
				var process = new Process();
				process.StartInfo.FileName = @"cscript";
				process.StartInfo.Arguments = string.Format("//B //Nologo {0} {1} /ipk {2}",Environment.SystemDirectory,@"\slmgr.vbs",ProdKey.Trim());
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				process.Start();
				process.WaitForExit();
				process.Close();
				process.StartInfo.Arguments = string.Format("//B //Nologo {0} {1} /ato {2}",Environment.SystemDirectory,@"\slmgr.vbs",ProdKey.Trim());
				process.Start();
				process.WaitForExit();
				process.Close();
			} catch (Exception ex) {
				LogHandler.Log(Name, ex.Message);
			}
		}
	}
}
