// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace WFCToolset
{

    public class ComponentDeconstructSlot : GH_Component
    {
        public ComponentDeconstructSlot() : base("WFC Deconstruct Slot", "WFCDeconSlot",
            "Deconstruct a WFC Slot into its cente point, base plane, diagonal and list of allowed modules.",
            "WaveFunctionCollapse", "Slot")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new SlotParameter(), "Slot", "S", "WFC Slot", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Slot Center", "P", "Center point of the slot", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Base plane",
                                       "B",
                                       "Grid space base plane. Defines orientation of the grid.",
                                       GH_ParamAccess.list);
            pManager.AddVectorParameter(
               "Grid Slot Diagonal",
               "D",
               "World grid slot diagonal vector specifying single grid slot dimension in base-plane-aligned XYZ axes",
               GH_ParamAccess.list
               );
            pManager.AddBooleanParameter("Allows Everything",
                                         "E",
                                         "The slot allows any module to be placed in it if true.",
                                         GH_ParamAccess.list
                                         );
            pManager.AddBooleanParameter("Allows Nothing",
                                         "N",
                                         "The slot allows no module to be placed in it if true.",
                                         GH_ParamAccess.list
                                         );
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Allowed Module Names",
                                  "M",
                                  "Initiate the slot with specified module names allowed.",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var slot = new Slot();

            if (!DA.GetData(0, ref slot))
            {
                return;
            }

            DA.SetDataList(0, new List<Point3d>() { slot.AbsoluteCenter });
            DA.SetDataList(1, new List<Plane>() { slot.BasePlane });
            DA.SetDataList(2, new List<Vector3d>() { slot.Diagonal });
            DA.SetDataList(3, new List<bool>() { slot.AllowedEverything });
            DA.SetDataList(4, new List<bool>() { slot.AllowedNothing });
            DA.SetDataList(5, slot.AllowedModules.Select(name => new ModuleName(name)));
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
                Properties.Resources.S;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("595773DB-868E-4999-90C0-1418FF3AC077");
    }
}
