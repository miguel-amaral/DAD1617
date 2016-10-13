using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;


namespace PuppetMaster {

	public class PuppetClient {

		private PuppetMasterRemoteServerObject remotePuppet = null;
		private string key = "";

		public void connect(string puppetIp, string port = "10000") {
			remotePuppet = (PuppetMasterRemoteServerObject)Activator.GetObject(
				typeof(PuppetMasterRemoteServerObject),
				"tcp://" + puppetIp + ":" + port + "/PuppetMasterRemoteServerObject");
			if (remotePuppet == null) throw new SocketException();

			try {
				int daemon_Port = 10001;
				key = "tcp://localhost:" + daemon_Port;
				remotePuppet.register(key, daemon_Port);
			} catch(RegistryRemoteException e) {
				System.Console.WriteLine("service already exists.\r\nError: " + e.Message);
				remotePuppet = null;
				key = "";
				return;
			}
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

		public string unregister() {
			try {
				if (remotePuppet != null)
					remotePuppet.unregister(key);
			} catch(SocketException e)	{
				return "Failed to unregister service; error: " + e.Message;
			}
			return "Successfully unregistered";
		}
	}
}
