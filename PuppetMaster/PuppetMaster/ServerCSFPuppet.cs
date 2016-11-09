using System;
using System.Net;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using System.Threading;
using System.Collections.Generic;


namespace PuppetMaster {
	public class ServerCSFPuppet {
		public virtual void extraCommands(string[] command) {
			if (splitStr [0].Equals ("report", StringComparison.OrdinalIgnoreCase)) {

			} else if (splitStr [0].Equals ("restart", StringComparison.OrdinalIgnoreCase)) {

			}
		}

		
	}
}
