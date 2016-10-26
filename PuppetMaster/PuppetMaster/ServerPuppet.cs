using System;
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



		private void doFirstStartConnections ()
		{
			if (firstStart) {
				//Make sure everything is created
				Thread.Sleep (500);
				//doFirstStart ();
				foreach (KeyValuePair<string, List<string>> item in downStreamOperators) {
					List<ConnectionPack> outputingReplicas;
					List<ConnectionPack> receivingReplicas;

					//Getting list of Outputing replicas
					if (operatorsConPacks.TryGetValue (item.Key, out outputingReplicas)) {
						//Then it is an operator
						//foreach output replica in outputOPerator
						foreach (ConnectionPack outPack in outputingReplicas) {
							//create processClient
							DADStormProcess.ClientProcess outReplica = new DADStormProcess.ClientProcess ();
							outReplica.connect (outPack);
							//foreach receivingOperator
							foreach (string receiving_operator in item.Value) {
								//Getting list of receiving replicas of operator
								if (operatorsConPacks.TryGetValue (receiving_operator, out receivingReplicas)) {
									//for each replica in the receivingOperator
									foreach (ConnectionPack receivingPack in outputingReplicas) {
										outReplica.addDownStreamOperator (receivingPack);
									}								
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
								System.Console.WriteLine ("will crash: " + receiving_operator);
								foreach (ConnectionPack receivingPack in receivingReplicas) {
									DADStormProcess.ClientProcess receivingReplica = new DADStormProcess.ClientProcess ();
									receivingReplica.connect (receivingPack);
									receivingReplica.addFile (item.Key);
								}								
							}
						}
					}
				}
				this.firstStart = false;
			} else {
				//we already started everything..
				return;
			}
		}

		/**
		  * Method that from a string[] does a command in a single replica
		  */
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
				DADStormProcess.ClientProcess cprocess = new DADStormProcess.ClientProcess ();
				cprocess.connect (conPack);

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
					DADStormProcess.ClientProcess cprocess = new DADStormProcess.ClientProcess ();
					cprocess.connect (conPack);

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

		void readCommandsFromFile (string fileLocation){
			String line;
			// Read the file and display it line by line.
			System.IO.StreamReader file =new System.IO.StreamReader(fileLocation);
			while((line = file.ReadLine()) != null) {
				//Ignoring comments
				if(line.StartsWith("%")){
					continue;
				}


				String[] splitStr = line.Split(new[] { ',', ' ','"' }, StringSplitOptions.RemoveEmptyEntries);
				if(splitStr.Length == 0){
					continue;
				}
				if((splitStr[1].Equals("input", StringComparison.OrdinalIgnoreCase) && splitStr[2].Equals("ops", StringComparison.OrdinalIgnoreCase))
				   || splitStr[1].Equals("input_ops", StringComparison.OrdinalIgnoreCase)){
					this.createNewOperator (splitStr);
					//Process files TODO this.
				} else if(splitStr[0].Equals("freeze", StringComparison.OrdinalIgnoreCase)
					||splitStr[0].Equals("unfree", StringComparison.OrdinalIgnoreCase)
					||splitStr[0].Equals("crash", StringComparison.OrdinalIgnoreCase)
					||splitStr[0].Equals("start", StringComparison.OrdinalIgnoreCase)
				){
					this.replicaTargetOperations (splitStr);
				} else if(splitStr[0].Equals("interval", StringComparison.OrdinalIgnoreCase)) {
					this.operatorTargetOperations(splitStr);
				}
				//Status TODO
			}
			file.Close();
		}

		private void createNewOperator (String[] splitStr){
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
				String inputOperator = splitStr [counter];
				if (downStreamOperators.TryGetValue (inputOperator, out existingOperators)) {
					//found something
					//Add current operator to downStreamOperators of inputOperator
					existingOperators.Add (current_operator_id);
				} else {
					//was not initialized yet
					existingOperators = new List<String> ();
					downStreamOperators.Add (inputOperator, existingOperators);
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
				System.Console.WriteLine ("Connection Pack: " + parsedUrl [1] + " : " + parsedUrl [2]);
				ConnectionPack cp = new ConnectionPack (parsedUrl [1], Int32.Parse (parsedUrl [2]));
				currentConnectionPacks.Add (cp);
				counter++;
			}
			System.Console.WriteLine("current: " + current_operator_id + " : " + currentConnectionPacks.Count + " replicas");
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
				//;
			} else if (operatorType.Equals ("FILTER", StringComparison.OrdinalIgnoreCase)) {
				// field_number;
				// condition;
				// value;
			}
		

			string arg1 = "olá";
			string arg2 = "mundo!";
			string[] argumentos = { arg1, arg2 };
			//Create the Processes
			foreach(ConnectionPack cp in currentConnectionPacks){
				Daemon.ClientDaemon cd = new Daemon.ClientDaemon ();
				cd.connect (new ConnectionPack (cp.Ip, 10001));
				cd.newThread (dll, className, methodName, cp.Port.ToString(),argumentos);
			}


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

				//		Daemon.ClientDaemon daemon1 = new Daemon.ClientDaemon();
				//		daemon1.connect("10001",pc1);
				//		System.Console.WriteLine("daemon 1: "+daemon1.ping());

				Daemon.ClientDaemon daemon2 = new Daemon.ClientDaemon ();
				try {
					ConnectionPack cp_daemon = new ConnectionPack (pc2, 10001);
					daemon2.connect (cp_daemon, false);
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


					//	DADStormProcess.ClientProcess process1 = new DADStormProcess.ClientProcess();
					DADStormProcess.ClientProcess process2 = new DADStormProcess.ClientProcess ();
					DADStormProcess.ClientProcess process3 = new DADStormProcess.ClientProcess ();

					//DADStormProcess.ClientProcess process3 = new DADStormProcess.ClientProcess();

					//	process1.connect(port1,pc1);
					ConnectionPack cp_process1 = new ConnectionPack (pc2, port2);
					ConnectionPack cp_process2 = new ConnectionPack (pc2, port1);

					process2.connect (cp_process1);
					//process2.addDownStreamOperator(cp_process2);
					process2.addTuple (argumentos);
					process2.addTuple (argumentos);
					process2.addTuple (argumentos);
					process2.addTuple (argumentos);
					process2.addTuple (argumentos);
					process2.addTuple (argumentos);
					process2.addTuple (argumentos);
					process2.addTuple (argumentos);


					process2.addDownStreamOperator (cp_process2);

					process3.connect (cp_process2);
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
