using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace RemotingSample {

	class Daemon {

		static void Main(string[] args) {

			TcpChannel channel = new TcpChannel(10001);
			//ChannelServices.RegisterChannel(channel,false);

			//RemotingConfiguration.RegisterWellKnownServiceType(	typeof(MyRemoteObject),"MyRemoteObjectName",WellKnownObjectMode.Singleton);
			PuppetClient pc = new PuppetMaster.PuppetClient();
			pc.connect("localhost");
			System.Console.WriteLine("<enter> para sair...");
			System.Console.ReadLine();
		}
	}
}
