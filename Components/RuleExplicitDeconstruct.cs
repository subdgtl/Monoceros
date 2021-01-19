using System;
using Grasshopper.Kernel;

namespace Monoceros {
    // TODO: Consider making one Deconstruct for both types of Rule
    public class ComponentDeconstructRuleExplicit : GH_Component {
        public ComponentDeconstructRuleExplicit( )
            : base("Deconstruct Explicit Rule",
                   "DeconRuleExp",
                   "Deconstruct an Monoceros Explicit Rule (connector-to-connector) " +
                   "into module names and connector numbers.",
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
                                  "Monoceros Rule (Explicit)",
                                  GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
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

            if (!rule.IsExplicit) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "The provided Rule is not Explicit.");
                return;
            }

            DA.SetData(0, new ModuleName(rule.Explicit.SourceModuleName));
            DA.SetData(1, rule.Explicit.SourceConnectorIndex);
            DA.SetData(2, new ModuleName(rule.Explicit.TargetModuleName));
            DA.SetData(3, rule.Explicit.TargetConnectorIndex);
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.rule_explicit_deconstruct;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("0678B7D6-580E-4493-A960-026B9C3C862B");
    }
}
