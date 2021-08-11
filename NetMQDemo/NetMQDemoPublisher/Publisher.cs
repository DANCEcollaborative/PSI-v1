using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace NetMQDemoPublisher
{
    public class Publisher:IPublisher
    {
        private object _lockObject = new object();

        private PublisherSocket _publisherSocket;

        public Publisher(string endPoint)
        {
            _publisherSocket = new PublisherSocket();
            _publisherSocket.Options.SendHighWatermark = 1000;
            _publisherSocket.Bind(endPoint);
        }
        #region Implementation of IDisposable

        public void Dispose()
        {
            lock (_lockObject)
            {
                _publisherSocket.Close();
                _publisherSocket.Dispose();
            }
        }


        public void Publish(string topicName, string data)
        {
            lock (_lockObject)
            {
                _publisherSocket.SendMoreFrame(topicName).SendFrame(data);
            }
        }

        #endregion
    }
}
