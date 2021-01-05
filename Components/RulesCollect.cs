﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;

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
            pManager.AddParameter(new RuleParameter(), "Rules", "R", "WFC Rules", GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var modules = new List<Module>();
            var rulesAllowed = new List<Rule>();
            var rulesDisallowed = new List<Rule>();

            if (!DA.GetDataList(0, modules))
            {
                return;
            }

            if (!DA.GetDataList(1, rulesAllowed))
            {
                return;
            }

            DA.GetDataList(2, rulesDisallowed);

            var rulesDisallowedExplicit = rulesDisallowed.Where(rule => rule.IsExplicit());
            var rulesDisallowedTyped = rulesDisallowed
                .Where(rule => rule.IsTyped())
                .Select(rule => rule.Typed);
            var rulesDisallowedTypedUnwrapped = rulesDisallowedTyped
                .SelectMany(ruleTyped => ruleTyped.ToRuleExplicit(rulesDisallowedTyped, modules))
                .Select(ruleExplicit => new Rule(ruleExplicit));

            var rulesDisallowedProcessed = rulesDisallowedExplicit
                .Concat(rulesDisallowedTypedUnwrapped)
                .Distinct();


            var rulesAllowedExplicit = new List<Rule>();
            var rulesAllowedTypedUnwrapped = new List<Rule>();
            var rulesAllowedTypedWrapped = new List<Rule>();


            Module.GenerateNamedEmptySingleModule(Configuration.OUTER_MODULE_NAME,
                                                  Configuration.INDIFFERENT_TAG,
                                                  new Rhino.Geometry.Vector3d(1, 1, 1),
                                                  out var moduleOut,
                                                  out var rulesOut);
            rulesAllowed.AddRange(
                rulesOut.Select(ruleExplicit => new Rule(ruleExplicit))
                );
            modules.Add(moduleOut);

            var rulesAllowedTyped = rulesAllowed.Where(rule => rule.IsTyped()).Select(rule => rule.Typed);

            foreach (var rule in rulesAllowed)
            {
                if (rule.IsExplicit())
                {
                    rulesAllowedExplicit.Add(rule);
                    continue;
                }
                if (rule.IsTyped())
                {
                    var ruleTyped = rule.Typed;
                    if (
                        rulesDisallowedProcessed.Any(ruleDisallowed =>
                            ruleDisallowed.IsExplicit() &&
                            (ruleDisallowed.Explicit.SourceModuleName == ruleTyped.ModuleName &&
                             ruleDisallowed.Explicit.SourceConnectorIndex == ruleTyped.ConnectorIndex) ||
                            (ruleDisallowed.Explicit.TargetModuleName == ruleTyped.ModuleName &&
                             ruleDisallowed.Explicit.TargetConnectorIndex == ruleTyped.ConnectorIndex)
                            )
                        )
                    {
                        var rulesTypedUnwrapped = ruleTyped
                            .ToRuleExplicit(rulesAllowedTyped, modules)
                            .Select(ruleExplicit => new Rule(ruleExplicit));
                        rulesAllowedTypedUnwrapped.AddRange(rulesTypedUnwrapped);
                    }
                    else
                    {
                        rulesAllowedTypedWrapped.Add(rule);
                    }
                }
            }

            var rulesAllowedProcessed = rulesAllowedExplicit
                .Concat(rulesAllowedTypedUnwrapped)
                .Concat(rulesAllowedTypedWrapped)
                .Distinct();

            var rules = rulesAllowedProcessed.Except(rulesDisallowedProcessed);

            DA.SetDataList(0, rules);
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                Properties.Resources.C;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("41CC16C9-739A-41C3-B37A-97969D6D5DAF");
    }
}
