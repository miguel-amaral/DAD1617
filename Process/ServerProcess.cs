using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Reflection;


namespace DADStormProcess {
	public class ServerProcess {

		private static int port;
		private static string   dllName;
		private static string   className;
		private static string   methodName;
		private static string[] dllArgs;

		public ServerProcess(string strPort){
			try {
				int parsedPort = Int32.Parse(strPort);
				if(parsedPort < 10002 || parsedPort > 65535) {
					throw new FormatException("Port out of possible");
				}
				port = parsedPort;
			} catch (FormatException e) {
				Console.WriteLine(e.Message);
			}
		}

		/**
		  * !!TODO!!DANGER!!TODO!!
		  * Please read the information below carefully
		  * if channel is created in some fancy function that ends garbage collector will clean
		  * our server object and system will fail categoricly
		  * !!TODO!!DANGER!!TODO!!
		  */
		public static void executeProcess(){
			Assembly assembly = Assembly.LoadFile(@dllName);
			Type     type     = assembly.GetType(className);
			var      obj      = Activator.CreateInstance(type);

			type.InvokeMember(methodName,
				BindingFlags.Default | BindingFlags.InvokeMethod,
				null, obj, dllArgs);
			System.Console.WriteLine("Another Process bites the dust\r\n");
		}

		/**
		  * Debug method Servers are launched by Daemon exclusively
		  */
		public static void Main(string[] args) {
			foreach (string str in args){
				System.Console.WriteLine("ServerProcess Argument: " + str);
			}
			int argsSize = args.Length;
			if (argsSize > 3) {
				string strPort = args[0];
				try {
					int parsedPort = Int32.Parse(strPort);
					if(parsedPort < 10002 || parsedPort > 65535) {
						throw new FormatException("Port out of possible range");
					}
					port = parsedPort;

					dllName    = args[1];
					className  = args[2];
					methodName = args[3];
					if (argsSize > 4) {
						dllArgs = new string[argsSize - 4 ];
						Array.Copy(args, 3, dllArgs, 0, argsSize - 4 );
					} else {
						dllArgs = null;
					}
					TcpChannel channel = new TcpChannel(port);
					ChannelServices.RegisterChannel(channel, false);
					ProcessRemoteServerObject myServerObj = new ProcessRemoteServerObject();
					RemotingServices.Marshal(myServerObj, "ProcessRemoteServerObject",typeof(ProcessRemoteServerObject));
					System.Console.WriteLine("ProcessServer is ONLINE: port is: " + port);

					executeProcess();

					System.Console.WriteLine("<enter> to exit...");
					System.Console.ReadLine();
				} catch (FormatException e) {
					Console.WriteLine(e.Message);
				}
			} else {
				System.Console.WriteLine("ERROR: No port specifiend" );
			}
		}
	}
}
