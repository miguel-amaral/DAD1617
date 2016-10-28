using System;
using System.Net;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using System.Threading;
using System.Collections.Generic;


namespace PuppetMaster {
	public class ServerPuppet {

		
		private static int port = 10000;
		private bool fullLog = false;
		private bool firstStart = true;
		private static ServerPuppet instance = null;
		// key is the operator that emits the tuple
		// value is List of operators that will receive said tuples
		private Dictionary<string, List<string>> downStreamOperators = new Dictionary<string, List<string>>();
		// key is operator name
		// value is List of connections packs off said operator
		private Dictionary<string, List<ConnectionPack>> operatorsConPacks = new Dictionary<string, List<ConnectionPack>>();
		public static ServerPuppet Instance {
			get {
				if (instance == null) {
					System.Console.WriteLine("New ServerPuppet instance created");
					instance = new ServerPuppet();
				}
				return instance;
			}
		}

		private void doStatus (string opID)
		{
			List<ConnectionPack> listConPacks;
			if (operatorsConPacks.TryGetValue (opID, out listConPacks)) {
				Console.ForegroundColor = ConsoleColor.DarkCyan;
				System.Console.WriteLine ("Operator: " + opID + " status");
				Console.ResetColor();
				foreach (ConnectionPack cp in listConPacks) {
					DADStormProcess.ClientProcess process = new DADStormProcess.ClientProcess (cp);
					string status = process.status ();
					if (status.Equals ("Machine Failed")) {
						Console.ForegroundColor = ConsoleColor.Red;
					} else  {
						Console.ForegroundColor = ConsoleColor.Green;
					}
					System.Console.WriteLine (cp + " " + status);
					Console.ResetColor();
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
									System.Console.WriteLine ("Added Connection\nOutOperator: " + outPack + " Receiver: " + receiving_operator);
									outReplica.addDownStreamOperator (receivingReplicas);
									//}								
								}
							}
						}
					} else {
						//else it must be a file
						//foreach receivingOperator
						foreach (string receiving_operator in item.Value) {
							//Getting list of receiving replicas of operator
							if (operatorsConPacks.TryGetValue (receiving_operator, out receivingReplicas)) {
								//for each replica in the receivingOperator
								System.Console.WriteLine ("adding file: " + item.Key + " to: " + receiving_operator);
								foreach (ConnectionPack receivingPack in receivingReplicas) {
									DADStormProcess.ClientProcess receivingReplica = new DADStormProcess.ClientProcess (receivingPack);
									receivingReplica.addFile (item.Key);
								}								
							}
						}
					}
				}
				IPHostEntry host = Dns.GetHostEntry (Dns.GetHostName ());
				string ip = host.AddressList [0].ToString();
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
				System.Console.WriteLine("Operator: " + operator_id + " not in list");
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
			} else if (splitStr [0].Equals ("status", StringComparison.OrdinalIgnoreCase)) {
				if(splitStr.Length > 1) {
					doStatus (splitStr [1]);
				} else {
					doStatus ();
				}
			} else if ((splitStr [1].Equals ("input", StringComparison.OrdinalIgnoreCase) && splitStr [2].Equals ("ops", StringComparison.OrdinalIgnoreCase))
			    || splitStr [1].Equals ("input_ops", StringComparison.OrdinalIgnoreCase)) {
				this.createNewOperator (splitStr);
				//Process files TODO this.
			} else if (splitStr [0].Equals ("freeze", StringComparison.OrdinalIgnoreCase)
			           || splitStr [0].Equals ("unfree", StringComparison.OrdinalIgnoreCase)
			           || splitStr [0].Equals ("crash", StringComparison.OrdinalIgnoreCase)
			           || splitStr [0].Equals ("start", StringComparison.OrdinalIgnoreCase)) {
				this.replicaTargetOperations (splitStr);
			} else if (splitStr [0].Equals ("interval", StringComparison.OrdinalIgnoreCase)) {
				this.operatorTargetOperations (splitStr);
			} else if (splitStr [0].Equals ("wait", StringComparison.OrdinalIgnoreCase)) {
				Thread.Sleep (Int32.Parse (splitStr [1]));
			} else if (splitStr [0].Equals ("LoggingLevel", StringComparison.OrdinalIgnoreCase)) {
				this.fullLog = splitStr [1].Equals ("full", StringComparison.OrdinalIgnoreCase);
			} else if (splitStr [0].Equals ("Semantics", StringComparison.OrdinalIgnoreCase)) {
				//TODO
				//this.fullLog = splitStr [1].Equals ("full", StringComparison.OrdinalIgnoreCase);
			} 
		}

		private void readCommandsFromFile (string fileLocation){
			String line;
			// Read the file and display it line by line.
			System.IO.StreamReader file = new System.IO.StreamReader(fileLocation);
			while((line = file.ReadLine()) != null) {
				doCommand (line);
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

			//reading word routing 
			counter++;
			string routing = splitStr [counter++];

			//reading word address 
			counter++;
			List<ConnectionPack> currentConnectionPacks = new List<ConnectionPack> ();
			while (!(splitStr [counter].Equals ("operator", StringComparison.OrdinalIgnoreCase) && splitStr [counter + 1].Equals ("spec", StringComparison.OrdinalIgnoreCase))
			       && !splitStr [counter].Equals ("operator_spec", StringComparison.OrdinalIgnoreCase)) {

				string url = splitStr [counter];
				string[] parsedUrl = url.Split (new[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);

				string ip = parsedUrl [1];
				if (ip.Equals ("localhost", StringComparison.OrdinalIgnoreCase)) {
					IPHostEntry host = Dns.GetHostEntry (Dns.GetHostName ());
					ip = host.AddressList [0].ToString();
				}
				ConnectionPack cp = new ConnectionPack (ip, Int32.Parse (parsedUrl [2]));
				currentConnectionPacks.Add (cp);
				counter++;
			}
			System.Console.WriteLine("Operator: " + current_operator_id + " has " + currentConnectionPacks.Count + " replicas");
			operatorsConPacks.Add (current_operator_id, currentConnectionPacks);

			if (splitStr [counter].Equals ("operator", StringComparison.OrdinalIgnoreCase) && splitStr [counter + 1].Equals ("spec", StringComparison.OrdinalIgnoreCase)) {
				counter++;
				counter++;
				// then it must 
			} else if (splitStr [counter].Equals ("operator_spec", StringComparison.OrdinalIgnoreCase)) {
				counter++;
			}

			string operatorType = splitStr [counter++];
			string dll       = null;
			string className = null;
			string methodName= null;

			if (operatorType.Equals ("CUSTOM", StringComparison.OrdinalIgnoreCase)) {
				dll        = splitStr [counter++];
				className  = splitStr [counter++];
				methodName = splitStr [counter++];
			} else if (operatorType.Equals ("UNIQ", StringComparison.OrdinalIgnoreCase)) {
				//field_number;
			} else if (operatorType.Equals ("COUNT", StringComparison.OrdinalIgnoreCase)) {
				//;
			} else if (operatorType.Equals ("DUP", StringComparison.OrdinalIgnoreCase)) {
				dll = "Default.dll";
				className = "Default";
				methodName = "Dup";
			} else if (operatorType.Equals ("FILTER", StringComparison.OrdinalIgnoreCase)) {
				// field_number;
				// condition;
				// value;
			}
		

			//XXX TODO
			string arg1 = "olá";
			string arg2 = "mundo!";
			string[] staticAsrguments = { arg1, arg2 };
			staticAsrguments = null;
			//staticAsrguments = null;
			//Create the Processes
			foreach(ConnectionPack cp in currentConnectionPacks){
				Daemon.ClientDaemon cd = new Daemon.ClientDaemon (new ConnectionPack (cp.Ip, 10001),fullLog);
				cd.newThread (dll, className, methodName, cp.Port.ToString(),staticAsrguments);
			}
			//Make sure everything is created
			Thread.Sleep (100);



		}

		public void logTupple(string senderUrl, string[] tuple){
			//tuple replica URL, < list − of − tuple − f ields >
			string toPrint = "tuple url: " + senderUrl + " <";
			foreach(string str in tuple){
				toPrint += " " + str;
			}
			toPrint += " >";
			System.Console.WriteLine(toPrint);
		}

		public static void Main (string[] args)
		{
			TcpChannel channel = new TcpChannel (port);
			ChannelServices.RegisterChannel (channel, false);
			PuppetMasterRemoteServerObject myServerObj = new PuppetMasterRemoteServerObject ();
			RemotingServices.Marshal (myServerObj, "PuppetMasterRemoteServerObject", typeof(PuppetMasterRemoteServerObject));

			System.Console.WriteLine ("PuppetMaster Server Online : port: " + port);
			System.Console.WriteLine ("<enter> to exit...");

			System.Console.WriteLine ("Hello, World! Im Controller and I will be your captain today");

			string configFileLocation;
			ServerPuppet sp = ServerPuppet.Instance;
			if (args.Length > 0) {
				configFileLocation = args [0];
				sp.readCommandsFromFile (configFileLocation);
			} else {

				//string pc1   = "lab7p2";
				string pc2 = "localhost";
				int port1 = 42154;
				int port2 = 42155;
				string arg1 = "olá";
				string arg2 = "mundo!";
				string[] argumentos = { arg1, arg2 };
				argumentos = null;

				//		Daemon.ClientDaemon daemon1 = new Daemon.ClientDaemon();
				//		daemon1.connect("10001",pc1);
				//		System.Console.WriteLine("daemon 1: "+daemon1.ping());

				try {
					ConnectionPack cp_daemon = new ConnectionPack (pc2, 10001);
					Daemon.ClientDaemon daemon2 = new Daemon.ClientDaemon (cp_daemon, false);
					System.Console.WriteLine ("daemon 2: " + daemon2.ping ());

					//	daemon1.newThread( "hello.dll" , "Hello" , "Hello0", port1);
					daemon2.newThread ("hello.dll", "Hello", "retornaInt", port2.ToString (), argumentos);
					daemon2.newThread ("hello.dll", "Hello", "retornaInt", port1.ToString (), argumentos);
					argumentos = new string[2];
					argumentos [0] = arg2;
					argumentos [1] = arg1;
					//			daemon2.newThread( "hello.dll" , "Hello" , "Hello2", port1, argumentos);
					//		daemon2.newThread( "hello.dll" , "Hello" , "Main", port1, argumentos);


					// Allow for the creation of services
					// Kinda of start method
					Thread.Sleep (1000);

					ConnectionPack cp_process1 = new ConnectionPack (pc2, port2);
					ConnectionPack cp_process2 = new ConnectionPack (pc2, port1);
					//	DADStormProcess.ClientProcess process1 = new DADStormProcess.ClientProcess();
					DADStormProcess.ClientProcess process2 = new DADStormProcess.ClientProcess (cp_process1);
					DADStormProcess.ClientProcess process3 = new DADStormProcess.ClientProcess (cp_process2);

					//DADStormProcess.ClientProcess process3 = new DADStormProcess.ClientProcess();

					//	process1.connect(port1,pc1);


					//process2.addDownStreamOperator(cp_process2);
					process2.addTuple (argumentos);
					process2.addTuple (argumentos);
					process2.addTuple (argumentos);
					process2.addTuple (argumentos);
					process2.addTuple (argumentos);
					process2.addTuple (argumentos);
					process2.addTuple (argumentos);
					process2.addTuple (argumentos);

					List<ConnectionPack> replicas = new List<ConnectionPack>();
					replicas.Add(cp_process2);
					process2.addDownStreamOperator (replicas);

					Thread.Sleep (1000);

					process2.start ();
					Thread.Sleep (5000);

					process3.start ();



					//			process3.connect(port1,pc2);

					//	System.Console.WriteLine("process 1: "+process1.ping());
					System.Console.WriteLine ("process 2: " + process2.ping ());
					//			System.Console.WriteLine("process 3: "+process3.ping());
				} catch (RemotingException e) {
					Console.ForegroundColor = ConsoleColor.Red;
					System.Console.WriteLine ("Connection failed, you sure Daemon Server is online at ip: " + pc2 + " ? ");
					System.Console.WriteLine ("Reason: " + e.Message);
					Console.ResetColor ();
				}
			}

			System.Console.WriteLine("Goodbye World! It was a pleasure to serve you today");
			System.Console.ReadLine();
		}
	}
}
