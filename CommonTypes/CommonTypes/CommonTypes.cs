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

namespace PuppetMaster {
	public class RegistryRemoteException : RemotingException, ISerializable
	{
		// ...
	}
}

public interface DADStormRemoteTupleReceiver {
	void addTuple (string senderUrl, IList<string> tuple);
}

public class MainClass{
	public static void Main(string[] args) {}
}

public class Default {
    int innerCounter = 0;
    List<string> seenTuples = new List<string>();

    public IList<IList<string>> Dup(IList<string> tuple)
    {
        List<IList<string>> newTuple = new List<IList<string>>();
        newTuple.Add(tuple);
        return newTuple;
    }

    public IList<IList<string>> Count(IList<string> tuple)
    {
        string toReturn = (innerCounter++).ToString();
        tuple = new List<string>();
        tuple.Add(toReturn);
        List<IList<string>> newTuple = new List<IList<string>>();
        newTuple.Add(tuple);
        return newTuple;
    }

    private bool tuploUnico(IList<string> tuple, int index) {
        string newId = tuple[index];
        if (seenTuples.Contains(newId)) {
            return false;
        } else {
            seenTuples.Add(newId);
            return true;
        }
    }

	private IList<IList<string>> emptyTuple(){
		List<IList<string>> returning = new List<IList<string>>();
		IList<string> newTuple = new List<string>();
		returning.Add(newTuple);
		return returning;
	}

    public IList<IList<string>> Uniq(IList<string> tuple) {
        int index = Int32.Parse(tuple[0]);
        tuple.RemoveAt(0);
        if ( tuploUnico(tuple,index) ) {
            return Dup(tuple);
        } else {
			return emptyTuple();
        }
    }

	//	FILTER field number, condition, value
	public IList<IList<string>> Filter(IList<string> tuple) {
		int index = Int32.Parse(tuple[0]);
        tuple.RemoveAt(0);
		string condition = tuple[0];
		tuple.RemoveAt(0);
		string staticValue = tuple[0];
		tuple.RemoveAt(0);
		string dinamicValue = tuple[index];

		if(condition.Equals(">")){
			if( staticValue > dinamicValue ){
				return Dup(tuple);
			}
		} else if (condition.Equals("<")){
			if( staticValue < dinamicValue ){
				return Dup(tuple);
			}
		} else if (condition.Equals("="){
			if( staticValue == dinamicValue ){
				return Dup(tuple);
			}
		}
		return emptyTuple();
}
