using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

//High bandwidth to several different peers is abnominal behvaiour
namespace DADStormProcess {
	public class CSF_HighUpload : CSF_HighDataDiffPeers {

		//Setup
		private const minimumHighUpload = 1000*1000*1000; //1000 MB

		public override void processTuple (IList<string> tuple) {
			string destIp   = tuple [destinIpIndex];
			string sourceIp = tuple [sourceIpIndex];
			int size = Int32.Parse(tuple[sizeOfPacketIndex]);
			registerOneWayConnection(sourceIp,destIp,size);
		}

		public override void reportBack() {
			string toReturn = "";
			lock(connections){
				foreach(KeyValuePair<string, Hashtable> sourceEntry in connections) {
					string ip = sourceEntry.Key;
					Hashtable table = sourceEntry.Value;
					int uploadSize = 0;
					foreach (DictionaryEntry pair in table) {
						uploadSize += pair.Value;
					}
					if( uploadSize > minimumHighUpload ){
						toReturn += ip + " " + uploadSize + " ";
						System.Console.WriteLine ( ip + " uploaded " + uploadSize + ", triggered when more than " + minimumHighUpload+ " MB of data uploaded");
					}
				}
			}
		}
	}
}
//Do big upload several ips
