using System;
using System.Net;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.Net.Sockets;

namespace DADStormProcess {

    public class ServerProcess {

        private static ServerProcess instance = null;
        private int milliseconds = 0;
        private decimal lastRead = 100;
        private bool frozen = true;
        private bool fullLog = false;
        private bool primmary = true;
        private bool sync = false;
        private int numberProcessed = 0;
        private IList<string> processStaticArgs;
        private RoutingTechinic routTechnic;
        private GenerateStrategy generateStrategy;
        private List<string> printing = new List<string>(); //Simply for locking System.Console
        private List<string> syncing = new List<string>(); //Simply for locking syncinc
        private bool started = false;
        private string operatorID;
        private ConnectionPack myConPack;
        private ConnectionPack puppetMasterConPack;

        private int semantics = 0;
        private bool suiciding = false;
        public bool Suiciding {
            get { return suiciding; }
        }

        private List<int> suicidingLock = new List<int>();

        List<QueueRequests> requests = new List<QueueRequests>();


        private int PFDetectionInterval = 1000; //ms
        private int ForgotenTDInterval = 5000; //ms

        private DADStormRemoteTupleReceiver puppetRemote;
        private ProcessRemoteServerObject myServerObj;
        private Queue<IList<string>> dllArgs = new Queue<IList<string>>();

        //Base List for order reference
        private Dictionary<string, List<ConnectionPack>> downStreamNodes = new Dictionary<string, List<ConnectionPack>>();
        private Dictionary<string, List<ConnectionPack>> aliveDownStreamNodes = new Dictionary<string, List<ConnectionPack>>();
        private List<ConnectionPack> operatorReplicas; //Base List for order reference ??
        private List<ConnectionPack> aliveOperatorReplicas;

        private List<string> filesLocation = new List<string>();
        private List<string> filesToRemove = new List<string>();

        private Dictionary<string, string[]> filesContent = new Dictionary<string, string[]>();
        private Dictionary<string, int> filesIndex = new Dictionary<string, int>();
        //List of processed IDS
        private List<string> processedIDs = new List<string>();

        //all tuple ids that operator is responsible for and respectively result
        private Dictionary<string, IList<IList<string>>> responsability = new Dictionary<string, IList<IList<string>>>();
        //Structure that for each tuple has list of resulting tuplesID
        private Dictionary<string, List<string>> responsabilityLinks = new Dictionary<string, List<string>>();
        //structures that guards the operator responsible for each tuple  <id, operator>
        private Dictionary<string, string> idTranslation = new Dictionary<string, string>();
        //Structure that guards for each oldID in responsability the brother who is ensuring it is processed (whoever done backup)
        private Dictionary<string, ConnectionPack> brotherIDsResponsible = new Dictionary<string, ConnectionPack>();

        private Dictionary<string, ConnectionPack> needsDivertList = new Dictionary<string, ConnectionPack>();

        //struture that keeps a tupleID and the timer at which it was read
        private Dictionary<string, List<int>> mightBeForgottenTuples = new Dictionary<string, List<int>>();

        private int syncNumber = 0;

        public int SyncNumber {
            get { return syncNumber; }
            set { syncNumber = value; }
        }
        public bool Sync {
            get { return sync; }
        }

        private int clock = 0;

        public ConnectionPack PuppetMasterConPack {
            get { return puppetMasterConPack; }
            set { puppetMasterConPack = value; }
        }
        public ConnectionPack MyConPack {
            get { return myConPack; }
            set { myConPack = value; }
        }
        public List<ConnectionPack> OperatorReplicas {
            get { return operatorReplicas; }
            set { operatorReplicas = value; }
        }
        public int Milliseconds {
            get { return milliseconds; }
            set { milliseconds = value; }
        }
        public bool FullLog {
            get { return fullLog; }
            set { fullLog = value; }
        }
        public bool Frozen {
            get { return frozen; }
        }
        //public bool Primmary {
        //	get	{ return  primmary; }
        //	set	{ primmary = value; }
        //}

        public IList<string> ProcessStaticArgs {
            get { return processStaticArgs; }
            set { processStaticArgs = value; }
        }
        public RoutingTechinic RoutTechnic {
            get { return routTechnic; }
            set { routTechnic = value; }
        }
        private ServerProcess() { }

        public static ServerProcess Instance {
            get {
                if (instance == null) {
                    System.Console.WriteLine("New ServerProcess instance created");
                    instance = new ServerProcess();
                }
                return instance;
            }
        }

        /**
		  * method that returns the next tuple to be processed
		  */
        private IList<string> nextTuple() {
            IList<string> nextArg = null;
            lock (syncing) {
                while (this.sync) {
                    Monitor.Wait(syncing);
                }
            }
            lock (dllArgs) {
                while (dllArgs.Count == 0 || frozen) {
                    if (frozen) {
                        ProcessDebug("frozen: " + dllArgs.Count + " tuples are waiting");
                    } else {
                        //Read 1 tuple from each file
                        lock (filesLocation) {
                            if (filesLocation.Count > 0) {
                                foreach (string fileLocation in filesLocation) {
                                    ProcessDebug("reading one tuple from " + fileLocation);
                                    readTuple(fileLocation);
                                }
                                lock (filesToRemove) {
                                    if (filesToRemove.Count > 0) {
                                        foreach (string file in filesToRemove) {
                                            filesLocation.Remove(file);
                                        }
                                    }
                                    filesToRemove = new List<string>();
                                }
                                continue; // try again
                            }
                        }
                    }
                    Monitor.Wait(dllArgs);
                }
                nextArg = dllArgs.Dequeue();
                return nextArg;
            }
        }

        /**
		  * method that reads from the file all the tuples in it
		  * might need to become process and avoiding reading same tuple two times.. :(
		  */
        private void readTuple(string fileLocation) {
            string[] content;
            if (!filesContent.TryGetValue(fileLocation, out content)) {
                //Not found -> File not yet read
                content = File.ReadAllLines(fileLocation);
                filesContent.Add(fileLocation, content);
            }
            //getIndex
            int index = this.nextIndex(fileLocation);
            if (index >= 0) {
                if (index < content.Length) {
                    string line = content[index];
                    String[] tuple = line.Split(new[] { ',', ' ', '"' }, StringSplitOptions.RemoveEmptyEntries);

                    if (tuple.Length == 0) {
                        //Ignore
                    } else if (!tuple[0].StartsWith("%")) {
                        ProcessDebug("Read Tuple: < " + String.Join(", ", tuple) + " > ");
                        //Only adds non commentaries
                        List<string> toAdd = new List<string>();
                        foreach (string str in tuple) {
                            toAdd.Add(str);
                        }

                        string id = this.operatorID + "-" + fileLocation + "-" + index.ToString();
                        insertMetadataOnRead(id, toAdd);
                        this.addTuple(toAdd);
                    }
                    if (!primmary) {
                        lock (filesIndex) {
                            filesIndex[fileLocation] = index;
                        }
                    }
                    lastRead = decimal.Divide(index, content.Length) * 100;//Operacao de divisao
                } else {
                    lastRead = 100;

                    ProcessDebug("file: " + fileLocation + " has been read completly");
                    //File has been read totally, removing from known files
                    lock (filesToRemove) {
                        filesToRemove.Add(fileLocation);
                    }
                }

            } else {
                ProcessDebug("Index returned negative, someone acessed it without primmary permission");
            }
        }

        private ProcessRemoteServerObject getPrimmary() {
            ConnectionPack primmary = operatorReplicas[0];
            if (primmary.Equals(myConPack)) {
                return myServerObj;
            } else {
                return (ProcessRemoteServerObject)Activator.GetObject(typeof(ProcessRemoteServerObject), "tcp://" + primmary.Ip + ":" + primmary.Port + "/op");
            }

        }

        private int nextIndex(string file) {
            return this.getPrimmary().getIndexFromPrimmary(file);
        }

        /// <summary>
        /// Method that adds the static args to the beggining of a tuple
        /// </summary>
        /// <param name="tuple">Tuple.</param>
        private IList<string> addStaticArgs(IList<string> nextTuple) {
            IList<string> finalTuple = new List<string>();
            if (processStaticArgs != null) {
                finalTuple = new List<string>(processStaticArgs);
                foreach (string str in nextTuple) {
                    finalTuple.Add(str);
                }
            } else {
                finalTuple = nextTuple;
            }

            string tuplePlusArgs = "<";
            foreach (string str in finalTuple) {
                tuplePlusArgs += " " + str;
            }
            tuplePlusArgs += ">";
            ProcessDebug("Next Tuple: " + tuplePlusArgs);
            return finalTuple;
        }

