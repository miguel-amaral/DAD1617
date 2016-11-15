using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

//High bandwidth to several different peers is abnominal behvaiour
namespace DADStormProcess {
	public class CSF_HighUpload : CSF_HighDataDiffPeers {

		//Setup
		private const int minimumHighUpload = 102*1024*1024; //100 MB

		public override void processTuple (IList<string> tuple) {
			string destIp   = tuple [destinIpIndex];
			string sourceIp = tuple [sourceIpIndex];
			int size = Int32.Parse(tuple[sizeOfPacketIndex]);
			registerOneWayConnection(sourceIp,destIp,size);
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
					int uploadSize = 0;
					foreach (DictionaryEntry pair in table) {
						uploadSize += (int)pair.Value;
					}
					if( uploadSize > minimumHighUpload ){
						System.Console.WriteLine ( ip + " uploaded " + uploadSize + ", triggered when more than " + minimumHighUpload+ " MB of data uploaded");
                        metricSinners.Add(ip, uploadSize);
                    }
                }
			}
            CSF_metric metric = new CSF_metric(metricName, metricSinners);
            return metric;
        }
	}
}
//Do big upload several ips
