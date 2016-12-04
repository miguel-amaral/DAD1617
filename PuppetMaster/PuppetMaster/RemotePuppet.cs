using System;
using System.Runtime.Remoting;
using System.Collections.Generic;

namespace PuppetMaster {
	public class PuppetMasterRemoteServerObject : MarshalByRefObject, DADStormRemoteTupleReceiver {

		public string ping() {
			Console.WriteLine("Ping from PuppetMasterRemote");
			return "Pong";
		}

		public void addTuple (string senderUrl, IList<string> tuple){
			ServerPuppet.Instance.logTupple(senderUrl,tuple);
		}

        public override object InitializeLifetimeService()
        {

            return null;

        }
    }
}
