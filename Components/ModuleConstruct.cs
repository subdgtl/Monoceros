using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Monoceros {
    /// <summary>
    /// <para>
    /// Grasshopper component: Monoceros Construct Module
    /// </para>
    /// <para>
    /// Construct a Monoceros <see cref="Module"/> from <see cref="Point3d"/>s inside
    /// World grid cells marking those that should become submodules of the
    /// created <see cref="Module"/>.  The <see cref="Point3d"/>s can be
    /// generated independently in Grasshopper or using
    /// <see cref="ComponentPopulateGeometryWithSlotCenters"/>. Redundant
    /// <see cref="Point3d"/>s will be removed. Production
    /// <see cref="GeometryBase"/> will be used by the
    /// <see cref="ComponentMaterializeSlot"/> to materialize the result of the Monoceros
    /// <see cref="ComponentSolver"/>. The production
    /// <see cref="GeometryBase"/> is unrelated to the <see cref="Module"/> cage
    /// and <see cref="Slot"/>s it may occupy. 
    /// </para>
    /// <para>
    /// Grasshopper inputs:
    /// <list type="bullet">
    ///     <item>
    ///         <term><see cref="ModuleName"/> Name</term>
    ///         <description><see cref="Module"/> name to be used as its unique
    ///             identifier. Disallowed names are listed in
    ///             <see cref="Config.RESERVED_NAMES"/>.  The Name will be
    ///             converted to lowercase. Item access. No default.
    ///             </description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Point3d"/> Submodule Points</term>
    ///         <description><see cref="Point3d"/>s inside World grid cells
    ///             marking those that should become submodules of the created
    ///             <see cref="Module"/>. The <see cref="Point3d"/>s can be
    ///             generated independently in Grasshopper or using
    ///             <see cref="ComponentPopulateGeometryWithSlotCenters"/>.
    ///             Redundant <see cref="Point3d"/>s will be removed. List
    ///             access. No default.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="GeometryBase"/> Production Geometry</term>
    ///         <description><see cref="GeometryBase"/> used by the
    ///             <see cref="ComponentMaterializeSlot"/> to materialize the result
    ///             of the Monoceros <see cref="ComponentSolver"/>. Production
    ///             Geometry does not have to fit into the generated
    ///             <see cref="Module"/> cages and can be larger, smaller,
    ///             different or none.  Supports any geometry. List access. No
    ///             default. Optional.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Plane"/> Base Plane</term>
    ///         <description>Grid space base plane. Defines orientation of the
    ///             grid. Item access. Default <see cref="Plane.WorldXY"/>.
    ///             </description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Vector3d"/> Grid Slot Diagonal</term>
    ///         <description>World grid <see cref="Slot"/> diagonal
    ///             <see cref="Vector3d"/> specifying single grid cell dimension
    ///             in base-plane-aligned XYZ axes. Item access. Default
    ///             <c>Vector3d(1.0, 1.0, 1.0)</c>.</description>
    ///     </item>
    /// </list>
    /// </para>
    /// <para>
    /// Grasshopper outputs:
    /// <list type="bullet">
    ///     <item>
    ///         <term><see cref="Module"/> Module</term>
    ///         <description>Monoceros Module encapsulating the input geometry and
    ///             containing the same input geometry. Item access.
    ///             </description>
    ///     </item>
    /// </list>
    /// </para>
    /// </summary>
    public class ComponentConstructModule : GH_Component {
        public ComponentConstructModule( ) : base("Construct Module",
                                                  "ConstModule",
                                                  "Construct a Monoceros Module from slot centers. " +
                                                  "The specified production geometry will be " +
                                                  "used in Monoceros solver result.",
                                                  "Monoceros", "Module") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Name",
                                  "N",
                                  "Module name (except '" + Config.RESERVED_TO_STRING + "'). " +
                                  "The Name will be converted to lowercase.",
                                  GH_ParamAccess.item);
            pManager.AddPointParameter("Slot Points",
                                       "Pt",
                                       "Points inside the slot to be occupied by the module",
                                       GH_ParamAccess.list);
            pManager.AddGeometryParameter("Production Geometry",
                                          "G",
                                          "Geometry used to materialize the result of the " +
                                          "Monoceros Solver. Production geometry does not have to fit " +
                                          "into the generated module cage and can be larger, " +
                                          "smaller, different or none.",
                                          GH_ParamAccess.list);
            pManager[2].Optional = true;
            pManager.AddPlaneParameter("Base Plane",
                                       "B",
                                       "Grid space base plane. Defines orientation of the grid.",
                                       GH_ParamAccess.item,
                                       Plane.WorldXY);
            pManager.AddVectorParameter("Grid Slot Diagonal",
                                        "D",
                                        "World grid slot diagonal vector specifying single grid " +
                                        "slot dimension in base-plane-aligned XYZ axes.",
                                        GH_ParamAccess.item,
                                        new Vector3d(1.0, 1.0, 1.0));
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new ModuleParameter(),
                                  "Module",
                                  "M",
                                  "Monoceros Module",
                                  GH_ParamAccess.item);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var slotCenters = new List<Point3d>();
            var productionGeometryRaw = new List<IGH_GeometricGoo>();
            var nameRaw = new ModuleName();
            var basePlane = new Plane();
            var slotDiagonal = new Vector3d();

            if (!DA.GetData(0, ref nameRaw)) {
                return;
            }

            if (!DA.GetDataList(1, slotCenters)) {
                return;
            }

            DA.GetDataList(2, productionGeometryRaw);

            if (!DA.GetData(3, ref basePlane)) {
                return;
            }

            if (!DA.GetData(4, ref slotDiagonal)) {
                return;
            }

            var name = nameRaw.Name.ToLower();

            if (name.Length == 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module name is empty.");
                return;
            }

            if (Config.RESERVED_NAMES.Contains(name)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "The module name cannot be \"" + name +
                                  "\" because it is reserved by Monoceros.");
                return;
            }

            if (name.Contains(":") || name.Contains("=")) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "The module name cannot contain \":\" or \"=\"");
                return;
            }

            if (!slotCenters.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Failed to collect Slot Points.");
                return;
            }

            if (slotDiagonal.X <= 0 || slotDiagonal.Y <= 0 || slotDiagonal.Z <= 0) {
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
            var submoduleCenters = slotCenters.Select(center => {
                center.Transform(normalizationTransform);
                center.Transform(worldAlignmentTransform);
                return new Point3i(center);
            }).Distinct()
            .ToList();

            var productionGeometryClean = productionGeometryRaw
               .Where(goo => goo != null)
               .Select(ghGeo => {
                   var geo = ghGeo.Duplicate();
                   return GH_Convert.ToGeometryBase(geo);
               });

            var module = new Module(name,
                                    productionGeometryClean,
                                    basePlane,
                                    submoduleCenters,
                                    slotDiagonal);

            if (module.Geometry.Count != productionGeometryRaw.Count) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Some geometry was not used.");
            }

            if (!module.IsValid) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, module.IsValidWhyNot);
            }

            DA.SetData(0, module);
        }


        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to
        /// septenary), each of which can be combined with the
        /// GH_Exposure.obscure flag, which ensures the component will only be
        /// visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.M;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("2A632429-44EF-4970-ABAC-27E948858689");
    }
}
