using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Monoceros {
    public class ComponentAreSlotsBoundary : GH_Component {

        public ComponentAreSlotsBoundary( ) : base("Are Slots Boundary",
                                                   "AreSlotsBound",
                                                   "Are Monoceros Slots on the boundary of the world?",
                                                   "Monoceros",
                                                   "Slot") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new SlotParameter(),
                                  "Slots",
                                  "S",
                                  "All Monoceros Slots",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddBooleanParameter("Boolean Pattern",
                                         "B",
                                         "True if the Monoceros Slot is on the boundary of the world",
                                         GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var slots = new List<Slot>();

            if (!DA.GetDataList(0, slots)) {
                return;
            }

            var slotsClean = new List<Slot>();
            foreach (var slot in slots) {
                if (slot == null || !slot.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot is null or invalid.");
                } else {
                    slotsClean.Add(slot);
                }
            }

            if (!slotsClean.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Slots collected.");
                return;
            }

            var diagonal = slotsClean.First().Diagonal;

            if (slotsClean.Any(slot => slot.Diagonal != diagonal)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slots are not defined with the same diagonal.");
                return;
            }

            if (!Slot.AreSlotLocationsUnique(slotsClean)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot locations are not unique.");
                return;
            }

            var boundaryPattern = Enumerable.Repeat(true, slotsClean.Count).ToList();

            for (var i = 0; i < slotsClean.Count; i++) {
                var slot = slotsClean[i];
                var neighborsCount = 0;
                foreach (var other in slotsClean) {
                    if (slot.RelativeCenter.IsNeighbor(other.RelativeCenter) && ++neighborsCount == 6) {
                        boundaryPattern[i] = false;
                        break;
                    }
                }
            }

            DA.SetDataList(0, boundaryPattern);
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to
        /// septenary), each of which can be combined with the
        /// GH_Exposure.obscure flag, which ensures the component will only be
        /// visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override Bitmap Icon => Properties.Resources.S;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("079C0EE8-72D2-4591-9342-D0AE027FAF16");

    }
}
