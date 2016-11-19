using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

//Use of UPnP protocol: Clients behind a NAT usually use UPnP to ask the router to open
//ports which will be used to receive incoming connections from another peers.The use of
//this protocol will increase the chance that the client is using BitTorrent. On the other hand
//there are several legitimate applications that make use of this protocol (like Skype) which
//might trigger false positives

namespace DADStormProcess {
	public class CSF_ProtocolUPnP : CSF_TupleStructure {
		//String is source ip; hastable key -> destIp , value -> number of communications
		private Dictionary<string,int> sinnerList = new Dictionary<string,int>();
		private string protocol = "";

		public CSF_ProtocolUPnP(string protocol){
			this.protocol = protocol;
		}

		public override void processTuple (IList<string> tuple) {
			string destIp       = tuple [destinIpIndex];
			string usedProtocol = tuple [protocolIndex];
			string sourceIp = tuple [sourceIpIndex];

			if(usedProtocol.Equals(this.protocol, StringComparison.OrdinalIgnoreCase)){
                lock (sinnerList) {
                    if (sinnerList.ContainsKey(sourceIp)) {
                        sinnerList[sourceIp] = sinnerList[sourceIp] + 1;
                    } else {
                        sinnerList[sourceIp] = 1;
                    }
                }
			}
		}

		public override MemoryStream reportBack(){
			Dictionary<string, int> metricSinners = sinnerList;
			string metricName = this.GetType().Name;

            //--------------------------//
            //   calculate metrc value  //
            //--------------------------//

            //the sinnerList already has value
            MemoryStream stream;
            lock (sinnerList) {
                CSF_metric metric = new CSF_metric(metricName, sinnerList);

                DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(CSF_metric));
                stream = new MemoryStream();
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
