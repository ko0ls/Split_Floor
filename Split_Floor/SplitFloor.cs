using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Split_Floor.Library;
using Autodesk.Revit.UI.Selection;
using System.Xml.Linq;

namespace Split_Floor
{
    [Transaction(TransactionMode.Manual)]
    public class SplitFloor : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            ISelectionFilter Floorfil = new FloorFilter();
            IList<ElementId> lstFloorID = new List<ElementId>();
            lstFloorID = Sel.Sel_ElementbyFilter(uidoc, Floorfil);
            try
            {
                foreach (ElementId id in lstFloorID)
                {
                    Floor fl = doc.GetElement(id) as Floor;
                    // Get floor's info
                    FloorType floorType = fl.FloorType;
                    Level level = fl.Document.GetElement(fl.LevelId) as Level;

                    // Get all dependent elements
                    Transaction transTemp = new Transaction(doc);
                    transTemp.Start("tempDelete");
                    ICollection<ElementId> ids = doc.Delete(fl.Id);
                    transTemp.RollBack();

                    List<ModelLine> mLines = new List<ModelLine>();
                    foreach (ElementId _id in ids)
                    {
                        Element ele = doc.GetElement(_id);
                        if (ele is ModelLine)
                        {
                            mLines.Add(ele as ModelLine);
                        }
                    }
                    
                    // Get the point list of model line
                    List<XYZ> points = new List<XYZ>();
                    for (int i = 0; i < mLines.Count; i++)
                    {
                        points.Add(mLines[i].GeometryCurve.GetEndPoint(1));
                        points.Add(mLines[i].GeometryCurve.GetEndPoint(0));
                    }                    

                    // Distinct point list
                    PointCompare pointCompare = new PointCompare();
                    for (int i = 0; i < points.Count-1; i++)
                    {
                        for (int j = i + 1; j < points.Count; j++)
                        {
                            if (((pointCompare.Compare(points[i], points[j]) == 0) ? true : false))
                            {
                                for (int k = j; k < points.Count - 1; k++)
                                {
                                    points[k] = points[k + 1];                                    
                                }
                                points.RemoveAt(points.Count - 1);
                            }
                        }
                    }                    

                    // Get boundingbox of floor
                    XYZ Max = fl.get_BoundingBox(doc.ActiveView).Max;
                    XYZ Min = fl.get_BoundingBox(doc.ActiveView).Min;

                    //Check the point inside of boundingbox and add to point list
                    List<double> yCoord = new List<double>();
                    yCoord.Add(Min.Y);
                    yCoord.Add(Max.Y);

                    List<XYZ> Inside = new List<XYZ>();
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (points[i].X > Min.X && points[i].X < Max.X && points[i].Y > Min.Y && points[i].Y < Max.Y)
                        {
                            Inside.Add(points[i]);
                            points.Add(new XYZ(Min.X, points[i].Y, points[i].Z));
                            points.Add(new XYZ(Max.X, points[i].Y, points[i].Z));
                            yCoord.Add(points[i].Y);
                        }
                    }
                    //points.AddRange(Inside);

                    yCoord.Sort();
                    yCoord = yCoord.Distinct().ToList();

                    // Distinct point list
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        for (int j = i + 1; j < points.Count; j++)
                        {
                            if (((pointCompare.Compare(points[i], points[j]) == 0) ? true : false))
                            {
                                for (int k = j; k < points.Count - 1; k++)
                                {
                                    points[k] = points[k + 1];
                                }
                                points.RemoveAt(points.Count - 1);
                            }
                        }
                    }

                    //sort point list base on X, Y
                    List<List<XYZ>> SortPoints = new List<List<XYZ>>(); 
                    for (int i = 0; i < yCoord.Count; i++)
                    {
                        List<XYZ> subList = new List<XYZ>();
                        for (int j = 0; j < points.Count; j++)
                        {
                            if (Math.Abs(points[j].Y - yCoord[i]) <= 0.0002)
                            {
                                subList.Add(points[j]);
                            }
                        }
                        for (int k = 0; k < subList.Count-1; k++)
                        {
                            for (int m = k+1; m < subList.Count; m++)
                            {
                                if (((pointCompare.Compare(subList[k], subList[m]) == 1) ? true : false))
                                {
                                    XYZ tg = subList[k];
                                    subList[k] = subList[m];
                                    subList[m] = tg;
                                }
                            }
                        }
                        SortPoints.Add(subList);
                    }

                    // delete master floor
                    Transaction transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "Delete master floor");
                    transaction.Start();
                    commandData.Application.ActiveUIDocument.Document.Delete(fl.Id);
                    transaction.Commit();

                    // Get point to create floor
                    for (int i = 0; i < yCoord.Count - 1; i++)
                    {
                        for (int j = 0; j < SortPoints[i].Count; j++)
                        {
                            List<XYZ> _points = new List<XYZ>();
                            if (j % 2 == 0 && j + 1 < SortPoints[i].Count)
                            {                                
                                XYZ p0 = SortPoints[i][j];
                                XYZ p1 = SortPoints[i][j+1];
                                XYZ p2 = new XYZ(SortPoints[i][j + 1].X, yCoord[i + 1], SortPoints[i][j].Z);
                                XYZ p3 = new XYZ(SortPoints[i][j].X, yCoord[i + 1], SortPoints[i][j].Z);
                                _points.Add(p0);
                                _points.Add(p1);
                                _points.Add(p2);
                                _points.Add(p3);

                                Transaction tran = new Transaction(commandData.Application.ActiveUIDocument.Document, "Generate Floor");
                                tran.Start();
                                CreateElement.CreateFloor(uiapp, floorType, level, _points);
                                tran.Commit();
                            }
                            foreach (XYZ _p in _points)
                            {
                                var _pos = Inside.Where(a => pointCompare.Compare(a, _p) == 0);
                                if (_pos != null)
                                {
                                    foreach (XYZ _po in _pos) 
                                    {
                                        var a1 = SortPoints[i + 1].FirstOrDefault(a => pointCompare.Compare(a, _po) == 0);
                                        SortPoints[i + 1].Remove(a1);
                                    }
                                }                                
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //TaskDialog.Show("Error", ex.ToString());
                return Result.Failed;
            }
            //TaskDialog.Show("Tittle", "Hello world.");
            return Result.Succeeded;
        }
    }
}
