// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace WFCPlugin
{
    /// <summary>
    /// <para>
    /// Grasshopper component: WFC Deconstruct Module To Components
    /// </para>
    /// <para>
    /// Outputs the internal fields and properties of the <see cref="Module"/> in the same
    /// fashion and order of the inputs of <see cref="Module"/> constructors. This way a 
    /// <see cref="Module"/> can be deconstructed and directly reconstructed.
    /// </para>
    /// </summary>
    public class ComponentModuleDeconstruct : GH_Component
    {
        public ComponentModuleDeconstruct() : base("WFC Deconstruct Module To Components",
                                                   "WFCDeconModule",
                                                   "Deconstruct WFC Module into name, base plane, " +
                                                   "connector planes, connector numbers and properties.",
                                                   "WaveFunctionCollapse",
                                                   "Module")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ModuleParameter(), "Module", "M", "WFC Module", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Name",
                                  "N",
                                  "Module name (converted to lowercase).",
                                  GH_ParamAccess.list);
            pManager.AddPointParameter("Slot Centers",
                                       "SC",
                                       "Used slots centers to be used for module reconstruction.",
                                       GH_ParamAccess.list);
            pManager.AddGeometryParameter("Geometry",
                                          "G",
                                          "Geometry contained in the module.",
                                          GH_ParamAccess.list);
            pManager.AddPlaneParameter("Base Plane",
                                       "B",
                                       "Grid space base plane. Defines orientation of the grid.",
                                       GH_ParamAccess.list);
            pManager.AddVectorParameter("Grid Slot Diagonal",
                                        "D",
                                        "Grid slot diagonal vector specifying slot dimension in base-plane-aligned axes.",
                                        GH_ParamAccess.list);
            pManager.AddPlaneParameter("Connectors",
                                       "C",
                                       "Connector planes",
                                       GH_ParamAccess.list);
            pManager.AddIntegerParameter("Connector Indices",
                                         "I",
                                         "Connector indices - a list parallel to C",
                                         GH_ParamAccess.list);
            pManager.AddVectorParameter("Connector Direction",
                                        "D",
                                        "Connector direction base-plane-aligned axis vector - a list parallel to C.",
                                        GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Module module = null;

            if (!DA.GetData(0, ref module))
            {
                return;
            }

            var baseAlignmentTransform = Transform.PlaneToPlane(Plane.WorldXY, module.BasePlane);
            var scalingTransform = Transform.Scale(module.BasePlane,
                                                   module.SlotDiagonal.X,
                                                   module.SlotDiagonal.Y,
                                                   module.SlotDiagonal.Z);

            var submoduleCentersNormalized = module.SubmoduleCenters.Select(center => center.ToPoint3d());
            var submoduleCenters = submoduleCentersNormalized.Select(center =>
            {
                center.Transform(baseAlignmentTransform);
                center.Transform(scalingTransform);
                return center;
            });

            DA.SetDataList(0, new List<ModuleName> { new ModuleName(module.Name) });
            DA.SetDataList(1, submoduleCenters);
            DA.SetDataList(2, module.Geometry);
            DA.SetDataList(3, new List<Plane> { module.BasePlane });
            DA.SetDataList(4, new List<Vector3d> { module.SlotDiagonal });
            var connectors = module.Connectors;
            DA.SetDataList(5, connectors.Select(connector => connector.AnchorPlane));
            DA.SetDataList(6, connectors.Select(connector => connector.ConnectorIndex));
            DA.SetDataList(7, connectors.Select(connector => connector.Direction.ToVector()));
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
                Properties.Resources.M;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("DD254DF4-9369-4FF3-B7BD-BA8F7FB2327E");
    }
}
