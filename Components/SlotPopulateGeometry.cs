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

    public class ComponentPopulateGeometryWithSlots : GH_Component
    {
        public ComponentPopulateGeometryWithSlots() : base("WFC Populate Geometry With Slots", "WFCPopSlots",
            "Populate geometry with WFC Slots. Supports Point, Curve, Brep, Mesh. Prefer Mesh to BRep.",
            "WaveFunctionCollapse", "Slot")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Geometry", "G", "Geometry to populate with slots", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Base plane",
                                       "B",
                                       "Grid space base plane. Defines orientation of the grid.",
                                       GH_ParamAccess.item,
                                       Plane.WorldXY);
            pManager.AddVectorParameter(
               "Grid Slot Diagonal",
               "D",
               "World grid slot diagonal vector specifying single grid slot dimension in base-plane-aligned XYZ axes",
               GH_ParamAccess.item,
               new Vector3d(1.0, 1.0, 1.0)
               );
            pManager.AddBooleanParameter("Allow Everything",
                                         "E",
                                         "Initiate all slots with all modules allowed. False = Allow nothing, True = Allow everything.",
                                         GH_ParamAccess.item,
                                         true);
            pManager.AddIntegerParameter("Processing Method",
                                         "P",
                                         "0 = only wrap geometry surface, 1 = only fill geometry volume, 2 = wrap surface and fill volume",
                                         GH_ParamAccess.item,
                                         2);
            pManager.AddNumberParameter("Precision", "P", "Module slicer precision (lower = more precise & slower)", GH_ParamAccess.item, 0.5);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new SlotParameter(), "Slots", "S", "WFC Slots", GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var geometryRaw = new List<IGH_GeometricGoo>();
            var basePlane = new Plane();
            var diagonal = new Vector3d();
            var allowEverything = true;
            var method = 2;
            var precision = 0.5;



            if (!DA.GetDataList(0, geometryRaw))
            {
                return;
            }

            if (!DA.GetData(1, ref basePlane))
            {
                return;
            }

            if (!DA.GetData(2, ref diagonal))
            {
                return;
            }

            if (!DA.GetData(3, ref allowEverything))
            {
                return;
            }

            if (!DA.GetData(4, ref method))
            {
                return;
            }

            if (!DA.GetData(5, ref precision))
            {
                return;
            }


            if (diagonal.X <= 0 || diagonal.Y <= 0 || diagonal.Z <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or more slot dimensions are not larger than 0.");
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

            // Scale down to unit size
            var normalizationTransform = Transform.Scale(basePlane, 1 / diagonal.X, 1 / diagonal.Y, 1 / diagonal.Z);
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
                    if (method == 0 || method == 2)
                    {
                        var populatePoints = Populate.PopulateSurface(divisionLength, geo);
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
                    if (method == 1 || method == 2)
                    {
                        var populatePoints = Populate.PopulateVolume(divisionLength, geo);
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
            }

            var slots = submoduleCenters.Select(c =>
                new Slot(basePlane, c, diagonal, allowEverything, false, new List<string>(), new List<string>(), 0)
            );

            DA.SetDataList(0, slots);
        }


        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.secondary;

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
        public override Guid ComponentGuid => new Guid("F4CB7062-F85C-4E92-8215-034C4CC3941C");
    }
}
