using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;

namespace WFCPlugin {
    // TODO: Consider doing this entirely in Grasshopper as a user object / cluster
    public class ComponentRuleIndifferentUnused : GH_Component {
        public ComponentRuleIndifferentUnused( )
            : base("Rule Typed Indifferent For Unused Connectors",
                   "RuleIndiffUnused",
                   "Allow unused connectors of a WFC Module to connect to any opposite " +
                   "indifferent connector of any WFC Module.",
                   "WaveFunctionCollapse",
                   "Rule") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleParameter(),
                                  "Module",
                                  "M",
                                  "WFC module for indifferent rule generation",
                                  GH_ParamAccess.item);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "All existing WFC rules",
                                  GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "WFC Rules",
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

            DA.GetDataList(1, existingRules);

            var thisModulesUsedConnectors = new List<int>();

            foreach (var existingRule in existingRules) {
                if (existingRule.IsExplicit() &&
                    existingRule.Explicit.SourceModuleName == module.Name
                    ) {
                    thisModulesUsedConnectors.Add(existingRule.Explicit.SourceConnectorIndex);
                }

                if (existingRule.IsExplicit() &&
                    existingRule.Explicit.TargetModuleName == module.Name
                    ) {
                    thisModulesUsedConnectors.Add(existingRule.Explicit.TargetConnectorIndex);
                }

                if (existingRule.IsTyped() &&
                    existingRule.Typed.ModuleName == module.Name
                    ) {
                    thisModulesUsedConnectors.Add(existingRule.Typed.ConnectorIndex);
                }
            }

            var rules = module.Connectors
                .Where((_, index) => !thisModulesUsedConnectors.Contains(index))
                .Select((connector, index) => new Rule(module.Name, index, type));

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
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.C;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("3AE4F441-BD29-4977-A259-5C8FE84685E0");
    }
}
