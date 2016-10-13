using System;
using System.Runtime.Remoting;

public class PuppetMasterRemoteServerObject : MarshalByRefObject {

	public string ping() {
		Console.WriteLine("Ping");
		return "Pong";
	}
}
