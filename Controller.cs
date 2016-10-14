using System;
using System.Threading;
public class Controller {

	public static void Main(string[] args) {

	  //string pc1   = "lab7p2";
		string pc2   = "lab7p8";
		string port1 = "42154" ;
		string port2 = "42155" ;
		string arg1 = "olá";
		string arg2 = "mundo!";
		string[] argumentos = { arg1 , arg2 };

//		Daemon.ClientDaemon daemon1 = new Daemon.ClientDaemon();
//		daemon1.connect("10001",pc1);
//		System.Console.WriteLine("daemon 1: "+daemon1.ping());

		Daemon.ClientDaemon daemon2 = new Daemon.ClientDaemon();
		daemon2.connect("10001",pc2);
		System.Console.WriteLine("daemon 2: "+daemon2.ping());

	//	daemon1.newThread( "hello.dll" , "Hello" , "Hello0", port1);
		daemon2.newThread( "hello.dll" , "Hello" , "Main", port2, argumentos);
//		daemon2.newThread( "hello.dll" , "Hello" , "Main", port1, argumentos);

//		Thread.Sleep(10000);


	//	DADStormProcess.ClientProcess process1 = new DADStormProcess.ClientProcess();
		DADStormProcess.ClientProcess process2 = new DADStormProcess.ClientProcess();
		DADStormProcess.ClientProcess process3 = new DADStormProcess.ClientProcess();

	//	process1.connect(port1,pc1);
		process2.connect(port1,pc2);
	//	process3.connect(port2,pc2);

	//	System.Console.WriteLine("process 1: "+process1.ping());
		System.Console.WriteLine("process 2: "+process2.ping());
	//	System.Console.WriteLine("process 3: "+process3.ping());

		System.Console.WriteLine("Hello, World!");
	}
}
