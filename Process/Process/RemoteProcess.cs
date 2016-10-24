using System;
using System.Runtime.Remoting;

namespace DADStormProcess {
	public class ProcessRemoteServerObject : MarshalByRefObject {

		public void addTuple(string[] tuple) {
			ServerProcess.Instance.addTuple(tuple);
		}

		public string ping() {
			Console.WriteLine("Ping in ProcessRemote");
			return "Pong";
		}

		public void addDownStreamOperator(ConnectionPack cp){
			ServerProcess.Instance.addDownStreamOperator(cp);
		}

		public void start()    { this.defreeze(); }
		public void freeze()   { ServerProcess.Instance.freeze();   }
		public void defreeze() { ServerProcess.Instance.defreeze(); }
	}
}
