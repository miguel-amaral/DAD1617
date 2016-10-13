all:
	gmcs -t:library hello.cs
	gmcs -t:library ./CommonTypes/CommonTypes.cs -r:System.Runtime.Remoting.dll
	#PuppetMaster
	gmcs -t:library ./PuppetMaster/RemotePuppet.cs -r:System.Runtime.Remoting.dll
	gmcs -t:library ./PuppetMaster/ClientPuppet.cs -r:PuppetMaster/RemotePuppet.dll,System.Runtime.Remoting.dll
	gmcs ./PuppetMaster/ClientPuppet.cs -r:PuppetMaster/RemotePuppet.dll,System.Runtime.Remoting.dll
	gmcs ./PuppetMaster/ServerPuppet.cs -r:PuppetMaster/RemotePuppet.dll,System.Runtime.Remoting.dll
	#Daemon
	gmcs ./Daemon/ServerDaemon.cs -r:System.Runtime.Remoting.dll,PuppetMaster/ClientPuppet.dll,mscorlib.dll ./Daemon/RemoteDaemon.cs
	gmcs -t:library ./Daemon/RemoteDaemon.cs -r:System.Runtime.Remoting.dll,PuppetMaster/ClientPuppet.dll ./Daemon/ServerDaemon.cs
	gmcs -t:library ./Daemon/ClientDaemon.cs -r:System.Runtime.Remoting.dll,Daemon/RemoteDaemon.dll
	gmcs ./Daemon/ClientDaemon.cs -r:System.Runtime.Remoting.dll,Daemon/RemoteDaemon.dll
	#Process
	gmcs -t:library ./Process/RemoteProcess.cs -r:System.Runtime.Remoting.dll
	gmcs -t:library ./Process/ClientProcess.cs -r:Process/RemoteProcess.dll,System.Runtime.Remoting.dll
	gmcs ./Process/ClientProcess.cs -r:Process/RemoteProcess.dll,System.Runtime.Remoting.dll
	gmcs ./Process/ServerProcess.cs -r:Process/RemoteProcess.dll,System.Runtime.Remoting.dll



clean:
	bash deleteFiles
