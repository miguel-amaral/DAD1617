using System;
using System.Runtime.Remoting;

namespace PuppetMaster {
	public class PuppetMasterRemoteServerObject : MarshalByRefObject, DADStormRemoteTupleReceiver {

		public string ping() {
			Console.WriteLine("Ping from PuppetMasterRemote");
			return "Pong";
		}

		public void addTuple (string senderUrl, string[] tuple){
			ServerPuppet.Instance.logTupple(senderUrl,tuple);
		}
	}
}
