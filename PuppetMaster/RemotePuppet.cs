using System;
using System.Runtime.Remoting;

namespace PuppetMaster {
	public class PuppetMasterRemoteServerObject : MarshalByRefObject {

		public string ping() {
			Console.WriteLine("Ping");
			return "Pong";
		}
	}
}
