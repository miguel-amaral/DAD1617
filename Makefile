all:
	#PuppetMaster
	gmcs -t:library ./PuppetMaster/CommonTypes/CommonTypes.cs -r:System.Runtime.Remoting.dll
	gmcs -t:library ./PuppetMaster/PuppetClient.cs -r:PuppetMaster/CommonTypes/CommonTypes.dll,System.Runtime.Remoting.dll
	gmcs ./PuppetMaster/PuppetServer.cs -r:PuppetMaster/CommonTypes/CommonTypes.dll,System.Runtime.Remoting.dll
	#Daemon
	gmcs ./Daemon/ServerDaemon.cs -r:System.Runtime.Remoting.dll,PuppetMaster/PuppetClient.dll ./Daemon/RemoteDaemon.cs
	gmcs -t:library ./Daemon/RemoteDaemon.cs -r:System.Runtime.Remoting.dll,PuppetMaster/PuppetClient.dll ./Daemon/ServerDaemon.cs
	gmcs -t:library ./Daemon/ClientDaemon.cs -r:System.Runtime.Remoting.dll,Daemon/RemoteDaemon.dll
	gmcs ./Daemon/ClientDaemon.cs -r:System.Runtime.Remoting.dll,Daemon/RemoteDaemon.dll


clean:
	bash deleteFiles
