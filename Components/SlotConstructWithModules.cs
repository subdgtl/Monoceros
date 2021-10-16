using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Monoceros {

    public class ComponentConstructSlotWithModules : GH_Component {
        public ComponentConstructSlotWithModules( )
            : base("Construct Slot with Listed Modules Allowed",
                   "SlotModules",
                   "Construct a Monoceros Slot with allowed Monoceros Module names.",
                   "Monoceros",
                   "Slot") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddPointParameter("Slot Point",
                                       "Pt",
                                       "Point inside the slot",
                                       GH_ParamAccess.item);
            pManager.AddPlaneParameter("Base Plane",
                                       "B",
                                       "Grid space base plane. Defines orientation of the grid.",
                                       GH_ParamAccess.item,
                                       Plane.WorldXY);
            pManager.AddVectorParameter(
               "Grid Slot Diagonal",
               "D",
               "World grid slot diagonal vector specifying single grid slot dimension " +
               "in base-plane-aligned XYZ axes",
               GH_ParamAccess.item,
               new Vector3d(1.0, 1.0, 1.0)
               );
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Allowed Module Names",
                                  "MN",
                                  "Initiate the slot with specified Module names allowed.",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new ModuleParameter(),
                                  "Modules",
                                  "M",
                                  "All available Monoceros Modules. (Optional)",
                                  GH_ParamAccess.list);
            pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new SlotParameter(), "Slot", "S", "Monoceros Slot", GH_ParamAccess.item);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var point = new Point3d();
            var basePlane = new Plane();
            var diagonal = new Vector3d();
            var allowedModulesRaw = new List<ModuleName>();
            var allModules = new List<Module>();
            var modulesProvided = false;

            if (!DA.GetData(0, ref point)) {
                return;
            }

            if (!DA.GetData(1, ref basePlane)) {
                return;
            }

            if (!DA.GetData(2, ref diagonal)) {
                return;
            }

            if (!DA.GetDataList(3, allowedModulesRaw)) {
                return;
            }

            if (DA.GetDataList(4, allModules)) {
                modulesProvided = true;
            }

            var allowedModules = allowedModulesRaw
                .Where(name => name != null)
                .Select(name => name.Name).Distinct();

            if (Config.RESERVED_CHARS.Any(chars => allowedModules.Contains(chars))) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input text contains " +
                "a forbidden content: :, ->, = or newline.");
                return;
            }

            if (diagonal.X <= 0 || diagonal.Y <= 0 || diagonal.Z <= 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "One or more slot dimensions are not larger than 0.");
                return;
            }

            // Scale down to unit size
            var normalizationTransform = Transform.Scale(basePlane,
                                                         1 / diagonal.X,
                                                         1 / diagonal.Y,
                                                         1 / diagonal.Z);
            // Orient to the world coordinate system
            var worldAlignmentTransform = Transform.PlaneToPlane(basePlane, Plane.WorldXY);
            point.Transform(normalizationTransform);
            point.Transform(worldAlignmentTransform);
            // Round point location
            // Slot dimension is for the sake of this calculation 1,1,1
            var slotCenterPoint = new Point3i(point);

            var allModulesCount = modulesProvided
                ? allModules.Aggregate(0, (count, module) => count + module.PartCenters.Count)
                : 0;

            var allowedParts = modulesProvided
                ? allModules
                .Where(module => allowedModules.Contains(module.Name))
                .SelectMany(module => module.PartNames)
                .ToList()
                : new List<string>();

            var slot = new Slot(basePlane,
                                slotCenterPoint,
                                diagonal,
                                false,
                                allowedModules.ToList(),
                                allowedParts,
                                allModulesCount);

            if (!slot.IsValid) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, slot.IsValidWhyNot);
                return;
            }

            DA.SetData(0, slot);
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.slot_construct;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("3CC63A96-F075-4C39-9725-D5547959BE55");
    }
}
