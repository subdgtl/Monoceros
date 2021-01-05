// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;

namespace WFCPlugin
{
    public class ComponentRuleIndifferentUnused : GH_Component
    {
        public ComponentRuleIndifferentUnused() : base("WFC Set Unused Connectors To Indifferent",
                                                       "WFCRuleIndifferentUnused",
                                                       "Allow unused connectors to connect to any opposite Indifferent connector.",
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
            var module = new Module();
            var existingRules = new List<Rule>();
            var type = Configuration.INDIFFERENT_TAG;

            if (!DA.GetData(0, ref module))
            {
                return;
            }

            DA.GetDataList(1, existingRules);

            var thisModulesUsedConnectors = new List<int>();

            foreach (var existingRule in existingRules)
            {
                if (existingRule.IsExplicit() &&
                    existingRule.Explicit.SourceModuleName == module.Name
                    )
                {
                    thisModulesUsedConnectors.Add(existingRule.Explicit.SourceConnectorIndex);
                }

                if (existingRule.IsExplicit() &&
                    existingRule.Explicit.TargetModuleName == module.Name
                    )
                {
                    thisModulesUsedConnectors.Add(existingRule.Explicit.TargetConnectorIndex);
                }

                if (existingRule.IsTyped() &&
                    existingRule.Typed.ModuleName == module.Name
                    )
                {
                    thisModulesUsedConnectors.Add(existingRule.Typed.ConnectorIndex);
                }
            }

            var rules = module.Connectors
                .Where(connector => !thisModulesUsedConnectors.Contains(connector.ConnectorIndex))
                .Select(connector => new Rule(connector.ModuleName, connector.ConnectorIndex, type));

            DA.SetDataList(0, rules);
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                Properties.Resources.C;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("3AE4F441-BD29-4977-A259-5C8FE84685E0");
    }
}
