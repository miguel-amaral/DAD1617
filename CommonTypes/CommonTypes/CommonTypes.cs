using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Serialization;

public static class DEBUG{
	public const bool DAEMON  = true;
	public const bool PROCESS = true;
	public const bool PUPPET  = true;
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
		return "ip: " + this.Ip + " : " + this.Port;
	}
}

namespace PuppetMaster {
	public class RegistryRemoteException : RemotingException, ISerializable
	{
		// ...
	}
}

public class MainClass{
	public static void Main(string[] args) {}
}

