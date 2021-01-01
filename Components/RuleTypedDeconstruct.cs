// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Grasshopper.Kernel;

namespace WFCToolset
{

    public class ComponentDeconstructRuleTyped : GH_Component
    {
        public ComponentDeconstructRuleTyped() : base("WFC Deconstruct Typed Rule To Components", "WFCDeconRuleTyp",
            "Deconstruct a Typed WFC Rule (connector-to-all-same-type-connectors) into module name, connector number and connector type.",
            "WaveFunctionCollapse", "Rule")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new RuleParameter(), "Rule", "R", "WFC Rule (Typed)", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ModuleNameParameter(), "Module", "M", "Module name", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Connector Index", "C", "Connector number", GH_ParamAccess.item);
            pManager.AddTextParameter("Type", "T", "Connector type", GH_ParamAccess.item);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var rule = new Rule();

            if (!DA.GetData(0, ref rule))
            {
                return;
            }

            if (!rule.IsTyped())
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The provided rule is not typed.");
                DA.SetData(0, null);
                DA.SetData(1, null);
                DA.SetData(2, null);
                return;
            }

            DA.SetData(0, new ModuleName(rule.RuleTyped.ModuleName));
            DA.SetData(1, rule.RuleTyped.ConnectorIndex);
            DA.SetData(2, rule.RuleTyped.ConnectorType);
        }


        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.secondary;

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
        public override Guid ComponentGuid => new Guid("F2FA0E48-398F-45B8-B2A8-F370365738E7");
    }
}
