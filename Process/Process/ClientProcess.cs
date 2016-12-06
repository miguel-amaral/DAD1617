using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Json;

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

		public void addDownStreamOperator(List<ConnectionPack> cp, string opID) {
			remoteProcess.addDownStreamOperator(cp,opID);
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
            try {
                remoteProcess.crash();
            } catch (SocketException) {
                //Process just crashed dont owrry about no exception
            }
        }

        public void interval(int milli) {
            try {
                remoteProcess.interval(milli);
            } catch (SocketException e) {
                System.Console.WriteLine(e.Message);
            }
        }


        public string addTuple(IList<string> tuple){
			return remoteProcess.addTuple(tuple);
		}

		public void addFile(string file) {
			remoteProcess.addFile (file);
		}

		public void assignPuppetConPack (ConnectionPack puppetCp) {
			remoteProcess.assignPuppetConPack (puppetCp);
		}

		public void assignReplicaList (List<ConnectionPack> replicaList) {
			remoteProcess.assignReplicaList (replicaList);
		}

		public string status() {
			try {
				return remoteProcess.status ();
			} catch (Exception ) {
				return "Machine Failed";
			}
		}

        public MemoryStream reportBack() {
            return remoteProcess.reportBack();
        }

        public void reset() {
            remoteProcess.reset();
        }

        public void receiveReplicaBackup(string oldID, IList<IList<string>> result) {
            remoteProcess.receiveReplicaBackup(oldID, result);
        }

        public void loseResponsability(string id) {
            remoteProcess.loseResponsability(id);
        }
        public void loseReplicaResponsability(string id) {
            remoteProcess.loseReplicaResponsability(id);
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
