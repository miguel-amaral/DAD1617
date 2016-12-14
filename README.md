# DAD1617
Project for DAD course

#Instructions
Launch Daemon.exe as the PCS (under $RootFolder\Daemon\Daemon\bin\Release)

config file is $RootFolder\PuppetMaster\PuppetMaster\first_config.txt
Launch PuppetServer.exe ( under $RootFolder\PuppetMaster\PuppetMaster\bin\Release with config file as first and only argument )

some configurations files are included in PuppetMaster\PuppetMaster\

Not implemented:
no tolerance failure is given in case of the reading operator crashes
	-if it is the primmary all following tuples will not be read 

