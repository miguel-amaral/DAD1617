using System;
using System.Runtime.Remoting;
//using Daemon.ServerDaemon;



namespace Daemon {
	public class DaemonRemoteServerObject : MarshalByRefObject {

		public void newThread(string dllName, string className , string methodName , string processPort ,object[] args = null){
			ServerDaemon.Instance.newThread(dllName, className , methodName, processPort , args);
		}

		public string ping() {
			Console.WriteLine("Ping");
			return "Pong";
		}
	}
}
