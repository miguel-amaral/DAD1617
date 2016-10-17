using System;
using System.Threading;
using System.Runtime.Remoting;

public class Controller {

	public static void Main(string[] args) {

		System.Console.WriteLine("Hello, World! Im Controller and I will be your captain today");
	  //string pc1   = "lab7p2";
		string pc2   = "127.0.0.1";
		string port1 = "42154" ;
		string port2 = "42155" ;
		string arg1 = "olá";
		string arg2 = "mundo!";
		string[] argumentos = { arg1 , arg2 };

//		Daemon.ClientDaemon daemon1 = new Daemon.ClientDaemon();
//		daemon1.connect("10001",pc1);
//		System.Console.WriteLine("daemon 1: "+daemon1.ping());

		Daemon.ClientDaemon daemon2 = new Daemon.ClientDaemon();
		try {
			daemon2.connect("10001",pc2);
			System.Console.WriteLine("daemon 2: "+daemon2.ping());

			//	daemon1.newThread( "hello.dll" , "Hello" , "Hello0", port1);
			daemon2.newThread( "hello.dll" , "Hello" , "Hello2", port2, argumentos);
			argumentos = new string[2];
			argumentos[0] = arg2 ;
			argumentos[1] = arg1 ;
//			daemon2.newThread( "hello.dll" , "Hello" , "Hello2", port1, argumentos);
			//		daemon2.newThread( "hello.dll" , "Hello" , "Main", port1, argumentos);


			//Allow for the creation of services
			Thread.Sleep(1000);


			//	DADStormProcess.ClientProcess process1 = new DADStormProcess.ClientProcess();
			DADStormProcess.ClientProcess process2 = new DADStormProcess.ClientProcess();
			//DADStormProcess.ClientProcess process3 = new DADStormProcess.ClientProcess();

			//	process1.connect(port1,pc1);
			process2.connect(port2,pc2);
			Thread.Sleep(500);
			process2.addTuple(argumentos);
			Thread.Sleep(500);
			process2.addTuple(argumentos);
			Thread.Sleep(500);
			process2.addTuple(argumentos);
			Thread.Sleep(500);
			process2.addTuple(argumentos);
			Thread.Sleep(500);
			process2.addTuple(argumentos);
			Thread.Sleep(500);
			process2.addTuple(argumentos);
			Thread.Sleep(500);
			process2.addTuple(argumentos);
			Thread.Sleep(500);
			process2.addTuple(argumentos);
			process2.defreeze();
//			process3.connect(port2,pc2);

			//	System.Console.WriteLine("process 1: "+process1.ping());
			System.Console.WriteLine("process 2: "+process2.ping());
//			System.Console.WriteLine("process 3: "+process3.ping());
		} catch (RemotingException e) {
			Console.ForegroundColor = ConsoleColor.Red;
			System.Console.WriteLine("Connection failed, you sure Daemon Server is online at ip: " + pc2 + " ? ");
			System.Console.WriteLine("Reason: " + e.Message );
			Console.ResetColor();
		}


		System.Console.WriteLine("Goodbye World! It was a pleasure to serve you today");
	}
}
