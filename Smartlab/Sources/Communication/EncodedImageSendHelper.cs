using Microsoft.Psi;
using Microsoft.Psi.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMU.Smartlab.Communication
{
    public class EncodedImageSendHelper
    {
        public volatile CommunicationManager manager;
        public string Name;
        public string SendingTopic;
        public object Lock;
        private DateTime frameTime = new DateTime(0);

        public string SizeTopic
        {
            get
            {
                return $"{SendingTopic}_Size";
            }
        }

        public string PropertyTopic
        {
            get
            {
                return $"{SendingTopic}_Prop";
            }
        }

        public string ImageTopic
        {
            get
            {
                return $"{SendingTopic}_Image";
            }
        }

        public EncodedImageSendHelper(CommunicationManager manager, string name, string topic, object sendingLock)
        {
            this.Name = name;
            this.SendingTopic = topic;
            this.Lock = sendingLock;
            this.manager = manager;
        }

        public void SendImage(Shared<EncodedImage> image, Envelope envelope)
        {
            EncodedImage rawData = image.Resource;
            Task task = new Task(() =>
            {
                lock (this.Lock)
                {
                    try
                    {
                        this.manager.SendText(this.SizeTopic, $"{rawData.Width}:{rawData.Height}");
                        this.manager.SendText(this.PropertyTopic, $"camera_id:str:{this.Name}");
                        this.manager.SendText(this.PropertyTopic, $"timestamp:int:{this.frameTime.Ticks}");
                        this.manager.SendText(this.PropertyTopic, $"END");
                        this.manager.SendText(this.ImageTopic, Convert.ToBase64String(rawData.GetBuffer()));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                    finally
                    {
                        System.Threading.Thread.Sleep(50);
                        this.manager.Occupied = false;
                    }
                }
            });
            if (!this.manager.Occupied && envelope.OriginatingTime.CompareTo(this.frameTime) > 0)
            {
                this.manager.Occupied = true;
                frameTime = envelope.OriginatingTime;
                task.Start();
            }
        }
    }
}
