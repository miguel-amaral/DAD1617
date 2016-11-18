using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

//High bandwidth to several different peers is abnominal behvaiour
namespace DADStormProcess {
	public class CSF_HighDownload : CSF_HighDataDiffPeers {

		//Setup
		private const int minimumHighDownload = 102*1024*1024; //100 MB

		public override void processTuple (IList<string> tuple) {
			string destIp   = tuple [destinIpIndex];
			string sourceIp = tuple [sourceIpIndex];
			int size = Int32.Parse(tuple[sizeOfPacketIndex]);
			registerOneWayConnection(destIp,sourceIp,size);
		}

		public override CSF_metric reportBack() {
            Dictionary<string, int> metricSinners = new Dictionary<string, int>();
            string metricName = this.GetType().Name;

            //--------------------------//
            //   calculate metrc value  //
            //--------------------------//
			lock(connections){
				foreach(KeyValuePair<string, Hashtable> sourceEntry in connections) {
					string ip = sourceEntry.Key;
					Hashtable table = sourceEntry.Value;
					int downloadSize = 0;
					foreach (DictionaryEntry pair in table) {
						downloadSize += (int)pair.Value;
					}
					if( downloadSize > minimumHighDownload ){
						System.Console.WriteLine ( ip + " uploaded " + downloadSize + ", triggered when more than " + minimumHighUpload+ " MB of data uploaded");
                        metricSinners.Add(ip, downloadSize);
                    }
                }
			}
            CSF_metric metric = new CSF_metric(metricName, this.connections);
            return metric;
        }
	}
}
//Do big upload several ips
