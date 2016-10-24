using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Serialization;

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


