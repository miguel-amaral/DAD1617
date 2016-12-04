using System;
using System.Runtime.Remoting;
using System.Threading;

namespace Daemon {
	public class ClientDaemon{

		private DaemonRemoteServerObject remoteDaemon = null;

		public ClientDaemon(ConnectionPack cp, bool fullLog) {
			remoteDaemon = (DaemonRemoteServerObject)Activator.GetObject(
				typeof(DaemonRemoteServerObject),
				"tcp://" + cp.Ip + ":" + cp.Port + "/DaemonRemoteServerObject");
			remoteDaemon.fullLog(fullLog);
			//TODO if (remoteDaemon == null) throw new SocketException();
		}

		public string ping() {
			return remoteDaemon.ping();
		}

		/**
		  * Interface provided for the creation of a remote Thread
		  */
		public void newThread(string dllName, string className , string methodName, string processPort, string proccessIp, int semantics, string routing, string operatorID, object[] args = null) {
			remoteDaemon.newThread(dllName, className , methodName, processPort, proccessIp, semantics, routing, operatorID, args);

		}


		/**
		  * ClientDaemon Debug Method
		  */
/*
		static void Main(string[] args) {

			ClientDaemon cd = new ClientDaemon();
			cd.connect();
			System.Console.WriteLine(cd.ping());
			string arg1 = "olá";
			string arg2 = "mundo!";
			string[] argumentos = { arg1 , arg2 };

			cd.newThread("hello.dll","Hello","Hello2", "12345", argumentos);

			Thread.Sleep(100); //call for debug in main method

			cd.ping();
			System.Console.WriteLine("<enter> para sair...");
			System.Console.ReadLine();
		}
*/
	}
}
