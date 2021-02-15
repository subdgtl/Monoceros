using System;
using Grasshopper.Kernel;

namespace Monoceros {
    // TODO: Consider making one Deconstruct for both types of Rule
    public class ComponentDeconstructRuleTyped : GH_Component {
        public ComponentDeconstructRuleTyped( )
            : base("Deconstruct Typed Rule",
                   "DeconRuleTyp",
                   "Deconstruct a Monoceros Typed Rule (connector-to-all-same-type-connectors) into " +
                   "Module name, Connector index and Connector type.",
                   "Monoceros",
                   "Rule") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new RuleParameter(),
                                  "Rule",
                                  "R",
                                  "Monoceros Rule (Typed)",
                                  GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Module Name",
                                  "MN",
                                  "Module name",
                                  GH_ParamAccess.item);
            pManager.AddIntegerParameter("Connector Index",
                                         "C",
                                         "Connector index",
                                         GH_ParamAccess.item);
            pManager.AddTextParameter("Type", "T", "Connector type", GH_ParamAccess.item);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var rule = new Rule();

            if (!DA.GetData(0, ref rule)) {
                return;
            }

            if (rule == null || !rule.IsValid) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The rule is null or invalid.");
                return;
            }

            if (!rule.IsTyped) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The provided Rule is not Typed.");
                return;
            }

            DA.SetData(0, new ModuleName(rule.Typed.ModuleName));
            DA.SetData(1, rule.Typed.ConnectorIndex);
            DA.SetData(2, rule.Typed.ConnectorType);
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.rule_typed_deconstruct;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("F2FA0E48-398F-45B8-B2A8-F370365738E7");
    }
}
