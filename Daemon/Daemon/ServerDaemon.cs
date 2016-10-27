using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using DADStormProcess;
using System.Diagnostics;


namespace Daemon {
	public class ServerDaemon {

		private static int port = 10001;
		private static ServerDaemon instance = null;
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

		public void newThread(string dllName, string className, string methodName, string processPort, object[] args = null){
			Process process = new Process();
			// Configure the process using the StartInfo properties.
			process.StartInfo.FileName = "Process.exe";

			string processArguments = processPort + " " + dllName + " " + className + " " + methodName + " " + fullLog.ToString();
			foreach (string str in args) {
				processArguments += " " + str;
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

			DaemonRemoteServerObject myServerObj = new DaemonRemoteServerObject();
			RemotingServices.Marshal(myServerObj, "DaemonRemoteServerObject",typeof(DaemonRemoteServerObject));

			System.Console.WriteLine("Daemon Server Online : port: " + port);
//			System.Console.WriteLine("<enter> para sair...");
			System.Console.WriteLine("<ctrl+c> para sair...");

			//Change this to a pulse monitor situation
			while (true){
				/*XXX!XXX!XXX!*/ Thread.Sleep(100); /*XXX!XXX!XXX!*/ //TODO
			}
			Console.ForegroundColor = ConsoleColor.Red;
			System.Console.WriteLine("Daemon Server is going OFFLINE" );
			Console.ResetColor();
		}
	}
}
