using System;
using System.Collections.Generic;
using FOG;

namespace FOG
{
	/// <summary>
	/// A general interface for an event.
	/// The 'Observer' in the observer pattern
	/// </summary>
	public interface EventObserver
	{
		void onEvent(EventHandler.Events trigger,  Dictionary<String, String> data);
	}
}
