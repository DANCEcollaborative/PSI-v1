using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetMQDemoPublisher
{
    public partial class PublisherForm : Form
    {
        private IPublisher publisher;
        public PublisherForm()
        {
            InitializeComponent();
            publisher = new Publisher("tcp://127.0.0.1:8888");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string strContent = textBox1.Text;
            ListViewItem item = new ListViewItem(string.Format("topic:NetMQ,Data:{0}",  strContent));
            listView1.Items.Add(item);
            publisher.Publish("NetMQ", strContent);
        }
    }
}
