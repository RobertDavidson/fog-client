using System;
using System.Collections.Generic;

namespace FOG
{
	/// <summary>
	/// A general interface for an event.
	/// The 'Observer' in the observer pattern
	/// </summary>
	public interface EventObserver
	{
		void onEvent(Events trigger,  Dictionary<String, String> data);
	}
}
