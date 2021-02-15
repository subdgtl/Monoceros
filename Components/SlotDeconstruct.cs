using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Monoceros {

    public class ComponentDeconstructSlot : GH_Component {
        public ComponentDeconstructSlot( )
            : base("Deconstruct Slot",
                   "DeconSlot",
                   "Deconstruct a Monoceros Slot into its center point, base plane, diagonal " +
                   "and list of allowed Monoceros Modules.",
                   "Monoceros",
                   "Slot") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new SlotParameter(), "Slot", "S", "Monoceros Slot", GH_ParamAccess.item);
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Module Names",
                                  "MN",
                                  "All available Monoceros Module names. (Optional)",
                                  GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddPointParameter("Center",
                                       "Pt",
                                       "Center point of the slot",
                                       GH_ParamAccess.list);
            pManager.AddPlaneParameter("Base Plane",
                                       "B",
                                       "Grid space base plane. Defines orientation of the grid.",
                                       GH_ParamAccess.list);
            pManager.AddVectorParameter(
               "Diagonal",
               "D",
               "World grid slot diagonal vector specifying single grid slot dimension " +
               "in base-plane-aligned XYZ axes",
               GH_ParamAccess.list
               );
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Allowed Module Names",
                                  "MN",
                                  "Initiate the slot with specified Module names allowed.",
                                  GH_ParamAccess.list);
            pManager.AddBooleanParameter("Allows All Modules",
                                         "A",
                                         "The Slot allows any Module to be placed in it if true.",
                                         GH_ParamAccess.list
                                         );
            pManager.AddBooleanParameter("Allows Nothing",
                                         "N",
                                         "The Slot allows no Module to be placed in it if true.",
                                         GH_ParamAccess.list
                                         );
            pManager.AddBooleanParameter("Is Valid",
                                        "V",
                                        "Is the Slot valid for the Monoceros WFC Solver?",
                                        GH_ParamAccess.item);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var slot = new Slot();
            var moduleNames = new List<ModuleName>();
            var modulesProvided = false;

            if (!DA.GetData(0, ref slot)) {
                return;
            }

            if (slot == null || !slot.IsValid) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The slot is null or invalid.");
                return;
            }

            if (DA.GetDataList(1, moduleNames)) {
                modulesProvided = true;
            }

            DA.SetDataList(0, new List<Point3d>() { slot.AbsoluteCenter });
            DA.SetDataList(1, new List<Plane>() { slot.BasePlane });
            DA.SetDataList(2, new List<Vector3d>() { slot.Diagonal });
            if (modulesProvided
                && moduleNames != null
                && slot.AllowsAnyModule
                && slot.AllowedModuleNames.Count == 0) {
                DA.SetDataList(3, moduleNames);
            } else {
                DA.SetDataList(3, slot.AllowedModuleNames.Select(name => new ModuleName(name)));
            }
            if (modulesProvided
                && moduleNames != null
                && slot.AllowedModuleNames.Count >= moduleNames.Count
                && moduleNames.All(name => slot.AllowedModuleNames.Contains(name.ToString()))) {
                DA.SetDataList(4, new List<bool>() { true });
            } else {
                DA.SetDataList(4, new List<bool>() { slot.AllowsAnyModule });
            }
            DA.SetDataList(5, new List<bool>() { slot.AllowsNothing });
            if (modulesProvided
                && moduleNames != null
                && (slot.AllowedModuleNames.Count > moduleNames.Count
                || !slot.AllowedModuleNames.All(name => moduleNames.Any(moduleName => moduleName.Name == name))
                )) {
                DA.SetData(6, false);
            } else {
                DA.SetData(6, slot.IsValid);
            }
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.slot_deconstruct;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("595773DB-868E-4999-90C0-1418FF3AC077");
    }
}
