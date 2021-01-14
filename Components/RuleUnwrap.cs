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
                                  "Module",
                                  "M",
                                  "Monoceros module for indifferent rule generation",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "All Monoceros rules, including Explicit",
                                  GH_ParamAccess.list);
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
            var rulesInput = new List<Rule>();

            if (!DA.GetDataList(0, modules)) {
                return;
            }

            if (!DA.GetDataList(1, rulesInput)) {
                return;
            }

            var modulesClean = new List<Module>();
            foreach (var module in modules) {
                if (module == null || !module.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The module is null or invalid.");
                } else {
                    modulesClean.Add(module);
                }
            }

            if (rulesInput.Any(rule => rule == null || !rule.IsValid)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Some of the rules are null or invalid.");
            }

            var rulesTyped = rulesInput
                .Where(rule => rule != null && rule.IsTyped)
                .Select(rule => rule.Typed);

            Module.GenerateEmptySingleModule(Config.OUTER_MODULE_NAME,
                                             Config.INDIFFERENT_TAG,
                                             new Rhino.Geometry.Vector3d(1, 1, 1),
                                             out var moduleOut,
                                             out var rulesOut);
            rulesTyped = rulesTyped.Concat(rulesOut);
            modulesClean.Add(moduleOut);

            var rulesTypedUnwrapped = rulesTyped
                .SelectMany(ruleTyped => ruleTyped.ToRulesExplicit(rulesTyped, modulesClean))
                .Select(ruleExplicit => new Rule(ruleExplicit));

            var rulesExplicit = rulesInput.Where(rule => rule != null && rule.IsExplicit);

            var rules = rulesExplicit.Concat(rulesTypedUnwrapped).Distinct();

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
        protected override Bitmap Icon => Properties.Resources.R;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("08CD1CF1-A33C-485D-9E10-436B3E36EA56");
    }
}
