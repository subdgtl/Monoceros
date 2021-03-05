using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Monoceros {
    public class ComponentRuleOuterFromPoint : GH_Component {
        public ComponentRuleOuterFromPoint( )
            : base("Rule At Boundary From Point",
                   "RuleBoundPt",
                   "Rule allowing the Monoceros Module to touch the boundary of the Envelope with " +
                   "a Connector marked with a Point. All connectors with an Indifferent Rule can " +
                   "automatically touch the boundary.",
                   "Monoceros",
                   "Rule") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleParameter(),
                                  "Modules",
                                  "M",
                                  "All available Monoceros modules",
                                  GH_ParamAccess.list);
            pManager.AddPointParameter("Point Tag",
                                       "Pt",
                                       "Point marking a connector",
                                       GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "Monoceros Rules",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var modules = new List<Module>();
            var point = new Point3d();
            var targetName = Config.OUTER_MODULE_NAME;

            if (!DA.GetDataList(0, modules)) {
                return;
            }

            if (!DA.GetData(1, ref point)) {
                return;
            }

            var rules = new List<Rule>();

            foreach (var module in modules) {
                if (module == null || !module.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The Module is null or invalid.");
                    continue;
                }
                for (var connectorIndex = 0; connectorIndex < module.Connectors.Count; connectorIndex++) {
                    var connector = module.Connectors[connectorIndex];
                    var oppositeDirection = connector.Direction.ToFlipped();
                    if (connector.ContaininsPoint(point)) {
                        rules.Add(
                            new Rule(module.Name,
                                     (uint)connectorIndex,
                                     targetName,
                                     oppositeDirection.DirectionToSingleModuleConnectorIndex())
                            );
                    }
                }
            }

            if (!rules.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                                  "The point does not mark any Module connector.");
            }

            foreach (var rule in rules) {
                if (!rule.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, rule.IsValidWhyNot);
                }
            }

            DA.SetDataList(0, rules);
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to
        /// septenary), each of which can be combined with the
        /// GH_Exposure.obscure flag, which ensures the component will only be
        /// visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.rule_out;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("1BC41AF7-60CC-48C4-9133-704CAB800DC0");
    }
}
