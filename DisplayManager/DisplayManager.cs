
using System;
using System.Collections.Generic;
using System.Management;


namespace FOG {
	/// <summary>
	/// Change the resolution of the display
	/// </summary>
	public class DisplayManager : AbstractModule {
		private Display display;
		
			
		public DisplayManager() : base() {
			setName("DisplayManager");
			setDescription("hange the resolution of the display");	
			this.display = new Display();
			
			addTrigger(EventHandler.Events.Start);
			addTrigger(EventHandler.Events.Display);
		}
		
		public override void onEvent(EventHandler.Events trigger, Dictionary<String, String> data) {
			if(trigger == EventHandler.Events.Start) {
				updateDisplay();
			} else if(trigger == EventHandler.Events.Display) {
				if(updateDisplay(data))
					saveSettings(data);
			}
		}

		private void updateDisplay() {
			var data = new Dictionary<String, String>();
			data["width"]   = RegistryHandler.GetModuleSetting(getName(), "width");
			data["height"]  = RegistryHandler.GetModuleSetting(getName(), "height");
			data["refresh"] = RegistryHandler.GetModuleSetting(getName(), "refresh");
			updateDisplay(data);
		}
		
		private bool updateDisplay(Dictionary<String, String> data) {
			display.updateSettings();
			if(display.settingsLoaded()) {
				try {
					int x = int.Parse(data["width"]);
					int y = int.Parse(data["height"]);
					int r = int.Parse(data["refresh"]);
					if(getDisplays().Count > 0)
						changeResolution(getDisplays()[0], x, y, r);
					else
						changeResolution("", x, y, r);
					
					return true;
				} catch (Exception ex) {
					LogHandler.Log(getName(), "ERROR");
					LogHandler.Log(getName(), ex.Message);
				}
			} else {
				LogHandler.Log(getName(), "Settings are not populated; will not attempt to change resolution");
			}
			return false;
		}
		
		private void saveSettings(Dictionary<String, String> data) {
			try {
				int x = int.Parse(data["width"]);
				int y = int.Parse(data["height"]);
				int r = int.Parse(data["refresh"]);
				
				RegistryHandler.SetModuleSetting(getName(), "width", x.ToString());
				RegistryHandler.SetModuleSetting(getName(), "height", y.ToString());
				RegistryHandler.SetModuleSetting(getName(), "refresh", r.ToString());
			} catch (Exception ex) {
				LogHandler.Log(getName(), "ERROR");
				LogHandler.Log(getName(), ex.Message);				
			}
		}
		
		//Change the resolution of the screen
		private void changeResolution(String device, int width, int height, int refresh) {
			if(!(width.Equals(display.getSettings().dmPelsWidth) && height.Equals(display.getSettings().dmPelsHeight) && refresh.Equals(display.getSettings().dmDisplayFrequency))) {
				LogHandler.Log(getName(), "Current Resolution: " + display.getSettings().dmPelsWidth.ToString() + " x " + 
				               display.getSettings().dmPelsHeight.ToString() + " " + display.getSettings().dmDisplayFrequency + "hz");
				LogHandler.Log(getName(), "Attempting to change resoltution to " + width.ToString() + " x " + height.ToString() + " " + refresh.ToString() + "hz");
				LogHandler.Log(getName(), "Display name: " + device);
				
				display.changeResolution(device, width, height, refresh);
				
			} else {
				LogHandler.Log(getName(), "Current resolution is already set correctly");
			}
		}
		
		private List<String> getDisplays() {
			var displays = new List<String>();			
			var monitorSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DesktopMonitor");
			
			foreach (ManagementObject monitor in monitorSearcher.Get()) {
				displays.Add(monitor["Name"].ToString());
			}
			return displays;
		}
		
		
	}
}