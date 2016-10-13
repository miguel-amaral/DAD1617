using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
//using PuppetMaster;
using System.Reflection;
using System.Threading;

namespace Daemon {
	public class ServerDaemon {

		private static ServerDaemon instance = null;

		private ServerDaemon() {}

		public static ServerDaemon Instance {
			get {
				if (instance == null) {
					instance = new ServerDaemon();
				}
				return instance;
			}
		}

		public void newThread(string dllName, string className, string methodName, object[] args = null){


			Assembly assembly = Assembly.LoadFile(@dllName);
			Type     type     = assembly.GetType(className);
			var      obj      = Activator.CreateInstance(type);

			Thread task0 = new Thread(() => {
				//do something
				type.InvokeMember(methodName,
					BindingFlags.Default | BindingFlags.InvokeMethod,
					null, obj, args);

				System.Console.WriteLine("Another Thread bites the dust\r\n");
			});
			task0.Start();
		}

		static void Main(string[] args) {

			TcpChannel channel = new TcpChannel(10001);
			ChannelServices.RegisterChannel(channel,false);

			DaemonRemoteServerObject myServerObj = new DaemonRemoteServerObject();
			RemotingServices.Marshal(myServerObj, "DaemonRemoteServerObject",typeof(DaemonRemoteServerObject));

			//RemotingConfiguration.RegisterWellKnownServiceType(	typeof(MyRemoteObject),"MyRemoteObjectName",WellKnownObjectMode.Singleton);

//			System.Console.WriteLine("<enter> if PuppetMaster Server is ON...");
//			System.Console.ReadLine();
//			PuppetClient pc = new PuppetMaster.PuppetClient();
//			pc.connect("localhost");
			System.Console.WriteLine("<enter> para sair...");
			System.Console.ReadLine();
		}
	}
}
