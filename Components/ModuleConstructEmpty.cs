// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace WFCToolset
{
    public class ComponentModuleEmpty : GH_Component
    {
        public ComponentModuleEmpty() : base("WFC Construct Empty Module", "WFCModuleEmpty",
            "Construct an empty WFC Module.",
            "WaveFunctionCollapse", "Module")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Base plane", "B", "Grid space base plane. Defines orientation of the grid.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddVectorParameter(
               "Grid Slot Diagonal",
               "D",
               "World grid slot diagonal vector specifying single grid slot dimension in base-plane-aligned XYZ axes",
               GH_ParamAccess.item,
               new Vector3d(1.0, 1.0, 1.0)
               );
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ModuleParameter(), "Module", "M", "WFC Module", GH_ParamAccess.item);
            pManager.AddParameter(new RuleParameter(), "Rules", "R", "WFC Rules", GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var basePlane = new Plane();
            var slotDiagonal = new Vector3d();

            if (!DA.GetData(0, ref basePlane))
            {
                return;
            }

            if (!DA.GetData(1, ref slotDiagonal))
            {
                return;
            }

            if (slotDiagonal.X <= 0 || slotDiagonal.Y <= 0 || slotDiagonal.Z <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or more slot dimensions are not larger than 0.");
                return;
            }

            Module.GenerateNamedEmptySingleModuleWithBasePlane(Configuration.EMPTY_TAG,
                                                               Configuration.INDIFFERENT_TAG,
                                                               basePlane,
                                                               slotDiagonal,
                                                               out var moduleEmpty,
                                                               out var rulesExternal);

            DA.SetData(0, moduleEmpty);
            DA.SetDataList(1, rulesExternal);
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
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                Properties.Resources.M;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("3473FD2C-D314-44C7-A5B6-EE30B008B04C");
    }
}
