using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetMQDemoSubscriber
{
    public partial class SubscriberForm : Form
    {
        private ISubscriber subscriber;
        public SubscriberForm()
        {
            InitializeComponent();
        }

        private void SubscriberForm_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            subscriber = new Subscriber("tcp://127.0.0.1:8888");
            subscriber.RegisterSbuscriberAll();
            subscriber.Nofity+= delegate(string s, string s1)
            {
                ListViewItem item = new ListViewItem(string.Format("topic:{0},Data:{1}", s, s1));
                listView1.Items.Add(item);
            };
        }
    }
}
