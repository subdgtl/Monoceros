using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Monoceros {

    public class ComponentRuleExplicitFromCurve : GH_Component {
        public ComponentRuleExplicitFromCurve( )
            : base("Explicit Rule From Curve",
                   "RuleExpCrv",
                   "Create an Monoceros Explicit Rule (connector-to-connector) " +
                   "from a curve connecting two opposite connectors.",
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
            pManager.AddCurveParameter("Connection Curve",
                                       "C",
                                       "Curve connecting two opposite connectors",
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
            Curve curve = null;

            if (!DA.GetDataList(0, modules)) {
                return;
            }

            if (!DA.GetData(1, ref curve)) {
                return;
            }

            var rules = new List<Rule>();

            if (curve.IsPeriodic || curve.IsClosed) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "The connecting curve is closed or periodic.");
                return;
            }

            foreach (var startModule in modules) {
                if (startModule == null || !startModule.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The module is null or invalid.");
                    continue;
                }
                for (var startConnectorIndex = 0; startConnectorIndex < startModule.Connectors.Count; startConnectorIndex++) {
                    var startConnector = startModule.Connectors[startConnectorIndex];
                    if (startConnector.ContaininsPoint(curve.PointAtStart)) {
                        foreach (var endModule in modules) {
                            if (endModule == null || !endModule.IsValid) {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The module is null or invalid.");
                                continue;
                            }
                            for (var endConnectorIndex = 0; endConnectorIndex < endModule.Connectors.Count; endConnectorIndex++) {
                                var endConnector = endModule.Connectors[endConnectorIndex];
                                if (endConnector.ContaininsPoint(curve.PointAtEnd)) {
                                    rules.Add(new Rule(startModule.Name,
                                                       (uint)startConnectorIndex,
                                                       endModule.Name,
                                                       (uint)endConnectorIndex)
                                    );
                                }
                            }
                        }
                    }
                }
            }

            foreach (var rule in rules) {
                if (!rule.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, rule.IsValidWhyNot);
                }
            }

            DA.SetDataList(0, rules);
        }


        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to
        /// septenary), each of which can be combined with the
        /// GH_Exposure.obscure flag, which ensures the component will only be
        /// visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.rule_explicit_transparent;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("119E048F-D0D0-49E6-ABE2-76C4B7ECE492");
    }
}
