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
        public void addUpperStreamOperator(List<ConnectionPack> cp, string opID) {
            remoteProcess.addUpperStreamOperator(cp, opID);
        }

        public void IamAliveOnceAgain(ConnectionPack reborningGuy, string opID) {
            remoteProcess.IamAliveOnceAgain(reborningGuy, opID);
        }

        public string ping() {
            string res;
            res = remoteProcess.ping();
            return res;
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


        public string addTuple(IList<string> tuple) {
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

        public void receiveReplicaBackup(string oldID,ConnectionPack author, IList<IList<string>> result) {
            try {
                remoteProcess.receiveReplicaBackup(oldID, author, result);
            } catch(SocketException) {
                //Hoppefully someone will notice
            }
        }
        public bool isAlive(ConnectionPack brother) {
            try {
                return remoteProcess.isAlive(brother);
            } catch (SocketException) {
                //If he is dead he does not count
                return true;
            }
        }

        public int SyncNumber() {
            return remoteProcess.SyncNumber();
        }

        public void reborn(ConnectionPack deadGuy) {
            remoteProcess.reborn(deadGuy);
        }

        public SnapShot getSnapShot() {
            return remoteProcess.getSnapShot();
        }

        public void loseResponsability(string id) {
            try { 
                remoteProcess.loseResponsability(id);
            } catch (SocketException) {
                //Hoppefully someone will notice
            }
        }
        public void loseReplicaResponsability(string id) {
            try {
                remoteProcess.loseReplicaResponsability(id);
            } catch (SocketException) {
                //Hoppefully someone will notice
            }
        }

        public int currentClock() {
            return remoteProcess.currentClock();
        }
        public int firstTupleInListClock() {
            return remoteProcess.firstTupleInListClock();
        }
        public bool isItDoneYet(string tupleID) {
            return remoteProcess.isItDoneYet(tupleID);
        }

        public List<string> getResponsabilityList() {
            return remoteProcess.getResponsabilityList();
        }

        public ConnectionPack responsibleBrother(string tupleID) {
            return remoteProcess.responsibleBrother(tupleID);
        }

        public IList<IList<string>> requestResultFromID(string tupleID) {
            return remoteProcess.requestResultFromID(tupleID);
        }

        public string needsDivert(string tupleID, ConnectionPack nextOwner) {
            return  remoteProcess.needsDivert(tupleID, nextOwner);
        }

        public void warnBrothersDead(List<ConnectionPack> deadBrothers) {
            try {
                remoteProcess.warnBrothersDead(deadBrothers);
            } catch (SocketException) {
                //We are warning about dead people, if another fails they will notice that during sync
            }
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
