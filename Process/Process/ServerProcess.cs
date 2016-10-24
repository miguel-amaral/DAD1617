using System;
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
		private bool     frozen = true;
		private bool     fullLog= false;
		private string   file = "";
		private string   dllName;
		private string   className;
		private string   methodName;
		private string[] processStaticArgs;
		private Queue<string[]> dllArgs = new Queue<string[]>();
		private ProcessRemoteServerObject myServerObj;
		private List<ConnectionPack> downStreamNodes = new List<ConnectionPack>();

		public int Port {
			get	{ return port; }
			set	{ port = value;}
		}
		public bool FullLog {
			get	{ return  fullLog; }
			set	{ fullLog = value; }
		}
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


		public static ServerProcess Instance {
			get {
				if (instance == null) {
					System.Console.WriteLine("New ServerProcess instance created");
					instance = new ServerProcess();
				}
				return instance;
			}
		}

		public void addDownStreamOperator(ConnectionPack cp){
			downStreamNodes.Add ( cp );
		}

		private ServerProcess(){}
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

		public void freeze() {
			this.frozen = true;
		}
		public void defreeze() {
			this.frozen = false;
			lock(dllArgs) {
				Monitor.Pulse(dllArgs);
			}
		}

		/**
		  * method that returns the next tuple to be processed
		  */
		private string[] nextTuple() {
			lock (dllArgs) {
				while (dllArgs.Count == 0 || frozen ) {
					if(frozen) {
						System.Console.WriteLine("frozen: " + dllArgs.Count + " tuples are waiting");
					}
					else {
						System.Console.WriteLine("no tuples");
					}
					Monitor.Wait(dllArgs);
				}
				string[] nextArg = dllArgs.Dequeue();
				return nextArg;
			}
		}

		private void loadFile(string file) {
			
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

			System.Console.WriteLine ("Setup\r\ndllName   : " + dllName + "\r\nclassName : " + className + "\r\nmethodName: " + methodName);
			while (true) {
				Object[] methodArgs = { this.nextTuple () };

				//returnValue object is assumed to be a string[] in DADStorm context
				object returnValue = type.InvokeMember (methodName,	BindingFlags.Default | BindingFlags.InvokeMethod, null, obj, methodArgs);

				if(returnValue.GetType () == typeof(string[])) {
					emitTuple ((string[])returnValue);
				} else {
					throw new Exception ("dll method did not return a string[]");
				}
			}
		}

		/**
		  * After processing tuple this method emits it to downStream and puppet master
		  */  
		private void emitTuple(string[] tuple){
			if(fullLog == true) {
				logToPuppetMaster (tuple);
			}
			sendToNextOperator(tuple);
			System.Console.WriteLine("Another tuple bites the dust, result: " + tuple + "\r\n");
		}

		/**
		  * In the full logging mode, all tuple emissions need to be reported to Puppetmaster
		  */ 
		private void logToPuppetMaster (string[] tuple) {
			System.Console.WriteLine("Puppet Master informed");
		}

		/**
		  * Method that sends a tuple to downstream operator
		  */
		private void sendToNextOperator(string[] tuple) {
			ConnectionPack nextOperatorCp = findTupleReceiver();
			if (nextOperatorCp != null) {
				DADStormProcess.ClientProcess nextProcess = new DADStormProcess.ClientProcess();
				nextProcess.connect(nextOperatorCp);
				nextProcess.addTuple(tuple);
				System.Console.WriteLine("Port: " + Port + " Next Operator has received damn tuple");

			} else {
				System.Console.WriteLine("Port: " + Port + " No one to send tuple to :(");
				//Case where there is no one to receive..
				//Maybe its last operator
			}
		}

		private ConnectionPack findTupleReceiver(){
			if(downStreamNodes.Count > 0){
				// primary routing 
				return downStreamNodes[0];
			} else {
			//	return new string[]{ "" };
				return null;
			}
		}

		/**
		  * Method that will be in loop (passively) and will be processing input
		  */
		public void createAndProcess() {

			TcpChannel channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(channel, false);
			myServerObj = new ProcessRemoteServerObject();
			RemotingServices.Marshal(myServerObj, "op",typeof(ProcessRemoteServerObject));

			Console.ForegroundColor = ConsoleColor.Green;
			System.Console.WriteLine("ProcessServer is ONLINE: port is: " + port);
			Console.ResetColor();

			executeProcess();
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
						dllArgsInputMain = new string[argsSize - 4 ];
						Array.Copy(args, numberOfParameters-1, dllArgsInputMain, 0, numberOfParameters - 4 );
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
					System.Console.WriteLine("ERROR: No port specifiend" );
				}
			} catch (FormatException e) {
				Console.WriteLine(e.Message);
			}
		}
	}
}
