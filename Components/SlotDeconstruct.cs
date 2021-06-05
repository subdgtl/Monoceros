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
            pManager.AddParameter(new SlotParameter(),
                                  "Slot",
                                  "S",
                                  "Monoceros Slot",
                                  GH_ParamAccess.item);
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
               GH_ParamAccess.list);
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Allowed Module Names",
                                  "MN",
                                  "Initiate the slot with specified Module names allowed.",
                                  GH_ParamAccess.list);
            // The following are output as lists to preserve the same tree structure with the Module Names
            pManager.AddBooleanParameter("Is Deterministic",
                                        "Det",
                                        "The Slot allow placement of exactly one Module Part if true.",
                                        GH_ParamAccess.list);
            pManager.AddBooleanParameter("Allows All Modules",
                                         "All",
                                         "The Slot allows placement of any Module if true.",
                                         GH_ParamAccess.list
                                         );
            pManager.AddBooleanParameter("Allows Nothing",
                                         "Nil",
                                         "The Slot allows placement of no Module if true.",
                                         GH_ParamAccess.list
                                         );
            pManager.AddBooleanParameter("Is Valid",
                                        "Val",
                                        "The Slot valid for the Monoceros WFC Solver if true.",
                                        GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var slot = new Slot();
            var moduleNames = new List<ModuleName>();
            var moduleNamesProvided = false;

            if (!DA.GetData(0, ref slot)) {
                return;
            }

            if (slot == null || !slot.IsValid) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The slot is null or invalid.");
                return;
            }

            if (DA.GetDataList(1, moduleNames)) {
                moduleNamesProvided = true;
            } else {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No Modules provided." +
                    " List of Modules allowed by the Slot could not be determined.");
            }

            DA.SetDataList(0, new [] { slot.AbsoluteCenter });
            DA.SetDataList(1, new [] { slot.BasePlane });
            DA.SetDataList(2, new [] { slot.Diagonal });
            if (moduleNamesProvided
                && moduleNames != null
                && slot.AllowsAnyModule
                && slot.AllowedModuleNames.Count == 0) {
                DA.SetDataList(3, moduleNames);
            } else {
                DA.SetDataList(3, slot.AllowedModuleNames.Select(name => new ModuleName(name)));
            }
            if (moduleNamesProvided
                && moduleNames != null
                && slot.AllowedModuleNames.Count >= moduleNames.Count
                && moduleNames.All(name => slot.AllowedModuleNames.Contains(name.ToString()))) {
                DA.SetDataList(5, new [] { true });
            } else {
                DA.SetDataList(5, new [] { slot.AllowsAnyModule });
            }
            DA.SetDataList(6, new [] { slot.IsContradictory });
            if (moduleNamesProvided
                && moduleNames != null
                && (slot.AllowedModuleNames.Count > moduleNames.Count
                || !slot.AllowedModuleNames.All(name => moduleNames.Any(moduleName => moduleName.Name == name))
                )) {
                DA.SetDataList(7, new [] { false });
            } else {
                DA.SetDataList(7, new [] { slot.IsValid });
            }
            DA.SetDataList(4, new [] { slot.IsDeterministic });
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
        public override Guid ComponentGuid => new Guid("92C84CC3-2777-4A08-9204-D02B0066CF84");
    }
}
