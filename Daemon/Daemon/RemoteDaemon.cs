using System;
using System.Runtime.Remoting;


namespace Daemon {
	public class DaemonRemoteServerObject : MarshalByRefObject {

		public void newThread(string dllName, string className , string methodName , string processPort, string routing, object[] args){
			ServerDaemon.Instance.newThread(dllName, className , methodName, processPort , routing, args);
		}

		public void fullLog(bool fullLog) {
			ServerDaemon.Instance.FullLog = fullLog;
		}

		public string ping() {
			Console.WriteLine("Ping from DaemonRemote");
			return "Pong";
		}

	}
}
