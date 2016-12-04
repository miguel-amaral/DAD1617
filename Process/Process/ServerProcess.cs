using System;
using System.Net;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;

namespace DADStormProcess {

    public class ServerProcess {

        private static ServerProcess instance = null;
        private int milliseconds = 0;
        private decimal lastRead = 100;
        private bool frozen = true;
        private bool fullLog = false;
        private bool primmary = true;
        private IList<string> processStaticArgs;
        private RoutingTechinic routTechnic;
        private GenerateStrategy generateStrategy;

        private string operatorID;
        private ConnectionPack myConPack;
        private ConnectionPack puppetMasterConPack;

        private DADStormRemoteTupleReceiver puppetRemote;
        private ProcessRemoteServerObject myServerObj;
        private Queue<IList<string>> dllArgs = new Queue<IList<string>>();

        private List<List<ConnectionPack>> downStreamNodes = new List<List<ConnectionPack>>();
        private List<ConnectionPack> operatorReplicas;
        private List<string> filesLocation = new List<string>();
        private List<string> filesToRemove = new List<string>();

        private Dictionary<string, string[]> filesContent = new Dictionary<string, string[]>();
        private Dictionary<string, int> filesIndex = new Dictionary<string, int>();
        private Dictionary<string, IList<IList<string>>> processedIDs = new Dictionary<string, IList<IList<string>>>();
        private Dictionary<string, IList<IList<string>>> responsability = new Dictionary<string, IList<IList<string>>>();

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
                                } continue; // try again
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

                        string id = this.operatorID + fileLocation + index.ToString();
                        toAdd = insertMetadataOnRead(id, toAdd);
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

                List<string> metadata = getMetadata(nextTuple);
                string ip = metadata[0];
                string port = metadata[1];
                string oldID = metadata[2];

