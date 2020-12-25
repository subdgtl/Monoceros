// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace WFCToolset
{

    public class ModuleParameter : GH_PersistentParam<Module>, IGH_PreviewObject, IGH_BakeAwareObject
    {
        public ModuleParameter()
      : base("WFC Module", "WFC-M", "Module definition.", "WaveFunctionCollapse", "Parameters") { }
        public override Guid ComponentGuid => new Guid("976AE239-A098-4B77-978F-416B974DD146");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon =>
               Properties.Resources.M;

        public bool Hidden { get; set; }

        public bool IsPreviewCapable => true;

        public BoundingBox ClippingBox => Preview_ComputeClippingBox();

        protected override GH_GetterResult Prompt_Plural(ref List<Module> values)
        {
            values = new List<Module>();
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Singular(ref Module value)
        {
            value = new Module();
            return GH_GetterResult.success;
        }
        public void DrawViewportWires(IGH_PreviewArgs args)
        {
            Preview_DrawWires(args);
        }

        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            Preview_DrawMeshes(args);
        }

        public bool IsBakeCapable => true;

        public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            BakeGeometry(doc, null, obj_ids);
        }

        public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            if (att == null)
            {
                att = doc.CreateDefaultAttributes();
            }

            foreach (IGH_BakeAwareObject item in m_data)
            {
                if (item != null)
                {
                    var idsOut = new List<Guid>();
                    item.BakeGeometry(doc, att, idsOut);
                    obj_ids.AddRange(idsOut);
                }
            }
        }
    }
}
