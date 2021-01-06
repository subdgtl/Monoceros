using System;
using Grasshopper.Kernel;

namespace WFCPlugin {

    public class ComponentConstructRuleTyped : GH_Component {
        public ComponentConstructRuleTyped( )
            : base("WFC Construct Typed Rule From Components",
                   "WFCConstRuleTyp",
                   "Construct a typed WFC Rule (connector-to-all-same-type-connectors) from " +
                   "module name, connector number and connector type. The existence of the " +
                   "module and connector is not being checked. The connector Type will be " +
                   "converted to lowercase.",
                   "WaveFunctionCollapse",
                   "Rule") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Module",
                                  "M",
                                  "Module name",
                                  GH_ParamAccess.item);
            pManager.AddIntegerParameter("Connector", "C", "Connector number", GH_ParamAccess.item);
            pManager.AddTextParameter("Type", "T", "Connector type", GH_ParamAccess.item);
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
            var moduleNameRaw = new ModuleName();
            var connector = 0;
            var type = "";

            if (!DA.GetData(0, ref moduleNameRaw)) {
                return;
            }

            if (!DA.GetData(1, ref connector)) {
                return;
            }

            if (!DA.GetData(2, ref type)) {
                return;
            }

            var moduleName = moduleNameRaw.Name;

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
        public override Guid ComponentGuid => new Guid("1C74EDBE-C2DC-4C3B-922F-7E6C662340BC");
    }
}
