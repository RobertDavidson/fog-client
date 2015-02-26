using System;

namespace FOG {
	class Program {
		public static void Main(string[] args) {
			
			LogHandler.setConsoleMode(true);
			CommunicationHandler.GetAndSetServerAddress();
			
			LogHandler.NewLine();
			LogHandler.PaddedHeader("SocketIO");
			LogHandler.NewLine();
			
			CommunicationHandler.OpenSocketIO();

			Console.ReadLine();
			//CommunicationHandler.CloseSocketIO();
			
		}
	}
}