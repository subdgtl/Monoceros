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

            var usePattern = Enumerable.Repeat(false, blockLength).ToArray();

            foreach (var slot in slots) {
                var index1D = slot.RelativeCenter.To1D(minPoint, maxPoint);
                usePattern[index1D] = true;
            }

            var minDepth = Enumerable.Repeat(int.MaxValue, blockLength).ToArray();

            for (var y = minPoint.Y; y <= maxPoint.Y; y++) {
                for (var z = minPoint.Z; z <= maxPoint.Z; z++) {
                    var depth = 0;
                    for (var x = minPoint.X; x <= maxPoint.X; x++) {
                        var p = new Point3i(x, y, z);
                        var index1D = p.To1D(minPoint, maxPoint);
                        if (usePattern[index1D]) {
                            depth = Math.Min(minDepth[index1D], depth + 1);
                        } else {
                            depth = 0;
                        }
                        minDepth[index1D] = depth;
                    }
                }
            }

            for (var y = minPoint.Y; y <= maxPoint.Y; y++) {
                for (var z = minPoint.Z; z <= maxPoint.Z; z++) {
                    var depth = 0;
                    for (var x = maxPoint.X; x >= minPoint.X; x--) {
                        var p = new Point3i(x, y, z);
                        var index1D = p.To1D(minPoint, maxPoint);
                        if (usePattern[index1D]) {
                            depth = Math.Min(minDepth[index1D], depth + 1);
                        } else {
                            depth = 0;
                        }
                        minDepth[index1D] = depth;
                    }
                }
            }

            for (var x = minPoint.X; x <= maxPoint.X; x++) {
                for (var z = minPoint.Z; z <= maxPoint.Z; z++) {
                    var depth = 0;
                    for (var y = minPoint.Y; y <= maxPoint.Y; y++) {
                        var p = new Point3i(x, y, z);
                        var index1D = p.To1D(minPoint, maxPoint);
                        if (usePattern[index1D]) {
                            depth = Math.Min(minDepth[index1D], depth + 1);
                        } else {
                            depth = 0;
                        }
                        minDepth[index1D] = depth;
                    }
                }
            }

            for (var x = minPoint.X; x <= maxPoint.X; x++) {
                for (var z = minPoint.Z; z <= maxPoint.Z; z++) {
                    var depth = 0;
                    for (var y = maxPoint.Y; y >= minPoint.Y; y--) {
                        var p = new Point3i(x, y, z);
                        var index1D = p.To1D(minPoint, maxPoint);
                        if (usePattern[index1D]) {
                            depth = Math.Min(minDepth[index1D], depth + 1);
                        } else {
                            depth = 0;
                        }
                        minDepth[index1D] = depth;
                    }
                }
            }

            for (var x = minPoint.X; x <= maxPoint.X; x++) {
                for (var y = minPoint.Y; y <= maxPoint.Y; y++) {
                    var depth = 0;
                    for (var z = minPoint.Z; z <= maxPoint.Z; z++) {
                        var p = new Point3i(x, y, z);
                        var index1D = p.To1D(minPoint, maxPoint);
                        if (usePattern[index1D]) {
                            depth = Math.Min(minDepth[index1D], depth + 1);
                        } else {
                            depth = 0;
                        }
                        minDepth[index1D] = depth;
                    }
                }
            }

            for (var x = minPoint.X; x <= maxPoint.X; x++) {
                for (var y = minPoint.Y; y <= maxPoint.Y; y++) {
                    var depth = 0;
                    for (var z = maxPoint.Z; z >= minPoint.Z; z--) {
                        var p = new Point3i(x, y, z);
                        var index1D = p.To1D(minPoint, maxPoint);
                        if (usePattern[index1D]) {
                            depth = Math.Min(minDepth[index1D], depth + 1);
                        } else {
                            depth = 0;
                        }
                        minDepth[index1D] = depth;
                    }
                }
            }

            foreach (var slot in slots) {
                var index1D = slot.RelativeCenter.To1D(minPoint, maxPoint);
                usePattern[index1D] = true;
            }

            var layerPattern = slots.Select(slot => {
                var index1D = slot.RelativeCenter.To1D(minPoint, maxPoint);
                var depth = minDepth[index1D];
                return depth > 0 && depth <= layers;
            });

            DA.SetDataList(0, layerPattern);
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
