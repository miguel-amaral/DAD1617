using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

//High bandwidth to several different peers is abnominal behvaiour
namespace DADStormProcess {
	public class CSF_HighDataDiffPeers : CSF_TupleStructure {

		//Setup
		private const minimumDataConnection = 100*1000*1000; //100Mb

		//String is source ip; hastable key -> destIp , value -> size of communications
		private Dictionary<string,Hashtable> connections = new Dictionary<string,Hashtable>();

		public override object generateTuple (IList<string> tuple) {
			string destIp   = tuple [destinIpIndex];
			string sourceIp = tuple [sourceIpIndex];
			int size = Int32.Parse(tuple[sizeOfPacketIndex]);
			addTalk(destIp,sourceIp,size);
		}

		private void addTalk(ip1,ip2,size){
			registerOneWayConnection(ip1,ip2,size);
			registerOneWayConnection(ip2,ip1,size);
		}

		private void registerOneWayConnection(sourceIp,destIp,size){
			Hashtable existingTalks;
			//Check if source ip in dictionary
			if (connections.TryGetValue (ip, out talks)) {
				//check if it is the first time they talk
				if (existingTalks.ContainsKey (destIp) {
					existingTalks [destIp] = (int)existingTalks [destIp] + size;
				} else {
					existingTalks [destIp] = size;
				}
			} else {
				//First time sourceIp is caught with hand in cookie jar
				existingTalks = new Hashtable ();
				existingTalks [destIp] = size;
				connections [sourceIp] = existingTalks;
			}
		}

		public override void reportBack(){
			foreach(KeyValuePair<string, Hashtable> sourceEntry in sinnerList) {
				string ip = sourceEntry.Key;
				Hashtable table = sourceEntry.Value;
				int bigConnectionsCount = 0;
				foreach (DictionaryEntry pair in table) {
					bigConnectionsCount += pair.Value;
				}
				if( bigConnectionsCount > 0 ){
					System.Console.WriteLine ( ip + " talked to " + bigConnectionsCount + " IPs with more than " + minimumDataConnection+ " of MB of data exchanged");
				}
			}
		}
	}
}
//Do big upload several ips
