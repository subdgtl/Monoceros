using System;
using Grasshopper.Kernel;

namespace Monoceros {

    public class ComponentConstructRuleTyped : GH_Component {
        public ComponentConstructRuleTyped( )
            : base("Construct Typed Rule",
                   "RuleTyp",
                   "Construct a Monoceros Typed Rule (connector-to-all-same-type-connectors) from " +
                   "Monoceros Module name, Connector index and Connector Type. The existence of the " +
                   "Module and Connector is not being checked. The Connector Type will be " +
                   "converted to lowercase.",
                   "Monoceros",
                   "Rule") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Module Name",
                                  "MN",
                                  "Module name",
                                  GH_ParamAccess.item);
            pManager.AddParameter(new ConnectorIndexParameter(),
                                  "Connector Index",
                                  "C",
                                  "Connector number",
                                  GH_ParamAccess.item);
            pManager.AddTextParameter("Type", "T", "Connector type", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
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
            var connectorParam = new ConnectorIndex();
            var type = "";

            if (!DA.GetData(0, ref moduleNameRaw)) {
                return;
            }

            if (!DA.GetData(1, ref connectorParam)) {
                return;
            }
            var connector = connectorParam.Index;

            if (!DA.GetData(2, ref type)) {
                return;
            }

            var moduleName = moduleNameRaw.Name;

            if (moduleName.Contains("\n")
                || moduleName.Contains(":")
                || moduleName.Contains("=")
                || type.Contains("\n")
                || type.Contains(":")
                || type.Contains("=")) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input text contains " +
                    "a forbidden content: :, ->, = or newline.");
                return;
            }

            var rule = new Rule(moduleName, connector, type);

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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.rule_typed_construct;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("1C74EDBE-C2DC-4C3B-922F-7E6C662340BC");
    }
}
