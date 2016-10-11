using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace PuppetMaster
{
    public class Client
    {
        private static int port = 10000;
        public static void Main(string[] args)
        {
            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(MyChatRemoteServerObject),
                "MyChatRemoteServerObject",
                WellKnownObjectMode.Singleton);

            System.Console.WriteLine("Server port is: " + port);
            System.Console.WriteLine("<enter> to exit...");
            System.Console.ReadLine();
        }
    }
}

using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Windows.Forms;

namespace PuppetMaster
{
	class ClientManager
	{
		private PuppetMasterRemoteServerObject remoteObj;

        public void connect() {
            remoteObj = (MyChatRemoteServerObject)Activator.GetObject(
                typeof(MyChatRemoteServerObject),
                "tcp://localhost:8086/MyChatRemoteServerObject");

            if (remoteObj == null) throw new SocketException();

            myClientObj = new MyChatRemoteClientObject(form, d);
            RemotingServices.Marshal(myClientObj, "MyChatRemoteClientObject",
                typeof(MyChatRemoteClientObject));

			try {
					remoteObj.reg(nick, "tcp://localhost:" + possiblePort + "/MyChatRemoteClientObject", possiblePort);
			}catch(RegistryRemoteException e) {
				MessageBox.Show("Nickname already exists.\r\nError: " + e.Message);
				remoteObj = null;
				return;
			}
			mNick = nick;
		}

        public string ping()
        {
            if (remoteObj != null)
            {
                string res;
                try
                {
                    res = remoteObj.ping();
                }
                catch (SocketException e)
                {
                    return "Error: " + e.Message;
                }
                return res;
            }
            else
                return "You did not connect the chat.";
        }
        public void sendMsg(string msg)
        {
            if (remoteObj != null) remoteObj.sendMsg(mNick, msg);
        }

        public void unreg()
        {
            try { if (remoteObj != null) remoteObj.unreg(mNick); }
            catch(SocketException ex)
            {
                return;
            }
        }

        private bool CheckAvailableServerPort(int port)
        {
            bool isAvailable = true;

            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endpoint in tcpConnInfoArray)
            {
                if (endpoint.Port == port)
                {
                    isAvailable = false;
                    break;
                }
            }

            return isAvailable;
        }
    }
}
