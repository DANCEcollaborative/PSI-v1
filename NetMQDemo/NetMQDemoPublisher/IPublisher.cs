
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQDemoPublisher
{
    public interface IPublisher:IDisposable
    {
        void Publish(string topicName, string data);
    }
}
