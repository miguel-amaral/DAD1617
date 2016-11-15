using System;
using System.Net;
using System.Security;
using System.Security.Permissions;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;


namespace DADStormProcess {
	public class CSF_IpInName : CSF_TupleStructure {
		//Inner variables required
		//String is source ip; hastable key -> destIp , value -> number of communications
		private Dictionary<string,Hashtable> sinnerList = new Dictionary<string,Hashtable>();
		//Setup
		private const int minimumNumberOfMatches = 2; //between 1 and 4;
		public CSF_IpInName ()	{}

		public override void processTuple (IList<string> tuple) {

			string destIp = tuple [destinIpIndex];
			string destDNSname;
			try {
//				IPAddress addr = IPAddress.Parse();
				IPHostEntry host = Dns.GetHostEntry (destIp);
				destDNSname = host.HostName;

			} catch (Exception) {
				//System.Console.WriteLine ( "ip not found: " + destIp );

				//Lets ignore this troublemaker tuple then..
				return ;
			}
			string[] ipFields = destIp.Split (new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
			int intMatches = 0;
			foreach (string field in ipFields) {
				if (destDNSname.Contains (field)) {
					intMatches++;
				}
			}
			//System.Console.WriteLine ("Matches " + intMatches + "\nip: " + destIp +"\nname: " + destDNSname);

			if (intMatches >= minimumNumberOfMatches) {
				// So someone is talking with a domestic IP peer
				Hashtable existingCommunications;

				lock(sinnerList){
					//Check if source ip in dictionary
					if (sinnerList.TryGetValue (tuple [sourceIpIndex], out existingCommunications)) {
						//check if it is the first time they talk
						if (existingCommunications.ContainsKey (tuple [destinIpIndex])) {
							existingCommunications [tuple [destinIpIndex]] = (int)existingCommunications [tuple [destinIpIndex]] + 1;
						} else {
							existingCommunications [tuple [destinIpIndex]] = 1;
						}
					} else {
						//First time sourceIp is caught with hand in cookie jar
						existingCommunications = new Hashtable ();
						existingCommunications [tuple [destinIpIndex]] = 1;
						sinnerList [tuple [sourceIpIndex]] = existingCommunications;
					}
				}
			}
		}

		public override CSF_metric reportBack(){
            Dictionary<string, int> metricSinners = new Dictionary<string, int>();
            string metricName = this.GetType().Name;

            //--------------------------//
            //   calculate metric value  //
            //--------------------------//
            lock (sinnerList){
				//We do register if a connection happens only once or more often, but we ignore that..
				foreach(KeyValuePair<string, Hashtable> sourceEntry in sinnerList) {
					System.Console.WriteLine (sourceEntry.Key + " talked to " + sourceEntry.Value.Count + " domestic IPs");
                    metricSinners.Add(sourceEntry.Key, sourceEntry.Value.Count);
                }
            }
            CSF_metric metric = new CSF_metric(metricName, metricSinners);
            return metric;
        }

		public override void reset(){
			lock(sinnerList){
				sinnerList = new Dictionary<string,Hashtable>();
			}
		}
	}
}
/*
Connection to several domestic IP Peers which can be found through reverse IP tools: It
is not common that client machine connects directly to others clients. Usually communications
are done using known public servers. When using BitTorrent, the client connects
directly to another peers. Generally such domestics connections use an IP which reverse
name is predictable (ex: 92.95.1.165 reverse name is 165.1.95.92.rev.sfr.net) versus a server
IP address which is not (ex: Nexus RNL Server: 193.136.164.129 which reverse name
is nexus.rnl.tecnico.ulisboa.pt).*/
