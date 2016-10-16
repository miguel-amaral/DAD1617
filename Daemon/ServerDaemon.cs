using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
//using PuppetMaster;
using System.Threading;
using DADStormProcess;
using System.Diagnostics;


namespace Daemon {
	public class ServerDaemon {

		private static int port = 10001;
		private static ServerDaemon instance = null;
		private static TcpChannel channel;
		private ServerDaemon() {}

		public static ServerDaemon Instance {
			get {
				if (instance == null) {
					instance = new ServerDaemon();
				}
				return instance;
			}
		}

		public void newThread(string dllName, string className, string methodName, string processPort, object[] args = null){
			Process process = new Process();
			// Configure the process using the StartInfo properties.
			process.StartInfo.FileName = "Process/ServerProcess.exe";

			string processArguments = processPort + " " + dllName + " " + className + " " + methodName;
			foreach (string str in args){
				processArguments += " " + str;
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
			System.Console.WriteLine("<enter> para sair...");
			while (true){
				/*XXX!XXX!XXX!*/ Thread.Sleep(100); /*XXX!XXX!XXX!*/ //TODO
			}
			Console.ForegroundColor = ConsoleColor.Red;
			System.Console.WriteLine("Daemon Server is going OFFLINE" );
			Console.ResetColor();
		}
	}
}
