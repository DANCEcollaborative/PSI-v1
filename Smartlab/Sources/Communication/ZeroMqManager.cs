using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace CMU.Smartlab.Communication
{
    public class ZeroMqManager
    {
        private string serverip;
        private RequestSocket requester;

        public ZeroMqManager()
        {

        }

        public ZeroMqManager(string serverip, int port)        {
            this.requester.Connect("tcp://" + serverip + ":" + port);
            }

        public void Connect (string ip)
        {
            this.requester.Connect(ip);
        }
        public void Send(string str)
        {
            using (var requester = new RequestSocket())
            {
                requester.Connect("tcp://127.0.0.1:5555");
                this.requester.SendFrame(str);
            }

        }

        public string Recieve()
        {
            using (var requester = new RequestSocket())
            {
                requester.Connect("tcp://127.0.0.1:5555");
                string str = this.requester.ReceiveFrameString();
                return str;
            }
        }
        }


    }

