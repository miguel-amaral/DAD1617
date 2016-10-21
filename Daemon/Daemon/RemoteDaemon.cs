using System;
using System.Runtime.Remoting;


namespace Daemon {
	public class DaemonRemoteServerObject : MarshalByRefObject {


		//FIXME this method is giving nullPTR exception
		public void newThread(string dllName, string className , string methodName , string processPort){
			ServerDaemon.Instance.newThread(dllName, className , methodName, processPort , null);
		}

		public void newThread(string dllName, string className , string methodName , string processPort, object[] args){
			ServerDaemon.Instance.newThread(dllName, className , methodName, processPort , args);
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