        /**
		  * !!TODO!!DANGER!!TODO!!
		  * Please read the information below carefully
		  * if channel is created in some fancy function that ends, garbage collector will clean
		  * our server object and system will fail categoricly!!
		  * !!TODO!!DANGER!!TODO!!
		  */
        private void executeProcess() {
            string staticArgs = "<";
            if (processStaticArgs != null) {
                foreach (string str in processStaticArgs) {
                    staticArgs += " " + str;
                }
            }
            staticArgs += " >";
            System.Console.WriteLine("Setup");
            System.Console.WriteLine("routing: " + RoutTechnic.methodName());
            System.Console.WriteLine("Static Args: " + staticArgs);
            while (true) {

                IList<string> nextTuple = this.nextTuple();
                nextTuple.RemoveAt(0); //Removing clock
                List<string> metadata = getMetadata(nextTuple);
                string oldID = metadata[2];
                string port = metadata[1];
                string ip = metadata[0];
                ConnectionPack previous = new ConnectionPack(ip, Int32.Parse(port));
                lock (needsDivertList) {
                    if (needsDivertList.ContainsKey(oldID)) {
                        ProcessDebug("oldID: " + oldID + "needs diverting from " + previous + " to " + needsDivertList[oldID]);
                        previous = needsDivertList[oldID];
                    }
                }

                IList<string> finalTuple = addStaticArgs(nextTuple);

                object resultObject = this.generateStrategy.generateTuple(finalTuple);
                IList<IList<string>> result = (IList<IList<string>>)resultObject;
                if (result != null) {
                    emitTuple(previous, oldID, result);
                } else {
                    ProcessError("returned " + resultObject.GetType().ToString());
                    throw new Exception("dll method did not return a IList<IList<string>>");
                }

                //By default milliseconds is zero, but puppet master may want to slow things down..
                if (milliseconds > 0) {
                    Thread.Sleep(milliseconds);
                }
            }
        }

        private delegate List<string> GetMetadata(IList<string> tuple);
        private GetMetadata getMetadata;
        private List<string> getMetadataAtMost(IList<string> tuple) {
            List<string> toReturn = new List<string>();
            toReturn.Insert(0, "");
            toReturn.Insert(0, "0");
            toReturn.Insert(0, "0.0.0.0");
            return toReturn;
        }
        private List<string> getMetadataAtLeast(IList<string> nextTuple) {
            string ip = nextTuple[0];
            nextTuple.RemoveAt(0);
            string port = nextTuple[0];
            nextTuple.RemoveAt(0);
            string oldID = nextTuple[0];
            nextTuple.RemoveAt(0);

            List<string> toReturn = new List<string>();
            toReturn.Insert(0, ip);
            toReturn.Insert(1, port);
            toReturn.Insert(2, oldID);
            return toReturn;
        }
        private List<string> getMetadataExactly(IList<string> nextTuple) {
            string ip = nextTuple[0];
            nextTuple.RemoveAt(0);
            string port = nextTuple[0];
            nextTuple.RemoveAt(0);
            string oldID = nextTuple[0];
            nextTuple.RemoveAt(0);

            List<string> toReturn = new List<string>();
            toReturn.Insert(0, ip);
            toReturn.Insert(1, port);
            toReturn.Insert(2, oldID);
            return toReturn;
        }
        private delegate string GetID(IList<string> tuple);
        private GetID getID;
        private string getIDAtMost(IList<string> tuple) {
            return "";
        }
        private string getIDAtLeast(IList<string> nextTuple) {
            string oldID = nextTuple[2];
            return oldID;
        }
        private string getIDExactly(IList<string> nextTuple) {
            string oldID = nextTuple[2];
            return oldID;
        }

        private delegate void InsertMetadata(string oldID, List<string> tuple, int indexResult, int indexOperator);
        private InsertMetadata insertMetadata;
        private void insertMetadataAtMost(string oldID, List<string> tuple, int indexResult, int indexOperator) {

        }
        private void insertMetadataAtLeast(string oldID, List<string> tuple, int indexResult, int indexOperator) {
            string newID = getNewId(oldID, indexResult, indexOperator);

            tuple.Insert(0, newID);
            string ip = MyConPack.Ip;
            string port = MyConPack.Port.ToString();

            tuple.Insert(0, port);
            tuple.Insert(0, ip);

            //return tuple;
        }
        private void insertMetadataExactly(string oldID, List<string> tuple, int indexResult, int indexOperator) {
            string newID = getNewId(oldID, indexResult, indexOperator);

            tuple.Insert(0, newID);
            string ip = MyConPack.Ip;
            string port = MyConPack.Port.ToString();

            tuple.Insert(0, port);
            tuple.Insert(0, ip);
            //return tuple;
        }
        private delegate void InsertMetadataOnRead(string ID, List<string> tuple);
        private InsertMetadataOnRead insertMetadataOnRead;
        private void insertMetadataOnReadAtMost(string ID, List<string> tuple) {
            //return tuple;
        }
        private void insertMetadataOnReadAtLeast(string ID, List<string> toAdd) {
            toAdd.Insert(0, ID);
            toAdd.Insert(0, "0");
            toAdd.Insert(0, "0.0.0.0");
            //return toAdd;
        }
        private void insertMetadataOnReadExactly(string ID, List<string> toAdd) {
            toAdd.Insert(0, ID);
            toAdd.Insert(0, "0");
            toAdd.Insert(0, "0.0.0.0");
            //return toAdd;
        }
        private delegate void AssureSemanticsOnEmit(ConnectionPack previous, string oldID, IList<IList<string>> result);
        private AssureSemanticsOnEmit assureSemanticsOnEmit;
        private void assureSemanticsOnEmitAtMost(ConnectionPack previous, string ID, IList<IList<string>> result) {
        }
        private void assureSemanticsOnEmitAtLeast(ConnectionPack previous, string ID, IList<IList<string>> result) {
            doBackup(ID, result);
            assumeResponsability(previous, ID);
        }
        private void assureSemanticsOnEmitExactly(ConnectionPack previous, string ID, IList<IList<string>> result) {
            doBackup(ID, result);
            assumeResponsability(previous, ID);
        }
        private delegate void AssureSemanticsNextOperatorFailed(string opID);
        private AssureSemanticsNextOperatorFailed assureNextFailed;
        private void assureNextFailedOnEmitAtMost(string opID) { }
        private void assureNextFailedOnEmitAtLeast(string opID) {
            assureNextFailedOnEmitExactly(opID);
        }
        private void assureNextFailedOnEmitExactly(string opID) {
            //XXX if(semantic)

            Thread.Sleep(100);
            if (!guarantieIamAlive()) { return; };
            // i am dead dont worry about nothing ..
            lock (idTranslation) {
                foreach (KeyValuePair<string, string> entry in idTranslation) {
                    if (entry.Value.Equals(opID)) {
                        string tupleID = entry.Key;
                        string originalTupleID = getIdKey(tupleID);

                        if (originalTupleID != "") {
                            ProcessWarning("Trying original: " + originalTupleID);
                            ConnectionPack responsible;
                            if (brotherIDsResponsible.TryGetValue(originalTupleID, out responsible)) {
                                if (responsible.Equals(myConPack)) {
                                    addTupleToForgottenList(tupleID, opID);
                                }
                            }
                        }
                    }
                }
            }

        }

        private string getNewId(string baseID, int indexResult, int indexOperator) {
            string newID = baseID;

            if (indexResult > -1) {
                newID += "_" + indexResult;
            }
            if (indexOperator > -1) {
                newID += ":" + indexOperator;
            }
            return newID;
        }

        /// <summary>
        /// Functions that creates a link between two tuples
        /// allow for a tuple to generate more than 1 tuple and send to severall operators
        /// </summary>
        /// <param name="oldID"></param>
        /// <param name="newID"></param>
        private void createLink(string oldID, string newID, string opID) {

            lock (responsabilityLinks) {
                List<string> links;
                if (!responsabilityLinks.TryGetValue(oldID, out links)) {
                    links = new List<string>();
                }
                if (links.Contains(newID)) { ProcessError("Repeating IDS: " + newID); }
                links.Add(newID);
                //ProcessDebug("creating link between: " + oldID + " : " + newID);
                responsabilityLinks[oldID] = links;
            }

            lock (idTranslation) {
                idTranslation[newID] = opID;
            }
        }

