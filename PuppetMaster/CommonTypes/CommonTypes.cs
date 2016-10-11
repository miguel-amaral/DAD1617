using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;

public class ConnectionPack {
	private string url;
	private int port;

	public ConnectionPack(string url, int port) {
		this.url = url;
		this.port = port;
	}

	public int Port {
		get { return port;	}
		set { port = value;	}
	}
	public string Url {
		get { return url;	}
		set { url = value;	}
	}
}

namespace PuppetMaster {
	public class PuppetMasterRemoteServerObject : MarshalByRefObject {
		private SortedDictionary<string, ConnectionPack> connectedDaemons = new SortedDictionary<string, ConnectionPack>();

		public string ping() {
			Console.WriteLine("Ping");
			return "Pong";
		}

		public void register(string url, int port) {
			string key = url+":"+port;
			if (connectedDaemons.ContainsKey(key)){
				Console.WriteLine("A client at : " + key + " already exists.");
				throw new RegistryRemoteException();
			} else {
				Console.WriteLine("A new client as been registered at: " + key);
				lock (connectedDaemons) {
					connectedDaemons.Add(nick, new ConnectionPack(url, port));
				}
			}
		}

		public void unregister(string url, int port)	{
			string key = url+":"+port;
			Console.WriteLine("Client at: " + key + " is leaving.");
			lock (connectedDaemons) {
				connectedDaemons.Remove(nick);
			}
		}

		public string list(){
			string daemons = "";
			lock (connectedDaemons) {
				foreach (ConnectionPack element in connectedDaemons) {
					string daemon = element.Url + " " + element.Port;
					daemons = daemons + daemon + "\r\n";
				}
			}
		}
	}
}
