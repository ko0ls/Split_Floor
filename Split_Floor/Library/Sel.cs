using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RDB = Autodesk.Revit.DB;
using RA = Autodesk.Revit.ApplicationServices;
using RUI = Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Split_Floor.Library
{
    public static class Sel
    {
        public static IList<ElementId> Sel_ElementbyFilter(RUI.UIDocument uidoc, ISelectionFilter filter)
        {
            IList<Element> pickedElements = uidoc.Selection.PickElementsByRectangle(filter, "Select floor:");
            if (pickedElements.Count > 0)
            {
                IList<ElementId> idsToSelect = new List<ElementId>(pickedElements.Count);
                foreach (Element element in pickedElements)
                {
                    idsToSelect.Add(element.Id);
                }

                // Update the current selection
                uidoc.Selection.SetElementIds(idsToSelect);
                return idsToSelect;
            }
            return null;
        }
    }

    public class FloorFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is Floor) return true;

            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            if (reference.ElementReferenceType == ElementReferenceType.REFERENCE_TYPE_NONE) return false;
            if (reference.ElementReferenceType == ElementReferenceType.REFERENCE_TYPE_SURFACE) return true;
            if (reference.ElementReferenceType == ElementReferenceType.REFERENCE_TYPE_CUT_EDGE) return true;
            return false;
        }
        
    }
}
