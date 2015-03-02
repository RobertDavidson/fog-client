using System;
using System.Collections.Generic;

namespace FOG {
	
	class Program {
		
		public static void Main(string[] args) {
			LogHandler.setConsoleMode(true);
			
			LogHandler.NewLine();
			LogHandler.PaddedHeader("Dictionaries");
			LogHandler.NewLine();
			
			var myTest = new Dictionary<String, int>();
			myTest.Add("Favorite", 24);
			
			LogHandler.WriteLine(myTest["Favorite"].ToString());

			Console.ReadLine();

		}
	}
}