using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;


namespace PuppetMaster {

	public class ClientPuppet {

		private PuppetMasterRemoteServerObject remotePuppet = null;
		private string key = "";

		public void connect(string port = "10000",string puppetIp = "localhost") {
			remotePuppet = (PuppetMasterRemoteServerObject)Activator.GetObject(
				typeof(PuppetMasterRemoteServerObject),
				"tcp://" + puppetIp + ":" + port + "/PuppetMasterRemoteServerObject");
			//TODO Exceprion
			//if (remotePuppet == null) throw new SocketException();
		}

		public string ping() {
			if (remotePuppet != null) {
				string res;
				try {
					res = remotePuppet.ping();
				} catch (SocketException e) {
					return "Error: " + e.Message;
				}
				return res;
			} else {
				return "You did not connect to PuppetMaster yet";
			}
		}
		static void Main(string[] args) {

			TcpChannel channel = new TcpChannel(10001);
			//ChannelServices.RegisterChannel(channel,false);

			//RemotingConfiguration.RegisterWellKnownServiceType(	typeof(MyRemoteObject),"MyRemoteObjectName",WellKnownObjectMode.Singleton);
			ClientPuppet pc = new PuppetMaster.ClientPuppet();
			pc.connect();
			System.Console.WriteLine(pc.ping());
			System.Console.WriteLine("<enter> para sair...");
			System.Console.ReadLine();
		}
	}
}