                ConnectionPack previous = new ConnectionPack(ip, Int32.Parse(port));

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
            toReturn.Add("");
            toReturn.Add("0");
            toReturn.Add("0.0.0.0");
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
            toReturn.Add(oldID);
            toReturn.Add(port);
            toReturn.Add(ip);
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
            toReturn.Add(oldID);
            toReturn.Add(port);
            toReturn.Add(ip);
            return toReturn;
        }
        private delegate List<string> InsertMetadata(string oldID, List<string> tuple, int index);
        private InsertMetadata insertMetadata;
        private List<string> insertMetadataAtMost(string oldID, List<string> tuple, int index) {
            return tuple;
        }
        private List<string> insertMetadataAtLeast(string oldID, List<string> tuple, int index) {
            string newID = oldID;
            if (index > -1) {
                newID = oldID + index;
            }
            tuple.Insert(0, newID);
            string ip = MyConPack.Ip;
            string port = MyConPack.Port.ToString();

            tuple.Insert(0, port);
            tuple.Insert(0, ip);
            return tuple;
        }
        private List<string> insertMetadataExactly(string oldID, List<string> tuple, int index) {
            string newID = oldID;
            if (index > -1) {
                newID = oldID + index;
            }
            tuple.Insert(0, newID);
            string ip = MyConPack.Ip;
            string port = MyConPack.Port.ToString();

            tuple.Insert(0, port);
            tuple.Insert(0, ip);
            return tuple;
        }
        private delegate List<string> InsertMetadataOnRead(string ID, List<string> tuple);
        private InsertMetadataOnRead insertMetadataOnRead;
        private List<string> insertMetadataOnReadAtMost(string ID, List<string> tuple) {
            return tuple;
        }
        private List<string> insertMetadataOnReadAtLeast(string ID, List<string> toAdd) {
            toAdd.Insert(0, ID);
            toAdd.Insert(0, "0");
            toAdd.Insert(0, "0.0.0.0");
            return toAdd;
        }
        private List<string> insertMetadataOnReadExactly(string ID, List<string> toAdd) {
            toAdd.Insert(0, ID);
            toAdd.Insert(0, "0");
            toAdd.Insert(0, "0.0.0.0");
            return toAdd;
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

        public void receiveReplicaBackup(string oldID, IList<IList<string>> result) {
            processedIDs.Add(oldID, result); //Assure only once
            int index = 0;
            foreach (List<string> tuple in result) {
                index++;
                //Lets ignore empty tuples shall we
                if (tuple.Count > 0) {
                    string newID = oldID;
                    if (result.Count > 1) {
                        newID = oldID + index;
                    }
                    //tuple.Insert(0, newID);
                    IList<IList<string>> toBeResponsible = new List<IList<string>>();
                    toBeResponsible.Add(tuple);
                    lock (responsability) {
                        responsability.Add(newID, toBeResponsible);
                    }
                }
            }
        }

        private void doBackup(string oldID, IList<IList<string>> result) {
            foreach (ConnectionPack replica in operatorReplicas) {
                if (!replica.Equals(myConPack)) {
                    DADStormProcess.ClientProcess replicaProcess = new DADStormProcess.ClientProcess(replica);
                    replicaProcess.receiveReplicaBackup(oldID, result);
                } else {
                    this.receiveReplicaBackup(oldID, result);
                }
            }
        }

        private void assumeResponsability(ConnectionPack previous, string oldID) {
            if(previous.Port != 0) {
                DADStormProcess.ClientProcess previousProcess = new DADStormProcess.ClientProcess(previous);
                previousProcess.loseResponsability(oldID);
            }
        }


        /**
		  * After processing tuple this method emits it to downStream and puppet master
		  */
        private void emitTuple(ConnectionPack previous, string oldID, IList<IList<string>> result){
            assureSemanticsOnEmit(previous,oldID,result);
            

            int index = -1;
            foreach (List<string> tuple in result) {
                if(result.Count > 1) index++;
                //Lets ignore empty tuples shall we
                if (tuple.Count > 0) {
                    List<string> tupleToSend = insertMetadata(oldID, tuple, index);
                    ProcessDebug("Another tuple generated: <" + String.Join(", ", tupleToSend) + ">");
                    if (fullLog == true) {
                        logToPuppetMaster(tupleToSend);
                    }
                    sendToNextOperators(tupleToSend);
                }
            }
		}

		/**
		  * In the full logging mode, all tuple emissions need to be reported to Puppetmaster
		  */
		private void logToPuppetMaster (IList<string> tuple) {
			if(puppetRemote == null) {
				puppetRemote = (DADStormRemoteTupleReceiver)Activator.GetObject(
					typeof(DADStormRemoteTupleReceiver), "tcp://" + puppetMasterConPack.Ip  + ":" + puppetMasterConPack.Port + "/PuppetMasterRemoteServerObject");
			}
			puppetRemote.addTuple ("[ " + myConPack.Ip +":" +myConPack.Port+ " ]", tuple);
			ProcessDebug("Puppet Master informed");
		}

		/**
		  * Method that sends a tuple to every downstream operator
		  */
		private void sendToNextOperators (IList<string> tuple) {

			if (downStreamNodes.Count > 0) {
				//Foreach operator
				foreach(List<ConnectionPack> receivingOperator in downStreamNodes){
					ConnectionPack nextOperatorCp = this.RoutTechnic.nextDestination(receivingOperator, tuple);
					DADStormProcess.ClientProcess nextProcess = new DADStormProcess.ClientProcess (nextOperatorCp);
					nextProcess.addTuple (tuple);
					ProcessDebug (nextOperatorCp + " received tuple");
				}
			} else {
				ProcessDebug ("No one to send tuple to :(");
				//Case where there is no one to receive..
				//probably its last operator
			}
		}

        private void removeResponsabilityFromOperator(string ID) {
            //Do not do remote call on ourselves FIXME TODO
            foreach (ConnectionPack replica in operatorReplicas) {
                if (!replica.Equals(myConPack)) {
                    DADStormProcess.ClientProcess replicaProcess = new DADStormProcess.ClientProcess(replica);
                    replicaProcess.loseResponsability(ID);
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
        public void buildServer(ConnectionPack myCp, string dllName, string className, string methodName, string routingTechnic, bool fullLogging, int semantics, string operatorID)
        {
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

            if (semantics == 0) {
                //at most  once
                getMetadata = getMetadataAtMost;
                insertMetadata = insertMetadataAtMost;
                insertMetadataOnRead = insertMetadataOnReadAtMost;
                assureSemanticsOnEmit = assureSemanticsOnEmitAtMost;
            } else if (semantics == 1) {
                //at least once
                getMetadata = getMetadataAtLeast;
                insertMetadata = insertMetadataAtLeast;
                insertMetadataOnRead = insertMetadataOnReadAtLeast;
                assureSemanticsOnEmit = assureSemanticsOnEmitAtLeast;
            } else if (semantics == 2) {
                //exactly  once
                getMetadata = getMetadataExactly;
                insertMetadata = insertMetadataExactly;
                insertMetadataOnRead = insertMetadataOnReadExactly;
                assureSemanticsOnEmit = assureSemanticsOnEmitExactly;
            }
        }

        public void addDownStreamOperator(List<ConnectionPack> cp){
			ProcessDebug ("Down Stream Op added: " + cp);
			downStreamNodes.Add ( cp );
		}

		public void crash() {
            new Thread(() => {
                Thread.Sleep(100); //allow remote method to return
                Environment.Exit(1);
            }).Start();
            return;
		}

		public void freeze() {
			this.frozen = true;
		}

		public void defreeze() {
			this.frozen = false;
			lock(dllArgs) {
				Monitor.Pulse(dllArgs);
			}
		}

		//Public that must be called by
		public int getIndexFromPrimmary (string file) {
			if (primmary) {
				lock (filesIndex) {
					int counter;
					if (!filesIndex.TryGetValue (file, out counter)) {
						counter = 0;
						filesIndex.Add (file, counter);
					}
					filesIndex [file] = counter + 1;
					return counter;
				}
			}
			return -1;
		}

		//Method that adds a file that this replica will read
		public void addFile(string file){
			lock(filesLocation){
				filesLocation.Add (file);
			}
			lock(dllArgs){
				Monitor.Pulse (dllArgs);
			}
		}

		/**
		  * method that adds a tuple to be processed
		  */
		public void addTuple(IList<string> nextArg) {
			lock (dllArgs) {
				dllArgs.Enqueue( nextArg );
				Monitor.Pulse(dllArgs);
			}
		}

		/// <summary>
		/// Method that will be in loop (passively) and will be processing input
		/// </summary>
		public void createAndProcess() {

			TcpChannel channel = new TcpChannel(myConPack.Port);
			ChannelServices.RegisterChannel(channel, false);
			myServerObj = new ProcessRemoteServerObject();
			RemotingServices.Marshal(myServerObj, "op",typeof(ProcessRemoteServerObject));

			Console.ForegroundColor = ConsoleColor.Green;
			System.Console.WriteLine("ProcessServer is ONLINE: port is: " + myConPack.Port);
			Console.ResetColor();

			executeProcess();
		}

		//CSF method
		public MemoryStream reportBack(){
			return this.generateStrategy.reportBack ();
		}

		//CSF method
		public void reset(){
			this.generateStrategy.reset ();
		}

        public void loseResponsability(string ID) {
            lock(responsability) {
                responsability.Remove(ID);
            }
        }

        public string status ()	{
			string status = "";
			if(this.frozen) {
				status += "FROZEN | ";
			}
			lock (dllArgs) {
				status += "tuples waiting: " + dllArgs.Count;
			}
			lock (filesLocation) {
                status += " | incomplete files: " + filesLocation.Count + " : " +lastRead+"%";
            }
            lock(responsability) {
                status += " still responsible for: " + responsability.Count;
            }
			return status;
		}

		private void ProcessDebug(string msg) {
			if(DEBUG.PROCESS){
				System.Console.WriteLine("[ Process : " +myConPack.Port + " ] " + msg);
			}
		}
		private void ProcessError(string msg) {
			Console.ForegroundColor = ConsoleColor.Red;
			System.Console.WriteLine("[ Process : " +myConPack.Port + " : ERROR ] " + msg);
			Console.ResetColor();
		}
		public static void Main(string[] args) {

			int argsSize = args.Length;
			try {
				int numberOfParameters = 9;
				//Configuring Process
				if (argsSize >= numberOfParameters) {
					string strPort = args[0];
					string dllNameInputMain    = args[1];
					string classNameInputMain  = args[2];
					string methodNameInputMain = args[3];
					string semantics = args[4];
                    string routingTechnic      = args[5];
					//Bool that indicates whether full logging or not
					bool   fullLogging         = Convert.ToBoolean(args[6]);
                    string ip = args[7];
                    string operatorID = args[8];
					string[] dllArgsInputMain = null;
					if (argsSize > numberOfParameters) {
						dllArgsInputMain = new string[argsSize - numberOfParameters];
						Array.Copy(args, numberOfParameters, dllArgsInputMain, 0, argsSize - numberOfParameters );
					}

					ServerProcess sp = ServerProcess.Instance;
					int parsedPort = Int32.Parse(strPort);
					if(parsedPort < 10002 || parsedPort > 65535) {
						throw new FormatException("Port out of possible range");
					}
                    int semanticsInt = Int32.Parse(semantics);
                    if(semanticsInt < 0 || semanticsInt > 2)
                    {
                        semanticsInt = 0;
                    }
                    ConnectionPack myCp = new ConnectionPack(ip,parsedPort);

					sp.ProcessStaticArgs = dllArgsInputMain;
					sp.buildServer(myCp, dllNameInputMain, classNameInputMain, methodNameInputMain, routingTechnic, fullLogging, semanticsInt, operatorID);

					sp.createAndProcess();

					Console.ForegroundColor = ConsoleColor.Red;
					System.Console.WriteLine("ProcessServer is going OFFLINE" );
					Console.ResetColor();
				} else {
					System.Console.WriteLine("Not everything was specified.." );
				}
			} catch (FormatException e) {
				Console.WriteLine(e.Message);
			}
		}
	}
}
