using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Monoceros {
    /// <summary>
    /// Wraps <see cref="Slot"/> type so that it can be used in Grasshopper as an input, output or a floating parameter.
    /// </summary>
    public class SlotParameter : GH_PersistentParam<Slot>, IGH_PreviewObject, IGH_BakeAwareObject {
        public SlotParameter( )
      : base("Slot", "S", "Contains a collection of Monoceros Slots.", "Monoceros", "Parameters") { }
        public override Guid ComponentGuid => new Guid("75063FD9-1A1F-4173-817B-E3C01428FA7A");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SP;

        public bool Hidden { get; set; }

        public bool GetIsPreviewCapable( ) {
            return true;
        }

        public BoundingBox GetClippingBox( ) {
            return Preview_ComputeClippingBox();
        }

        protected override GH_GetterResult Prompt_Plural(ref List<Slot> values) {
            values = new List<Slot>();
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Singular(ref Slot value) {
            value = new Slot();
            return GH_GetterResult.success;
        }
        public void DrawViewportWires(IGH_PreviewArgs args) {
            Preview_DrawWires(args);
        }

        public void DrawViewportMeshes(IGH_PreviewArgs args) {
            Preview_DrawMeshes(args);
        }

        public bool IsBakeCapable => true;

        public bool IsPreviewCapable => true;

        public BoundingBox ClippingBox => Preview_ComputeClippingBox();

        public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids) {
            BakeGeometry(doc, null, obj_ids);
        }

        public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids) {
            if (att == null) {
                att = doc.CreateDefaultAttributes();
            }

            foreach (IGH_BakeAwareObject item in m_data) {
                if (item != null) {
                    var idsOut = new List<Guid>();
                    item.BakeGeometry(doc, att, idsOut);
                    obj_ids.AddRange(idsOut);
                }
            }
        }
    }
}
