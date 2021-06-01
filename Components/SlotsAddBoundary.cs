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

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new SlotParameter(),
                                  "Slots",
                                  "S",
                                  "All Monoceros Slots",
                                  GH_ParamAccess.list);
            pManager.AddIntegerParameter("Layers",
                                        "L",
                                        "Number of outer layers to add",
                                        GH_ParamAccess.item,
                                        1);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddPointParameter("Slot Centers",
                                       "Pt",
                                       "Points ready to be used as Monoceros Slot centers",
                                       GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA) {
            var slots = new List<Slot>();
            var layers = 1;

            if (!DA.GetDataList(0, slots)) {
                return;
            }

            if (!DA.GetData(1, ref layers)) {
                return;
            }

            if (slots.Any(slot => slot == null || !slot.IsValid)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot is null or invalid.");
            }

            var slotsClean = slots.Where(slot => slot != null || slot.IsValid);

            if (!slotsClean.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Slots collected.");
                return;
            }

            if (!Slot.AreSlotDiagonalsCompatible(slotsClean)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slots are not defined with the same diagonal.");
                return;
            }
            var diagonal = slotsClean.First().Diagonal;

            if (!Slot.AreSlotBasePlanesCompatible(slotsClean)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slots are not defined with the same base plane.");
                return;
            }
            var basePlane = slotsClean.First().BasePlane;

            if (!Slot.AreSlotLocationsUnique(slotsClean)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot centers are not unique.");
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

            var allSlotCenters = slotsClean
                .Select(slot => slot.RelativeCenter);

            var neighborCenters = allSlotCenters
                .SelectMany(center => potentialRelativeNeighborCenters.Select(newRelativeCenter => center + newRelativeCenter))
                .Distinct()
                .Except(allSlotCenters)
                .Select(p3i => p3i.ToCartesian(basePlane, diagonal))
                .ToList();

            DA.SetDataList(0, neighborCenters);
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override Bitmap Icon => Properties.Resources.slot_add_boundary;

        public override Guid ComponentGuid => new Guid("B6F18DAC-C230-4159-A036-E6CE3E8D17AA");

    }
}
