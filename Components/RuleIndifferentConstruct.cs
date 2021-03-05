using System;
using Grasshopper.Kernel;

namespace Monoceros {
    public class ComponentRuleIndifferentConstruct : GH_Component {
        public ComponentRuleIndifferentConstruct( )
            : base("Construct Indifferent Rule",
                   "RuleIndiff",
                   "Selected Connectors of a Monoceros Module connect to any opposite " +
                   "indifferent connector of any Monoceros Module.",
                   "Monoceros",
                   "Rule") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleNameParameter(),
                                 "Module Name",
                                 "MN",
                                 "Module name",
                                 GH_ParamAccess.item);
            pManager.AddParameter(new ConnectorIndexParameter(),
                                  "Source Connector Index",
                                  "SC",
                                  "Source connector number",
                                  GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
            pManager.AddParameter(new RuleParameter(),
                                  "Rule",
                                  "R",
                                  "Monoceros Rule",
                                  GH_ParamAccess.item);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var moduleNameRaw = new ModuleName();
            var sourceConnectorParam = new ConnectorIndex();

            if (!DA.GetData(0, ref moduleNameRaw)) {
                return;
            }
            var moduleName = moduleNameRaw.Name;


            if (!DA.GetData(1, ref sourceConnectorParam)) {
                return;
            }
            var connectorIndex = sourceConnectorParam.Index;

            var rule = new Rule(moduleName, connectorIndex, Config.INDIFFERENT_TAG);


            DA.SetData(0, rule);
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to
        /// septenary), each of which can be combined with the
        /// GH_Exposure.obscure flag, which ensures the component will only be
        /// visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.rule_indifferent;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("1B946C56-18C2-4D7F-9D54-E76F11E67953");
    }
}
