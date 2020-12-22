using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Linq;

namespace CSScriptLib
{
    class BuildServer
    {
    
        public static int serverPort = 17001;

        static public string Request(string request)
        {
            using (var clientSocket = new TcpClient())
            {
                clientSocket.Connect(IPAddress.Loopback, serverPort);
                clientSocket.WriteAllBytes(request.GetBytes());
                return clientSocket.ReadAllBytes().GetString();
            }
        }

        static public string SendBuildRequest(string[] args)
        {
            try
            {
                // first arg is the compiler identifier: csc|vbc

                string request = string.Join("\n", args.Skip(1));
                string response = BuildServer.Request(request);

                return response;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
    }
}
