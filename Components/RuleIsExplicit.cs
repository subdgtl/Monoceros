using System;
using Grasshopper.Kernel;

namespace Monoceros {
    // TODO: Consider making this part of Deconstruct
    public class ComponentIsRuleExplicit : GH_Component {
        public ComponentIsRuleExplicit( ) : base("Is Rule Explicit", "IsRuleExp",
            "Returns true if the provided Monoceros Rule is Explicit (connector-to-connector).",
            "Monoceros", "Rule") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new RuleParameter(),
                                  "Rule",
                                  "R",
                                  "Monoceros Rule",
                                  GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddBooleanParameter("Boolean Pattern",
                                         "B",
                                         "True if the Monoceros Rule is explicit",
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

            DA.SetData(0, rule.IsExplicit);
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.R;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("151FB46F-6AFB-43CF-813A-214A0CA3B822");
    }
}
