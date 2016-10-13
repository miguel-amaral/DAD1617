using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Process {
	public class Server {

//		private static int port = 10000;

		public static void Main(string[] args) {
			int port = 44556;
			TcpChannel channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(channel, false);
			ProcessRemoteServerObject myServerObj = new ProcessRemoteServerObject();
			RemotingServices.Marshal(myServerObj, "ProcessRemoteServerObject",typeof(ProcessRemoteServerObject));
			/*RemotingConfiguration.RegisterWellKnownServiceType(
			typeof(ProcessRemoteServerObject),
			"ProcessRemoteServerObject",
			WellKnownObjectMode.Singleton);
			*/
			//TODO ADDRESS RUNNING INFO?
			System.Console.WriteLine("Server port is: " + port);
			System.Console.WriteLine("<enter> to exit...");
			System.Console.ReadLine();
		}
	}
}
