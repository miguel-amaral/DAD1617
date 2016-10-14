using System;
using System.Runtime.Remoting;

namespace DADStormProcess {
	public class ProcessRemoteServerObject : MarshalByRefObject {

		public string ping() {
			Console.WriteLine("Ping from ProcessRemote");
			return "Pong";
		}
	}
}
