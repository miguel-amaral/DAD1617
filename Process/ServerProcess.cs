using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Reflection;
using System.Threading;

namespace DADStormProcess {
	public class ServerProcess {

		private int port;
		private string   dllName;
		private string   className;
		private string   methodName;
		private string[] dllArgs;

		//public int Port {
		//	get	{ return port; }
		//	set	{ port = value;}
		//}

		public ServerProcess(string strPort, string dllName, string className, string methodName, string[] dllArgs){

			try {
				int parsedPort = Int32.Parse(strPort);
				if(parsedPort < 10002 || parsedPort > 65535) {
					throw new FormatException("Port out of possible");
				}
				port = parsedPort;
				this.dllName    = dllName;
				this.className  = className;
				this.methodName = methodName;
				this.dllArgs    = dllArgs;
			} catch (FormatException e) {
				Console.WriteLine(e.Message);
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

			System.Console.WriteLine(dllName + " " + className + " " + methodName + " " + dllArgs);

			type.InvokeMember(methodName,
				BindingFlags.Default | BindingFlags.InvokeMethod,
				null, obj, dllArgs);
			System.Console.WriteLine("Another Process bites the dust\r\n");
		}

		/**
		  * Method that will be in loop (passively) and will be processing input
		  */
		public void createAndProcess() {
			TcpChannel channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(channel, false);
			ProcessRemoteServerObject myServerObj = new ProcessRemoteServerObject();
			RemotingServices.Marshal(myServerObj, "ProcessRemoteServerObject",typeof(ProcessRemoteServerObject));

			Console.ForegroundColor = ConsoleColor.Green;
			System.Console.WriteLine("ProcessServer is ONLINE: port is: " + port);
			Console.ResetColor();

			executeProcess();
			while (true){
				/*XXX!XXX!XXX!*/ Thread.Sleep(100); /*XXX!XXX!XXX!*/ //TODO
			}
		}

		public static void Main(string[] args) {

			int argsSize = args.Length;
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
				ServerProcess sp = new ServerProcess(strPort, dllNameInputMain, classNameInputMain, methodNameInputMain, dllArgsInputMain);
				sp.createAndProcess();

				Console.ForegroundColor = ConsoleColor.Red;
				System.Console.WriteLine("ProcessServer is going OFFLINE" );
				Console.ResetColor();


			} else {
				System.Console.WriteLine("ERROR: No port specifiend" );
			}
		}
	}
}
