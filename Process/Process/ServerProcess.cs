using System;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;

namespace DADStormProcess {
	public class ServerProcess {

		private static ServerProcess instance = null;
		private int      port;
		private int      milliseconds =0;
		private bool     frozen  = true;
		private bool     fullLog = false;
		private bool     primmary= true;
		private string   dllName;
		private string   className;
		private string   methodName;
		private string[] processStaticArgs;
		private string   puppetMasterUrl= "tcp://localhost:10000/PuppetMasterRemoteServerObject";
		private DADStormRemoteTupleReceiver puppetRemote;
		private Queue<string[]> dllArgs = new Queue<string[]>();
		private ProcessRemoteServerObject myServerObj;
		private List<List<ConnectionPack>> downStreamNodes = new List<List<ConnectionPack>>();
		private List<string> filesLocation = new List<string>();
		private List<string> filesToRemove = new List<string>();
		private Dictionary<string, string[]> filesContent = new Dictionary<string, string[]>(); 
		private Dictionary<string, int> filesIndex = new Dictionary<string, int> ();

		public int Port {
			get	{ return port; }
			set	{ port = value;}
		}
		public int Milliseconds {
			get	{ return milliseconds; }
			set	{ milliseconds = value;}
		}
		public bool FullLog {
			get	{ return  fullLog; }
			set	{ fullLog = value; }
		}		
		//public bool Primmary {
		//	get	{ return  primmary; }
		//	set	{ primmary = value; }
		//}
		public string ClassName {
			get	{ return className; }
			set	{ className = value;}
		}
		public string DllName {
			get	{ return dllName; }
			set	{ dllName = value;}
		}
		public string MethodName {
			get	{ return methodName; }
			set	{ methodName = value;}
		}
		public string[] ProcessStaticArgs {
			get	{ return processStaticArgs; }
			set	{ processStaticArgs = value;}
		}

		private ServerProcess(){}

		public static ServerProcess Instance {
			get {
				if (instance == null) {
					System.Console.WriteLine("New ServerProcess instance created");
					instance = new ServerProcess();
				}
				return instance;
			}
		}


//		public ServerProcess(string strPort, string dllName, string className, string methodName, string[] processArgs){
//
//			try {
//				int parsedPort = Int32.Parse(strPort);
//				if(parsedPort < 10002 || parsedPort > 65535) {
//					throw new FormatException("Port out of possible");
//				}
//				port = parsedPort;
//				this.dllName    = dllName;
//				this.className  = className;
//				this.methodName = methodName;
//				this.processArgs= processArgs;
//			} catch (FormatException e) {
//				Console.WriteLine(e.Message);
//			}
//		}
		

		/**
		  * method that returns the next tuple to be processed
		  */
		private string[] nextTuple ()
		{
			lock (dllArgs) {
				while (dllArgs.Count == 0 || frozen) {
					if (frozen) {
						ProcessDebug ("frozen: " + dllArgs.Count + " tuples are waiting");
					} else {
						//Read 1 tuple from each file?
						lock (filesLocation) {
							if (filesLocation.Count > 0) {
								foreach (string fileLocation in filesLocation) {
									ProcessDebug ("reading one tuple from " + fileLocation);
									readTuple (fileLocation);
								}
								lock (filesToRemove) {
									if (filesToRemove.Count > 0) {
										foreach (string file in filesToRemove) {
											filesLocation.Remove (file);
										}
									}
									filesToRemove = new List<string> ();
								}
								continue; // try again
							}
						}
					}
					Monitor.Wait (dllArgs);
				}
				string[] nextArg = dllArgs.Dequeue();
				return nextArg;
			}
		}

		/**
		  * method that reads from the file all the tuples in it
		  * might need to become process and avoiding reading same tuple two times.. :(
		  */
		private void readTuple (string fileLocation)
		{
			string[] content;
			if (!filesContent.TryGetValue (fileLocation, out content)) {
				//Not found -> File not yet read
				content = File.ReadAllLines (fileLocation);
				filesContent.Add (fileLocation, content);
			}
			//getIndex
			int index = this.nextIndex (fileLocation);
			if (index >= 0) {
				if (index < content.Length) {
					string line = content [index];
					String[] tuple = line.Split (new[] { ',', ' ', '"' }, StringSplitOptions.RemoveEmptyEntries);

					if (!tuple [0].StartsWith ("%")) {
						ProcessDebug ("Read Tuple: " + line);
						//Only adds non commentaries
						this.addTuple (tuple);
					}
				} else {
					System.Console.WriteLine ("file: " + fileLocation + " has been read completly");
					//File has been read totally, removing from known files
					lock (filesToRemove) {
						filesToRemove.Add (fileLocation);
					}
				}

			} else {
				System.Console.WriteLine ("Index returned negative, someone acessed it without primmary permission");
			}
		}

