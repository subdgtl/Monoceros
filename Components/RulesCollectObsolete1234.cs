using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;

namespace Monoceros {
    public class ComponentCollectRulesObsolete1234 : GH_Component {
        public ComponentCollectRulesObsolete1234( ) : base("Collect Rules",
                                               "CollectRules",
                                               "Collect, convert to Explicit, deduplicate, sort and " +
                                               "remove disallowed Monoceros Rules. Automatically " +
                                               "generates an Out Module and its Rules.",
                                               "Monoceros",
                                               "Rule") {
        }

        public override bool Obsolete => true;
        public override GH_Exposure Exposure => GH_Exposure.hidden;

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
                                  "Rules Allowed",
                                  "RA",
                                  "All allowed Monoceros Rules",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules Disallowed",
                                  "RD",
                                  "All disallowed Monoceros Rules (optional)",
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
            var allowed = new List<Rule>();
            var disallowed = new List<Rule>();

            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                              "This is an obsolete component and the results are wrong! Please " +
                              "use the updated Collect Rules component.");

            if (!DA.GetDataList(0, modules)) {
                return;
            }

            if (!DA.GetDataList(1, allowed)) {
                return;
            }

            DA.GetDataList(2, disallowed);

            var modulesClean = new List<Module>();
            foreach (var module in modules) {
                if (module == null || !module.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The module is null or invalid.");
                } else {
                    modulesClean.Add(module);
                }
            }

            Module.GenerateEmptySingleModule(Config.OUTER_MODULE_NAME,
                                            Config.INDIFFERENT_TAG,
                                            new Rhino.Geometry.Vector3d(1, 1, 1),
                                            out var moduleOut,
                                            out var rulesOut);

            var allowedClean = allowed.Concat(
                rulesOut.Select(ruleExplicit => new Rule(ruleExplicit))
                );
            modulesClean.Add(moduleOut);


            if (allowed.Any(rule => rule == null || !rule.IsValid)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Some of the allowed Rules are null or invalid.");
            }

            var allowedOriginalClean = allowedClean
                .Where(rule => rule.IsValidWithModules(modulesClean))
                .Distinct();

            if (disallowed == null || !disallowed.Any()) {
                var earlyRules = allowedOriginalClean.ToList();
                earlyRules.Sort();
                DA.SetDataList(0, earlyRules);
                return;
            }

            if (disallowed.Any(rule => rule == null || !rule.IsValid)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Some of the disallowed rules are null or invalid.");
            }

            var allowedExplicit = allowedOriginalClean
                .Where(rule => rule.IsExplicit)
                .Select(rule => rule.Explicit);
            var allowedTyped = allowedOriginalClean
                .Where(rule => rule.IsTyped)
                .Select(rule => rule.Typed);

            var disallowedOriginalClean = disallowed
                .Where(rule => rule.IsValidWithModules(modulesClean))
                .Distinct();

            var disallowedExplicit = disallowedOriginalClean
                .Where(rule => rule.IsExplicit)
                .Select(rule => rule.Explicit);
            var disallowedTyped = disallowedOriginalClean
                .Where(rule => rule.IsTyped)
                .Select(rule => rule.Typed);

            var allTypedRules = allowedTyped.Concat(disallowedTyped);

            var allTypedByType = new Dictionary<string, List<RuleTyped>>();
            foreach (var rule in allTypedRules) {
                var type = rule.ConnectorType;
                if (allTypedByType.ContainsKey(type)) {
                    allTypedByType[type].Add(rule);
                } else {
                    allTypedByType.Add(type, new List<RuleTyped>() { rule });
                }
            }

            var disallowedTypedByType = new Dictionary<string, List<RuleTyped>>();
            foreach (var rule in disallowedTyped) {
                var type = rule.ConnectorType;
                if (disallowedTypedByType.ContainsKey(type)) {
                    disallowedTypedByType[type].Add(rule);
                } else {
                    disallowedTypedByType.Add(type, new List<RuleTyped>() { rule });
                }
            }

            var finalTyped = new List<RuleTyped>();
            var finalExplicit = new List<RuleExplicit>();

            foreach (var entry in allTypedByType) {
                var type = entry.Key;
                var rules = entry.Value;
                if (disallowedTypedByType.ContainsKey(type)) {
                    var rulesExplicit = rules.SelectMany(rule => rule.ToRulesExplicit(rules, modulesClean));
                    var disallowedRules = disallowedTypedByType[type];
                    foreach (var rule in rulesExplicit) {
                        if (!disallowedRules.Any(disallowedRule =>
                            (disallowedRule.ModuleName == rule.SourceModuleName
                             && disallowedRule.ConnectorIndex == rule.SourceConnectorIndex)
                            || (disallowedRule.ModuleName == rule.TargetModuleName
                                && disallowedRule.ConnectorIndex == rule.TargetConnectorIndex))
                            && !disallowedExplicit.Any(disallowedRule => disallowedRule.Equals(rule))) {
                            finalExplicit.Add(rule);
                        }
                    }
                } else {
                    finalTyped.AddRange(rules);
                }
            }

            foreach (var rule in allowedExplicit) {
                if (!disallowedExplicit.Any(disallowedRule => disallowedRule.Equals(rule))) {
                    finalExplicit.Add(rule);
                }
            }

            var outRules = finalExplicit
                .Where(rule => !(rule.SourceModuleName == Config.OUTER_MODULE_NAME && rule.TargetModuleName == Config.OUTER_MODULE_NAME))
                .Select(explicitRule => new Rule(explicitRule))
                .Concat(finalTyped.Select(ruleTyped => new Rule(ruleTyped)))
                .Distinct()
                .ToList();

            outRules.Sort();

            foreach (var rule in outRules) {
                if (!rule.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, rule.IsValidWhyNot);
                }
            }

            DA.SetDataList(0, outRules);
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to
        /// septenary), each of which can be combined with the
        /// GH_Exposure.obscure flag, which ensures the component will only be
        /// visible on panel dropdowns.
        /// </summary>

        protected override Bitmap Icon => Properties.Resources.rules_collect;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("41CC16C9-739A-41C3-B37A-97969D6D5DAF");
    }
}
