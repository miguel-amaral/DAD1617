all:
	gmcs -t:library ./PuppetMaster/CommonTypes/CommonTypes.cs -r:System.Runtime.Remoting.dll
	gmcs ./PuppetMaster/PuppetServer.cs -r:PuppetMaster/CommonTypes/CommonTypes.dll,System.Runtime.Remoting.dll
	gmcs ./PuppetMaster/PuppetClient.cs -t:library -r:PuppetMaster/CommonTypes/CommonTypes.dll,System.Runtime.Remoting.dll

clean:
	bash deleteFiles
