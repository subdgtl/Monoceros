using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;

namespace WFCPlugin {
    public class ComponentCollectRules : GH_Component {
        public ComponentCollectRules( ) : base("Collect Rules",
                                               "CollectRules",
                                               "Collect, convert to Explicit, deduplicate and " +
                                               "remove disallowed WFC Rules. Automatically " +
                                               "generates an Out Module and its Rules.",
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
                                  GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules Allowed",
                                  "RA",
                                  "All allowed WFC rules",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules Disallowed",
                                  "RD",
                                  "All disallowed WFC rules (optional)",
                                  GH_ParamAccess.list);
            pManager[2].Optional = true;
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
            var modules = new List<Module>();
            var allowed = new List<Rule>();
            var disallowed = new List<Rule>();

            if (!DA.GetDataList(0, modules)) {
                return;
            }

            if (!DA.GetDataList(1, allowed)) {
                return;
            }

            DA.GetDataList(2, disallowed);

            var disallowedExplicit = disallowed.Where(rule => rule.IsExplicit());
            var disallowedTyped = disallowed
                .Where(rule => rule.IsTyped())
                .Select(rule => rule.Typed);
            var disallowedTypedUnwrapped = disallowedTyped
                .SelectMany(ruleTyped => ruleTyped.ToRuleExplicit(disallowedTyped, modules))
                .Select(ruleExplicit => new Rule(ruleExplicit));

            var disallowedProcessed = disallowedExplicit
                .Concat(disallowedTypedUnwrapped)
                .Distinct();


            var allowedExplicit = new List<Rule>();
            var allowedTypedUnwrapped = new List<Rule>();
            var allowedTypedWrapped = new List<Rule>();


            Module.GenerateEmptySingleModule(Config.OUTER_MODULE_NAME,
                                             Config.INDIFFERENT_TAG,
                                             new Rhino.Geometry.Vector3d(1, 1, 1),
                                             out var moduleOut,
                                             out var rulesOut);
            allowed.AddRange(
                rulesOut.Select(ruleExplicit => new Rule(ruleExplicit))
                );
            modules.Add(moduleOut);

            var allowedTyped = allowed
                .Where(rule => rule.IsTyped())
                .Select(rule => rule.Typed);

            foreach (var rule in allowed) {
                if (rule.IsExplicit()) {
                    allowedExplicit.Add(rule);
                    continue;
                }
                if (rule.IsTyped()) {
                    var typed = rule.Typed;
                    if (
                        disallowedProcessed.Any(ruleDisallowed =>
                            ruleDisallowed.IsExplicit() &&
                            (ruleDisallowed.Explicit.SourceModuleName == typed.ModuleName &&
                             ruleDisallowed.Explicit.SourceConnectorIndex == typed.ConnectorIndex) ||
                            (ruleDisallowed.Explicit.TargetModuleName == typed.ModuleName &&
                             ruleDisallowed.Explicit.TargetConnectorIndex == typed.ConnectorIndex)
                            )
                        ) {
                        var typedUnwrapped = typed
                            .ToRuleExplicit(allowedTyped, modules)
                            .Select(ruleExplicit => new Rule(ruleExplicit));
                        allowedTypedUnwrapped.AddRange(typedUnwrapped);
                    } else {
                        allowedTypedWrapped.Add(rule);
                    }
                }
            }

            var allowedProcessed = allowedExplicit
                .Concat(allowedTypedUnwrapped)
                .Concat(allowedTypedWrapped)
                .Distinct();

            var rules = allowedProcessed.Except(disallowedProcessed);

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
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override Bitmap Icon => Properties.Resources.C;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("41CC16C9-739A-41C3-B37A-97969D6D5DAF");
    }
}
