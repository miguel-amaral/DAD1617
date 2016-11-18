using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

//High bandwidth to several different peers is abnominal behvaiour
namespace DADStormProcess {
	public class CSF_HighDataDiffPeers : CSF_TupleStructure {

		//Setup
		private const int minimumDataConnection = 100*1024*1024; //100MB

		//String is source ip; hastable key -> destIp , value -> size of communications
		protected Dictionary<string,Hashtable> connections = new Dictionary<string,Hashtable>();

		public override void processTuple (IList<string> tuple) {
			string destIp   = tuple [destinIpIndex];
			string sourceIp = tuple [sourceIpIndex];
			int size = Int32.Parse(tuple[sizeOfPacketIndex]);
			this.addTalk(destIp,sourceIp,size);
		}

		private void addTalk(string ip1,string ip2,int size){
			this.registerOneWayConnection(ip1,ip2,size);
			this.registerOneWayConnection(ip2,ip1,size);
		}

		protected void registerOneWayConnection(string sourceIp,string destIp,int size){
			Hashtable existingTalks;
			//Check if source ip in dictionary
			lock(connections){
				if (connections.TryGetValue (sourceIp, out existingTalks)) {
					//check if it is the first time they talk
					if (existingTalks.ContainsKey (destIp)) {
						existingTalks [destIp] = (int)existingTalks [destIp] + size;
					} else {
						existingTalks [destIp] = size;
					}
				} else {
					//First time sourceIp is caught with hand in cookie jar
					existingTalks = new Hashtable ();
					existingTalks [destIp] = size;
					this.connections [sourceIp] = existingTalks;
				}
			}
		}

		public override CSF_metric reportBack(){
			Dictionary<string, int> sinners = new Dictionary<string, int>();
			string metricName = "CSF_HighDataDiffPeers";

			lock (connections){
				foreach(KeyValuePair<string, Hashtable> sourceEntry in connections) {
					string ip = sourceEntry.Key;
					Hashtable table = sourceEntry.Value;
					int bigConnectionsCount = 0;
					foreach (DictionaryEntry pair in table) {
						if((int)pair.Value > minimumDataConnection) {
							bigConnectionsCount++;
						}
					}
					if( bigConnectionsCount > 0 ){
						System.Console.WriteLine ( ip + " talked to " + bigConnectionsCount + " IPs with more than " + minimumDataConnection + " of MB of data exchanged");
						sinners.Add(ip, bigConnectionsCount);
					}
				}
			}

            CSF_metric metric = new CSF_HighDataDiffPeers(metricName, connections);
            return metric;
        }

        public override void reset() {
			lock(connections){
				connections = new Dictionary<string,Hashtable>();
			}
		}
	}
}
//Do big upload several ips
