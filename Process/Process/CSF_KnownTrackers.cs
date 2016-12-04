using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

//High bandwidth to several different peers is abnominal behvaiour
namespace DADStormProcess {
//	private class IpPort {
//		public string ip;
//		public string port;
//		public IpPort(string ip, string port) {
//			this.ip = ip;
//			this.port = port;
//		}
//
//		public override bool Equals(Object obj) {
//			//Check for null and compare run-time types.
//			if ((obj == null) || ! this.GetType().Equals(obj.GetType())) {
//				return false;
//			} else {
//				IpPort other = (IpPort) obj;
//				bool comparisson = this.Ip.Equals(other.ip)
//				if( ! this.port.Equals("*")){
//
//				}
//				return (x == p.x) && (y == p.y);
//			}
//		}
//	}
	public class CSF_KnownTrackers : CSF_TupleStructure {
		// List with known trackers
		private List<string> trackers;
        //private string fileLocation = @"..\..\knownTrackerIpsPorts.txt";
        private string fileLocation = @"knownTrackerIpsPorts.txt";

        // List with sinners
        //String is source ip; hastable key -> known tracker , value -> number of communications
        private Dictionary<string,Hashtable> sinnerList = new Dictionary<string,Hashtable>();

		public CSF_KnownTrackers(){
			generateTrackers();
		}

		//Method that will get the more recent trackers
		public void generateTrackers(){
			trackers = new List<string>();

			string[] content;
			content = File.ReadAllLines (fileLocation);
			foreach(string line in content) {
				String[] splitStr = line.Split (new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				string toAdd = "";
				if( splitStr.Length > 1 ) {
					toAdd = splitStr[0] + ":" + splitStr[1];
				} else {
					toAdd = splitStr[0];
				}
				trackers.Add(toAdd);
			}
		}

		public override void processTuple (IList<string> tuple) {
			string destIp   = tuple [destinIpIndex];
			string destPort = tuple [destinPortIndex];

			string sourceIp = tuple [sourceIpIndex];
			string sourcePort = tuple [sourcePortIndex];

			if(trackers.Contains(destIp) || trackers.Contains(destIp+":"+destPort)){
				addTalk(sourceIp,destIp);
			} else if (trackers.Contains(sourceIp) || trackers.Contains(sourceIp+":"+sourcePort)) {
				addTalk(destIp,sourceIp);
			}
		}

		private void addTalk(string sinner,string tracker) {
			Hashtable existingCommunications;

			lock(sinnerList){
				//Check if source ip in dictionary
				if (sinnerList.TryGetValue (sinner, out existingCommunications)) {
					//check if it is the first time they talk
					if (existingCommunications.ContainsKey (tracker)) {
						existingCommunications [tracker] = (int)existingCommunications [tracker] + 1;
					} else {
						existingCommunications [tracker] = 1;
					}
				} else {
					//First time sourceIp is caught with hand in cookie jar
					existingCommunications = new Hashtable ();
					existingCommunications [tracker] = 1;
					sinnerList [sinner] = existingCommunications;
				}
			}
		}

		public override MemoryStream reportBack(){
            //Dictionary<string, int> metricSinners = new Dictionary<string, int>();
            string metricName = this.GetType().Name;

            //--------------------------//
            //   calculate metrc value  //
            //--------------------------//
            //lock (sinnerList) {
            //    //We do register if a connection happens only once or more often, but we ignore that..
            //    foreach (KeyValuePair<string, Hashtable> sourceEntry in sinnerList) {
            //        System.Console.WriteLine(sourceEntry.Key + " talked to " + sourceEntry.Value.Count + " trackers");
            //        metricSinners.Add(sourceEntry.Key, sourceEntry.Value.Count);
            //    }
            //}

            MemoryStream stream = new MemoryStream();
            lock (sinnerList)
            {
                CSF_metric metric = new CSF_metric(metricName, sinnerList);

                DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(CSF_metric));
                js.WriteObject(stream, metric);
            }
            return stream;

        }

		public override void reset(){
            lock (sinnerList)
            {
                sinnerList = new Dictionary<string, Hashtable>();
            }
		}
	}
}
