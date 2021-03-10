using System;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;

namespace Monoceros {

    public class ComponentRuleExplicitBetweenTwoSets : GH_Component {
        public ComponentRuleExplicitBetweenTwoSets( )
            : base("Construct Explicit Rule Between 2 Lists",
                   "RuleExp2Lists",
                   "Construct a Monoceros Explicit Rule (connector-to-connector) between " +
                   "all listed Connectors of all listed Modules of two lists. The existence " +
                   "of the Module and the Connector as well as whether the Connectors are " +
                   "opposite is not being checked. Use Collect Rues component to remove " +
                   "the invalid Rules.",
                   "Monoceros",
                   "Rule") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Source Modules",
                                  "SMN",
                                  "Source Module names",
                                  GH_ParamAccess.tree);
            pManager.AddParameter(new ConnectorIndexParameter(),
                                  "Source Connector Indices",
                                  "SC",
                                  "Source Connector numbers",
                                  GH_ParamAccess.tree);
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Target Modules",
                                  "TMN",
                                  "Target Module names",
                                  GH_ParamAccess.tree);
            pManager.AddParameter(new ConnectorIndexParameter(),
                                  "Target Connector Indices",
                                  "TC",
                                  "Target Connector numbers",
                                  GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "Monoceros Rules",
                                  // TODO: consider returning a tree
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {

            if (!DA.GetDataTree(0, out GH_Structure<ModuleName> sourceNamesRaw)) {
                return;
            }

            if (!DA.GetDataTree(1, out GH_Structure<ConnectorIndex> sourceConnectorIndicesRaw)) {
                return;
            }

            if (!DA.GetDataTree(2, out GH_Structure<ModuleName> targetNamesRaw)) {
                return;
            }

            if (!DA.GetDataTree(3, out GH_Structure<ConnectorIndex> targetConnectorIndicesRaw)) {
                return;
            }

            var targetConnector = targetConnectorParam.Index;

            var sourceName = sourceNameRaw.Name;
            var targetName = targetNameRaw.Name;

            if (Config.RESERVED_CHARS.Any(chars => sourceName.Contains(chars))
                || Config.RESERVED_CHARS.Any(chars => targetName.Contains(chars))) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input text contains " +
                    "a forbidden content: :, ->, = or newline.");
                return;
            }

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
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.rule_explicit_construct;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("6274094F-6084-4B1B-9CAA-4CBA2C7836FF");
    }
}
