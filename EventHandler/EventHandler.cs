using System;
using System.Collections.Generic;

namespace FOG
{
	/// <summary>
	/// Acts a global event handler (The 'subject' in an observer pattern)
	/// </summary>
	/// 
	public static class EventHandler
	{
		public enum Events { 
			Start, 
			Update, 
			Exit, 
			Server_Connect, 
			Server_Disconnect, 
			Server_Message,
			Snapin,
			Display,
			GreenFOG,
			Hostname,
			Notification
		};
		private static Dictionary<Events, List<EventObserver>> observers = 
			new Dictionary<Events, List<EventObserver>>();
		
		public static void Notify(Events trigger, Dictionary<String, String> data) 
		{
			if(observers.ContainsKey(trigger)) {
				foreach(var observer in observers[trigger])
					observer.onEvent(trigger, data);
			}
		}
		
		public static void Subscribe(Events trigger, EventObserver observer) 
		{
			if(!observers.ContainsKey(trigger))
				observers.Add(trigger, new List<EventObserver>());
			
			observers[trigger].Add(observer);
		}
		
		public static void Unsubscribe(Events trigger, EventObserver observer) 
		{
			if(observers.ContainsKey(trigger))
				observers[trigger].Remove(observer);
		}
		
		
	}
	

}