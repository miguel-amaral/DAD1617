using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using PuppetMaster;

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

		public void newThread(){
			System.Console.WriteLine("Another Thread bites the dust\r\n");
		}

		static void Main(string[] args) {

			TcpChannel channel = new TcpChannel(10001);
			ChannelServices.RegisterChannel(channel,false);

			DaemonRemoteServerObject myServerObj = new DaemonRemoteServerObject();
			RemotingServices.Marshal(myServerObj, "DaemonRemoteServerObject",typeof(DaemonRemoteServerObject));

			//RemotingConfiguration.RegisterWellKnownServiceType(	typeof(MyRemoteObject),"MyRemoteObjectName",WellKnownObjectMode.Singleton);

			System.Console.ReadLine();
			System.Console.WriteLine("<enter> if PuppetMaster Server is ON...");
			PuppetClient pc = new PuppetMaster.PuppetClient();
			pc.connect("localhost");
			System.Console.WriteLine("<enter> para sair...");
			System.Console.ReadLine();
		}
	}
}
