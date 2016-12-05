using System;
using System.Net;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using System.Threading;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PuppetMaster {
	public class ServerPuppet {

        protected static int port = 10001;
        protected static PuppetMasterRemoteServerObject myServerObj;
        private static int daemonPort = 10000;
		private bool fullLog = false;
		private bool firstStart = true;
		private static ServerPuppet instance = null;
		// key is the operator that emits the tuple
		// value is List of operators that will receive said tuples
		private Dictionary<string, List<string>> downStreamOperators = new Dictionary<string, List<string>>();
		// key is operator name
		// value is List of connections packs off said operator
		protected Dictionary<string, List<ConnectionPack>> operatorsConPacks = new Dictionary<string, List<ConnectionPack>>();
		public static ServerPuppet Instance {
			get {
				if (instance == null) {
					System.Console.WriteLine("New ServerPuppet instance created");
					instance = new ServerPuppet();
				}
				return instance;
			}
		}
        public static ServerPuppet CSFInstance {
            get
            {
                if (instance == null)
                {
                    System.Console.WriteLine("New ServerCSFPuppet instance created");
                    instance = new ServerCSFPuppet();
                }
                return instance;
            }
        }
        public static int semantics = 0; //semantics 0 - at most once; 1 - at least once; 2 - exactly once

        private void doStatus(string[] command)  {
            if (command.Length > 2) {
                doStatus(command[1], Int32.Parse(command[2]));
            }
            else if (command.Length > 1) {
                doStatus(command[1]);
            }
            else {
                doStatus();
            }
        }
        private void doStatus(string opID, int index) {
            List<ConnectionPack> listConPacks;
            if (operatorsConPacks.TryGetValue(opID, out listConPacks)) {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                System.Console.WriteLine("Operator: " + opID + " status");
                Console.ResetColor();
                doStatus(listConPacks[index]);
            }
        }
        private void doStatus(ConnectionPack cp) {
            DADStormProcess.ClientProcess process = new DADStormProcess.ClientProcess(cp);
            string status = process.status();
            if (status.Equals("Machine Failed")) {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            System.Console.WriteLine(cp + " " + status);
            Console.ResetColor();
        }

        private string[] ignoreHostname = { "DESKTOP-AMS9BIJ" };

        private void doStatus (string opID)	{
			List<ConnectionPack> listConPacks;
			if (operatorsConPacks.TryGetValue (opID, out listConPacks)) {
				Console.ForegroundColor = ConsoleColor.DarkCyan;
				System.Console.WriteLine ("Operator: " + opID + " status");
				Console.ResetColor();
				foreach (ConnectionPack cp in listConPacks) {
                    doStatus(cp);
				}
			}
		}

		private void doStatus() {
			foreach( string op in operatorsConPacks.Keys) {
				doStatus (op);
			}
		}

		/// <summary>
		/// Dos the first start connections.
		/// </summary>
		private void doFirstStartConnections ()	{
			if (firstStart) {
				System.Console.WriteLine ();
				PuppetDebug ("Deploying Connections in network");
				System.Console.WriteLine ();
				//Creating the network betwen all operators
				foreach (KeyValuePair<string, List<string>> item in downStreamOperators) {
					List<ConnectionPack> outputingReplicas;
					List<ConnectionPack> receivingReplicas;
					string emitingOperator = item.Key;
					//Getting list of Outputing replicas
					if (operatorsConPacks.TryGetValue (emitingOperator, out outputingReplicas)) {
						//Then it is an operator
						//foreach output replica in outputOPerator
						foreach (ConnectionPack outPack in outputingReplicas) {
							//create processClient
							DADStormProcess.ClientProcess outReplica = new DADStormProcess.ClientProcess (outPack);
							//foreach receivingOperator
							foreach (string receiving_operator in item.Value) {
								//Getting list of receiving replicas of operator
								if (operatorsConPacks.TryGetValue (receiving_operator, out receivingReplicas)) {
									//for each replica in the receivingOperator
									//foreach (ConnectionPack receivingPack in receivingReplicas) {
									PuppetDebug ("Connection: Out: " + outPack + " In: " + receiving_operator);
									outReplica.addDownStreamOperator (receivingReplicas);
									//}
								}
							}
						}
					} else {
						//else it must be a file
						//foreach receivingOperator
						foreach (string receiving_operator in item.Value) {
                            addFileToOperator(item.Key, receiving_operator);
						}
					}
				}
                string ip = getMyIp();
                ConnectionPack myConPack = new ConnectionPack (ip,port);
				foreach(List<ConnectionPack> list in operatorsConPacks.Values) {
					foreach(ConnectionPack cp in list){
						DADStormProcess.ClientProcess process = new DADStormProcess.ClientProcess (cp);
						//Telling every operator's replica all its replicas
						process.assignReplicaList (list);
						//Telling every operator's replica where puppetMaster is
						process.assignPuppetConPack (myConPack);
					}
				}
				this.firstStart = false;
			} else {
				//we already started everything..
				return;
			}
		}

        protected void addFileToOperator(string fileLocation, string opID) {
            List<ConnectionPack> receivingReplicas;
            //Getting list of receiving replicas of operator
            if (operatorsConPacks.TryGetValue(opID, out receivingReplicas)) {
                //for each replica in the receivingOperator
                PuppetDebug("adding file: " + fileLocation + " to: " + opID);
                foreach (ConnectionPack receivingPack in receivingReplicas) {
                    DADStormProcess.ClientProcess receivingReplica = new DADStormProcess.ClientProcess(receivingPack);
                    receivingReplica.addFile(fileLocation);
                }
            }
        }

		/// <summary>
		/// Method that from a string[] does a command in a single replica
		/// </summary>
		private void  replicaTargetOperations (string[] splitStr) {
			if(splitStr.Length < 3) {
				operatorTargetOperations (splitStr);
				return;
			}
			string command     = splitStr[0];
			string operator_id = splitStr[1];

			//Must be an integer
			int process_number = Int32.Parse(splitStr[2]);
			List<ConnectionPack> operatorList;
			if(operatorsConPacks.TryGetValue(operator_id,out operatorList)){
				ConnectionPack conPack = operatorList [process_number];
				DADStormProcess.ClientProcess cprocess = new DADStormProcess.ClientProcess (conPack);

				if(command.Equals("freeze", StringComparison.OrdinalIgnoreCase)){
					cprocess.freeze();
				} else if(command.Equals("unfreeze", StringComparison.OrdinalIgnoreCase)){
					cprocess.unfreeze();
				} else if(command.Equals("crash", StringComparison.OrdinalIgnoreCase)){
					cprocess.crash();
				} else if(command.Equals("start", StringComparison.OrdinalIgnoreCase)){
					if(firstStart){
						doFirstStartConnections ();
					}
					cprocess.start();
				}

			} else {
				PuppetError("Operator: " + operator_id + " not in list");
			}
		}

		/**
		  * Method that from a string[] does a command in a whole operator, i.e., all its replicas
		  */
		private void operatorTargetOperations (string[] splitStr) {
			string command     = splitStr[0];
			string operator_id = splitStr[1];

			//Must be an integer
			List<ConnectionPack> operatorList;
			if(operatorsConPacks.TryGetValue(operator_id,out operatorList)){
				foreach(ConnectionPack conPack in operatorList){
					DADStormProcess.ClientProcess cprocess = new DADStormProcess.ClientProcess (conPack);

					if(command.Equals("start", StringComparison.OrdinalIgnoreCase)){
						if(firstStart){
							doFirstStartConnections ();
						}
						cprocess.start();
					} else if(command.Equals("interval", StringComparison.OrdinalIgnoreCase)){
						cprocess.interval(Int32.Parse(splitStr[2]));
					} /*else if(command.Equals("crash", StringComparison.OrdinalIgnoreCase)){
						cprocess.crash();
					}*/
				}
			} else {
				System.Console.WriteLine("Operator: " + operator_id + " not in list");
			}
		}

		/// <summary>
		/// From a line does a command, needs to be complete
		/// </summary>
		/// <param name="line">Line.</param>
		public void doCommand (string line) {
			//Ignoring comments
			if (line.StartsWith ("%")) {
				return;
			}

			String[] splitStr = line.Split (new[] { ',', ' ', '"' }, StringSplitOptions.RemoveEmptyEntries);
			if (splitStr.Length == 0) {
				return;
			}
            else if (splitStr[0].Equals("status", StringComparison.OrdinalIgnoreCase)) {
                doStatus(splitStr);
            }
            else if (splitStr[0].Equals("read_file", StringComparison.OrdinalIgnoreCase)) {
                readCommandsFromFile(splitStr[1]);
            } else if (splitStr [0].Equals ("freeze", StringComparison.OrdinalIgnoreCase)
			           || splitStr [0].Equals ("unfreeze", StringComparison.OrdinalIgnoreCase)
			           || splitStr [0].Equals ("crash", StringComparison.OrdinalIgnoreCase)
			           || splitStr [0].Equals ("start", StringComparison.OrdinalIgnoreCase)) {
				this.replicaTargetOperations (splitStr);
			} else if (splitStr [0].Equals ("interval", StringComparison.OrdinalIgnoreCase)) {
				this.operatorTargetOperations (splitStr);
			} else if (splitStr [0].Equals ("wait", StringComparison.OrdinalIgnoreCase)) {
				Thread.Sleep (Int32.Parse (splitStr [1])); //wait sleep
			} else if (splitStr[0].Equals("LoggingLevel", StringComparison.OrdinalIgnoreCase))            {
                this.fullLog = splitStr[1].Equals("full", StringComparison.OrdinalIgnoreCase);
            } else if (splitStr [0].Equals ("Semantics", StringComparison.OrdinalIgnoreCase)) {
                if(splitStr[1].Equals("at-most-once", StringComparison.OrdinalIgnoreCase)) {
                    semantics = 0;
                } else if (splitStr[1].Equals("at-least-once", StringComparison.OrdinalIgnoreCase)) {
                    semantics = 1;
                } else if (splitStr[1].Equals("exactly-once", StringComparison.OrdinalIgnoreCase)) {
                    semantics = 2;
                }
            } else if (splitStr.Length > 1 && ((splitStr[1].Equals("input", StringComparison.OrdinalIgnoreCase) && splitStr[2].Equals("ops", StringComparison.OrdinalIgnoreCase))
                      || splitStr[1].Equals("input_ops", StringComparison.OrdinalIgnoreCase))) {
                this.createNewOperator(splitStr);
                //Process files TODO this.
            } else {
				//nothing so far, maybe an extra command
				if(!this.extraCommands(splitStr)) {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    System.Console.WriteLine("Command: " + line + " not found");
                    Console.ResetColor();
                }
			}
		}

		public virtual bool extraCommands(string[] command) { return false;/*CSF will add its own commands*/ }

		protected void readCommandsFromFile (string fileLocation){
			String line;
            // Read the file and display it line by line.
            System.Console.WriteLine("Loading file: " + fileLocation);
            System.IO.StreamReader file = new System.IO.StreamReader(@fileLocation);
            System.Console.WriteLine("Please specify whether you want step by step or all in");
            System.Console.WriteLine("Type \"step\" or \"all\"");
            string mode = System.Console.ReadLine();
            while (!(mode.Equals("all", StringComparison.OrdinalIgnoreCase) || mode.Equals("step", StringComparison.OrdinalIgnoreCase)))
            {
                mode = System.Console.ReadLine();
            }
            bool stepByStep = mode.Equals("step", StringComparison.OrdinalIgnoreCase);
            if (stepByStep) {
                System.Console.WriteLine("Ok just hit enter whenever you want to do a step");
            }
            while ((line = file.ReadLine()) != null) {
                if (stepByStep) {
                    System.Console.WriteLine("Next Step: " + line);
                    string next = System.Console.ReadLine();
                    if(next.Equals("all", StringComparison.OrdinalIgnoreCase)) { stepByStep = false; }
                    else if(next.Equals("stop", StringComparison.OrdinalIgnoreCase)) { break; }
                }
                doCommand(line);
			}
			file.Close();
		}

		private void createNewOperator (String[] splitStr)
		{
			//foreach(string str in splitStr){
			//	System.Console.WriteLine (str);
			//}
			int counter = 0;
			string current_operator_id = splitStr [0];

			// ----------------- //
			//  INput Operators  //
			// ----------------- //
			if (splitStr [1].Equals ("input", StringComparison.OrdinalIgnoreCase) && splitStr [2].Equals ("ops", StringComparison.OrdinalIgnoreCase)) {
				counter = 3;
				// then it must
			} else if (splitStr [1].Equals ("input_ops", StringComparison.OrdinalIgnoreCase)) {
				counter = 2;
			}

			while (!(splitStr [counter].Equals ("rep", StringComparison.OrdinalIgnoreCase) && splitStr [counter + 1].Equals ("fact", StringComparison.OrdinalIgnoreCase))
			       && !splitStr [counter].Equals ("rep_fact", StringComparison.OrdinalIgnoreCase)) {

				List<String> existingOperators;
				String emitingOperator = splitStr [counter];
				if (downStreamOperators.TryGetValue (emitingOperator, out existingOperators)) {
					//found something
					//Add current operator to downStreamOperators of inputOperator
					existingOperators.Add (current_operator_id);
				} else {
					//was not initialized yet
					existingOperators = new List<String> ();
					downStreamOperators.Add (emitingOperator, existingOperators);
					//Add current operator to downStreamOperators of inputOperator
					existingOperators.Add (current_operator_id);
				}
				counter++;
			}

			// ------------------ //
			// Replication Factor //
			// ------------------ //
			if (splitStr [counter].Equals ("rep", StringComparison.OrdinalIgnoreCase) && splitStr [counter + 1].Equals ("fact", StringComparison.OrdinalIgnoreCase)) {
				counter++;
				counter++;
				// then it must
			} else if (splitStr [counter].Equals ("rep_fact", StringComparison.OrdinalIgnoreCase)) {
				counter++;
			}
			string rep_factor = splitStr [counter++];

			//ignoring word routing
			counter++;
			string routing = splitStr [counter++];

			//ignoring word address
			counter++;
			List<ConnectionPack> currentConnectionPacks = new List<ConnectionPack> ();
			while (!(splitStr [counter].Equals ("operator", StringComparison.OrdinalIgnoreCase) && splitStr [counter + 1].Equals ("spec", StringComparison.OrdinalIgnoreCase))
			       && !splitStr [counter].Equals ("operator_spec", StringComparison.OrdinalIgnoreCase)) {

				string url = splitStr [counter];
				string[] parsedUrl = url.Split (new[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);

				string ip = parsedUrl [1];
				if (ip.Equals ("localhost", StringComparison.OrdinalIgnoreCase)) {
                    ip = getMyIp();
                }
				ConnectionPack cp = new ConnectionPack (ip, Int32.Parse (parsedUrl [2]));
				currentConnectionPacks.Add (cp);
				counter++;
			}
			operatorsConPacks.Add (current_operator_id, currentConnectionPacks);

			if (splitStr [counter].Equals ("operator", StringComparison.OrdinalIgnoreCase) && splitStr [counter + 1].Equals ("spec", StringComparison.OrdinalIgnoreCase)) {
				counter++;
				counter++;
				// then it must
			} else if (splitStr [counter].Equals ("operator_spec", StringComparison.OrdinalIgnoreCase)) {
				counter++;
			}

			string operatorType = splitStr [counter++];
			string dll       = "CommonTypes.dll";
            string className = "Default";
			string methodName= null;
            string[] staticAsrguments = null;


            if (operatorType.Equals ("CUSTOM", StringComparison.OrdinalIgnoreCase)) {
				dll        = splitStr [counter++];
				className  = splitStr [counter++];
				methodName = splitStr [counter++];
			} else if (operatorType.Equals ("UNIQ", StringComparison.OrdinalIgnoreCase)) {
                methodName = "Uniq";
                string[] args = { splitStr [counter++] };
                staticAsrguments = args;
                //field_number;
            } else if (operatorType.Equals ("COUNT", StringComparison.OrdinalIgnoreCase)) {
                methodName = "Count";
            } else if (operatorType.Equals ("DUP", StringComparison.OrdinalIgnoreCase)) {
				methodName = "Dup";
            } else if (operatorType.Equals("OUTPUT", StringComparison.OrdinalIgnoreCase)) {
                methodName = "Output";
                string[] args = { splitStr[counter++] }; //File to Output
                staticAsrguments = args;
            } else if (operatorType.Equals ("FILTER", StringComparison.OrdinalIgnoreCase)) {
                methodName = "Filter";
                string field_number  = splitStr [counter++]; // field_number;
                string condition     = splitStr [counter++]; // condition;
                string comparedValue = splitStr [counter++]; // value;
				string[] args = { field_number, condition, comparedValue };
                staticAsrguments = args;
            }

			//Create the Processes
			foreach(ConnectionPack cp in currentConnectionPacks){
				Daemon.ClientDaemon cd = new Daemon.ClientDaemon (new ConnectionPack (cp.Ip, daemonPort),fullLog);
				cd.newThread (dll, className, methodName, cp.Port.ToString(), cp.Ip, semantics, routing, current_operator_id,staticAsrguments);
			}
			Thread.Sleep (100);  //Make sure everything is created before we try anything else
            PuppetDebug("Operator:" + current_operator_id + " has " + currentConnectionPacks.Count + " replicas, created");
        }

        private void killOperator(string opID) {
            List<ConnectionPack> listConPacks;
            if (operatorsConPacks.TryGetValue(opID, out listConPacks)) {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                System.Console.WriteLine("Killing Operator: " + opID);
                Console.ResetColor();
                foreach (ConnectionPack cp in listConPacks) {
                    try  {
                        killProcess(cp);
                    } catch (Exception) {
                        // He is probably dead already
                    }
                }
            }
        }

        private void killProcess(ConnectionPack cp) {
            DADStormProcess.ClientProcess process = new DADStormProcess.ClientProcess(cp);
            process.crash();
        }

        public void killRemainingOperators() {
            foreach (string op in operatorsConPacks.Keys) {
                killOperator(op);
            }
        }

        private string getMyIp() {
            string hostname = Environment.MachineName;
            int pos = Array.IndexOf(ignoreHostname, hostname);
            if (pos > -1)
            {
                //There will be trouble if we dont ignore this hostname
                return "localhost";
            }
            // Get a list of all network interfaces (usually one per network card, dialup, and VPN connection)
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface network in networkInterfaces)
            {
                // Read the IP configuration for each network
                IPInterfaceProperties properties = network.GetIPProperties();

                // Each network interface may have multiple IP addresses
                foreach (IPAddressInformation address in properties.UnicastAddresses)
                {
                    // We're only interested in IPv4 addresses for now
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    // Ignore loopback addresses (e.g., 127.0.0.1)
                    if (IPAddress.IsLoopback(address.Address))
                        continue;

                    return address.Address.ToString();
                }
            }
            return "localhost";
        }

		public void logTupple(string senderUrl, IList<string> tuple){
			//tuple replica URL, < list − of − tuple − f ields >
			string toPrint = "tuple url: " + senderUrl + " <";
			foreach(string str in tuple){
				toPrint += " " + str;
			}
			toPrint += " >";
			System.Console.WriteLine(toPrint);
		}
        
        private void PuppetDebug(string msg) {
            System.Console.WriteLine("[ PuppetMaster ] " + msg);
        }
        private void PuppetError(string msg) {
            Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("[ Error ] " + msg);
            Console.ResetColor();

        }
        public static void Main (string[] args)	{
            TcpChannel channel = new TcpChannel (port);
			ChannelServices.RegisterChannel (channel, false);
			myServerObj = new PuppetMasterRemoteServerObject ();
			RemotingServices.Marshal (myServerObj, "PuppetMasterRemoteServerObject", typeof(PuppetMasterRemoteServerObject));

            Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine ("PuppetMaster Server Online : port: " + port);
            Console.ResetColor();
            System.Console.WriteLine ("<enter> to exit...");

			System.Console.WriteLine ("Hello, World! Im Controller and I will be your captain today");

			string configFileLocation;
			ServerPuppet sp = ServerPuppet.Instance;
			if (args.Length > 0) {
                configFileLocation = args [0];
                sp.readCommandsFromFile (@configFileLocation);
			}

			System.Console.WriteLine("we are now in manual writing commands, write EXIT to stop");
			string line = System.Console.ReadLine ();
			while(!line.Equals("exit", StringComparison.OrdinalIgnoreCase )) {
				sp.doCommand (line);
                line = System.Console.ReadLine ();
			}

			System.Console.WriteLine("Goodbye World! It was a pleasure to serve you today");
            Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("Puppet Server is going OFFLINE");
            Console.ResetColor();
            sp.killRemainingOperators();
            Thread.Sleep(1000);//Ensuring everything is offline
        }
	}
}
