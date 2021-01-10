﻿using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace WFCPlugin {
    public class ComponentRuleOuterFromPoint : GH_Component {
        public ComponentRuleOuterFromPoint( )
            : base("Rule Allow Out From Point Tag",
                   "RuleOutPt",
                   "Allow the Monoceros Moudle's connector marked with a Point to connect to an " +
                   "Out Module. All Out module's connectors are Indifferent.",
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
                                  GH_ParamAccess.list);
            pManager.AddPointParameter("Point Tag",
                                       "PT",
                                       "Point marking a connector",
                                       GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "Monoceros Rules",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var modules = new List<Module>();
            var point = new Point3d();
            var targetName = Config.OUTER_MODULE_NAME;

            if (!DA.GetDataList(0, modules)) {
                return;
            }

            if (!DA.GetData(1, ref point)) {
                return;
            }

            var rules = new List<Rule>();

            foreach (var module in modules) {
                for (var connectorIndex = 0; connectorIndex < module.Connectors.Count; connectorIndex++) {
                    var connector = module.Connectors[connectorIndex];
                    if (connector.ContaininsPoint(point)) {
                        rules.Add(
                            new Rule(module.Name,
                                     connectorIndex,
                                     targetName,
                                     DirectionToSingleModuleConnectorIndex(
                                         connector.Direction.ToFlipped()))
                            );
                    }
                }
            }

            if (rules.Count == 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                                  "The point does not mark any module connector.");
            }

            foreach (var rule in rules) {
                if (!rule.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, rule.IsValidWhyNot);
                }
            }

            DA.SetDataList(0, rules);
        }

        /// <summary>
        /// Converts the <see cref="Direction"/> to a submodule connector index,
        /// according to the convention: (submoduleIndex * 6) + faceIndex, where
        /// faceIndex is X=0, Y=1, Z=2, -X=3, -Y=4, -Z=5. This method is the
        /// source of truth.
        /// </summary>
        /// <returns>Submodule connector index.</returns>
        private int DirectionToSingleModuleConnectorIndex(Direction direction) {
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
            return -1;
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.R;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("1BC41AF7-60CC-48C4-9133-704CAB800DC0");
    }
}
