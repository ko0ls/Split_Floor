using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Split_Floor.Library
{
    public class PointCompare : IComparer<Autodesk.Revit.DB.XYZ>  
    {
        public int Compare(Autodesk.Revit.DB.XYZ x, Autodesk.Revit.DB.XYZ y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if (y == null)
                {
                    return 1;
                }
                else
                {
                    if (Math.Abs(x.X - y.X) <= 0.0002 && Math.Abs(x.Y - y.Y) <= 0.0002 && Math.Abs(x.Z - y.Z) <=0.0002)
                    {
                        return 0;
                    }
                    else if(x.X > y.X)
                    {
                        return 1;
                    }
                    else { return -1;}
                }
            }
        }
    }
}
