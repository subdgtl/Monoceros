using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Monoceros {
    public class ComponentRuleOuterConstruct : GH_Component {
        public ComponentRuleOuterConstruct( )
            : base("Construct Rule At Boundary",
                   "RuleBound",
                   "Rule allowing the Monoceros Module to touch the boundary of the Envelope with " +
                   "a Connector of the given index. All connectors with an Indifferent Rule can " +
                   "automatically touch the boundary.",
                   "Monoceros",
                   "Rule") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleParameter(),
                                   "Modules",
                                   "M",
                                   "All available Monoceros modules",
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
            var module = new Module();
            var sourceConnectorParam = new ConnectorIndex();
            var targetName = Config.OUTER_MODULE_NAME;

            if (!DA.GetData(0, ref module)) {
                return;
            }

            if (!DA.GetData(1, ref sourceConnectorParam)) {
                return;
            }
            var connectorIndex = sourceConnectorParam.Index;
            if (connectorIndex >= module.Connectors.Count) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module \""
                    + module.Name + "\" does not have Connector "
                    + connectorIndex + ".");
                return;
            }

            if (module == null || !module.IsValid) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, 
                    "The Module is null or invalid.");
                return;
            }
            var connector = module.Connectors[(int)connectorIndex];
            var oppositeDirection = connector.Direction.ToFlipped();

            var rule = new Rule(module.Name,
                             (uint)connectorIndex,
                             targetName,
                             oppositeDirection.DirectionToSingleModuleConnectorIndex());
            if (!rule.IsValid) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, rule.IsValidWhyNot);
            }

            DA.SetData(0, rule);
        }

        /// <summary>
        /// Converts the <see cref="Direction"/> to a part connector index,
        /// according to the convention: (partIndex * 6) + faceIndex, where
        /// faceIndex is X=0, Y=1, Z=2, -X=3, -Y=4, -Z=5. This method is the
        /// source of truth.
        /// </summary>
        /// <returns>Part connector index.</returns>
        private uint DirectionToSingleModuleConnectorIndex(Direction direction) {
            // Connector numbering convention: 
            // faceIndex is X=0, Y=1, Z=2, -X=3, -Y=4, -Z=5
            if (direction.Axis == Axis.X && direction.Orientation == Orientation.Positive) {
                return 0;
            }
            if (direction.Axis == Axis.Y && direction.Orientation == Orientation.Positive) {
                return 1;
            }
            if (direction.Axis == Axis.Z && direction.Orientation == Orientation.Positive) {
                return 2;
            }
            if (direction.Axis == Axis.X && direction.Orientation == Orientation.Negative) {
                return 3;
            }
            if (direction.Axis == Axis.Y && direction.Orientation == Orientation.Negative) {
                return 4;
            }
            if (direction.Axis == Axis.Z && direction.Orientation == Orientation.Negative) {
                return 5;
            }
            // Never
            return uint.MaxValue;
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.rule_out;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("13211766-AFCC-416F-A98B-BAA5799CC89E");
    }
}
