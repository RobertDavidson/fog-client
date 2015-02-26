using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FOG
{
	/// <summary>
	/// The base of all FOG Modules
	/// </summary>
	public abstract class AbstractModule : EventObserver {

		//Basic variables every module needs
		private String moduleName; 
		private String moduleDescription;
		private Scope scope;
		private List<EventHandler.Events> triggers;

		public enum Scope {
			User,
			System
		}

		protected AbstractModule() {
			//Define variables
			this.triggers = new List<EventHandler.Events>();
			setName("Generic Module");
			setDescription("Generic Description");
			setScope(Scope.System);
		}

		//Getters and setters
		public String getName() { return this.moduleName; }
		protected void setName(String name) { this.moduleName = name; }

		public String getDescription() { return this.moduleDescription; }
		protected void setDescription(String description) { this.moduleDescription = description; }

		public Scope getScope() { return this.scope; }
		public void setScope(Scope scope) { this.scope = scope; }
		
		public void addTrigger(EventHandler.Events trigger) { this.triggers.Add(trigger); }
		public List<EventHandler.Events> getTriggers() { return this.triggers; }
		
		public abstract void onEvent(EventHandler.Events trigger, Dictionary<String, String> data);

	}
}