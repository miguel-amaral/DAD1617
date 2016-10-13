using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Serialization;

public class ConnectionPack{
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
	public class RegistryRemoteException : RemotingException, ISerializable
	{
		// ...
	}
}
