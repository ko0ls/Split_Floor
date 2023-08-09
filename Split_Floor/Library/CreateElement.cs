using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Split_Floor.Library
{
    public static class CreateElement
    {
        public static Floor CreateFloor(UIApplication application, FloorType floorType, Level level, List<XYZ> points)
        {
            // Get the Revit document
            Autodesk.Revit.DB.Document document = application.ActiveUIDocument.Document;

            // Get the application creation object
            Autodesk.Revit.Creation.Application appCreation = application.Application.Create;

            // Build a floor profile for the floor creation
            XYZ first = points[0];
            XYZ second = points[1];
            XYZ third = points[2];
            XYZ fourth = points[3];
            CurveArray profile = new CurveArray();
            profile.Append(Line.CreateBound(first, second));
            profile.Append(Line.CreateBound(second, third));
            profile.Append(Line.CreateBound(third, fourth));
            profile.Append(Line.CreateBound(fourth, first));

            CurveLoop loop = new CurveLoop();
            foreach (Curve curve in profile)
            {
                loop.Append(curve);
            }
            List<CurveLoop> floorLoops = new List<CurveLoop> { loop };

            // The normal vector (0,0,1) that must be perpendicular to the profile.
            XYZ normal = XYZ.BasisZ;

            return document.Create.NewFloor(profile, floorType, level, true);
        }
    }
}
