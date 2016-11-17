using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

//Use of Local Peer discovery: Modern BitTorrent clients implements Local Peer Discovery.
   //Such protocol is implemented using HTTP-like messages over UDP multicast group
   //239.192.152.143 on port 6771. However other applications(like DropBox) also use this
   //protocol which, once again, might trigger false positives.This can be avoided by looking
   //into the packet contentnamespace DADStormProcess {

namespace DADStormProcess {
	public class CSF_LocalPeerDiscovery: CSF_TupleStructure {
		//String is source ip; hastable key -> destIp , value -> number of communications
		private Dictionary<string,int> sinnerList = new Dictionary<string,int>();
		public override void processTuple (IList<string> tuple) {
			string destIp     = tuple [destinIpIndex];
			string destPort   = tuple [destinPortIndex];
			if(destIp.Equals("239.192.152.143") && port.Equals("6771")){
				string sourceIp = tuple [sourceIpIndex];
				if (sinnerList.TryGetValue (tuple [sourceIp]){
					sinnerList[sourceIp] = sinnerList[sourceIp] + 1;
				} else {
					sinnerList[sourceIp] = 1;
				}
			}
		}
		
		public override CSF_metric reportBack(){
			Dictionary<string, int> metricSinners = sinnerList;
			string metricName = this.GetType().Name;
			
			//--------------------------//
			//   calculate metrc value  //
			//--------------------------//
			
			//the sinnerList already has value
			CSF_metric metric = new CSF_metric(metricName, metricSinners);
			return metric;
		}
		
		public override void reset(){
			sinnerList = new Dictionary<string,int>();
		}
	}
}