		private ProcessRemoteServerObject getPrimmary() {
			//TODO XXX
			return myServerObj;
		}

		private int nextIndex(string file){
			return this.getPrimmary().getIndexFromPrimmary (file);
		}

		/**
		  * !!TODO!!DANGER!!TODO!!
		  * Please read the information below carefully
		  * if channel is created in some fancy function that ends, garbage collector will clean
		  * our server object and system will fail categoricly!!
		  * !!TODO!!DANGER!!TODO!!
		  */
		private void executeProcess () {
			Assembly assembly = Assembly.LoadFile (@dllName);
			Type type = assembly.GetType (className);
			var obj = Activator.CreateInstance (type);
			string staticArgs = "<";
			foreach(string str in processStaticArgs ){
				staticArgs += " " + str;
			}
			staticArgs = " >";
			System.Console.WriteLine ("Setup\r\ndllName   : " + dllName + "\r\nclassName : " + className + "\r\nmethodName: " + methodName);
			System.Console.WriteLine("Static Args: " + staticArgs);
			while (true) {
				string[] nextTuple = this.nextTuple ();
				string[] finalTuple = new string[processStaticArgs.Length + nextTuple.Length];
				Array.Copy(processStaticArgs, finalTuple, processStaticArgs.Length);
				Array.Copy(nextTuple, 0, finalTuple, processStaticArgs.Length, nextTuple.Length);

				string tuplePlusArgs = "";
				foreach(string str in finalTuple ){
					tuplePlusArgs += " " + str;
				}

				ProcessDebug ("Next Tuple:" + tuplePlusArgs);
				Object[] methodArgs = { finalTuple };
				//returnValue object is assumed to be a string[] in DADStorm context
				object returnValue = type.InvokeMember (methodName,	BindingFlags.Default | BindingFlags.InvokeMethod, null, obj, methodArgs);

				if(returnValue.GetType () == typeof(string[])) {
					emitTuple ((string[])returnValue);
				} else {
					throw new Exception ("dll method did not return a string[]");
				}

				//By default milliseconds is zero, but puppet master may want to slow things down..
				if(milliseconds > 0) {
					Thread.Sleep (milliseconds);
				}
			}
		}

		/**
		  * After processing tuple this method emits it to downStream and puppet master
		  */  
		private void emitTuple(string[] tuple){
			ProcessDebug("Another tuple generated: <" + String.Join(", ", tuple) + ">");
			if(fullLog == true) {
				logToPuppetMaster (tuple);
			}
			sendToNextOperators(tuple);
		}

		/**
		  * In the full logging mode, all tuple emissions need to be reported to Puppetmaster
		  */ 
		private void logToPuppetMaster (string[] tuple) {
			if(puppetRemote == null) {
				puppetRemote = (DADStormRemoteTupleReceiver)Activator.GetObject(
					typeof(DADStormRemoteTupleReceiver), puppetMasterUrl);
				//"tcp://" + puppet.Ip + ":" + puppet.Port + "/op");
			}
			puppetRemote.addTuple ("TODO myUrl TODO", tuple);
			ProcessDebug("Puppet Master informed");
		}

		/**
		  * Method that sends a tuple to downstream operator
		  */
		private void sendToNextOperators (string[] tuple)
		{
			if (downStreamNodes.Count > 0) {
				foreach(List<ConnectionPack> receivingOperator in downStreamNodes){
					ConnectionPack nextOperatorCp = findTupleReceiver (receivingOperator);
					DADStormProcess.ClientProcess nextProcess = new DADStormProcess.ClientProcess (nextOperatorCp);
					nextProcess.addTuple (tuple);
					ProcessDebug ("Next received tuple");
				}
			} else {
				ProcessDebug ("No one to send tuple to :(");
				//Case where there is no one to receive..
				//probably its last operator
			}
		}

