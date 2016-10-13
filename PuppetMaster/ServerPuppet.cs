using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace PuppetMaster {
	public class ServerPuppet {

		private static int port = 10000;

		public static void Main(string[] args) {
			TcpChannel channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(channel, false);
			PuppetMasterRemoteServerObject myServerObj = new PuppetMasterRemoteServerObject();
			RemotingServices.Marshal(myServerObj, "PuppetMasterRemoteServerObject",typeof(PuppetMasterRemoteServerObject));

			System.Console.WriteLine("PuppetMaster Server Online : port: " + port);
			System.Console.WriteLine("<enter> to exit...");
			System.Console.ReadLine();
		}
	}
}
