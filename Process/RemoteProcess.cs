using System;
using System.Runtime.Remoting;

namespace Process {
	public class ProcessRemoteServerObject : MarshalByRefObject {

		public string ping() {
			Console.WriteLine("Ping");
			return "Pong";
		}
	}
}