        public void receiveReplicaBackup(string oldID, ConnectionPack brotherResponsible, IList<IList<string>> result) {
            lock (processedIDs) {
                processedIDs.Add(oldID); //Assure only once
            }

            if ((result.Count == 1 && result[0].Count == 0) //Nothing generated 
                || downStreamNodes.Count == 0) { //No one to forward output

                ProcessDebug("terminal case: " + oldID);
                //if(result.Count == 1 && result[0].Count == 0) { System.Console.WriteLine("empty result"); } 
                //if (downStreamNodes.Count == 0) { System.Console.WriteLine("last operator"); }

                return;
            }

            //Maybe check if responsible is myself..

            lock (brotherIDsResponsible) {
                brotherIDsResponsible[oldID] = brotherResponsible;
            }

            int indexResult = -1;
            foreach (List<string> tuple in result) {
                if (tuple.Count > 0) { //ignoring empty tuples
                    if (result.Count > 1) { indexResult++; }
                    int indexOperator = -1;
                    foreach (KeyValuePair<string, List<ConnectionPack>> entry in downStreamNodes) {
                        string opID = entry.Key;
                        if (downStreamNodes.Count > 1) { indexOperator++; }
                        string newID = getNewId(oldID, indexResult, indexOperator);
                        createLink(oldID, newID, opID);
                    }
                }
            }

            lock (responsability) {
                responsability.Add(oldID, result);
            }
        }

        private void doBackup(string oldID, IList<IList<string>> result) {
            lock (aliveOperatorReplicas) {
                foreach (ConnectionPack replica in aliveOperatorReplicas) {
                    if (!replica.Equals(myConPack)) {
                        DADStormProcess.ClientProcess replicaProcess = new DADStormProcess.ClientProcess(replica);
                        replicaProcess.receiveReplicaBackup(oldID, myConPack, result);
                    } else {
                        this.receiveReplicaBackup(oldID, myConPack, result);//MAYBE NULL
                    }
                }
            }
        }

        private void assumeResponsability(ConnectionPack previous, string oldID) {
            //Check if ID in diverted -> if so change previous TODO

            if (previous.Port != 0) { //Hack for when there is no previous
                DADStormProcess.ClientProcess previousProcess = new DADStormProcess.ClientProcess(previous);
                previousProcess.loseResponsability(oldID);
                ProcessDebug("telling " + previous + " i am responsible now for " + oldID);
            }
        }

        private void detectForgotenTuples() {
            if (mightBeForgottenTuples.Count > 0) {
                Dictionary<string, List<ConnectionPack>> aliveDownStreamNodesCOPY = getAliveDownStreamNodesCOPY();

                Dictionary<string, List<int>> downStreamClock = new Dictionary<string, List<int>>();
                foreach (KeyValuePair<string, List<ConnectionPack>> entry in downStreamNodes) {
                    string opID = entry.Key;
                    List<ConnectionPack> allEndpoints = entry.Value;
                    List<ConnectionPack> aliveEndpoints = aliveDownStreamNodesCOPY[opID];

                    List<int> clocks = new List<int>();
                    for (int i = 0; i < allEndpoints.Count; i++) {
                        int theirClock;
                        ConnectionPack endPoint = allEndpoints[i];
                        if (!aliveEndpoints.Contains(endPoint)) {
                            theirClock = -4;
                        } else {
                            DADStormProcess.ClientProcess replicaProcess = new DADStormProcess.ClientProcess(endPoint);
                            theirClock = replicaProcess.firstTupleInListClock();
                        }
                        clocks.Add(theirClock);
                    }
                    downStreamClock[opID] = clocks;

                }
                lock (mightBeForgottenTuples) {

                    List<string> keys = new List<string>(mightBeForgottenTuples.Keys);
                    for (int i = 0; i < keys.Count; i++) {
                        string tupleID = keys[i];
                        string opID = getIDnextOperator(tupleID);
                        List<int> updatedClocks = downStreamClock[opID];
                        List<int> savedClocks = mightBeForgottenTuples[tupleID];
                        bool forgotten = true;

                        for (int j = 0; j < savedClocks.Count; j++) {
                            int updated = updatedClocks[j];
                            int saved = savedClocks[j];

                            if (saved < 0) {
                                continue; // it was dead once dont worry
                            } else if (updated < 0) {
                                savedClocks[j] = updated;
                                //it is dead now
                            } else if (saved > updated) {
                                // not forgotten yet on this one
                                forgotten = false;
                                break;
                            } else {
                                // case where it is forgotten on that endpoint
                            }
                        }
                        if (forgotten) {
                            List<ConnectionPack> aliveEndpoints = aliveDownStreamNodesCOPY[opID];
                            bool done = false;
                            foreach (ConnectionPack next in aliveEndpoints) {
                                DADStormProcess.ClientProcess replicaProcess = new DADStormProcess.ClientProcess(next);
                                if (replicaProcess.isItDoneYet(tupleID)) {
                                    done = true;
                                    loseResponsability(tupleID);
                                    break;
                                }
                            }
                            mightBeForgottenTuples.Remove(tupleID);
                            if (done) {
                                continue;
                            } else {
                                rebuildAndSend(tupleID);
                                System.Console.WriteLine("tuple forgotten: " + tupleID);
                            }
                        }
                    }
                }
            }
        }

        public int currentClock() {
            return ++this.clock;
        }

        public int firstTupleInListClock() {
            int number = -5;
            lock (dllArgs) {
                if (dllArgs.Count > 0) {
                    IList<string> tuple = dllArgs.Peek();
                    number = Int32.Parse(tuple[0]);
                }
            }
            return number;
        }

        public bool isItDoneYet(string tupleID) {
            return processedIDs.Contains(tupleID);
        }

        private void rebuildAndSend(string tupleID) {
            string originalID = getIdKey(tupleID);
            IList<IList<string>> result = requestResultFromID(tupleID);
            if (result != null) {
                int indexResult = -1;
                foreach (List<string> tuple in result) {
                    if (tuple.Count > 0) { //ignoring empty tuples
                        if (result.Count > 1) { indexResult++; }
                        int indexOperator = -1;
                        foreach (KeyValuePair<string, List<ConnectionPack>> entry in downStreamNodes) {
                            string opID = entry.Key;
                            if (downStreamNodes.Count > 1) { indexOperator++; }
                            string newID = getNewId(originalID, indexResult, indexOperator);
                            if (newID.Equals(tupleID)) {
                                resendTuple(tuple, originalID, opID, aliveDownStreamNodes[opID], indexResult, indexOperator);
                                return;
                            }
                        }
                    }
                }
            } else {
                //ignore not our responsability anymore
            }
        }

        private void resendTuple(List<string> tuple, string baseID, string opID, List<ConnectionPack> receivingOperator, int indexResult, int indexOperators) {

            if (receivingOperator.Count == 0) {
                ProcessWarning("One Operator seems to have all its replicas down, tuples are being LOST");
                //Lose responsability over all that..
                return;
            }

            List<string> currentTuple = new List<string>(tuple); //Assure we are not always reediting the same

            insertMetadata(baseID, currentTuple, indexResult, indexOperators);
            ConnectionPack nextOperatorCp = this.RoutTechnic.nextDestination(receivingOperator, currentTuple);

            ProcessWarning("tuple will be resent: <" + String.Join(", ", currentTuple) + ">");
            try {
                sendTupleToOperator(nextOperatorCp, currentTuple);
                ProcessDebug("operator " + opID + " received tuple on " + nextOperatorCp);
            } catch (SocketException) {
                ProcessError("operator " + opID + " failed to received tuple on " + nextOperatorCp);
                List<ConnectionPack> cpList = new List<ConnectionPack>();
                cpList.Add(nextOperatorCp);
                nextOperatorFailed(opID, cpList);
                //Could not send the tuple but it will be detected later dont worry by PerfectFailureDetection
            }
        }

