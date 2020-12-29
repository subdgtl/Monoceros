// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace WFCToolset
{
    public class ComponentModuleFromGeometry : GH_Component
    {
        public ComponentModuleFromGeometry() : base("WFC Construct module from geometry", "WFCModuleGeo",
            "Construct a WFC Module from input geometry, which will be also used in WFC solver result. Prefer Mesh to BRep.",
            "WaveFunctionCollapse", "Module")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N",
                                      "Module name (except '" + Configuration.RESERVED_TO_STRING + "'). The Name will be converted to lowercase.",
                                      GH_ParamAccess.item);
            pManager.AddGeometryParameter("Geometry", "G", "Geometry defining the module. Point, Curve, Brep, Mesh. Prefer Mesh to BRep.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Base plane", "B", "Grid space base plane. Defines orientation of the grid.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddVectorParameter(
               "Grid Slot Diagonal",
               "D",
               "World grid slot diagonal vector specifying single grid slot dimension in base-plane-aligned XYZ axes",
               GH_ParamAccess.item,
               new Vector3d(1.0, 1.0, 1.0)
               );
            pManager.AddNumberParameter("Precision", "P", "Module slicer precision (lower = more precise & slower)", GH_ParamAccess.item, 0.5);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ModuleParameter(), "Module", "M", "WFC Module", GH_ParamAccess.item);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var geometryRaw = new List<IGH_GeometricGoo>();
            var name = "";
            var basePlane = new Plane();
            var slotDiagonal = new Vector3d();
            var precision = 0.5;

            if (!DA.GetData(0, ref name))
            {
                return;
            }

            if (!DA.GetDataList(1, geometryRaw))
            {
                return;
            }

            if (!DA.GetData(2, ref basePlane))
            {
                return;
            }

            if (!DA.GetData(3, ref slotDiagonal))
            {
                return;
            }

            if (!DA.GetData(4, ref precision))
            {
                return;
            }

            if (name.Length == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module name is empty.");
                return;
            }

            if (Configuration.RESERVED_NAMES.Contains(name))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The module name cannot be '" + name + "' because it is reserved by WFC.");
                return;
            }

            var geometryClean = geometryRaw
               .Where(goo => goo != null)
               .Select(ghGeo =>
               {
                   var geo = ghGeo.Duplicate();
                   return GH_Convert.ToGeometryBase(geo);
               }
               ).ToList();

            if (geometryClean.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The input geometry is insufficient to define a module.");
                return;
            }

            if (slotDiagonal.X <= 0 || slotDiagonal.Y <= 0 || slotDiagonal.Z <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or more slot dimensions are not larger than 0.");
                return;
            }

            // Scale down to unit size
            var normalizationTransform = Transform.Scale(basePlane, 1 / slotDiagonal.X, 1 / slotDiagonal.Y, 1 / slotDiagonal.Z);
            // Orient to the world coordinate system
            var worldAlignmentTransform = Transform.PlaneToPlane(basePlane, Plane.WorldXY);
            // Slot dimension is for the sake of this calculation 1,1,1
            var divisionLength = precision;
            var submoduleCenters = new List<Point3i>();
            foreach (var goo in geometryClean)
            {
                var geo = goo.Duplicate();
                if (geo.Transform(normalizationTransform) && geo.Transform(worldAlignmentTransform))
                {
                    // Populate with points (many)
                    var populatePoints = Populate.PopulateGeometry(divisionLength, geo);
                    foreach (var geometrypoint in populatePoints)
                    {
                        // Round point locations
                        // Slot dimension is for the sake of this calculation 1,1,1
                        var slotCenterPoint = new Point3i(geometrypoint);
                        // Deduplicate
                        if (!submoduleCenters.Contains(slotCenterPoint))
                        {
                            submoduleCenters.Add(slotCenterPoint);
                        }
                    }
                }
            }

            var module = new Module(name, geometryClean, basePlane, submoduleCenters, slotDiagonal);
            if (!module.Continuous)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The module is not continuous and therefore will not hold together.");
            }

            if (module.Geometry.Count != geometryRaw.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Some geometry could not be used.");
            }

            DA.SetData(0, module);
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
        public override Guid ComponentGuid => new Guid("45B1FD73-5450-4DBE-87F0-2D5AED14159E");
    }
}
