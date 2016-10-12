using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

/*



*/
namespace PuppetMaster {
	public class Server {

		private static int port = 10000;

		public static void Main(string[] args) {
			TcpChannel channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(channel, false);
			PuppetMasterRemoteServerObject myServerObj = new PuppetMasterRemoteServerObject();
			RemotingServices.Marshal(myServerObj, "PuppetMasterRemoteServerObject",typeof(PuppetMasterRemoteServerObject));
			/*RemotingConfiguration.RegisterWellKnownServiceType(
			typeof(PuppetMasterRemoteServerObject),
			"PuppetMasterRemoteServerObject",
			WellKnownObjectMode.Singleton);
			*/
			//TODO ADDRESS RUNNING INFO?
			System.Console.WriteLine("Server port is: " + port);
			System.Console.WriteLine("<enter> to exit...");
			System.Console.ReadLine();
		}
	}
}