		private ConnectionPack findTupleReceiver(List<ConnectionPack> possibleReplicas){
			//Check if more than one operator to send to
			// primary routing 
			return possibleReplicas[0];
		}

		/* -------------------------------------------------------------------- */
		/* -------------------------------------------------------------------- */
		/* --------------------------- Public Methods ------------------------- */
		/* -------------------------------------------------------------------- */
		/* -------------------------------------------------------------------- */

		public void addDownStreamOperator(List<ConnectionPack> cp){
			ProcessDebug ("Down Stream Op added: " + cp);
			downStreamNodes.Add ( cp );
		}

		public void crash() {
			Environment.Exit (1);
		}

		public void freeze() {
			this.frozen = true;
		}

		public void defreeze() {
			this.frozen = false;
			lock(dllArgs) {
				Monitor.Pulse(dllArgs);
			}
		}

		//Public that must be called by 
		public int getIndexFromPrimmary (string file) {
			if (primmary) {
				lock (filesIndex) {
					int counter;
					if (!filesIndex.TryGetValue (file, out counter)) {
						counter = 0;
						filesIndex.Add (file, counter);
					} 
					filesIndex [file] = counter + 1;
					return counter;
				}
			}
			return -1;
		}

		//Method that adds a file that this replica will read 
		public void addFile(string file){
			lock(filesLocation){
				filesLocation.Add (file);
			}
			lock(dllArgs){
				Monitor.Pulse (dllArgs);
			}
		}

		/**
		  * method that adds a tuple to be processed
		  */
		public void addTuple(string[] nextArg) {
			lock (dllArgs) {
				dllArgs.Enqueue( nextArg );
				Monitor.Pulse(dllArgs);
			}
		}

		/// <summary>
		/// Method that will be in loop (passively) and will be processing input
		/// </summary>
		public void createAndProcess() {

			TcpChannel channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(channel, false);
			myServerObj = new ProcessRemoteServerObject();
			RemotingServices.Marshal(myServerObj, "op",typeof(ProcessRemoteServerObject));

			Console.ForegroundColor = ConsoleColor.Green;
			System.Console.WriteLine("ProcessServer is ONLINE: port is: " + this.port);
			Console.ResetColor();

			executeProcess();
		}

		private void ProcessDebug(string msg) {
			if(DEBUG.PROCESS){
				System.Console.WriteLine("[ Process : " +this.Port + " ] " + msg);
			}
		}

		public static void Main(string[] args) {

			int argsSize = args.Length;
			try {
				//Configuring Process
				if (argsSize > 4) {
					string strPort = args[0];
					string dllNameInputMain    = args[1];
					string classNameInputMain  = args[2];
					string methodNameInputMain = args[3];
					//Bool that indicates whether full logging or not
					bool   fullLogging         = Convert.ToBoolean(args[4]);
					string[] dllArgsInputMain = null;
					int numberOfParameters = 5;
					if (argsSize > numberOfParameters) {
						dllArgsInputMain = new string[argsSize - numberOfParameters];
						Array.Copy(args, numberOfParameters, dllArgsInputMain, 0, argsSize - numberOfParameters );
					}
					string argumentos = "";
					foreach(string str in dllArgsInputMain){
						argumentos += str + " ";
					}

					ServerProcess sp = ServerProcess.Instance;
					int parsedPort = Int32.Parse(strPort);
					if(parsedPort < 10002 || parsedPort > 65535) {
						throw new FormatException("Port out of possible range");
					}
					sp.Port       		 = parsedPort;
					sp.DllName    		 = dllNameInputMain;
					sp.ClassName  		 = classNameInputMain;
					sp.MethodName 		 = methodNameInputMain;
					sp.ProcessStaticArgs = dllArgsInputMain;
					sp.FullLog			 = fullLogging;
					sp.createAndProcess();

					Console.ForegroundColor = ConsoleColor.Red;
					System.Console.WriteLine("ProcessServer is going OFFLINE" );
					Console.ResetColor();
				} else {
					System.Console.WriteLine("ERROR: No port specified" );
				}
			} catch (FormatException e) {
				Console.WriteLine(e.Message);
			}
		}
	}
}
