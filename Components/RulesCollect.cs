using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;

namespace Monoceros {
    public class ComponentCollectRules : GH_Component {
        public ComponentCollectRules( ) : base("Collect Rules",
                                               "CollectRules",
                                               "Collect, convert to Explicit, deduplicate and " +
                                               "remove disallowed Monoceros Rules. Automatically " +
                                               "generates an Out Module and its Rules.",
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
                                  GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules Allowed",
                                  "RA",
                                  "All allowed Monoceros rules",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules Disallowed",
                                  "RD",
                                  "All disallowed Monoceros rules (optional)",
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

            if (allowed.Any(rule => rule == null || !rule.IsValid)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Some of the allowed rules are null or invalid.");
            }


            Module.GenerateEmptySingleModule(Config.OUTER_MODULE_NAME,
                                             Config.INDIFFERENT_TAG,
                                             new Rhino.Geometry.Vector3d(1, 1, 1),
                                             out var moduleOut,
                                             out var rulesOut);
            allowed.AddRange(
                rulesOut.Select(ruleExplicit => new Rule(ruleExplicit))
                );
            modulesClean.Add(moduleOut);

            var allowedClean = allowed.Where(rule => rule.IsValidWithModules(modulesClean));

            if(disallowed == null || !disallowed.Any()) {
                DA.SetDataList(0, allowedClean);
                return;
            }

            if (disallowed.Any(rule => rule == null)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Some of the disallowed rules are null or invalid.");
            }

            var allowedTyped = allowedClean
                .Where(rule => rule != null && rule.IsTyped && rule.IsValidWithModules(modulesClean))
                .Select(rule => rule.Typed);

            var allAllowed = new List<RuleExplicit>();
            foreach (var rule in allowedClean) {
                if (rule != null && rule.IsValid && rule.IsValidWithModules(modulesClean)) {
                    if (rule.IsExplicit) {
                        allAllowed.Add(rule.Explicit);
                    }
                    if (rule.IsTyped) {
                        allAllowed.AddRange(rule.Typed.ToRulesExplicit(allowedTyped, modulesClean));
                    }
                }
            }

            var disallowedTyped = disallowed
                .Where(rule => rule != null && rule.IsTyped && rule.IsValidWithModules(modulesClean))
                .Select(rule => rule.Typed);

            var allDisallowed = new List<RuleExplicit>();
            foreach (var rule in disallowed) {
                if (rule != null && rule.IsValid && rule.IsValidWithModules(modulesClean)) {
                    if (rule.IsExplicit) {
                        allDisallowed.Add(rule.Explicit);
                    }
                    if (rule.IsTyped) {
                        allDisallowed.AddRange(rule.Typed.ToRulesExplicit(disallowedTyped, modulesClean));
                    }
                }
            }

            var outExplicit = allAllowed
                .Distinct()
                .Except(allDisallowed.Distinct());
            var outRules = outExplicit.Select(explicitRule => new Rule(explicitRule)).ToList();
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
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override Bitmap Icon => Properties.Resources.R;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("41CC16C9-739A-41C3-B37A-97969D6D5DAF");
    }
}
