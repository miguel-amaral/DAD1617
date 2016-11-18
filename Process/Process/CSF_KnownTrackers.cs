using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

//High bandwidth to several different peers is abnominal behvaiour
namespace DADStormProcess {
	public class CSF_KnownTrackers : CSF_TupleStructure {
		// List with known trackers
		private List<string> trackers;

		// List with sinners
		//String is source ip; hastable key -> known tracker , value -> number of communications
		private Dictionary<string,Hashtable> sinnerList = new Dictionary<string,Hashtable>();

		public CSF_KnownTrackers(){
			generateTrackers();
		}

		//Method that will get the more recent trackers
		public void generateTrackers(){
			trackers = new List<string>();
			//From file maybe?
		}

		public override void processTuple (IList<string> tuple) {
			string destIp   = tuple [destinIpIndex];
			string sourceIp = tuple [sourceIpIndex];
			if(trackers.Contains(destIp)){
				addTalk(sourceIp,destIp);
			} else if (trackers.Contains(sourceIp)) {
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

		public override CSF_metric reportBack(){
            Dictionary<string, int> metricSinners = new Dictionary<string, int>();
            string metricName = this.GetType().Name;

            //--------------------------//
            //   calculate metrc value  //
            //--------------------------//
            lock (sinnerList) {
                //We do register if a connection happens only once or more often, but we ignore that..
                foreach (KeyValuePair<string, Hashtable> sourceEntry in sinnerList)
                {
                    System.Console.WriteLine(sourceEntry.Key + " talked to " + sourceEntry.Value.Count + " trackers");
                    metricSinners.Add(sourceEntry.Key, sourceEntry.Value.Count);
                }
            }
            CSF_metric metric = new CSF_metric(metricName, sinnerList);
            return metric;

        }

		public override void reset(){
			sinnerList = new Dictionary<string,Hashtable>();
		}
	}
}
