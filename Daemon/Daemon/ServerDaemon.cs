using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using DADStormProcess;
using System.Diagnostics;


namespace Daemon {
	public class ServerDaemon {

		private static int port = 10000;
		private static ServerDaemon instance = null;
        private static DaemonRemoteServerObject myServerObj;
        private static TcpChannel channel;
		private bool   fullLog = false;
		private ServerDaemon() {}

		public static ServerDaemon Instance {
			get {
				if (instance == null) {
					instance = new ServerDaemon();
				}
				return instance;
			}
		}

		public bool FullLog {
			get	{ return  fullLog; }
			set	{ fullLog = value; }
		}

		public void newThread (string dllName, string className, string methodName, string processPort, string proccessIp, int semantics, string routing, string operatorID, object[] args = null) {
			Process process = new Process ();
			// Configure the process using the StartInfo properties.
			process.StartInfo.FileName = "Process.exe";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            string processArguments = processPort + " " + dllName + " " + className + " " + methodName + " " + semantics.ToString() + " "  + routing + " " + fullLog.ToString () + " " + proccessIp + " " + operatorID;
			if (args != null) {
				foreach (string str in args) {
					processArguments += " " + str;
				}
			}


			if(DEBUG.DAEMON){
				System.Console.WriteLine ("Launching Process.exe with arguments: " + processArguments);
			}
			process.StartInfo.Arguments = processArguments;

			//process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
			process.Start();
		}

		static void Main(string[] args) {

			ServerDaemon sd = ServerDaemon.Instance;

			channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(channel,false);

			myServerObj = new DaemonRemoteServerObject();
			RemotingServices.Marshal(myServerObj, "DaemonRemoteServerObject",typeof(DaemonRemoteServerObject));

			System.Console.WriteLine("Daemon Server Online : port: " + port);
//			System.Console.WriteLine("<enter> para sair...");
			System.Console.WriteLine("<ctrl+c> para sair...");

            System.Console.ReadLine();

			Console.ForegroundColor = ConsoleColor.Red;
			System.Console.WriteLine("Daemon Server is going OFFLINE" );
			Console.ResetColor();
            Thread.Sleep(2000); //Ensuring everything is offline
		}
	}
}
