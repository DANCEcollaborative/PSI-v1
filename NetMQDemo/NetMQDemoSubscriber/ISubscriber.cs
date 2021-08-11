
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQDemoSubscriber
{
    public interface ISubscriber:IDisposable
    {

        event Action<string, string> Nofity;


        void RegisterSubscriber(List<string> topics);


        void RegisterSbuscriberAll();

        void RemoveSbuscriberAll();
    }
}
