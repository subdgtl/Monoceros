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
    public class ComponentScanSlotsForRules : GH_Component {

        public ComponentScanSlotsForRules( ) : base("Scan Slots for Rules",
                                                   "RulesScan",
                                                   "Scan Slots and extract Rules describing " +
                                                   "connections in them.",
                                                   "Monoceros",
                                                   "Rule") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new SlotParameter(),
                                  "Slots",
                                  "S",
                                  "Monoceros Slots",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new ModuleParameter(),
                                  "Modules",
                                  "M",
                                  "All Monoceros Modules",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new RuleParameter(),
                                  "Rules Explicit",
                                  "R",
                                  "Explicit Monoceros Rules",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(),
                                  "Boundary Rules",
                                  "BR",
                                  "Rules allowing Modules to be placed to the boundary " +
                                  "of the Slot Envelope.",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var modules = new List<Module>();
            var slots = new List<Slot>();

            if (!DA.GetDataList(0, slots)) {
                return;
            }

            if (!DA.GetDataList(1, modules)) {
                return;
            }

            var invalidSlotCount = slots.RemoveAll(slot => slot == null || !slot.IsValid);

            if (invalidSlotCount > 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  invalidSlotCount + " Slots are null or invalid and were removed.");
            }

            if (!Slot.AreSlotDiagonalsCompatible(slots)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slots are not defined with the same diagonal.");
                return;
            }
            var diagonal = slots.First().Diagonal;

            if (!Slot.AreSlotBasePlanesCompatible(slots)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slots are not defined with the same base plane.");
                return;
            }
            var slotBasePlane = slots.First().BasePlane;

            if (!Slot.AreSlotLocationsUnique(slots)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot centers are not unique.");
                return;
            }


            var nonDeterministicSlotCount = slots.Count(slot => !slot.IsDeterministic);
            if (nonDeterministicSlotCount > 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    nonDeterministicSlotCount + " Slots are non-deterministic. " +
                    "All possible Rule combinations were extracted.");
            }

            var invalidModuleCount = modules.RemoveAll(module => module == null || !module.IsValid);

            if (invalidModuleCount > 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  invalidModuleCount + " Modules are null or invalid and were removed.");
            }

            if (!modules.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Modules collected.");
                return;
            }

            var allPartsCount = modules
               .Aggregate(0, (sum, module) => sum + module.PartCenters.Count);


            Module.GenerateEmptySingleModule(Config.OUTER_MODULE_NAME,
                                             Config.INDIFFERENT_TAG,
                                             new Rhino.Geometry.Vector3d(1, 1, 1),
                                             out var moduleOut,
                                             out var _);
            modules.Add(moduleOut);

            // Define world space (slots bounding box + 1 layer padding)
            Point3i.ComputeBlockBoundsWithOffset(slots, new Point3i(1, 1, 1), out var worldMin, out var worldMax);
            var worldLength = Point3i.ComputeBlockLength(worldMin, worldMax);
            var worldSlots = Enumerable.Repeat<Slot>(null, worldLength).ToList();
            foreach (var slot in slots) {
                var index = slot.RelativeCenter.To1D(worldMin, worldMax);
                worldSlots[index] = slot;
            }

            // Fill unused world slots with Out modules
            for (var i = 0; i < worldSlots.Count; i++) {
                var slot = worldSlots[i];
                var relativeCenter = Point3i.From1D(i, worldMin, worldMax);
                if (slot == null) {
                    worldSlots[i] = new Slot(slotBasePlane,
                                             relativeCenter,
                                             diagonal,
                                             false,
                                             new List<string>() { moduleOut.Name },
                                             moduleOut.PartNames,
                                             allPartsCount + 1);
                }
            }

            var slotInvalidWithModulesCount = worldSlots
                .RemoveAll(slot =>
                    slot.AllowedModuleNames.Any(moduleName =>
                        !modules.Any(module => module.Name == moduleName)));

            if (slotInvalidWithModulesCount > 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  slotInvalidWithModulesCount + " Slots refer to an unavailable " +
                                  "Module and were removed.");
            }

            var slotInvalidWithModulePartsCount = worldSlots
                .RemoveAll(slot =>
                    slot.AllowedPartNames.Any()
                    && slot.AllowedPartNames.Any(modulePartName =>
                        !modules.Any(module => module.PartNames.Contains(modulePartName))));

            if (slotInvalidWithModulePartsCount > 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  slotInvalidWithModulePartsCount + " Slots refer to an " +
                                  "unavailable Module Part and were removed.");
            }


            if (!worldSlots.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Slots collected.");
                return;
            }

            var allModulePartNames = modules.SelectMany(module => module.PartNames);

            var rulesCollected = new List<Rule>();
            foreach (var slot in worldSlots) {
                foreach (var slotOther in worldSlots) {
                    if (slot.RelativeCenter.IsNeighbor(slotOther.RelativeCenter)) {
                        var neighborVector = (slotOther.RelativeCenter - slot.RelativeCenter).ToVector3d();
                        if (Direction.FromVector(neighborVector, out var direction)
                            && direction.Orientation == Orientation.Positive) {

                            IEnumerable<string> currentPartNames = null;
                            if (slot.AllowsAnyModule) {
                                currentPartNames = allModulePartNames;
                            } else if (slot.AllowedPartNames.Any()) {
                                currentPartNames = slot.AllowedPartNames;
                            } else {
                                currentPartNames = slot.AllowedModuleNames
                                    .SelectMany(moduleName =>
                                        modules.First(module => module.Name == moduleName).PartNames)
                                    .Distinct();
                            }

                            IEnumerable<string> otherPartNames = null;
                            if (slotOther.AllowsAnyModule) {
                                otherPartNames = allModulePartNames;
                            } else if (slotOther.AllowedPartNames.Any()) {
                                otherPartNames = slotOther.AllowedPartNames;
                            } else {
                                otherPartNames = slotOther.AllowedModuleNames
                                    .SelectMany(moduleName =>
                                        modules.First(module => module.Name == moduleName).PartNames)
                                    .Distinct();
                            }

                            foreach (var currentPart in currentPartNames) {
                                foreach (var otherPart in otherPartNames) {
                                    if (modules.Any(module => module.ContainsPart(currentPart))
                                        && modules.Any(module => module.ContainsPart(otherPart))) {
                                        var ruleForSolver = new RuleForSolver(direction.Axis, currentPart, otherPart);
                                        if (RuleExplicit.FromRuleForSolver(ruleForSolver, modules, out var ruleExplicit)) {
                                            rulesCollected.Add(new Rule(ruleExplicit));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var rulesCollectedInsideEnvelope = new List<Rule>();
            var rulesCollectedOnBoundaryOfEnvelope = new List<Rule>();
            foreach (var rule in rulesCollected.Distinct()) {
                if (rule.UsesModule(moduleOut.Name)) {
                    rulesCollectedOnBoundaryOfEnvelope.Add(rule);
                } else {
                    rulesCollectedInsideEnvelope.Add(rule);
                }
            }

            rulesCollectedOnBoundaryOfEnvelope
                .RemoveAll(rule => rule.Explicit.SourceModuleName == Config.OUTER_MODULE_NAME
                && rule.Explicit.TargetModuleName == Config.OUTER_MODULE_NAME);

            rulesCollectedInsideEnvelope.Sort();
            rulesCollectedOnBoundaryOfEnvelope.Sort();
            DA.SetDataList(0, rulesCollectedInsideEnvelope);
            DA.SetDataList(1, rulesCollectedOnBoundaryOfEnvelope);
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to
        /// septenary), each of which can be combined with the
        /// GH_Exposure.obscure flag, which ensures the component will only be
        /// visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.quinary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override Bitmap Icon => Properties.Resources.rules_from_slots;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("14CD0308-26FC-4134-AB5A-C7B89B6405BF");
    }
}
