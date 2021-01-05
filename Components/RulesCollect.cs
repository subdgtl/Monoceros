using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WFCPlugin
{
    public class ComponentCollectRules : GH_Component
    {
        public ComponentCollectRules() : base("WFC Collect Rules",
                                              "WFCCollectRules",
                                              "Collect, convert to Explicit, deduplicate and " +
                                              "remove disallowed rules. Automatically generates " +
                                              "an Out module and its rules.",
                                              "WaveFunctionCollapse",
                                              "Rule")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
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
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
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
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Module> modules = new List<Module>();
            List<Rule> allowed = new List<Rule>();
            List<Rule> disallowed = new List<Rule>();

            if (!DA.GetDataList(0, modules))
            {
                return;
            }

            if (!DA.GetDataList(1, allowed))
            {
                return;
            }

            DA.GetDataList(2, disallowed);

            IEnumerable<Rule> disallowedExplicit = disallowed.Where(rule => rule.IsExplicit());
            IEnumerable<RuleTyped> disallowedTyped = disallowed
                .Where(rule => rule.IsTyped())
                .Select(rule => rule.Typed);
            IEnumerable<Rule> disallowedTypedUnwrapped = disallowedTyped
                .SelectMany(ruleTyped => ruleTyped.ToRuleExplicit(disallowedTyped, modules))
                .Select(ruleExplicit => new Rule(ruleExplicit));

            IEnumerable<Rule> disallowedProcessed = disallowedExplicit
                .Concat(disallowedTypedUnwrapped)
                .Distinct();


            List<Rule> allowedExplicit = new List<Rule>();
            List<Rule> allowedTypedUnwrapped = new List<Rule>();
            List<Rule> allowedTypedWrapped = new List<Rule>();


            Module.GenerateEmptySingleModule(Config.OUTER_MODULE_NAME,
                                             Config.INDIFFERENT_TAG,
                                             new Rhino.Geometry.Vector3d(1, 1, 1),
                                             out Module moduleOut,
                                             out List<RuleTyped> rulesOut);
            allowed.AddRange(
                rulesOut.Select(ruleExplicit => new Rule(ruleExplicit))
                );
            modules.Add(moduleOut);

            IEnumerable<RuleTyped> allowedTyped = allowed
                .Where(rule => rule.IsTyped())
                .Select(rule => rule.Typed);

            foreach (Rule rule in allowed)
            {
                if (rule.IsExplicit())
                {
                    allowedExplicit.Add(rule);
                    continue;
                }
                if (rule.IsTyped())
                {
                    RuleTyped typed = rule.Typed;
                    if (
                        disallowedProcessed.Any(ruleDisallowed =>
                            ruleDisallowed.IsExplicit() &&
                            (ruleDisallowed.Explicit.SourceModuleName == typed.ModuleName &&
                             ruleDisallowed.Explicit.SourceConnectorIndex == typed.ConnectorIndex) ||
                            (ruleDisallowed.Explicit.TargetModuleName == typed.ModuleName &&
                             ruleDisallowed.Explicit.TargetConnectorIndex == typed.ConnectorIndex)
                            )
                        )
                    {
                        IEnumerable<Rule> typedUnwrapped = typed
                            .ToRuleExplicit(allowedTyped, modules)
                            .Select(ruleExplicit => new Rule(ruleExplicit));
                        allowedTypedUnwrapped.AddRange(typedUnwrapped);
                    }
                    else
                    {
                        allowedTypedWrapped.Add(rule);
                    }
                }
            }

            IEnumerable<Rule> allowedProcessed = allowedExplicit
                .Concat(allowedTypedUnwrapped)
                .Concat(allowedTypedWrapped)
                .Distinct();

            IEnumerable<Rule> rules = allowedProcessed.Except(disallowedProcessed);

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
