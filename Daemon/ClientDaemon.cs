using System;
using System.Runtime.Remoting;

namespace Daemon {
	public class ClientDaemon{

		private DaemonRemoteServerObject remoteDaemon = null;

		public void connect(string daemonIp = "localhost", string port = "10001") {
			remoteDaemon = (DaemonRemoteServerObject)Activator.GetObject(
				typeof(DaemonRemoteServerObject),
				"tcp://" + daemonIp + ":" + port + "/DaemonRemoteServerObject");

			//TODO if (remoteDaemon == null) throw new SocketException();
		}

		public string ping() {
			if (remoteDaemon != null) {
				return remoteDaemon.ping();
			} else {
				return "TODO You did not connect to Daemon yet";
			}
		}

		public void newThread() {
			if (remoteDaemon != null) {
				remoteDaemon.newThread();
			} else {
				//TODO
				System.Console.WriteLine("TODO: YOU DID NOT CONNECT YET;");
			}
		}

		/**
		  * ClientDaemon Debug Method
		  */
		static void Main(string[] args) {

			ClientDaemon cd = new ClientDaemon();
			System.Console.WriteLine("<enter> if Daemon Server is ON...");
			cd.connect();
			System.Console.WriteLine(cd.ping());
			cd.newThread();
			System.Console.WriteLine("<enter> para sair...");
			System.Console.ReadLine();
		}
	}
}
