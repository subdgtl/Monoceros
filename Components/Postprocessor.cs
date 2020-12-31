// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace WFCToolset
{
    // TODO: Find a better name
    // TODO: Make bake aware and think about using blocks. Override baking output geometry.
    // TODO: Think about how to bake empty and non-deterministic slots.
    public class ComponentPostprocessor : GH_Component
    {
        public ComponentPostprocessor() : base("WFC Postprocessor", "WFCPostprocessor",
            "WFC Postprocessor.",
            "WaveFunctionCollapse", "Postprocessor")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new SlotParameter(), "Slot", "S", "WFC Slot", GH_ParamAccess.item);
            pManager.AddParameter(new ModuleParameter(), "Modules", "M", "All WFC Modules", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "Geometry placed into WFC Slot", GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var slot = new Slot();
            var modules = new List<Module>();

            if (!DA.GetData(0, ref slot))
            {
                return;
            }

            if (!DA.GetDataList(1, modules))
            {
                return;
            }

            var geometry = Enumerable.Empty<GeometryBase>();

            // TODO: Think about what to do with empty and non-deterministic slots.
            if (slot.AllowedSubmodules.Count == 1)
            {
                var slotSubmoduleName = slot.AllowedSubmodules.First();
                var placedModule = modules.FirstOrDefault(module => module.PivotSubmoduleName == slotSubmoduleName);
                if (placedModule != null)
                {
                    var slotPivot = slot.BasePlane.Clone();
                    slotPivot.Origin = slot.AbsoluteCenter;
                    geometry = placedModule.Geometry.Select(geo =>
                    {
                        var placedGeometry = geo.Duplicate();
                        placedGeometry.Transform(Transform.PlaneToPlane(placedModule.Pivot, slotPivot));
                        return placedGeometry;
                    });
                }
            }

            // Return placed geometry
            DA.SetDataList(0, geometry);
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                Properties.Resources.WFC;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("D32A9B20-4138-4C24-A11F-E139383776B2");
    }
}
