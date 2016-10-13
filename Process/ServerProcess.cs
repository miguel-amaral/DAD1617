using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;


namespace DADStormProcess {
	public class ServerProcess {

		private static int port;
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
		public void executeProcess(){
			System.Console.WriteLine("ProcessServer will be on port: " + port);
			TcpChannel channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(channel, false);
			ProcessRemoteServerObject myServerObj = new ProcessRemoteServerObject();
			RemotingServices.Marshal(myServerObj, "ProcessRemoteServerObject",typeof(ProcessRemoteServerObject));

			System.Console.WriteLine("ProcessServer is ONLINE: port is: " + port);
			System.Console.WriteLine("<enter> to exit...");
			System.Console.ReadLine();
		}

		/**
		  * Debug method Servers are launched by Daemon exclusively
		  */
		public static void Main(string[] args) {
			port = 44556;
			TcpChannel channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(channel, false);
			ProcessRemoteServerObject myServerObj = new ProcessRemoteServerObject();
			RemotingServices.Marshal(myServerObj, "ProcessRemoteServerObject",typeof(ProcessRemoteServerObject));

			System.Console.WriteLine("ProcessServer is ONLINE: port is: " + port);
			System.Console.WriteLine("<enter> to exit...");
			System.Console.ReadLine();
		}
	}
}
