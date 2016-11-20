using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

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
            string destIp = tuple[destinIpIndex];
            string destPort = tuple[destinPortIndex];
            string sourceIp   = tuple[sourceIpIndex];
            string protocol = tuple[protocolIndex];
            if (destIp.Equals("239.192.152.143") && destPort.Equals("6771") && protocol.Equals("UDP")){
                lock (sinnerList) {
                    if (sinnerList.ContainsKey(sourceIp)) {
                        sinnerList[sourceIp] = sinnerList[sourceIp] + 1;
                    }
                    else {
                        sinnerList[sourceIp] = 1;
                    }
                }
			}
		}
		
		public override MemoryStream reportBack(){
			//Dictionary<string, int> metricSinners = sinnerList;
			string metricName = this.GetType().Name;
			
			//--------------------------//
			//   calculate metrc value  //
			//--------------------------//
			
			//the sinnerList already has value
            MemoryStream stream = new MemoryStream();
            lock (sinnerList) { 
                CSF_metric metric = new CSF_metric(metricName, sinnerList);

                DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(CSF_metric));
                js.WriteObject(stream, metric);
            }
            return stream;
        }
		
		public override void reset(){
            lock (sinnerList) {
                sinnerList = new Dictionary<string, int>();
            }
		}
	}
}
