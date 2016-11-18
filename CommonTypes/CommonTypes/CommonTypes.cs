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

[Serializable]
public class CSF_metric {
    private string metric;
    private Dictionary<string, int> sinners;
	//String is source ip; hastable key -> destIp , value -> size/number of communications
	protected Dictionary<string,Hashtable> rawValues;

    public CSF_metric(string metric, Dictionary<string, int> sinners) {
        this.Metric = metric;
        this.Sinners = sinners;
    }

    public CSF_metric(string metric, Dictionary<string,Hashtable> sinners) {
        this.Metric = metric;
        this.Sinners = sinners;
    }

    public string Metric {
        get {
            return metric;
        }
        set {
            metric = value;
        }
    }

    public Dictionary<string, int> Sinners {
        get {
            return sinners;
        }
        set {
            sinners = value;
        }
    }

    public Dictionary<string, Hashtable> RawValues {
        get {
            return rawValues;
        }
        set {
            rawValues = value;
        }
    }

	void acept(MetricVisitor visitor){
		visitor.visit(this);
	}

	bool aceptWithBool(MetricVisitor visitor){
		return visitor.visitWithBool(this);
	}
}

public interface MetricVisitor {
	bool visitWithBool(CSF_metric);
	void visit(CSF_metric);
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
