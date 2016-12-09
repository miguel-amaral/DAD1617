using System;
using System.Runtime.Remoting;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Net.Sockets;
using System.Threading;

namespace DADStormProcess {
    public class ProcessRemoteServerObject : MarshalByRefObject {

        private void checkFrozen() {
            if (ServerProcess.Instance.Frozen || ServerProcess.Instance.Suiciding) {
                throw new SocketException();
            }
        }
        private void checkSync() {
            while (ServerProcess.Instance.Sync) {
                Thread.Sleep(50);
            }
        }
        
        public string addTuple(IList<string> tuple) {
            checkFrozen();
            checkSync();
            return ServerProcess.Instance.addTuple(tuple);
        }

        public string ping() {
            return ServerProcess.Instance.ping();
        }

        public void addDownStreamOperator(List<ConnectionPack> cp, string opID) {
            ServerProcess.Instance.addDownStreamOperator(cp, opID);
        }
        public void addUpperStreamOperator(List<ConnectionPack> cp, string opID) {
            ServerProcess.Instance.addUpperStreamOperator(cp, opID);
        }
        public void IamAliveOnceAgain(ConnectionPack reborningGuy, string opID) {
            ServerProcess.Instance.IamAliveOnceAgain(reborningGuy, opID);
        }
        public void crash() {
            ServerProcess.Instance.crash();
        }
        public void interval(int milli) {
            ServerProcess.Instance.Milliseconds = milli;
        }

        public void start() { ServerProcess.Instance.start(); }
        public void freeze() { ServerProcess.Instance.freeze(); }
        public void defreeze() { ServerProcess.Instance.defreeze(); }
        public void addFile(string fileLocation) { ServerProcess.Instance.addFile(fileLocation); }

        public int getIndexFromPrimmary(string file) {
            return ServerProcess.Instance.getIndexFromPrimmary(file);
        }

        public void assignPuppetConPack(ConnectionPack puppetCp) {
            ServerProcess.Instance.PuppetMasterConPack = puppetCp;
        }

        public void assignReplicaList(List<ConnectionPack> replicaList) {
            ServerProcess.Instance.addReplicas(replicaList);
//            ServerProcess.Instance.OperatorReplicas = replicaList;
        }

        public string status() {
            return ServerProcess.Instance.status();
        }

        public void receiveReplicaBackup(string oldID, ConnectionPack author, IList<IList<string>> result) {
            if(ServerProcess.Instance.Suiciding) {
                ServerProcess.Instance.addToQueueOfBackup(oldID, author, result);
                return;
            }
            ServerProcess.Instance.receiveReplicaBackup(oldID, author, result);
        }

        public bool isAlive(ConnectionPack brother) {
            return ServerProcess.Instance.isAlive(brother);
        }

        public void loseResponsability(string ID) {
            if (ServerProcess.Instance.Suiciding) {
                ServerProcess.Instance.addToQueueLoseResponsability(ID);
                return;
            }
            if (ServerProcess.Instance.Frozen ) {
                //IGNORE
                return;
            }
            ServerProcess.Instance.loseResponsability(ID);
        }

        public void reborn(ConnectionPack deadGuy) {
            if (ServerProcess.Instance.Frozen) {
                throw new SocketException();
            }
            ServerProcess.Instance.reborn(deadGuy);
        }

        public SnapShot getSnapShot() {
            checkFrozen();
            checkSync();
            return ServerProcess.Instance.getSnapShot();
        }

        public void loseReplicaResponsability(string ID) {
            if (ServerProcess.Instance.Frozen || ServerProcess.Instance.Suiciding) {
                //IGNORE
                return;
            }
            ServerProcess.Instance.loseReplicaResponsability(ID);
        }

        public void warnBrothersDead(List<ConnectionPack> deadBrothers) {
            ServerProcess.Instance.warnBrothersDead(deadBrothers);
        }

        public int SyncNumber() {
            checkFrozen();
            return ServerProcess.Instance.SyncNumber;
        }

        public List<string> getResponsabilityList() {
            checkFrozen();
            return ServerProcess.Instance.getResponsabilityList();
        }

        public ConnectionPack responsibleBrother(string tupleID) {
            return ServerProcess.Instance.responsibleBrother(tupleID);
        }

        public IList<IList<string>> requestResultFromID(string tupleID) {
            return ServerProcess.Instance.requestResultFromID(tupleID);
        }

        public string needsDivert(string tupleID, ConnectionPack nextOwner) {
            return ServerProcess.Instance.needsDivert(tupleID, nextOwner);
        }

        public int currentClock() {
            return ServerProcess.Instance.currentClock();
        }

        public int firstTupleInListClock() {
            return ServerProcess.Instance.firstTupleInListClock();
        }

        public bool isItDoneYet(string tupleID) {
            return ServerProcess.Instance.isItDoneYet(tupleID);
        }

        //CSF
        public MemoryStream reportBack() {
            return ServerProcess.Instance.reportBack();
        }

        //CSF
        public void reset() {
            ServerProcess.Instance.reset();
        }

        //Secret for eternal life
        public override object InitializeLifetimeService() {
            return null;
        }
    }
}
