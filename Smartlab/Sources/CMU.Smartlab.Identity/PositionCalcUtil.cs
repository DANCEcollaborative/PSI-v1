using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMU.Smartlab.Identity;

namespace CMU.Smartlab.Identity
{
    public class PositionCalc
    {
        public PositionCalc()
        {

        }

        public Point3D p_zero()
        {
            Point3D p = new Point3D(0, 0, 0);
            return p;
        }

        public double pp_distance(Point3D p0, Point3D p1)
        {
            double dx = p0.x - p1.x;
            double dy = p0.y - p1.y;
            double dz = p0.z - p1.z;
            double d = System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
            return d;
        }

        public double p_len(Point3D p)
        {
            double l = pp_distance(p, p_zero());
            return l;
        }
        public double pp_dot(Point3D p1, Point3D p2)
        {
            double d = p1.x * p2.x + p1.y * p2.y + p1.z * p2.z;
            return d;
        }
        public Point3D pp_cross(Point3D p1, Point3D p2)
        {
            Point3D p = new Point3D(p1.y * p2.z - p1.z * p2.y, p1.z * p2.x - p1.x * p2.z, p1.x * p2.y - p1.y * p2.x);
            return p;
        }
        public Point3D pp_mid(Point3D p1, Point3D p2)
        {
            Point3D p = new Point3D((p1.x + p2.x) / 2, (p1.y + p2.y) / 2, (p1.z + p2.z) / 2);
            return p;
        }
        public void ps_mid()
        {

        }
        public double pp_cos(Point3D p1, Point3D p2)
        {
            double cos = pp_dot(p1, p2) / p_len(p1) / p_len(p2);
            return cos;
        }
        public double pp_sin(Point3D p1, Point3D p2)
        {
            double sin = p_len(pp_cross(p1, p2)) / p_len(p1) / p_len(p2);
            return sin;
        }
        public double pl_distance(Point3D p, Line3D l)
        {
            double d = pp_distance(p, l.p0) * pp_sin(p.Sub(l.p0), l.t);
            return d;
        }
        public bool p_is_zero(Point3D p)
        {
            Point3D temp = p.Abs();
            double d = temp.x + temp.y + temp.z;
            bool b = d < 1e-10 ? true : false;
            return b;
        }
        public bool pp_parallel(Point3D p1, Point3D p2)
        {
            bool b = (p1.x == p2.x && p1.y ==p2.y && p1.z==p2.z) ? true : false;
            return b;
        }
        public void ll_nearest (Line3D l1, Line3D l2)
        {

        }

        public void calc_position(Point3D p1)
        {

        }
    }
}
