using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Reflection;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace DADStormProcess {
	public class ServerProcess {

		private static ServerProcess instance = null;
		private int port;
		private bool     frozen = false;
		private string   dllName;
		private string   className;
		private string   methodName;
		private string[] processStaticArgs;
		private Queue<string[]> dllArgs = new Queue<string[]>();
		private ProcessRemoteServerObject myServerObj;

		public int Port {
			get	{ return port; }
			set	{ port = value;}
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
					System.Console.WriteLine("New instance created");
					instance = new ServerProcess();
				}
				return instance;
			}
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
			System.Console.WriteLine("defreeze beg");
			this.frozen = false;
			lock(dllArgs) {
				Monitor.Pulse(dllArgs);
			}
			System.Console.WriteLine("defreeze end");
		}

		/**
		  * method that returns the next tuple to be processed
		  */
		private string[] nextTuple() {
			lock (dllArgs) {
				while (dllArgs.Count == 0 || frozen ) {
					System.Console.WriteLine("waiting: " + dllArgs.Count);
					Monitor.Wait(dllArgs);
				}
				System.Console.WriteLine("LOCK FREE");
				string[] nextArg = dllArgs.Dequeue();
				return nextArg;
			}
		}

		/**
		  * method that adds a tuple to be processed
		  */
		public void addTuple(string[] nextArg) {
			lock (dllArgs) {
				dllArgs.Enqueue( nextArg );
				System.Console.WriteLine("Pulsing: " + dllArgs.Count);
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
		private void executeProcess(){
			Assembly assembly = Assembly.LoadFile(@dllName);
			Type     type     = assembly.GetType(className);
			var      obj      = Activator.CreateInstance(type);


			System.Console.WriteLine("going for the tuple\r\n");
			string[] tuple = this.nextTuple();
			System.Console.WriteLine("got it: "+ tuple + "\r\n");

			System.Console.WriteLine(dllName + " " + className + " " + methodName + " " + tuple);

			type.InvokeMember(methodName,
				BindingFlags.Default | BindingFlags.InvokeMethod,
				null, obj, tuple);
			System.Console.WriteLine("Another tuple bites the dust\r\n");
		}

		/**
		  * Method that will be in loop (passively) and will be processing input
		  */
		public void createAndProcess() {

			TcpChannel channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(channel, false);
			myServerObj = new ProcessRemoteServerObject();
			RemotingServices.Marshal(myServerObj, "ProcessRemoteServerObject",typeof(ProcessRemoteServerObject));

			Console.ForegroundColor = ConsoleColor.Green;
			System.Console.WriteLine("ProcessServer is ONLINE: port is: " + port);
			Console.ResetColor();

			while(true){
				executeProcess();
			}
		}

		public static void Main(string[] args) {

			int argsSize = args.Length;
			try {
				if (argsSize > 3) {
					string strPort = args[0];
					string dllNameInputMain    = args[1];
					string classNameInputMain  = args[2];
					string methodNameInputMain = args[3];
					string[] dllArgsInputMain = null;
					if (argsSize > 4) {
						dllArgsInputMain = new string[argsSize - 4 ];
						Array.Copy(args, 3, dllArgsInputMain, 0, argsSize - 4 );
					}
					ServerProcess sp = ServerProcess.Instance;
					int parsedPort = Int32.Parse(strPort);
					if(parsedPort < 10002 || parsedPort > 65535) {
						throw new FormatException("Port out of possible range");
					}
					sp.Port       		= parsedPort;
					sp.DllName    		= dllNameInputMain;
					sp.ClassName  		= classNameInputMain;
					sp.MethodName 		= methodNameInputMain;
					sp.ProcessStaticArgs= dllArgsInputMain;

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
