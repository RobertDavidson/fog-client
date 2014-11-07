﻿
using System;
using Microsoft.Win32;
using System.Collections.Generic;

namespace FOG {
	/// <summary>
	/// Handle all interaction with the registry
	/// </summary>
	public static class RegistryHandler {

		private const String LOG_NAME = "RegistryHandler";
		private static String root = @"Software\FOG\";
		
		public static String getSystemSetting(String name) {
			if(getRegisitryValue(@"Software\Wow6432Node\FOG\", "Server") != null) {
				root = @"Software\Wow6432Node\FOG\";
				LogHandler.log(LOG_NAME, "64 bit registry detected");				
			}
			
			return getRegisitryValue(root, name);			

		}
		
		public static String getRegisitryValue(String keyPath, String keyName) {
			try {
				RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath);
	            if (key != null) {
	            	String keyValue = key.GetValue(keyName).ToString();
	            	if (keyValue != null) {
	            		return keyValue.Trim();
	                }
	            }	
			} catch (Exception ex) {
				LogHandler.log(LOG_NAME, "Error retrieving " + keyPath + keyName);
				LogHandler.log(LOG_NAME, "ERROR: " + ex.Message);
			}
			return null;
		}
		
		public static String getModuleSetting(String module, String keyName) {
			return getRegisitryValue(root + @"\" + module, keyName);
		}
		
		public static Boolean setModuleSetting(String module, String keyName, String value) {
			return setRegistryValue(root + @"\" + module, keyName, value);
		}
		
		public static Boolean deleteModuleSetting(String module, String keyName) {
			return deleteKey(root + @"\" + module, keyName);
		}
		
		public static Boolean deleteModule(String module) {
			return deleteFolder(root + @"\" + module);
		}
		
		
		public static Boolean setRegistryValue(String keyPath, String keyName, String value) {
			
			try {
				RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath);
				
				key.CreateSubKey(keyName);
				key.SetValue(keyName, value);
				
			} catch (Exception ex) {
				LogHandler.log(LOG_NAME, "Error setting " + keyPath + keyName);
				LogHandler.log(LOG_NAME, "ERROR: " + ex.Message);
			}			
			
			return false;
		}
		
		
		public static Boolean deleteFolder(String path) {
			try {
				RegistryKey key = Registry.LocalMachine.OpenSubKey(path, true);
				if (key != null) {
					key.DeleteSubKeyTree(path);
					return true;
				}
			} catch (Exception ex) {
				LogHandler.log(LOG_NAME, "Error while trying to remove " + path);
				LogHandler.log(LOG_NAME, "ERROR: " + ex.Message);
			}
			
			return false;
		}
		
		public static Boolean deleteKey(String keyPath, String keyName) {
			try {
				RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath, true);
				if (key != null) {
					key.DeleteValue(keyName);
					return true;
				}
			} catch (Exception ex) {
				LogHandler.log(LOG_NAME, "Error while trying to remove " + keyPath);
				LogHandler.log(LOG_NAME, "ERROR: " + ex.Message);
			}
			
			return false;			
		}

		public static String getRoot() { return root; }
		
	}
}