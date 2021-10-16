using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;

namespace Monoceros {
    public class ComponentUnwrapRules : GH_Component {
        public ComponentUnwrapRules( )
            : base("Unwrap Typed Rules",
                   "UnwrapRules",
                   "Convert Monoceros Typed Rules into Monoceros Explicit Rules and deduplicate.",
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
                                  "All Monoceros Modules",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules To Unwrap",
                                  "R",
                                  "Monoceros Rules to unwrap (Explicit Rules will pass through " +
                                  "intact).",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(),
                                  "All Typed Rules",
                                  "TR",
                                  "All Monoceros Rules needed for unwrapping (Explicit Rules " +
                                  "will be ignored). (Optional)",
                                  GH_ParamAccess.list);
            pManager[2].Optional = true;
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
            var modules = new List<Module>();
            var rulesToUnwrap = new List<Rule>();
            var allRules = new List<Rule>();

            if (!DA.GetDataList(0, modules)) {
                return;
            }

            if (!DA.GetDataList(1, rulesToUnwrap)) {
                return;
            }

            if (!DA.GetDataList(2, allRules)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                                  "List of All Typed Rules was not provided. The Rules to be " +
                                  "Unwrapped were considered to be all Rules in the setup.");
            }

            var invalidModuleCount = modules.RemoveAll(module => module == null || !module.IsValid);

            if (invalidModuleCount > 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  invalidModuleCount + " Modules are null or invalid and were removed.");
            }

            Module.GenerateEmptySingleModule(Config.OUTER_MODULE_NAME,
                                             Config.INDIFFERENT_TAG,
                                             new Rhino.Geometry.Vector3d(1, 1, 1),
                                             out var moduleOut,
                                             out var rulesOut);
            modules.Add(moduleOut);

            var invalidRuleCount = rulesToUnwrap
                .RemoveAll(rule => rule == null || !rule.IsValid || !rule.IsValidWithModules(modules));

            if (invalidRuleCount > 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  invalidRuleCount + " Rules are null or invalid and were removed.");
            }

            var invalidAllRulesCount = allRules
                .RemoveAll(rule => rule == null || !rule.IsValid || !rule.IsValidWithModules(modules));

            if (invalidAllRulesCount > 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  invalidAllRulesCount + " of All Rules are null or invalid and " +
                                  "were removed.");
            }


            var rulesToUnwrapTyped = rulesToUnwrap
                .Where(rule => rule.IsTyped)
                .Select(rule => rule.Typed);

            var typesToUnwrap = rulesToUnwrapTyped.Select(rule => rule.ConnectorType).Distinct();

            allRules
                .RemoveAll(rule => rule.IsExplicit || !typesToUnwrap.Contains(rule.Typed.ConnectorType));

            var allRulesTyped = allRules
                .Where(rule => rule.IsTyped)
                .Select(rule => rule.Typed)
                .Concat(rulesOut)
                .Concat(rulesToUnwrapTyped)
                .Distinct();

            // -----

            var allTypedByType = new Dictionary<string, List<RuleTyped>>();
            foreach (var rule in allRulesTyped) {
                var type = rule.ConnectorType;
                if (allTypedByType.ContainsKey(type)) {
                    allTypedByType[type].Add(rule);
                } else {
                    allTypedByType.Add(type, new List<RuleTyped>() { rule });
                }
            }

            var toUnwrapByType = new Dictionary<string, List<RuleTyped>>();
            foreach (var rule in rulesToUnwrapTyped) {
                var type = rule.ConnectorType;
                if (toUnwrapByType.ContainsKey(type)) {
                    toUnwrapByType[type].Add(rule);
                } else {
                    toUnwrapByType.Add(type, new List<RuleTyped>() { rule });
                }
            }

            var unwrappedExplicit = new List<Rule>();
            foreach (var entry in allTypedByType) {
                var type = entry.Key;
                var rules = entry.Value;
                if (toUnwrapByType.ContainsKey(type)) {
                    var rulesExplicit = rules.SelectMany(rule => rule.ToRulesExplicit(rules, modules));
                    var toBeUnwrappedRules = toUnwrapByType[type];
                    foreach (var ruleExplicit in rulesExplicit) {
                        if (toBeUnwrappedRules.Any(toBeUnwrappedRule =>
                            (toBeUnwrappedRule.ModuleName == ruleExplicit.SourceModuleName
                             && toBeUnwrappedRule.ConnectorIndex == ruleExplicit.SourceConnectorIndex)
                            || (toBeUnwrappedRule.ModuleName == ruleExplicit.TargetModuleName
                                && toBeUnwrappedRule.ConnectorIndex == ruleExplicit.TargetConnectorIndex))) {
                            unwrappedExplicit.Add(new Rule(ruleExplicit));
                        }
                    }
                }
            }

            var originalExplicit = rulesToUnwrap
                .Where(rule => rule.IsExplicit);

            var rulesDeduplicated = originalExplicit
                .Concat(unwrappedExplicit)
                .Distinct()
                .Where(rule => !(rule.IsExplicit && rule.Explicit.SourceModuleName == Config.OUTER_MODULE_NAME && rule.Explicit.TargetModuleName == Config.OUTER_MODULE_NAME))
                .ToList();
            rulesDeduplicated.Sort();

            if (!rulesDeduplicated.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to unwrap any Rule.");
                return;
            }

            foreach (var rule in rulesDeduplicated) {
                if (!rule.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, rule.IsValidWhyNot);
                }
            }

            DA.SetDataList(0, rulesDeduplicated);
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
        protected override Bitmap Icon => Properties.Resources.rules_unwrap;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("6BA8D8A8-A5C1-4C37-998D-94FA87F63724");
    }
}
