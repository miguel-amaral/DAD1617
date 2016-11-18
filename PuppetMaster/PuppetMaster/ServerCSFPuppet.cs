using System;
using System.Net;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using System.Threading;
using System.Collections.Generic;

//TODO
//Use of Local Peer discovery: Modern BitTorrent clients implements Local Peer Discovery.
    //Such protocol is implemented using HTTP-like messages over UDP multicast group
    //239.192.152.143 on port 6771. However other applications(like DropBox) also use this
    //protocol which, once again, might trigger false positives.This can be avoided by looking
    //into the packet content

//TCP Hole Punching: Another way for clients to connect to other peers behind a Firewall/NAT
    //is to ask trackers to help them(using TCP Hole Punching technique). When
    //1
    //this happen, a TCP socket will change its destination/origin without the end users to start
    //a new socket.This technique is not commonly used by other applications and can be related
    //to BitTorrent usage

//Use of UPnP protocol: Clients behind a NAT usually use UPnP to ask the router to open
    //ports which will be used to receive incoming connections from another peers.The use of
    //this protocol will increase the chance that the client is using BitTorrent. On the other hand
    //there are several legitimate applications that make use of this protocol (like Skype) which
    //might trigger false positives

//High bandwidth usage: More often than not a high bandwidth usage during a long periods
    //of time are synonym of BitTorrent traffic.

namespace PuppetMaster {
	public class ServerCSFPuppet : ServerPuppet {
		private List<CSF_metric> metrics = new List<CSF_metric>();
		public override void extraCommands(string[] command) {
			if (command[0].Equals ("report", StringComparison.OrdinalIgnoreCase)) {
                this.doOperation("report");

			} else if (command[0].Equals ("reset", StringComparison.OrdinalIgnoreCase)) {
                this.doOperation("reset");
				metrics = new List<CSF_metric>();
			}
		}

        private void doOperation(string operation) {
            foreach (string op in operatorsConPacks.Keys) {
                doOperation(operation,op);
            }
        }

        private void doOperation(string operation, string operatorID) {
            List<ConnectionPack> listConPacks;
            if (operatorsConPacks.TryGetValue(operatorID, out listConPacks)) {
                foreach (ConnectionPack cp in listConPacks) {
                    Thread.Sleep(3000);
                    doOperation(operation,cp);
                }
            }
        }


        private void doOperation(string operation, ConnectionPack cp) {
            DADStormProcess.ClientProcess process = new DADStormProcess.ClientProcess(cp);
            if(operation.Equals("report", StringComparison.OrdinalIgnoreCase)) {
                CSF_metric metric = process.reportBack();
                metrics.Add(metric);
            }
            if (operation.Equals("reset", StringComparison.OrdinalIgnoreCase)) {
                process.reset();
            }
        }
		private void mergeMetrics(List<CSF_metric> metrics){
			for(int index = 0 ; index < ( metrics.Length - 1) ; index++) {
				CSF_metric original = metrics[index];
				MergingVisitor visitor = new MergingVisitor(element);
				for(int endIndex = index ; endIndex < metrics.Length ; ) {
					CSF_metric toBeMerged = metrics[endIndex];
					if(toBeMerged.aceptWithBool(toBeMerged) {
						metrics.RemoveAt(endIndex);
					} else {
						endIndex++;
					}
				}
			}
		}

		private void processAllMetrics(List<CSF_metric> metrics) {

		}

        private void processMetric(CSF_metric metric) {
            //TODO
        }

        public static new void Main(string[] args) {
            System.Console.WriteLine("CSF");
            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, false);
            PuppetMasterRemoteServerObject myServerObj = new PuppetMasterRemoteServerObject();
            RemotingServices.Marshal(myServerObj, "PuppetMasterRemoteServerObject", typeof(PuppetMasterRemoteServerObject));

            Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine("PuppetMaster Server Online : port: " + port);
            Console.ResetColor();
            System.Console.WriteLine("<enter> to exit...");

            System.Console.WriteLine("Hello, World! Im Controller and I will be your captain today");

            string configFileLocation;
            ServerCSFPuppet sp = (ServerCSFPuppet) ServerPuppet.CSFInstance;
            if (args.Length > 0)
            {
                configFileLocation = args[0];
                sp.readCommandsFromFile(configFileLocation);
            }

            System.Console.WriteLine("we are now in manual writing commands, write EXIT to stop");
            string line = System.Console.ReadLine();
            while (!line.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                sp.doCommand(line);
                line = System.Console.ReadLine();
            }

            System.Console.WriteLine("Goodbye World! It was a pleasure to serve you today");
        }
    }
}