        /**
		  * After processing tuple this method emits it to downStream and puppet master
		  */
        private void emitTuple(ConnectionPack previous, string oldID, IList<IList<string>> result) {

            assureSemanticsOnEmit(previous, oldID, result);
            numberProcessed++;
            int indexResult = -1;
            foreach (List<string> tuple in result) {
                if (result.Count > 1) { indexResult++; }

                if (tuple.Count > 0) { //Lets ignore empty tuples shall we
                    if (fullLog == true) {
                        logToPuppetMaster(tuple);
                    }
                    int indexOperators = -1;
                    Dictionary<string, List<ConnectionPack>> aliveDownStreamNodesCOPY = getAliveDownStreamNodesCOPY();

                    if (aliveDownStreamNodesCOPY.Count > 0) {
                        //Foreach operator
                        foreach (KeyValuePair<string, List<ConnectionPack>> entry in aliveDownStreamNodesCOPY) {
                            List<ConnectionPack> receivingOperator = entry.Value;
                            if (receivingOperator.Count == 0) {
                                ProcessWarning("One Operator seems to have all its replicas down, tuples are being LOST");
                                //Lose responsability over all that..
                                continue;
                            }
                            string opID = entry.Key;
                            List<string> currentTuple = new List<string>(tuple); //Assure we are not always reediting the same
                            if (aliveDownStreamNodesCOPY.Count > 1) { indexOperators++; }

                            insertMetadata(oldID, currentTuple, indexResult, indexOperators);
                            ConnectionPack nextOperatorCp = this.RoutTechnic.nextDestination(receivingOperator, currentTuple);

                            ProcessDebug("Another tuple will be sent: <" + String.Join(", ", currentTuple) + ">");
                            try {
                                sendTupleToOperator(nextOperatorCp, currentTuple);
                                ProcessDebug("operator " + opID + " received tuple on " + nextOperatorCp);
                            } catch (SocketException) {
                                ProcessError("operator " + opID + " failed to received tuple on " + nextOperatorCp);
                                List<ConnectionPack> cpList = new List<ConnectionPack>();
                                cpList.Add(nextOperatorCp);
                                nextOperatorFailed(opID, cpList);
                                //Could not send the tuple but it will be detected later dont worry by PerfectFailureDetection
                            }
                        }
                    } else {
                        ProcessDebug("No one to send tuple to :(");
                        //Case where there is no one to receive..//probably its last operator}
                    }
                }
                ProcessDebug(oldID + " send complete");
            }
        }

        private Dictionary<string, List<ConnectionPack>> getAliveDownStreamNodesCOPY() {
            lock (aliveDownStreamNodes) {
                return new Dictionary<string, List<ConnectionPack>>(aliveDownStreamNodes);
            }
        }
        private List<ConnectionPack> getAliveOperatorCOPY() {
            lock (aliveOperatorReplicas) {
                return new List<ConnectionPack>(aliveOperatorReplicas);
            }
        }

        /**
		  * In the full logging mode, all tuple emissions need to be reported to Puppetmaster
		  */
        private void logToPuppetMaster(IList<string> tuple) {
            try {
                if (puppetRemote == null) {
                    puppetRemote = (DADStormRemoteTupleReceiver)Activator.GetObject(
                        typeof(DADStormRemoteTupleReceiver), "tcp://" + puppetMasterConPack.Ip + ":" + puppetMasterConPack.Port + "/PuppetMasterRemoteServerObject");
                }
                puppetRemote.addTuple("[ " + myConPack.Ip + ":" + myConPack.Port + " ]", tuple);
                ProcessDebug("Puppet Master informed");
            } catch (SocketException) {
                ProcessError("Puppet Master is down");
            }
        }

        /// <summary>
        /// method that sends a tuple to an endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="tuple"></param>
        private void sendTupleToOperator(ConnectionPack endpoint, IList<string> tuple) {
            DADStormProcess.ClientProcess nextProcess = new DADStormProcess.ClientProcess(endpoint);
            string status = nextProcess.addTuple(tuple);
            if (status.Equals("our responsability")) {
                this.loseResponsability(getID(tuple));
            }
        }

        private void removeResponsabilityFromOperator(string ID) {
            List<ConnectionPack> aliveOperatorReplicasCOPY;
            //Snapshot is more than enough
            lock (aliveOperatorReplicas) {
                aliveOperatorReplicasCOPY = new List<ConnectionPack>(aliveOperatorReplicas);
                //avoid do remote calls with locks
            }

            foreach (ConnectionPack replica in aliveOperatorReplicasCOPY) {
                if (!replica.Equals(myConPack)) {
                    DADStormProcess.ClientProcess replicaProcess = new DADStormProcess.ClientProcess(replica);
                    replicaProcess.loseReplicaResponsability(ID);
                }
            }
        }
        /* -------------------------------------------------------------------- */
        /* -------------------------------------------------------------------- */
        /* --------------------------- Public Methods ------------------------- */
        /* -------------------------------------------------------------------- */
        /* -------------------------------------------------------------------- */

