﻿using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Monoceros {

    /// <summary>
    /// Wraps <see cref="Module"/> type so that it can be used in Grasshopper as an input, output or a floating parameter.
    /// </summary>
    public class ModuleParameter : GH_PersistentParam<Module>, IGH_PreviewObject, IGH_BakeAwareObject {
        public ModuleParameter( )
      : base("Module", "M", "Contains a collection of Monoceros Modules.", "Monoceros", "Parameters") { }
        public override Guid ComponentGuid => new Guid("976AE239-A098-4B77-978F-416B974DD146");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.MP;

        public bool Hidden { get; set; }

        public bool GetIsPreviewCapable( ) {
            return true;
        }

        public BoundingBox GetClippingBox( ) {
            return Preview_ComputeClippingBox();
        }

        protected override GH_GetterResult Prompt_Plural(ref List<Module> values) {
            values = new List<Module>();
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Singular(ref Module value) {
            value = new Module();
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
