using System;
using System.Net;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using System.Threading;
using System.Collections.Generic;


namespace PuppetMaster {
	public class ServerCSFPuppet : ServerPuppet {
		public override void extraCommands(string[] command) {
			if (command[0].Equals ("report", StringComparison.OrdinalIgnoreCase)) {
                this.doOperation("report");
			} else if (command[0].Equals ("reset", StringComparison.OrdinalIgnoreCase)) {
                this.doOperation("reset");
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
                processMetric(metric);
            }
            if (operation.Equals("reset", StringComparison.OrdinalIgnoreCase)) {
                process.reset();
            }
        }

        private void processMetric(CSF_metric metric) {
            //TODO
        }

        public static new void Main(string[] args)
        {
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