        /// <summary>
        /// Setup the class
        /// </summary>
        public void buildServer(ConnectionPack myCp, string dllName, string className, string methodName, string routingTechnic, bool fullLogging, int semantics, string operatorID) {
            this.MyConPack = myCp;
            this.FullLog = fullLogging;
            this.operatorID = operatorID;
            RoutingTechinic technic = null;
            if (routingTechnic.Equals("random", StringComparison.OrdinalIgnoreCase)) {
                technic = new DADStormProcess.RandomRouting();
            } else if (routingTechnic.StartsWith("hashing", StringComparison.OrdinalIgnoreCase)) {
                String[] splitStr = routingTechnic.Split(new[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                int hashingNumber = Int32.Parse(splitStr[1]);
                technic = new DADStormProcess.Hashing(hashingNumber);
                // BY default lets use Primmary routing
            } else {
                technic = new DADStormProcess.Primmary();
            }

            this.RoutTechnic = technic;

            if (className.Equals("CSF_IpInName")) {
                this.generateStrategy = new CSF_IpInName();
            } else if (className.Equals("CSF_HighDownload")) {
                this.generateStrategy = new CSF_HighDownload();
            } else if (className.Equals("CSF_HighDataDiffPeers")) {
                this.generateStrategy = new CSF_HighDataDiffPeers();
            } else if (className.Equals("CSF_HighUpload")) {
                this.generateStrategy = new CSF_HighUpload();
            } else if (className.Equals("CSF_KnownTrackers")) {
                this.generateStrategy = new CSF_KnownTrackers();
            } else if (className.Equals("CSF_ProtocolUPnP")) {
                this.generateStrategy = new CSF_ProtocolUPnP("UPnP");
            } else if (className.Equals("CSF_LocalPeerDiscovery")) {
                this.generateStrategy = new CSF_LocalPeerDiscovery();
            } else {
                this.generateStrategy = new CustomDll(dllName, className, methodName);
            }

            this.semantics = semantics;
            if (semantics == 0) {

                //at most  once
                getMetadata = getMetadataAtMost;
                insertMetadata = insertMetadataAtMost;
                insertMetadataOnRead = insertMetadataOnReadAtMost;
                assureSemanticsOnEmit = assureSemanticsOnEmitAtMost;
                getID = getIDAtMost;
                assureNextFailed = assureNextFailedOnEmitAtMost;
            } else if (semantics == 1) {
                //at least once
                getMetadata = getMetadataAtLeast;
                insertMetadata = insertMetadataAtLeast;
                insertMetadataOnRead = insertMetadataOnReadAtLeast;
                assureSemanticsOnEmit = assureSemanticsOnEmitAtLeast;
                getID = getIDAtLeast;
                assureNextFailed = assureNextFailedOnEmitAtLeast;

            } else if (semantics == 2) {
                //exactly  once
                getMetadata = getMetadataExactly;
                insertMetadata = insertMetadataExactly;
                insertMetadataOnRead = insertMetadataOnReadExactly;
                assureSemanticsOnEmit = assureSemanticsOnEmitExactly;
                getID = getIDExactly;
                assureNextFailed = assureNextFailedOnEmitExactly;

            }



        }

        public void addDownStreamOperator(List<ConnectionPack> cp, string opID) {
            ProcessDebug("Down Stream Op added: " + cp);
            downStreamNodes[opID] = cp;
            List<ConnectionPack> alives = new List<ConnectionPack>(cp); //Copying array by value
            aliveDownStreamNodes[opID] = alives;
        }

        public void crash() {
            new Thread(() => {
                Thread.Sleep(100); //allow remote method to return
                Environment.Exit(1);
            }).Start();
            return;
        }

        public void freeze() {
            ProcessWarning("FROZEN");
            this.frozen = true;
        }

        public void defreeze() {
            ProcessWarning("BACK TO LIFE");
            this.frozen = false;
            if(semantics > 0) {
                doReborn();
            }
            lock (dllArgs) {
                Monitor.Pulse(dllArgs);
            }
        }

        //Public that must be called by
        public int getIndexFromPrimmary(string file) {
            if (primmary) {
                lock (filesIndex) {
                    int counter;
                    if (!filesIndex.TryGetValue(file, out counter)) {
                        counter = 0;
                        filesIndex.Add(file, counter);
                    }
                    filesIndex[file] = counter + 1;
                    return counter;
                }
            }
            return -1;
        }

        //Method that adds a file that this replica will read
        public void addFile(string file) {
            lock (filesLocation) {
                filesLocation.Add(file);
            }
            lock (dllArgs) {
                Monitor.Pulse(dllArgs);
            }
        }

        //Method that returns true if an ID is already responsability of this operator
        private bool checkResponsability(string ID) {
            //if (processedIDs.Contains(ID)) {
            //    //There was error somewhere
            //}

            return processedIDs.Contains(ID);
        }

        /// <summary>
        /// Detects failures in list, also removes any down process
        /// </summary>
        /// <param name="list"></param>
        /// <returns> true if any node failed</returns>
        private List<ConnectionPack> detectFailuresList(List<ConnectionPack> list) {
            List<ConnectionPack> failed = new List<ConnectionPack>();
            int inicialProccesses = list.Count;
            for (int i = 0; i < list.Count; i++) {
                ConnectionPack proccess = list[i];
                if (proccess.Equals(myConPack)) {
                    ; //skipping ourselves
                } else {
                    DADStormProcess.ClientProcess replicaProcess = new DADStormProcess.ClientProcess(proccess);
                    try {
                        replicaProcess.ping();
                    } catch (SocketException) {
                        ProcessWarning("Someone Failed: " + proccess);
                        failed.Add(proccess);
                        // seems like this process is dead, removing from list..
                    }
                }
            }
            return failed;
        }

        private bool removeBrotherFromList(ConnectionPack brother) {
            bool didIknow;
            lock (aliveOperatorReplicas) {
                didIknow = !aliveOperatorReplicas.Contains(brother);
                aliveOperatorReplicas.Remove(brother);
                ProcessWarning("Brother: " + brother + " removed from list");
            }
            return didIknow;
        }

        private void nextOperatorFailed(string opID, List<ConnectionPack> deadProcesses) {
            ProcessDebug("starting to recover Failure Detected in OP: " + opID);
            //Need to resend failed connection pack / maybe process them myself tupleID -> tuple
            lock (aliveDownStreamNodes) {
                ProcessDebug("starting to recovber and got the lock: " + opID);
                List<ConnectionPack> operatorAliveProccesses = aliveDownStreamNodes[opID];
                foreach (ConnectionPack proccess in deadProcesses) {
                    operatorAliveProccesses.Remove(proccess);
                }
                if (semantics > 0) {
                    foreach (ConnectionPack replica in operatorAliveProccesses) {
                        DADStormProcess.ClientProcess replicaProcess = new DADStormProcess.ClientProcess(replica);
                        replicaProcess.warnBrothersDead(deadProcesses);
                    }
                }
            }
            assureNextFailed(opID);
            ProcessDebug("recover complete");
        }

        private void brothersFailed(List<ConnectionPack> failed) {
            bool didIknow = false;
            foreach (ConnectionPack brother in failed) {
                didIknow = didIknow || removeBrotherFromList(brother);
            }
            ProcessDebug("didIknow: " + didIknow);
            if (!didIknow) {
                this.syncNumber++; //Another fatality
                ProcessWarning("Brother failed");
                enterSyncProcess();
            }
        }

        public List<string> getResponsabilityList() {

            List<string> tuples = new List<string>(responsability.Keys);
            return tuples;

        }

        private void addTupleToForgottenList(string tupleID, string opID) {
            ProcessWarning("Adding tuple to forgoten: " + tupleID);
            Dictionary<string, List<ConnectionPack>> aliveDownStreamNodesCOPY = getAliveDownStreamNodesCOPY();

            List<int> operatorClock = new List<int>();

            List<ConnectionPack> allEndpoints = downStreamNodes[opID];
            List<ConnectionPack> aliveEndpoints = aliveDownStreamNodesCOPY[opID];

            List<int> clocks = new List<int>();
            for (int i = 0; i < allEndpoints.Count; i++) {
                int theirClock;
                ConnectionPack endPoint = allEndpoints[i];
                if (!aliveEndpoints.Contains(endPoint)) {
                    theirClock = -4;
                } else {
                    DADStormProcess.ClientProcess replicaProcess = new DADStormProcess.ClientProcess(endPoint);
                    theirClock = replicaProcess.currentClock();
                }
                clocks.Add(theirClock);
            }
            lock (mightBeForgottenTuples) {
                mightBeForgottenTuples[tupleID] = clocks;
            }

        }

        public void reborn(ConnectionPack deadGuy) {
            lock (aliveOperatorReplicas) {
                if (!aliveOperatorReplicas.Contains(deadGuy)) {
                    aliveOperatorReplicas.Add(deadGuy);
                }
            }
        }

        /// <summary>
        /// Method that syncs all replicas
        /// </summary>
        /// <returns> true when successful fail if not</returns>
        private bool doSync() {
            ProcessWarning("Attempting SYNC ");
            if (!guarantieIamAlive()) { return true; /* even though it was not successful we are dead.. */}
            //Make sure everyone is in sync
            Dictionary<ConnectionPack, int> syncNumber = new Dictionary<ConnectionPack, int>();
            int begginingNumber = this.SyncNumber;
            lock (aliveOperatorReplicas) {
                foreach (ConnectionPack brother in aliveOperatorReplicas) {
                    try {
                        int proccessNumber;
                        if (brother.Equals(MyConPack)) {
                            proccessNumber = this.SyncNumber;
                        } else {
                            DADStormProcess.ClientProcess replicaProcess = new DADStormProcess.ClientProcess(brother);
                            proccessNumber = replicaProcess.SyncNumber();
                        }
                        if (proccessNumber != begginingNumber) {
                            ProcessWarning("Sync Failed: different number ");
                            return false;
                        }
                    } catch (SocketException) {
                        ProcessDebug("Sync Failed: brother died ");
                        //looks like we got ourselves another brother dead.
                        return false; //Restart Proccess
                    }
                }

                ////////////////////////////////// Syncing /////////////////////////////////////////////
                ProcessDebug("SYNCRONIZATION IN PLACE: sync number " + begginingNumber);
                //Thread.Sleep(100); //Let operators finishing processing their stuff
                foreach (ConnectionPack brother in aliveOperatorReplicas) {
                    try {
                        if (brother.Equals(MyConPack)) {
                            continue;
                        }
                        DADStormProcess.ClientProcess replicaProcess = new DADStormProcess.ClientProcess(brother);
                        List<string> brotherResponsabilityList = replicaProcess.getResponsabilityList();
                        foreach (string tupleID in brotherResponsabilityList) {
                            if (!responsability.ContainsKey(tupleID)) {
                                ConnectionPack responsibleBrother = replicaProcess.responsibleBrother(tupleID);
                                IList<IList<string>> result = replicaProcess.requestResultFromID(tupleID);
                                if (responsibleBrother == null || result == null) {
                                    continue;
                                    //if he stoped having this one is because someone told him to lose it and they had their reasons
                                }
                                this.receiveReplicaBackup(tupleID, responsibleBrother, result);
                            }
                        }
                        //this.mergeResponsabilityLists(brotherResponsabilityList);
                        //List<string> responsability = getResponsabilityList();

                        ////// Take into account: 
                        //brotherIDsResponsible
                        //idTranslation
                    } catch (SocketException) {
                        ProcessDebug("Sync Failed: brother died ");
                        //looks like we got ourselves another fatality
                        return false; //Restart Proccess
                    }
                }
                ////// Take into account: 
                //brotherIDsResponsible
                //gotta check all dead brothers
                List<ConnectionPack> deadBrothers = new List<ConnectionPack>();
                foreach (ConnectionPack brother in operatorReplicas) {
                    if (!aliveOperatorReplicas.Contains(brother)) {
                        deadBrothers.Add(brother);
                    }
                }
                ConnectionPack sacrificedOne = aliveOperatorReplicas[0];
                List<string> needsDivert = new List<string>();
                lock (brotherIDsResponsible) {
                    List<string> tuplesIDs = new List<string>(brotherIDsResponsible.Keys);
                    foreach (string tupleID in tuplesIDs) {
                        foreach (ConnectionPack brother in deadBrothers) {
                            if (brotherIDsResponsible[tupleID].Equals(brother)) {
                                ProcessWarning("Was of dead Guy: " + tupleID);
                                brotherIDsResponsible[tupleID] = sacrificedOne;
                                needsDivert.Add(tupleID);
                                break; //only one can have been the responsible
                            }
                        }
                    }
                }
                if (sacrificedOne.Equals(myConPack)) {
                    //warnAll..
                    foreach (string tupleID in needsDivert) {
                        List<string> resultingTuples = new List<string>();
                        if (responsabilityLinks.ContainsKey(tupleID)) {
                            resultingTuples = responsabilityLinks[tupleID];
                        } else {
                            ProcessWarning("needs divert not found in responsible: " + tupleID);
                        }
                        foreach (string forwardedTupleID in resultingTuples) {

                            string nextOperator = idTranslation[forwardedTupleID];
                            List<ConnectionPack> nextOperatorList = aliveDownStreamNodes[nextOperator];
                            if (!aliveDownStreamNodes.TryGetValue(nextOperator, out nextOperatorList)) {
                                continue; // all operator is dead..
                            }
                            ProcessWarning("Diverting: " + forwardedTupleID);
                            bool need = true;

                            foreach (ConnectionPack next in nextOperatorList) {
                                DADStormProcess.ClientProcess nextProcess = new DADStormProcess.ClientProcess(next);
                                if (nextProcess.needsDivert(forwardedTupleID, myConPack).Equals("our responsability")) {
                                    need = false;
                                    break;
                                }
                            }
                            if (!need) {
                                removeResponsabilityFromOperator(forwardedTupleID);
                            } else {
                                addTupleToForgottenList(forwardedTupleID, nextOperator);
                                ProcessWarning("Diverted with success: " + forwardedTupleID);
                            }
                        }
                    }
                }

                ////////////////////////////////// Syncing /////////////////////////////////////////////

                //Ping all brothers to ensure everyone is still ok;
                foreach (ConnectionPack brother in aliveOperatorReplicas) {
                    try {
                        int proccessNumber;
                        if (brother.Equals(MyConPack)) {
                            proccessNumber = this.SyncNumber;
                        } else {
                            DADStormProcess.ClientProcess replicaProcess = new DADStormProcess.ClientProcess(brother);
                            proccessNumber = replicaProcess.SyncNumber();
                        }
                        if (proccessNumber != begginingNumber) {
                            ProcessWarning("Sync Failed: different number ");
                            return false;
                        }
                    } catch (SocketException) {
                        ProcessWarning("Sync Failed: brother died ");
                        //looks like we got ourselves another brother dead.
                        return false; //Restart Proccess
                    }
                }
            }
            ProcessWarning("SYNC COMPLETE WITH SUCCESS");
            return true;
        }
        public string needsDivert(string tupleID, ConnectionPack nextOwner) {
            if (processedIDs.Contains(tupleID)) {
                return "our responsability";
            } else {
                needsDivertList[tupleID] = nextOwner;
                return "ack";
            }
        }

        public ConnectionPack responsibleBrother(string tupleID) {
            ConnectionPack responsible;
            lock (brotherIDsResponsible) {
                if (brotherIDsResponsible.TryGetValue(tupleID, out responsible)) {
                    return responsible;
                } else {
                    return null;
                }
            }
        }

        public IList<IList<string>> requestResultFromID(string tupleID) {
            IList<IList<string>> result;
            lock (responsability) {
                if (responsability.TryGetValue(tupleID, out result)) {
                    return result;
                } else {
                    return null;
                }
            }
        }

        private void enterSyncProcess() {
            //Parar processamento
            lock (syncing) {
                if (this.sync == true) {
                    return; //Already a sync is in process sync
                }
                this.sync = true;
            }
            while (doSync() != true) { Thread.Sleep(60); /* NOP until successful sync is achieved */ }
            lock (syncing) {
                this.sync = false;
                Monitor.Pulse(syncing);
            }
        }

        public void warnBrothersDead(List<ConnectionPack> deadBrothers) {
            new Thread(() => {
                this.brothersFailed(deadBrothers);
            }).Start();
        }

        private void detectFailures() {

            //Detect Brother failures
            List<ConnectionPack> failed;

            lock (aliveOperatorReplicas) {
                failed = detectFailuresList(aliveOperatorReplicas);
            }
            if (failed.Count > 0) {
                brothersFailed(failed);
            }

            //Detect Down the river failures
            lock (aliveDownStreamNodes) {
                var keys = new List<string>(aliveDownStreamNodes.Keys);
                foreach (string key in keys) {
                    List<ConnectionPack> operatorAliveProccesses = aliveDownStreamNodes[key];
                    failed = detectFailuresList(operatorAliveProccesses);
                    if (failed.Count > 0) {
                        nextOperatorFailed(key, failed);
                    }
                }
            }
        }

        public void start() {
            this.started = true;
            this.frozen = false;
            lock (dllArgs) {
                Monitor.Pulse(dllArgs);
            }
            if (semantics > 0) {
                launchFailureDetection();
                launchForgottenTupleDetectionService();
            }
        }

        private void launchFailureDetection() {
            new Thread(() => {
                ProcessWarning("Failure Detection Being started");
                while (true) {
                    Thread.Sleep(PFDetectionInterval);
                    if (!this.frozen) {
                        guarantieIamAlive();
                        detectFailures();
                    }
                }
            }).Start();
        }

        private bool guarantieIamAlive() {
            lock (suicidingLock) {
                if (suiciding) { return false; }
            }
            if (!amIalive()) {
                suicide();
                return false;
            }
            return true;
        }

        private void doReborn() {
            suicide();
            ProcessWarning("Reanimation started");
            while (!reanimationProcess()) { Thread.Sleep(1000)/* NOP */ ;  }
            ProcessWarning("Reanimation complete");
            //Warn operators in the back that i can receive stuff

        }

        private bool reanimationProcess() {


            //Creating list of alive people
            foreach (ConnectionPack brother in operatorReplicas) {
                if (!brother.Equals(MyConPack)) {
                    DADStormProcess.ClientProcess replicaProcess = new DADStormProcess.ClientProcess(brother);
                    try {
                        replicaProcess.reborn(myConPack);
                        lock (aliveOperatorReplicas) {
                            aliveOperatorReplicas.Add(brother);
                        }
                    } catch (SocketException) {
                        //this one is dead..
                    }
                } else {
                    lock (aliveOperatorReplicas) {
                        aliveOperatorReplicas.Add(brother);
                    }
                }
            }

            List<ConnectionPack> aliveBrothers = getAliveOperatorCOPY();
            SnapShot snapshot = null;
            foreach (ConnectionPack brother in aliveBrothers) {
                if (!brother.Equals(MyConPack)) {
                    DADStormProcess.ClientProcess replicaProcess = new DADStormProcess.ClientProcess(brother);
                    try {
                        snapshot = replicaProcess.getSnapShot();
                        if(snapshot != null) {
                            break;
                        }
                    } catch (SocketException) {
                        //damn failed..
                    }
                }
            }
            if (snapshot != null) {
                ProcessError("5 Locks remaining");
                lock (processedIDs) {
                    ProcessError("4 Locks remaining");
                    lock (brotherIDsResponsible) {
                        ProcessError("3 Locks remaining");
                        lock (responsabilityLinks) {
                            ProcessError("2 Locks remaining");
                            lock (idTranslation) {
                                ProcessError("1 Locks remaining");
                                lock (responsability) {
                                    ProcessError("No mas locks");
                                    this.brotherIDsResponsible = snapshot.brotherIDsResponsible.getDictionary();
                                    this.responsability = snapshot.responsability.getDictionary();
                                    this.responsabilityLinks = snapshot.responsabilityLinks.getDictionary();
                                    this.idTranslation = snapshot.idTranslation.getDictionary();
                                    this.processedIDs = snapshot.processedIDs;
                                }
                            }
                        }
                    }
                }
            } else {
                return false;
                //Reborn is failing
                //retry
            }
            for (int i = 0; i < requests.Count; i++) {
                QueueRequests request = requests[i];
                if (request.backup) {
                    if (!responsability.ContainsKey(request.tupleID)) {
                        this.receiveReplicaBackup(request.tupleID, request.author, request.result);
                    }
                } else {
                    if (responsability.ContainsKey(request.tupleID)) {
                        this.loseReplicaResponsability(request.tupleID);
                    }
                }
            }
            suiciding = false;
            ProcessWarning("Back from the dead");
            return true;
        }

        public SnapShot getSnapShot() {
            ProcessError("Doing snapshot");
            ProcessError("5 Locks remaining");
            lock (processedIDs) {
                ProcessError("4 Locks remaining");
                lock (brotherIDsResponsible) {
                    ProcessError("3 Locks remaining");
                    lock (responsabilityLinks) {
                        ProcessError("2 Locks remaining");
                        lock (idTranslation) {
                            ProcessError("1 Locks remaining");
                            lock (responsability) {
                                ProcessError("No mas locks");
                                return new SnapShot(responsability, idTranslation, responsabilityLinks, brotherIDsResponsible, processedIDs);
                            }
                        }
                    }
                }
            }
        }


        private void suicide() {
            //verify process is not in progress already
            lock(suicidingLock) {
                if(suiciding) { return; }
                suiciding = true;
            }
            ProcessWarning("trying to get lock of responsability");
            lock(responsability) {
                ProcessWarning("responsability ok");
                responsability.Clear();
            }
            ProcessWarning("trying to get lock of responsabilityLinks");
            lock (responsabilityLinks) {
                ProcessWarning("responsabilityLinks ok");
                responsabilityLinks.Clear();
            }
            ProcessWarning("trying to get lock of brotherIDsResponsible");
            lock (brotherIDsResponsible) {
                ProcessWarning("brotherIDsResponsible ok");
                brotherIDsResponsible.Clear();
            }
            ProcessWarning("trying to get lock of needsDivertList");
            lock (needsDivertList) {
                ProcessWarning("needsDivertList ok");
                needsDivertList.Clear();
            }
            ProcessWarning("trying to get lock of mightBeForgottenTuples");
            lock (mightBeForgottenTuples) {
                ProcessWarning("mightBeForgottenTuples ok");
                mightBeForgottenTuples.Clear();
            }
            lock (aliveOperatorReplicas) {
                aliveOperatorReplicas.Clear();
            }



           
            ProcessError("And now i feel so much more clean! DEAD..");
        }

        public void addToQueueOfBackup(string oldID, ConnectionPack author, IList<IList<string>> result) {
            requests.Add(new QueueRequests(oldID, author, result));

        }

        public void addToQueueLoseResponsability(string ID) {
            requests.Add(new QueueRequests(ID));
        }

        private void launchForgottenTupleDetectionService() {
            new Thread(() => {
                ProcessWarning("Forgotten Tuple Detection service started");
                while (true) {
                    Thread.Sleep(ForgotenTDInterval);
                    if (!this.frozen) {
                        guarantieIamAlive();
                        detectForgotenTuples();
                    }
                }
            }).Start();
        }

        private void printAlivesTable() {
            int maxNumberOfReplicas = 0;
            maxNumberOfReplicas = operatorReplicas.Count;

            var keys = new List<string>(downStreamNodes.Keys);
            foreach (string key in keys) {
                List<ConnectionPack> operatorProccesses = downStreamNodes[key];
                if (operatorProccesses.Count > maxNumberOfReplicas) {
                    maxNumberOfReplicas = operatorProccesses.Count;
                }
            }
            int tableColumns = maxNumberOfReplicas + 1; //nome operador

            List<string> messages = new List<string>(); ;
            messages.Add("  op ID  ");
            for (int i = 0; i < tableColumns - 1; i++) {
                messages.Add("replica ");
            }

            Dictionary<string, List<ConnectionPack>> downStreamNodesCOPY;
            Dictionary<string, List<ConnectionPack>> aliveDownStreamNodesCOPY;


            //Snapshot is more than enough
            lock (downStreamNodes) {
                downStreamNodesCOPY = new Dictionary<string, List<ConnectionPack>>(downStreamNodes);
            }
            //Snapshot is more than enough
            lock (aliveDownStreamNodes) {
                aliveDownStreamNodesCOPY = new Dictionary<string, List<ConnectionPack>>(aliveDownStreamNodes);
            }


            lock (printing) {
                printUpper(tableColumns);
                printUpperSpace(tableColumns);
                printLine(tableColumns, messages);
                messages = new List<string>();
                messages.Add("self");
                foreach (ConnectionPack brother in operatorReplicas) {
                    if (brother.Equals(myConPack)) {
                        messages.Add("current ");
                    } else {
                        if (aliveOperatorReplicas.Contains(brother)) {
                            messages.Add("online ");
                        } else {
                            messages.Add("offline ");
                        }
                    }
                }
                printCleanLine(tableColumns);
                printLine(tableColumns, messages);



                foreach (KeyValuePair<string, List<ConnectionPack>> entry in downStreamNodesCOPY) {
                    messages = new List<string>();
                    messages.Add(entry.Key + " ");
                    foreach (ConnectionPack proccess in entry.Value) {
                        List<ConnectionPack> operatorProccesses;
                        if (aliveDownStreamNodesCOPY.TryGetValue(entry.Key, out operatorProccesses)) {
                            if (operatorProccesses.Contains(proccess)) {
                                messages.Add("online ");
                            } else {
                                messages.Add("offline ");
                            }
                        } else {
                            messages.Add("offline ");
                        }
                    }
                    printCleanLine(tableColumns);
                    printLine(tableColumns, messages);
                }
                printDown(tableColumns);
            }
        }
        private void printUpperSpace(int columnNumber) {
            int numberChars = 9;

            string clean = "";

            for (int i = 0; i < numberChars; i++) {
                clean += " ";
            }
            clean += "|";
            string upperLine = "|"; //each field will have 9 chars
            for (int i = 0; i < columnNumber; i++) {
                upperLine += clean;
            }
            System.Console.WriteLine(upperLine);
        }
        private void printDown(int columnNumber) {
            int numberChars = 9;

            string clean = "";
            string underLines = "";

            for (int i = 0; i < numberChars; i++) {
                clean += " ";
                underLines += "_";
            }
            underLines += "|";
            string upperLine = "|";
            for (int i = 0; i < columnNumber; i++) {
                upperLine += underLines;
            }
            System.Console.WriteLine(upperLine);
        }
        private void printUpper(int columnNumber) {
            int numberChars = 9;

            string clean = "";
            string underLines = "";

            for (int i = 0; i < numberChars; i++) {
                clean += " ";
                underLines += "_";
            }
            underLines += "_";
            string upperLine = "_";
            for (int i = 0; i < columnNumber; i++) {
                upperLine += underLines;
            }
            System.Console.WriteLine(upperLine);
        }
        private void printCleanLine(int columnNumber) {
            int numberChars = 9;

            string clean = "";

            for (int i = 0; i < numberChars; i++) {
                clean += "_";
            }
            clean += "|";
            string upperLine = "|"; //each field will have 9 chars
            for (int i = 0; i < columnNumber; i++) {
                upperLine += clean;
            }
            System.Console.WriteLine(upperLine);
        }
        private void printLine(int columnNumber, List<string> messages) {
            int numberChars = 9;

            string clean = "";

            for (int i = 0; i < numberChars; i++) {
                clean += " ";
            }
            string line = "";

            System.Console.Write("|");
            for (int i = 0; i < columnNumber; i++) {
                if (i < messages.Count) {
                    if (messages[i].Equals("self")) {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        messages[i] = operatorID + " ";
                    }
                    line = String.Format("{0,9}", messages[i]);
                    if (messages[i].Equals("online ")) {
                        Console.ForegroundColor = ConsoleColor.Green;
                    } else if (messages[i].Equals("offline ")) {
                        Console.ForegroundColor = ConsoleColor.Red;
                    } else if (messages[i].Equals("current ")) {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                    }
                    System.Console.Write(line);
                    Console.ResetColor();
                    System.Console.Write("|");
                } else {
                    line = clean + "|";
                    System.Console.Write(line);
                }
            }
            System.Console.WriteLine();
        }

        private void printTuple(IList<string> tuple, string message) {
            ProcessDebug(message + " < " + String.Join(", ", tuple) + " > ");
        }

        /**
		  * method that adds a tuple to be processed
		  */
        public string addTuple(IList<string> nextArg) {
            printTuple(nextArg, "RECEIVED TUPLE: ");
            string oldID = getID(nextArg);
            if (checkResponsability(oldID)) {
                return "our responsability";
            }
            lock (dllArgs) {

                nextArg.Insert(0, clock.ToString());  //INSERTING TUPLE
                dllArgs.Enqueue(nextArg);
                Monitor.Pulse(dllArgs);
            }
            return "";
        }

        public void addReplicas(List<ConnectionPack> replicaList) {
            this.operatorReplicas = replicaList;
            List<ConnectionPack> alives = new List<ConnectionPack>(replicaList);
            this.aliveOperatorReplicas = alives;
        }

        public string ping() {
            if (this.started && this.frozen) {
                throw new SocketException();
            }
            return "ack";
        }

        /// <summary>
        /// Method that will be in loop (passively) and will be processing input
        /// </summary>
        public void createAndProcess() {

            TcpChannel channel = new TcpChannel(myConPack.Port);
            ChannelServices.RegisterChannel(channel, false);
            myServerObj = new ProcessRemoteServerObject();
            RemotingServices.Marshal(myServerObj, "op", typeof(ProcessRemoteServerObject));

            Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine("ProcessServer is ONLINE: port is: " + myConPack.Port);
            Console.ResetColor();

            executeProcess();
        }

        //CSF method
        public MemoryStream reportBack() {
            return this.generateStrategy.reportBack();
        }

        //CSF method
        public void reset() {
            this.generateStrategy.reset();
        }

        public bool isAlive(ConnectionPack brother) {
            lock (aliveOperatorReplicas) {
                foreach (ConnectionPack cp in aliveOperatorReplicas) {
                    if(brother.Equals(cp)) {
                        return true;
                    }                    
                }
            }
            return false;
        }

        private bool amIalive() {
            bool alive = true;
            List<ConnectionPack> alivesSnapshot = getAliveOperatorCOPY();
            foreach (ConnectionPack cp in alivesSnapshot) {
                if (!cp.Equals(myConPack)) {
                    DADStormProcess.ClientProcess replicaProcess = new DADStormProcess.ClientProcess(cp);
                    alive = alive && replicaProcess.isAlive(myConPack);
                }
            }

            return alive;
        }

        private string getIdKey(string ID) {
            string key = "";
            lock (responsabilityLinks) { // Doing a relock.. just to be sure
                foreach (KeyValuePair<string, List<string>> entry in responsabilityLinks) {
                    if (entry.Value.Contains(ID)) {
                        if (!key.Equals("")) { ProcessError("ID: " + ID + " has more than one key: " + key + " : " + entry.Key); }
                        key = entry.Key;
                    }
                }
            }
            return key;
        }

        private string getIDnextOperator(string ID) {
            return idTranslation[ID];
        }

        public void loseReplicaResponsability(string ID) {
            lock (responsabilityLinks) {
                string key = getIdKey(ID);
                ProcessDebug("Removing Responsability: " + ID);

                if (key.Equals("")) {
                    ProcessWarning("ID: " + ID + " not found in responsibles"); // Maybe i am getting this information twice..
                } else {
                    List<string> links;
                    responsabilityLinks.TryGetValue(key, out links);
                    links.Remove(ID);
                    if (links.Count == 0) { //it was last dependence
                        ProcessDebug("Last dependence of: " + key);
                        responsabilityLinks.Remove(key);
                        lock (responsability) {
                            responsability.Remove(key);
                        }
                    }
                }
            }
            lock (brotherIDsResponsible) {
                brotherIDsResponsible.Remove(ID);
            }
        }


        public void loseResponsability(string ID) {
            removeResponsabilityFromOperator(ID);
            loseReplicaResponsability(ID);
        }

        private void printResponsibleTable() {
            lock (responsabilityLinks) {
                lock (responsability) {
                    lock (printing) {
                        System.Console.WriteLine();
                        foreach (KeyValuePair<string, IList<IList<string>>> entry in responsability) {
                            string key = entry.Key;
                            List<string> links;
                            if (responsabilityLinks.TryGetValue(key, out links)) {
                                string print = "the ID " + key + " has the links <";
                                foreach (string link in links) {
                                    print += link + "[" + idTranslation[link] + "] ";
                                }
                                System.Console.WriteLine(print + ">");
                            } else {
                                ProcessWarning("key without links.. " + key);
                                IList<IList<string>> result = responsability[key];
                                foreach (IList<string> tuple in result) {
                                    ProcessWarning("Tuple: < " + String.Join(", ", tuple) + " > ");
                                }
                            }
                        }
                    }
                }
            }
        }

        public string status() {
            ProcessWarning("Processed: " + numberProcessed + " since last read");
            numberProcessed = 0;
            string status = "";
            if (this.sync) {
                ProcessWarning("SYNCING");
                status += "SYNC | ";
            }
            if (this.frozen) {
                status += "FROZEN | ";
            }
            lock (dllArgs) {
                status += "tuples waiting: " + dllArgs.Count;
            }
            if (DEBUG.PROCESS) {
                lock (filesLocation) {
                    status += " | incomplete files: " + filesLocation.Count + " : " + lastRead + "%";
                }
                lock (responsability) {
                    status += " still responsible for: " + responsability.Count;
                }
            }
            new Thread(() => {
                //if (this.frozen) {
                if (DEBUG.PROCESS) {
                    printResponsibleTable();
                }
                //}
                printAlivesTable();
            }).Start();
            return status;
        }

        private void ProcessDebug(string msg) {
            if (DEBUG.PROCESS) {
                lock (printing) {
                    System.Console.WriteLine("[ Process : " + myConPack.Port + " ] " + msg);
                }
            }
        }
        private void ProcessError(string msg) {
            //            lock (printing) {
            Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("[ Process : " + myConPack.Port + " :  ERROR ] " + msg);
            Console.ResetColor();
            //            }
        }
        private void ProcessWarning(string msg) {
            //            lock (printing) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine("[ Process : " + myConPack.Port + " : Warning ] " + msg);
            Console.ResetColor();
            //            }
        }
        public static void Main(string[] args) {

            int argsSize = args.Length;
            try {
                int numberOfParameters = 9;
                //Configuring Process
                if (argsSize >= numberOfParameters) {
                    string strPort = args[0];
                    string dllNameInputMain = args[1];
                    string classNameInputMain = args[2];
                    string methodNameInputMain = args[3];
                    string semantics = args[4];
                    string routingTechnic = args[5];
                    //Bool that indicates whether full logging or not
                    bool fullLogging = Convert.ToBoolean(args[6]);
                    string ip = args[7];
                    string operatorID = args[8];
                    string[] dllArgsInputMain = null;
                    if (argsSize > numberOfParameters) {
                        dllArgsInputMain = new string[argsSize - numberOfParameters];
                        Array.Copy(args, numberOfParameters, dllArgsInputMain, 0, argsSize - numberOfParameters);
                    }

                    ServerProcess sp = ServerProcess.Instance;
                    int parsedPort = Int32.Parse(strPort);
                    if (parsedPort < 10002 || parsedPort > 65535) {
                        throw new FormatException("Port out of possible range");
                    }
                    int semanticsInt = Int32.Parse(semantics);
                    if (semanticsInt < 0 || semanticsInt > 2) {
                        semanticsInt = 0;
                    }
                    ConnectionPack myCp = new ConnectionPack(ip, parsedPort);

                    sp.ProcessStaticArgs = dllArgsInputMain;
                    sp.buildServer(myCp, dllNameInputMain, classNameInputMain, methodNameInputMain, routingTechnic, fullLogging, semanticsInt, operatorID);

                    sp.createAndProcess();

                    Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine("ProcessServer is going OFFLINE");
                    Console.ResetColor();
                } else {
                    System.Console.WriteLine("Not everything was specified..");
                }
            } catch (FormatException e) {
                Console.WriteLine(e.Message);
            }
        }
    }
}
