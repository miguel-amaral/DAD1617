using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
//using PuppetMaster;
using System.Reflection;
using System.Threading;
using DADStormProcess;

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

			Assembly assembly = Assembly.LoadFile(@dllName);
			Type     type     = assembly.GetType(className);
			var      obj      = Activator.CreateInstance(type);

			//If using threads TCP channel will already be active and when we create a ServerProcess it will complain and FAIL
			Thread task0 = new Thread(() => {
				//do something
				//Lauching proccessServer
				DADStormProcess.ServerProcess sp = new DADStormProcess.ServerProcess(processPort);
				sp.executeProcess();

				//Perhaps this will happen inside Process
				type.InvokeMember(methodName,
					BindingFlags.Default | BindingFlags.InvokeMethod,
					null, obj, args);

				System.Console.WriteLine("Another Thread bites the dust\r\n");
			});
			task0.Start();
		}

		static void Main(string[] args) {

			channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(channel,false);

			DaemonRemoteServerObject myServerObj = new DaemonRemoteServerObject();
			RemotingServices.Marshal(myServerObj, "DaemonRemoteServerObject",typeof(DaemonRemoteServerObject));

			System.Console.WriteLine("Daemon Server Online : port: " + port);
			System.Console.WriteLine("<enter> para sair...");
			System.Console.ReadLine();
		}
	}
}
