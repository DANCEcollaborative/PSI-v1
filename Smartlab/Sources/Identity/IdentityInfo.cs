using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
namespace CMU.Smartlab.Identity
{
    public class IdentityInfo: IComparable
    {
        public DateTime timestamp 
        {
            get; set;
        }

        public String identity
        {
            get; set;
        }
            
        Point3D position
        {
            get; set;
        }

        public IdentityInfo()
        {

        }

        public IdentityInfo(DateTime timestamp, String identity, Point3D position)
        {
            this.timestamp = timestamp;
            this.identity = identity;
            this.position = position;
        }

        public static IdentityInfo Parse(long timestamp, string info)
        {
            string[] raw_info = info.Split('&');
            string id = raw_info[0];
            string[] raw_pos = raw_info[1].Split(':');
            double x = double.Parse(raw_pos[0]);
            double y = double.Parse(raw_pos[1]);
            double z = double.Parse(raw_pos[2]);
            return new IdentityInfo(new DateTime(timestamp), id, new Point3D(x, y, z));
        }

        public int CompareTo(object obj)
        {
            return timestamp.CompareTo(obj);
        }
    }
}
