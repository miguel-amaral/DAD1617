using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Serialization;

public static class DEBUG{
    //#define DEBUG_PROCESS 
    public const bool DAEMON  = true;
	public const bool PROCESS = false;
	public const bool PUPPET  = true;
    public const bool METRIC  = true;
}

[Serializable]
public class ConnectionPack{
	private string ip;
	private int port;

	public ConnectionPack(string ip, int port) {
		this.ip = ip;
		this.port = port;
	}

	public int Port {
		get { return port;	}
		set { port = value;	}
	}
	public string Ip {
		get { return ip;  }
		set { ip = value; }
	}
	public override string ToString(){
		return "ip: " + this.Ip + ":" + this.Port;
	}

	public override bool Equals(object obj) {
		ConnectionPack other = obj as ConnectionPack;
		return other.Port.Equals (this.Port) && other.Ip.Equals (this.Ip);
	}
	public override int GetHashCode() {
		return this.Ip.GetHashCode();
	}
}

namespace PuppetMaster {
	public class RegistryRemoteException : RemotingException, ISerializable
	{
		// ...
	}
}

public interface DADStormRemoteTupleReceiver {
	void addTuple (string senderUrl, IList<string> tuple);
}
