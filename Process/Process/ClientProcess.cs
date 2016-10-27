using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections.Generic;

namespace DADStormProcess {
	public class ClientProcess {

		private ProcessRemoteServerObject remoteProcess = null;

		public ClientProcess(ConnectionPack cp) {
			remoteProcess = (ProcessRemoteServerObject)Activator.GetObject(
				typeof(ProcessRemoteServerObject),
				"tcp://" + cp.Ip + ":" + cp.Port + "/op");
			//TODO Exceprion
			//if (remoteProcess == null) throw new SocketException();
		}

		public void addDownStreamOperator(List<ConnectionPack> cp){
			remoteProcess.addDownStreamOperator(cp);
		}

		public string ping() {
			if (remoteProcess != null) {
				string res;
//				try {
					res = remoteProcess.ping();
//				} catch (SocketException e) {
//					return "Error: " + e.Message;
//				}
				return res;
			} else {
				return "You did not connect to Process yet";
			}
		}

		public void start() {
			remoteProcess.start();
		}

		public void unfreeze() {
			remoteProcess.defreeze();
		}
		
		public void freeze() {
			remoteProcess.freeze();
		}
		
		public void crash() {
			remoteProcess.crash();
		}

		public void interval(int milli) {
			remoteProcess.interval(milli);
		}

		public void addTuple(string[] tuple){
			remoteProcess.addTuple(tuple);
		}

		public void addFile(string file) {
			remoteProcess.addFile (file);
		}
		/**
		  * Debug method
		  */
/*
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
*/
	}
}