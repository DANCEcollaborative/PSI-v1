using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMU.Smartlab.Identity
{
    public class Point3D
    {
        public double x;
        public double y;
        public double z;

        public Point3D(double x, double y=0, double z = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public double[] To_Vec()
        {
            double[] vec = { this.x, this.y, this.z};
            return vec;
        }
        public Point3D Normalize()
        {
            double l = System.Math.Sqrt(this.x * this.x + this.y * this.y +this.z*this.z);
            Point3D p = new Point3D(this.x / l, this.y / l,this.z/l);
            return p;
        }
        public Point3D Abs()
        {
            Point3D p = new Point3D(System.Math.Abs(this.x), System.Math.Abs(this.y),System.Math.Abs(this.z));
            return p;
        }
        public String Str()
        {
            String s = $"Point3D:({this.x},{this.y},{this.z})";
            return s;
        }

        public Point3D Add(Point3D p1)
        {
            Point3D p = new Point3D(this.x + p1.x, this.y + p1.y,this.z+p1.z);
            return p;
        }

        public Point3D Sub(Point3D p1)
        {
            Point3D p = new Point3D(this.x - p1.x, this.y - p1.y,this.z-p1.z);
            return p;
        }
        public double Mul(Point3D p1)
        {
            double r = this.x * p1.x + this.y * p1.y + this.z * p1.z;
            return r;
        }

        public Point3D LMul(double lam)
        {
            Point3D p = new Point3D(this.x * lam, this.y * lam,this.z*lam);
            return p;
        }
        public Point3D RMul(Point3D p1)
        {
            Point3D p = new Point3D(this.x * p1.x, this.y * p1.y, this.z*p1.z);
            return p;
        }
        public Point3D TrueDiv(Point3D p1)
        {
            Point3D p = new Point3D(this.x / p1.x, this.y / p1.y,this.y/p1.y);
            return p;
        }
        public Point3D Eq(Point3D p1)
        {
            Point3D p = new Point3D(this.x = p1.x, this.y = p1.y,this.z=p1.z);
            return p;
        }

    }
    public class Point2D
    {
        public double x;
        public double y;

        public Point2D(double x, double y = 0)
        {
            this.x = x;
            this.y = y;
        }
        public double[] To_Vec()
        {
            double[] vec = { this.x, this.y};
            return vec;
        }
        public Point2D Normalize()
        {
            double l = System.Math.Sqrt(this.x * this.x + this.y * this.y);
            Point2D p = new Point2D(this.x / l, this.y / l);
            return p;
        }
        public Point2D Abs()
        {            
            Point2D p = new Point2D(System.Math.Abs(this.x), System.Math.Abs(this.y));
            return p;
        }
        public String Str()
        {
            String s = $"Point2D:({this.x},{this.y})" ;
            return s;
        }

        public Point2D Add(Point2D p1)
        {
            Point2D p = new Point2D(this.x + p1.x, this.y + p1.y);
            return p;
        }

        public Point2D Sub(Point2D p1)
        {
            Point2D p = new Point2D(this.x - p1.x, this.y - p1.y);
            return p;
        }
        public double Mul(Point2D p1)
        {
            double r = this.x * p1.x+this.y * p1.y;
            return r;
        }

        public Point2D LMul(double lam)
        {
            Point2D p = new Point2D(this.x *lam, this.y *lam);
            return p;
        }
        public Point2D RMul(Point2D p1)
        {
            Point2D p = new Point2D(this.x * p1.x, this.y * p1.y);
            return p;
        }
        public Point2D TrueDiv(Point2D p1)
        {
            Point2D p = new Point2D(this.x / p1.x, this.y / p1.y);
            return p;
        }
        public Point2D Eq(Point2D p1)
        {
            Point2D p = new Point2D(this.x = p1.x, this.y = p1.y);
            return p;
        }

    }
    public class Line3D
    {
        public double x;
        public double y;
        public double z;
        public Point3D p0;
        public Point3D t;
        public Line3D(Point3D p0, Point3D t)
        {
            this.p0 = p0;
            this.t = t;
        }

        public Point3D Find_point_by_lambda(double lam)
        {
            Point3D p = this.p0.Add(this.t.LMul(lam));
            return p;
        }

        public Point3D Find_point_by_x(double x)
        {
            if (x != 0)
            {
                double lam = (x - this.p0.x) / this.t.x;
             Point3D p = this.p0.Add(this.t.LMul(lam));
                return p;
            }
            else
            {
                Console.WriteLine("Can't find point by x for a line vertical to x axis!");
                return this.p0;
            }            
        }
        public Point3D Find_point_by_y(double y)
        {
            if (x != 0)
            {
                double lam = (y - this.p0.y) / this.t.y;
                Point3D p = this.p0.Add(this.t.LMul(lam));
                return p;
            }
            else
            {
                Console.WriteLine("Can't find point by x for a line vertical to y axis!");
                return this.p0;
            }
        }
        public Point3D Find_point_by_z(double z)
        {
            if (x != 0)
            {
                double lam = (z - this.p0.z) / this.t.z;
                Point3D p = this.p0.Add(this.t.LMul(lam));
                return p;
            }
            else
            {
                Console.WriteLine("Can't find point by x for a line vertical to z axis!");
                return this.p0;
            }
        }
        public String Str()
        {
            String s = "Line3D:(p0:" ;
            return s;
        }
    }
}
