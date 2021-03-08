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

        public ComponentScanSlotsForRules( ) : base("Scan Slots For rules",
                                                   "RulesScan",
                                                   "Scan solved Slots and extract applied Rules.",
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
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var modulesRaw = new List<Module>();
            var slotsRaw = new List<Slot>();

            if (!DA.GetDataList(0, slotsRaw)) {
                return;
            }

            if (!DA.GetDataList(1, modulesRaw)) {
                return;
            }

            var transforms = new DataTree<Transform>();
            var geometry = new DataTree<GeometryBase>();

            var slotsClean = new List<Slot>();
            foreach (var slot in slotsRaw) {
                if (slot == null || !slot.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot is null or invalid and will be skipped.");
                    continue;
                }
                if (!slot.IsDeterministic) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Slot is not deterministic and will be skipped.");
                    continue;
                }
                slotsClean.Add(slot);
            }

            var modulesClean = new List<Module>();
            foreach (var module in modulesRaw) {
                if (module == null || !module.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module is null or invalid.");
                    continue;
                }
                if (module.Geometry.Count + module.ReferencedGeometry.Count == 0) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Module \"" + module.Name + "\" contains " +
                        "no geometry and therefore will be skipped.");
                    continue;
                }

                modulesClean.Add(module);
            }


            if (!modulesClean.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Modules collected.");
                return;
            }

            var slotsNonEmpty = slotsClean.Where(slot =>
                modulesClean.Any(module => module.ContainsPart(slot.AllowedPartNames[0]))
            );

            if (!slotsNonEmpty.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Slots collected.");
                return;
            }

            var rulesForSolver = new List<RuleForSolver>();
            foreach (var slot in slotsNonEmpty) {
                foreach (var slotOther in slotsNonEmpty) {
                    if (slot.RelativeCenter.IsNeighbor(slotOther.RelativeCenter)) {
                        var neighborVector = (slotOther.RelativeCenter - slot.RelativeCenter).ToVector3d();
                        if (Direction.FromVector(neighborVector, out var direction)
                            && direction.Orientation == Orientation.Positive) {
                            var currentPart = slot.AllowedPartNames[0];
                            var otherPart = slotOther.AllowedPartNames[0];
                            var ruleForSolver = new RuleForSolver(direction.Axis, currentPart, otherPart);
                            if (!rulesForSolver.Contains(ruleForSolver)) {
                                rulesForSolver.Add(ruleForSolver);
                            }
                        }
                    }
                }
            }

            var rulesOut = new List<Rule>();
            foreach (var ruleForSolver in rulesForSolver) {
                if (RuleExplicit.FromRuleForSolver(ruleForSolver, modulesClean, out var ruleExplicit)) {
                    var rule = new Rule(ruleExplicit);
                    if (!rulesOut.Contains(rule)) {
                        rulesOut.Add(rule);
                    }
                }
            }

            rulesOut.Sort();

            DA.SetDataList(0, rulesOut);
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
        protected override Bitmap Icon => Properties.Resources.materialize;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("14CD0308-26FC-4134-AB5A-C7B89B6405BF");
    }
}
