
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace NetMQDemoSubscriber
{
    public class Subscriber:ISubscriber
    {
        private SubscriberSocket _subscriberSocket = null;
        private string _endpoint = @"tcp://127.0.0.1:9876";

        public Subscriber(string endPoint)
        {
            _subscriberSocket = new SubscriberSocket();
            _endpoint = endPoint;
        }
        #region Implementation of IDisposable

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Implementation of ISubscriber

        public event Action<string, string> Nofity = delegate { };

        /// <summary>
        /// 注册订阅主题
        /// </summary>
        /// <param name="topics"></param>
        public void RegisterSubscriber(List<string> topics)
        {
            InnerRegisterSubscriber(topics);
        }

        /// <summary>
        /// 注册订阅
        /// </summary>
        public void RegisterSbuscriberAll()
        {
            InnerRegisterSubscriber();
        }

        /// <summary>
        /// 移除所有订阅消息，并关闭
        /// </summary>
        public void RemoveSbuscriberAll()
        {
            InnerStop();
        }

        #endregion

        #region 内部实现


        private void InnerRegisterSubscriber(List<string> topics = null)
        {
            InnerStop();
            _subscriberSocket = new SubscriberSocket();
            _subscriberSocket.Options.ReceiveHighWatermark = 1000;
            _subscriberSocket.Connect(_endpoint);
            if (null == topics)
            {
                _subscriberSocket.SubscribeToAnyTopic();
            }
            else
            {
                topics.ForEach(item => _subscriberSocket.Subscribe(item));
            }
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    string messageTopicReceived = _subscriberSocket.ReceiveFrameString();
                    string messageReceived = _subscriberSocket.ReceiveFrameString();
                    Nofity(messageTopicReceived, messageReceived);
                }
            });
        }

        private void InnerStop()
        {
            _subscriberSocket.Close();
        }

        #endregion
    }
}
