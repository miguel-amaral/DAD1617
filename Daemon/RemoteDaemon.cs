using System;
using System.Runtime.Remoting;
//using Daemon.ServerDaemon;



namespace Daemon {
	public class DaemonRemoteServerObject : MarshalByRefObject {

		public void newThread(){
			ServerDaemon.Instance.newThread();
		}

		public string ping() {
			Console.WriteLine("Ping");
			return "Pong";
		}
	}
}
