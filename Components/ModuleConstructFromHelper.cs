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
    /// <summary>
    /// <para>
    /// Grasshopper component: WFC Construct Module From Helper Geometry
    /// </para>
    /// <para>
    /// Construct a WFC <see cref="Module"/> from input helper <see cref="GeometryBase"/>. 
    /// This geometry will not become part of the <see cref="Module"/>. Instead, a different
    /// production <see cref="GeometryBase"/> will be used by the <see cref="ComponentPostprocessor"/>
    /// to materialize the result of the  WFC <see cref="ComponentFauxSolver"/>.
    /// The production <see cref="GeometryBase"/> is unrelated to the <see cref="Module"/> cage and 
    /// <see cref="Slot"/>s it may occupy. 
    /// </para>
    /// <para>
    /// Grasshopper inputs:
    /// <list type="bullet">
    /// <item>
    /// <term><see cref="ModuleName"/> Name</term>
    /// <description>
    /// <see cref="Module"/> name to be used as its unique identifier.
    /// Disallowed names are listed in <see cref="Configuration.RESERVED_NAMES"/>. 
    /// The Name will be converted to lowercase.
    /// Item access.
    /// No default.
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="GeometryBase"/> Helper Geometry</term>
    /// <description>
    /// <see cref="GeometryBase"/> defining the <see cref="Module"/> cage. The Helper Geometry
    /// will be lost after the <see cref="Module"/> will have been created.
    /// Supports <see cref="Point3d"/>, <see cref="Curve"/>, <see cref="Brep"/> and <see cref="Mesh"/>.
    /// For better and faster results prefer <see cref="Mesh"/> to <see cref="Brep"/> .
    /// List access.
    /// No default.
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="GeometryBase"/> Production Geometry</term>
    /// <description>
    /// <see cref="GeometryBase"/> used by the <see cref="ComponentPostprocessor"/> to materialize the result of 
    /// the WFC <see cref="ComponentFauxSolver"/>. Production Geometry does not have to 
    /// fit into the generated <see cref="Module"/> cages and can be larger, smaller, different or none. 
    /// Supports any geometry.
    /// List access.
    /// No default.
    /// Optional.
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="Plane"/> Base Plane</term>
    /// <description>
    /// Grid space base plane. Defines orientation of the grid.
    /// Item access.
    /// Default <see cref="Plane.WorldXY"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="Vector3d"/> Grid Slot Diagonal</term>
    /// <description>
    /// World grid <see cref="Slot"/> diagonal <see cref="Vector3d"/> specifying single grid cell dimension in base-plane-aligned XYZ axes.
    /// Item access.
    /// Default <c>Vector3d(1.0, 1.0, 1.0)</c>.
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="double"/> Precision</term>
    /// <description>
    /// Module slicer precision (lower is more precise but slower).
    /// Item access.
    /// Default <c>0.5</c>.
    /// </description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// Grasshopper outputs:
    /// <list type="bullet">
    /// <item>
    /// <term><see cref="Module"/> Module</term>
    /// <description>
    /// WFC Module encapsulating the input geometry and containing the same input geometry.
    /// Item access.
    /// </description>
    /// </item>
    /// </list>
    /// </para>
    /// </summary>
    public class ComponentModuleFromHelper : GH_Component
    {
        public ComponentModuleFromHelper() : base("WFC Construct Module From Helper Geometry",
                                                  "WFCModuleHlp",
                                                  "Construct a WFC Module from helper geometry. " +
                                                  "The specified production geometry bill be used in " +
                                                  "WFC solver result. Prefer Mesh to BRep.",
                                                  "WaveFunctionCollapse", "Module")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Name",
                                  "N",
                                  "Module name (except '" + Configuration.RESERVED_TO_STRING + "'). " +
                                  "The Name will be converted to lowercase.",
                                  GH_ParamAccess.item);
            pManager.AddGeometryParameter("Helper Geometry",
                                          "H",
                                          "Geometry defining the module cage. Point, Curve, Brep, Mesh. Prefer Mesh to BRep.",
                                          GH_ParamAccess.list);
            pManager.AddGeometryParameter("Production Geometry",
                                          "G",
                                          "Geometry used to materialize the result of the WFC Solver. " +
                                          "Production geometry does not have to fit into the generated " +
                                          "module cage and can be larger, smaller, different or none.",
                                          GH_ParamAccess.list);
            pManager[2].Optional = true;
            pManager.AddPlaneParameter("Base plane",
                                       "B",
                                       "Grid space base plane. Defines orientation of the grid.",
                                       GH_ParamAccess.item,
                                       Plane.WorldXY);
            pManager.AddVectorParameter("Grid Slot Diagonal",
                                        "D",
                                        "World grid slot diagonal vector specifying single grid slot dimension " +
                                        "in base-plane-aligned XYZ axes.",
                                        GH_ParamAccess.item,
                                        new Vector3d(1.0, 1.0, 1.0));
            pManager.AddNumberParameter("Precision",
                                        "P",
                                        "Module slicer precision (lower = more precise & slower)",
                                        GH_ParamAccess.item,
                                        0.5);
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
            var helperGeometryRaw = new List<IGH_GeometricGoo>();
            var productionGeometryRaw = new List<IGH_GeometricGoo>();
            var nameRaw = new ModuleName();
            var basePlane = new Plane();
            var slotDiagonal = new Vector3d();
            var precision = 0.5;

            if (!DA.GetData(0, ref nameRaw))
            {
                return;
            }

            if (!DA.GetDataList(1, helperGeometryRaw))
            {
                return;
            }

            DA.GetDataList(2, productionGeometryRaw);

            if (!DA.GetData(3, ref basePlane))
            {
                return;
            }

            if (!DA.GetData(4, ref slotDiagonal))
            {
                return;
            }

            if (!DA.GetData(5, ref precision))
            {
                return;
            }

            var name = nameRaw.Name.ToLower();

            if (name.Length == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module name is empty.");
                return;
            }

            if (Configuration.RESERVED_NAMES.Contains(name))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "The module name cannot be '" + name + "' because it is reserved by WFC.");
                return;
            }

            var helpersClean = helperGeometryRaw
               .Where(goo => goo != null)
               .Select(ghGeo =>
               {
                   var geo = ghGeo.Duplicate();
                   return GH_Convert.ToGeometryBase(geo);
               }
               ).ToList();

            if (helpersClean.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "The input geometry is insufficient to define a module.");
                return;
            }

            if (slotDiagonal.X <= 0 || slotDiagonal.Y <= 0 || slotDiagonal.Z <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "One or more slot dimensions are not larger than 0.");
                return;
            }

            // Scale down to unit size
            var normalizationTransform = Transform.Scale(basePlane,
                                                         1 / slotDiagonal.X,
                                                         1 / slotDiagonal.Y,
                                                         1 / slotDiagonal.Z);
            // Orient to the world coordinate system
            var worldAlignmentTransform = Transform.PlaneToPlane(basePlane, Plane.WorldXY);
            // Slot dimension is for the sake of this calculation 1,1,1
            var divisionLength = precision;
            var submoduleCenters = new List<Point3i>();
            foreach (var goo in helpersClean)
            {
                var geo = goo.Duplicate();
                if (geo.Transform(normalizationTransform) && geo.Transform(worldAlignmentTransform))
                {
                    // TODO: Let user choose the method of populating the geometry, just like with slots
                    // Populate with points (many)
                    var populatePoints = Populate.PopulateGeometry(divisionLength, geo);
                    foreach (var geometryPoint in populatePoints)
                    {
                        // Round point locations
                        // Slot dimension is for the sake of this calculation 1,1,1
                        var slotCenterPoint = new Point3i(geometryPoint);
                        // Deduplicate
                        if (!submoduleCenters.Contains(slotCenterPoint))
                        {
                            submoduleCenters.Add(slotCenterPoint);
                        }
                    }
                }
            }

            var productionGeometryClean = productionGeometryRaw
               .Where(goo => goo != null)
               .Select(ghGeo =>
               {
                   var geo = ghGeo.Duplicate();
                   return GH_Convert.ToGeometryBase(geo);
               }
               );

            var module = new Module(name, productionGeometryClean, basePlane, submoduleCenters, slotDiagonal);
            if (!module.Continuous)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                                  "The module is not continuous and therefore will not hold together.");
            }

            if (module.Geometry.Count != productionGeometryRaw.Count)
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
                Properties.Resources.M;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("7BE5F6EB-03CF-4EA3-9FD2-7831E996AB49");
    }
}
