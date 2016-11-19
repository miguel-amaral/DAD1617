using System;
using System.Net;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

namespace PuppetMaster {
	public class ServerCSFPuppet : ServerPuppet {

        private int fileInterval = 60 * 1000; 

        private DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(CSF_metric));
        private List<CSF_metric> metrics = new List<CSF_metric>();
        public override bool extraCommands(string[] command) {
            if (command[0].Equals("report", StringComparison.OrdinalIgnoreCase)) {
                this.doOperation("report");
                System.Console.WriteLine("merging..");
                this.mergeMetrics();
                System.Console.WriteLine("processing..");
                this.processAllMetrics();

            }
            else if (command[0].Equals("reset", StringComparison.OrdinalIgnoreCase)) {
                this.doOperation("reset");
                metrics = new List<CSF_metric>();
            } else if (command[0].Equals("fileInterval", StringComparison.OrdinalIgnoreCase)) {
                fileInterval = Int32.Parse(command[1]) * 1000; //seconds
            } else if (command[0].Equals("PACKET_FILE", StringComparison.OrdinalIgnoreCase)) {
                string opID = command[1];
                string file = command[2];

                new Thread(() => {
                    int i = 0;
                    while (true) { 
                        Thread.Sleep(fileInterval); //Wait for the creation of nextFile
                        i = i + 1;
                        //this.addFileToOperator(file + i, opID);
                        System.Console.WriteLine(file + i + " to op: " + opID);
                    }
                }).Start();
            } else {
                return false; //No command found..
            }
            return true;
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
                    doOperation(operation,cp);
                }
            }
        }


        private void doOperation(string operation, ConnectionPack cp) {
            DADStormProcess.ClientProcess process = new DADStormProcess.ClientProcess(cp);
            if(operation.Equals("report", StringComparison.OrdinalIgnoreCase)) {
                MemoryStream stream = process.reportBack();
                if(stream != null)
                {
                    stream.Position = 0;
                    CSF_metric metric = correctMetric((CSF_metric)js.ReadObject(stream));
                    metrics.Add(metric);
                }
            }
            if (operation.Equals("reset", StringComparison.OrdinalIgnoreCase)) {
                process.reset();
            }
        }

        private CSF_metric correctMetric(CSF_metric metric) {
            string strMetric = metric.Metric;
            CSF_metric toReturn = metric;
            switch (strMetric) {
                case "CSF_HighDataDiffPeers" :
                    toReturn = new CSF_metricHighDataDiffPeers(strMetric, metric.RawValues);
                    break;
                case "CSF_ProtocolUPnP":
                    toReturn = new CSF_metricProtocolUPnP(strMetric, metric.Sinners);
                    break;
                case "CSF_LocalPeerDiscovery":
                    toReturn = new CSF_metricLocalPeerDiscovery(strMetric, metric.Sinners);
                    break;
                case "CSF_KnownTrackers":
                    toReturn = new CSF_metricKnownTrackers(strMetric, metric.RawValues);
                    break;
                case "CSF_IpInName":
                    toReturn = new CSF_metricIpInName(strMetric, metric.RawValues);
                    break;
                case "CSF_HighUpload":
                    toReturn = new CSF_metricHighUpload(strMetric, metric.RawValues);
                    break;
                case "CSF_HighDownload":
                    toReturn = new CSF_metricHighDownload(strMetric, metric.RawValues);
                    break;
                default:
                    System.Console.WriteLine("\n\nNOT FOUND!!!\n\n" + strMetric + "\n\n");
                    break;
            }
            return toReturn;

        }

		private void mergeMetrics(){
			for(int index = 0 ; index < ( metrics.Count - 1) ; index++) {
				CSF_metric original = metrics[index];
				MergingVisitor visitor = new MergingVisitor(original);
				for(int endIndex = index+1 ; endIndex < metrics.Count; ) {
					CSF_metric toBeMerged = metrics[endIndex];
					if(toBeMerged.aceptWithBool(visitor)) {
						metrics.RemoveAt(endIndex);
					} else {
						endIndex++;
					}
				}
			}
		}

		private void processAllMetrics() {
            foreach(CSF_metric metric in metrics) {
                processMetric(metric);
            }
		}

        private void processMetric(CSF_metric metric) {
            MetricCalculatorVisitor visitor = new MetricCalculatorVisitor();
            metric.acept(visitor);
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
            if (args.Length > 0) {
                configFileLocation = args[0];
                sp.readCommandsFromFile(configFileLocation);
            }

            System.Console.WriteLine("we are now in manual writing commands, write EXIT to stop");
            string line = System.Console.ReadLine();
            while (!line.Equals("exit", StringComparison.OrdinalIgnoreCase)) {
                sp.doCommand(line);
                line = System.Console.ReadLine();
            }

            System.Console.WriteLine("Goodbye World! It was a pleasure to serve you today");
            Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("Puppet Server is going OFFLINE");
            Console.ResetColor();
            sp.killRemainingOperators();
            Thread.Sleep(2000); //Ensuring everything is offline 
            System.Environment.Exit(0);
        }
    }
}
