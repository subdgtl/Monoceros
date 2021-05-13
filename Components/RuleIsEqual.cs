using System;
using Grasshopper.Kernel;

namespace Monoceros {
    // TODO: Consider making this part of Deconstruct
    public class ComponentAreRulesEqual : GH_Component {
        public ComponentAreRulesEqual( ) : base("Are Rules Equal", "AreRulesEq",
            "Returns true if the provided Monoceros Rules are equal.",
            "Monoceros", "Rule") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new RuleParameter(),
                                  "Rule",
                                  "R",
                                  "First Monoceros Rule",
                                  GH_ParamAccess.item);
            pManager.AddParameter(new RuleParameter(),
                                  "Rule",
                                  "R",
                                  "Second Monoceros Rule",
                                  GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddBooleanParameter("Boolean Pattern",
                                         "B",
                                         "True if the provided Monoceros Rules are equal",
                                         GH_ParamAccess.item);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var ruleA = new Rule();
            var ruleB = new Rule();

            if (!DA.GetData(0, ref ruleA)) {
                return;
            }

            if (!DA.GetData(1, ref ruleB)) {
                return;
            }

            if (ruleA == null || !ruleA.IsValid || ruleB == null || !ruleB.IsValid) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The Rules are null or invalid.");
                return;
            }

            DA.SetData(0, ruleA.Equals(ruleB));
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.rule_equals;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("316D8535-4064-4D2B-A9B9-25164A2EB4A7");
    }
}
