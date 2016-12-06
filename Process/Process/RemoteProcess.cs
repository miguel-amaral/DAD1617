using System;
using System.Runtime.Remoting;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

namespace DADStormProcess {
    public class ProcessRemoteServerObject : MarshalByRefObject {

        public string addTuple(IList<string> tuple) {
            return ServerProcess.Instance.addTuple(tuple);
        }

        public string ping() {
            Console.WriteLine("Ping in ProcessRemote");
            return "Pong";
        }

        public void addDownStreamOperator(List<ConnectionPack> cp, string opID) {
            ServerProcess.Instance.addDownStreamOperator(cp, opID);
        }

        public void crash() {
            ServerProcess.Instance.crash();
        }
        public void interval(int milli) {
            ServerProcess.Instance.Milliseconds = milli;
        }

        public void start() { this.defreeze(); }
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
            ServerProcess.Instance.OperatorReplicas = replicaList;
        }

        public string status() {
            return ServerProcess.Instance.status();
        }

        public void receiveReplicaBackup(string oldID, IList<IList<string>> result) {
            ServerProcess.Instance.receiveReplicaBackup(oldID, result);
        }

        public void loseResponsability(string ID) {
            ServerProcess.Instance.loseResponsability(ID);
        }

        public void loseReplicaResponsability(string ID) {
            ServerProcess.Instance.loseReplicaResponsability(ID);
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
