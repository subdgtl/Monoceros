using System;
using Grasshopper.Kernel;

namespace WFCToolset
{

    public class ComponentRuleExplicitFromLiteral : GH_Component
    {
        public ComponentRuleExplicitFromLiteral() : base("WFC Create explicit rule from literal", "WFCRuleExpLit",
            "Create an explicit (connector-to-connector) WFC Rule from module name and connector number.",
            "WaveFunctionCollapse", "Rule")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("SModule", "SM", "Source module name", GH_ParamAccess.item);
            pManager.AddIntegerParameter("SConnector", "SC", "Source connector number", GH_ParamAccess.item);
            pManager.AddTextParameter("TModule", "TM", "Target module name", GH_ParamAccess.item);
            pManager.AddIntegerParameter("TConnector", "TC", "Target connector number", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new RuleParameter(), "Rule", "R", "WFC Rule", GH_ParamAccess.item);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string sourceName = "";
            int sourceConnector = 0;
            string targetName = "";
            int targetConnector = 0;

            if (!DA.GetData(0, ref sourceName)) return;
            if (!DA.GetData(1, ref sourceConnector)) return;
            if (!DA.GetData(2, ref targetName)) return;
            if (!DA.GetData(3, ref targetConnector)) return;

            if (sourceName == targetName && sourceConnector == targetConnector)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The connector connects to itself.");
            }

            var rule = new Rule(sourceName, sourceConnector, targetName, targetConnector);

            DA.SetData(0, rule);
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
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                Properties.Resources.C;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("6274094F-6084-4B1B-9CAA-4CBA2C7836FF");
    }
}