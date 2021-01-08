using System;
using Grasshopper.Kernel;

namespace WFCPlugin {

    public class ComponentConstructRuleExplicit : GH_Component {
        public ComponentConstructRuleExplicit( )
            : base("Construct Explicit Rule",
                   "RuleExp",
                   "Construct a WFC Explicit Rule (connector-to-connector) from WFC Module " +
                   "name and connector number. The existence of the module and connector as " +
                   "well as whether the connectors are opposite is not being checked.",
                   "WaveFunctionCollapse",
                   "Rule") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Source Module",
                                  "SM",
                                  "Source module name",
                                  GH_ParamAccess.item);
            pManager.AddIntegerParameter("Source Connector Index",
                                         "SC",
                                         "Source connector number",
                                         GH_ParamAccess.item);
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Target Module",
                                  "TM",
                                  "Target module name",
                                  GH_ParamAccess.item);
            pManager.AddIntegerParameter("Target Connector Index",
                                         "TC",
                                         "Target connector number",
                                         GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new RuleParameter(),
                                  "Rule",
                                  "R",
                                  "WFC Rule",
                                  GH_ParamAccess.item);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var sourceNameRaw = new ModuleName();
            var sourceConnector = 0;
            var targetNameRaw = new ModuleName();
            var targetConnector = 0;

            if (!DA.GetData(0, ref sourceNameRaw)) {
                return;
            }

            if (!DA.GetData(1, ref sourceConnector)) {
                return;
            }

            if (!DA.GetData(2, ref targetNameRaw)) {
                return;
            }

            if (!DA.GetData(3, ref targetConnector)) {
                return;
            }

            var sourceName = sourceNameRaw.Name;
            var targetName = targetNameRaw.Name;

            var rule = new Rule(sourceName, sourceConnector, targetName, targetConnector);

            if (!rule.IsValid) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, rule.IsValidWhyNot);
                return;
            }

            DA.SetData(0, rule);
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.C;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("6274094F-6084-4B1B-9CAA-4CBA2C7836FF");
    }
}
