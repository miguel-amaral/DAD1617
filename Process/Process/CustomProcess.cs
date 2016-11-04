using System;
using System.Net;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;

namespace DADStormProcess {

	public class CustomProcess : ServerProcess {
