using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;


namespace DADStormProcess {
	public class ClientProcess {

		private ProcessRemoteServerObject remoteProcess = null;

		public void connect(string port, string processIp = "localhost") {
			remoteProcess = (ProcessRemoteServerObject)Activator.GetObject(
				typeof(ProcessRemoteServerObject),
				"tcp://" + processIp + ":" + port + "/ProcessRemoteServerObject");
			//TODO Exceprion
			//if (remoteProcess == null) throw new SocketException();
		}

		public string ping() {
			if (remoteProcess != null) {
				string res;
				try {
					res = remoteProcess.ping();
				} catch (SocketException e) {
					return "Error: " + e.Message;
				}
				return res;
			} else {
				return "You did not connect to Process yet";
			}
		}

		public void defreeze() {
			remoteProcess.defreeze();
		}
		public void freeze() {
			remoteProcess.freeze();
		}

		public void addTuple(string[] tuple){
			remoteProcess.addTuple(tuple);
		}


		static void Main(string[] args) {

			TcpChannel channel = new TcpChannel(44444);
			//ChannelServices.RegisterChannel(channel,false);

			//RemotingConfiguration.RegisterWellKnownServiceType(	typeof(MyRemoteObject),"MyRemoteObjectName",WellKnownObjectMode.Singleton);
			ClientProcess pc = new ClientProcess();
			pc.connect("44556");
			System.Console.WriteLine(pc.ping());
			System.Console.WriteLine("<enter> para sair...");
			System.Console.ReadLine();
		}
	}
}
