using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;

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
            pManager.AddIntegerParameter("Layers",
                                         "L",
                                         "Number of outer layers to identify",
                                         GH_ParamAccess.item,
                                         1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddBooleanParameter("Boolean Pattern",
                                         "B",
                                         "True if the Monoceros Slot is on the boundary of the envelope",
                                         GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var slots = new List<Slot>();
            var layers = 1;

            if (!DA.GetDataList(0, slots)) {
                return;
            }

            if (!DA.GetData(1, ref layers)) {
                return;
            }

            var invalidCount = slots.RemoveAll(slot => slot == null || !slot.IsValid);

            if (invalidCount > 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  invalidCount + " Slots are null or invalid and were removed.");
            }

            if (!slots.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Slots collected.");
                return;
            }

            var diagonal = slots.First().Diagonal;

            if (slots.Any(slot => slot.Diagonal != diagonal)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slots are not defined with the same diagonal.");
                return;
            }

            if (!Slot.AreSlotLocationsUnique(slots)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot locations are not unique.");
                return;
            }

            Point3i.ComputeBlockBoundsWithOffset(slots,
                                                 new Point3i(0, 0, 0),
                                                 out var minPoint,
                                                 out var maxPoint);
            var blockLength = Point3i.ComputeBlockLength(minPoint, maxPoint);

            var depthPattern = Enumerable.Repeat(0, blockLength).ToArray();

            var slotIndices1D = slots.Select(slot => slot.RelativeCenter.To1D(minPoint, maxPoint));

            foreach (var index1D in slotIndices1D) {
                depthPattern[index1D] = int.MaxValue;
            }

            var relativeNeighbors = new List<Point3i>();
            for (var z = -1; z <= 1; z++) {
                for (var y = -1; y <= 1; y++) {
                    for (var x = -1; x <= 1; x++) {
                        if (!(x == 0 && y == 0 && z == 0)) {
                            relativeNeighbors.Add(new Point3i(x, y, z));
                        }
                    }
                }
            }

            var visitStack = new List<int>();


            for (var index1D = 0; index1D < depthPattern.Length; index1D++) {
                if (depthPattern[index1D] > 0) {
                    var position3D = Point3i.From1D(index1D, minPoint, maxPoint);
                    if (relativeNeighbors
                        .Select(relative => position3D + relative)
                        .Any(neighbor3D => IsOutOfBounds(neighbor3D, minPoint, maxPoint)
                            || depthPattern[neighbor3D.To1D(minPoint, maxPoint)] == 0)) {
                        visitStack.Add(index1D);
                    }
                }
            }

            for (var depth = 1; depth <= layers; depth++) {
                var nextVisitStack = new List<int>();
                foreach (var index1D in visitStack) {
                    depthPattern[index1D] = depth;
                    var position3D = Point3i.From1D(index1D, minPoint, maxPoint);
                    var neighbors1DDeeper = relativeNeighbors
                                            .Select(relative => position3D + relative)
                                            .Where(neighbor3D => !IsOutOfBounds(neighbor3D, minPoint, maxPoint))
                                            .Select(neighbor3D => neighbor3D.To1D(minPoint, maxPoint))
                                            .Where(neighbor1D => depthPattern[neighbor1D] > depth);
                    nextVisitStack.AddRange(neighbors1DDeeper);
                }
                visitStack = nextVisitStack;
            }

            var layerPattern = slotIndices1D.Select(index1D => {
                var depth = depthPattern[index1D];
                return depth > 0 && depth <= layers;
            });

            DA.SetDataList(0, layerPattern);
        }

        private bool IsOutOfBounds(Point3i p, Point3i min, Point3i max) {
            return p.X < min.X
                || p.X > max.X
                || p.Y < min.Y
                || p.Y > max.Y
                || p.Z < min.Z
                || p.Z > max.Z;
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
        protected override Bitmap Icon => Properties.Resources.slot_are_boundary;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("079C0EE8-72D2-4591-9342-D0AE027FAF16");

    }
}
