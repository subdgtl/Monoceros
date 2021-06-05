using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;

namespace Monoceros {
    // TODO: Consider doing this entirely in Grasshopper as a user object / cluster
    public class ComponentRuleIndifferentUnused : GH_Component {
        public ComponentRuleIndifferentUnused( )
            : base("Indifferent Rules for unused Connectors",
                   "RuleIndiffUnused",
                   "Unused connectors of Monoceros Modules connect to any opposite " +
                   "indifferent connector of any Monoceros Module.",
                   "Monoceros",
                   "Rule") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleParameter(),
                                  "Module",
                                  "M",
                                  "Monoceros module for indifferent rule generation",
                                  GH_ParamAccess.item);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "All existing Monoceros rules",
                                  GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new RuleParameter(),
                                  "Rules Indifferent",
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
            var module = new Module();
            var existingRules = new List<Rule>();
            var type = Config.INDIFFERENT_TAG;

            if (!DA.GetData(0, ref module)) {
                return;
            }

            if (!DA.GetDataList(1, existingRules)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No Existing Rules provided. " +
                    "All Module Connectors were marked Indifferent.");
            }

            if (module == null || !module.IsValid) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The module is null or invalid.");
            }

            var connectorCount = module.Connectors.Count;
            var connectorUsePattern = Enumerable.Repeat(false, module.Connectors.Count).ToList();

            foreach (var existingRule in existingRules) {
                if (existingRule == null || !existingRule.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The rule is null or invalid.");
                    continue;
                }

                if (existingRule.IsExplicit &&
                    existingRule.Explicit.SourceModuleName == module.Name &&
                    existingRule.Explicit.SourceConnectorIndex < connectorCount
                    ) {
                    connectorUsePattern[existingRule.Explicit.SourceConnectorIndex] = true;
                }

                if (existingRule.IsExplicit &&
                    existingRule.Explicit.TargetModuleName == module.Name &&
                    existingRule.Explicit.TargetConnectorIndex < connectorCount
                    ) {
                    connectorUsePattern[existingRule.Explicit.TargetConnectorIndex] = true;
                }

                if (existingRule.IsTyped &&
                    existingRule.Typed.ModuleName == module.Name &&
                    existingRule.Typed.ConnectorIndex < connectorCount
                    ) {
                    connectorUsePattern[existingRule.Typed.ConnectorIndex] = true;
                }
            }

            var rules = new List<Rule>();
            for (var connectorIndex = 0; connectorIndex < connectorUsePattern.Count; connectorIndex++) {
                var used = connectorUsePattern[connectorIndex];
                if (!used) {
                    var rule = new Rule(module.Name, (uint)connectorIndex, type);
                    if (rule.IsValid) {
                        rules.Add(rule);
                    } else {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, rule.IsValidWhyNot);
                    }
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.rule_indifferent_unused;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("3AE4F441-BD29-4977-A259-5C8FE84685E0");
    }
}
