using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;

namespace Monoceros {
    public class ComponentAddSlotsBoundary : GH_Component {

        public ComponentAddSlotsBoundary( ) : base("Add Boundary Layer",
                                                   "AddSlotBound",
                                                   "Add one layer of Monoceros Slots around the existing boundaries.",
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
            pManager.AddPointParameter("Slot Centers",
                                       "Pt",
                                       "Points ready to be used as Monoceros Slot centers",
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

            var potentialRelativeNeighborCenters = new List<Point3i> {
                new Point3i(-1, 0, 0),
                new Point3i(0, -1, 0),
                new Point3i(0, 0, -1),
                new Point3i(1, 0, 0),
                new Point3i(0, 1, 0),
                new Point3i(0, 0, 1)
            };

            var newNeighborCenters = new List<Point3i>();

            for (var i = 0; i < slotsClean.Count; i++) {
                var slot = slotsClean[i];
                var currentNeighborCenters = new List<Point3i>();
                foreach (var other in slotsClean) {
                    if (slot.RelativeCenter.IsNeighbor(other.RelativeCenter)) {
                        currentNeighborCenters.Add(other.RelativeCenter);
                        if (currentNeighborCenters.Count == 6) {
                            break;
                        }
                    }
                }
                if (currentNeighborCenters.Count < 6) {
                    var potentialNeighborCenters = potentialRelativeNeighborCenters
                        .Select(relativeNeighborCenter => slot.RelativeCenter + relativeNeighborCenter);
                    var currentNewlNeighborCenters = potentialNeighborCenters.Except(currentNeighborCenters);
                    newNeighborCenters.AddRange(currentNewlNeighborCenters);
                }
            }

            var newNeighborCentersP3d = newNeighborCenters.Distinct().Select(p => p.ToPoint3d());

            DA.SetDataList(0, newNeighborCentersP3d);
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
        protected override Bitmap Icon => Properties.Resources.slot_add_boundary_2;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("B6F18DAC-C230-4159-A036-E6CE3E8D17AA");

    }
}
