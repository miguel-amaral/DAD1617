using System;
using System.Runtime.Remoting;

namespace Daemon {
	public class ClientDaemon{

		private DaemonRemoteServerObject remoteDaemon = null;

		public void connect(string port = "10001",string daemonIp = "localhost") {
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

		/**
		  * Interface provided for the creation of a remote Thread
		  */
		public void newThread(string dllName, string className , string methodName , object[] args = null) {
			if (remoteDaemon != null) {
				remoteDaemon.newThread(dllName, className , methodName , args);
			} else {
				//TODO
				System.Console.WriteLine("TODO: YOU DID NOT CONNECT YET;");
			}
		}

		/**
		  * ClientDaemon Debug Method
		  */
		static void Main(string[] args) {

			System.Console.WriteLine("<enter> if Daemon Server is ON...");
			ClientDaemon cd = new ClientDaemon();
			cd.connect();
			System.Console.WriteLine(cd.ping());
			string arg1 = "olá";
			string arg2 = "mundo!";
			string[] argumentos = { arg1 , arg2 };

			cd.newThread("hello.dll","Hello","Hello2",argumentos);
			System.Console.WriteLine("<enter> para sair...");
			System.Console.ReadLine();
		}
	}
}
