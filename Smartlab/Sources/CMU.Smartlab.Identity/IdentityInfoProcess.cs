using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMU.Smartlab.Identity
{
    public class IdentityInfoProcess
    {

        public IdentityInfoProcess ()
        {

        }

        //寻找UNK的信息和现有最接近的人


        //更新信息，更新，添加id信息
        public void IdCompare(Dictionary<string, string[]> idInfo, Dictionary<string, string[]> idTemp)
        {
            foreach (KeyValuePair<string, string[]> id in idTemp)
            {
                if (id.Key.Equals("UNK"))
                {
                    string key = FindNearest(idInfo, id.Value);
                    idInfo[key] = id.Value;
                }
                else
                {
                    if (idInfo.ContainsKey(id.Key))
                    {
                        idInfo[id.Key] = id.Value;
                    }
                    else
                    {
                        idInfo.Add(id.Key,id.Value);
                    }
                }
                
            }

        }

        public Dictionary<string,string[]> MsgParse(string content)
        {
            string[] info = content.Split(';');
            Dictionary<string, string[]> location = new Dictionary<string, string[]>();
            int num = int.Parse(info[0]);
            string timestamp = info[1].Substring(info[1].IndexOf("[")+1, info[1].IndexOf("]"));
            for (int i = 2; i < info.Length; i++)
            {
                string[] idinfo = info[i].Split('&');
                string s = timestamp + ":" + idinfo[1];
                location.Add(idinfo[0], s.Split(':'));
            }
            return location;
        }

        private string FindNearest(Dictionary<string, string[]> identityList, string[] newLocation)
        {
            double distance = 10000;
            string key = null;
            if (identityList.Count >= 1)
            {
                foreach (KeyValuePair<string, string[]> id in identityList)
                {
                    double d = Distance(id.Value, newLocation);
                    key = d < distance ? id.Key : key;
                }
            }
            return key;
        }


        private double Distance(string[]c1, string[] c2)
        {
            double distance = 0;
            for (int i=1; i<4; i++)
            {
                distance += Math.Pow(double.Parse(c1[i]), double.Parse(c2[i]));
            }
            return distance;
        }


    }
}
