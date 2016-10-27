using System;
using System.Runtime.Remoting;
using System.Collections.Generic;

namespace DADStormProcess {
	public class ProcessRemoteServerObject : MarshalByRefObject {

		public void addTuple(string[] tuple) {
			ServerProcess.Instance.addTuple(tuple);
		}

		public string ping() {
			Console.WriteLine("Ping in ProcessRemote");
			return "Pong";
		}

		public void addDownStreamOperator(List<ConnectionPack> cp) {
			ServerProcess.Instance.addDownStreamOperator(cp);
		}
		
		public void crash(){
			ServerProcess.Instance.crash();
		}
		public void interval(int milli){
			ServerProcess.Instance.Milliseconds = milli;
		}

		public void start()    { this.defreeze(); }
		public void freeze()   { ServerProcess.Instance.freeze();   }
		public void defreeze() { ServerProcess.Instance.defreeze(); }
		public void addFile(string fileLocation) { ServerProcess.Instance.addFile(fileLocation); }

		public int getIndexFromPrimmary (string file){
			return ServerProcess.Instance.getIndexFromPrimmary (file);
		}
	}
}
