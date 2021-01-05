﻿using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace WFCPlugin
{

    public class ComponentRuleExplicitFromCurve : GH_Component
    {
        public ComponentRuleExplicitFromCurve()
            : base("WFC Create Explicit Rule From Curve",
                   "WFCRuleExpCrv",
                   "Create an Explicit WFC Rule (connector-to-connector) " +
                   "from a curve connecting two opposite connectors.",
                   "WaveFunctionCollapse",
                   "Rule")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ModuleParameter(),
                                  "Modules",
                                  "M",
                                  "All available WFC modules",
                                  GH_ParamAccess.list);
            pManager.AddCurveParameter("Connection Curve",
                                       "C",
                                       "Curve connecting two opposite connectors",
                                       GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "WFC Rules",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Module> modules = new List<Module>();
            Curve curve = new Curve();

            if (!DA.GetDataList(0, modules))
            {
                return;
            }

            if (!DA.GetData(1, ref curve))
            {
                return;
            }

            List<Rule> rules = new List<Rule>();

            if (curve.IsPeriodic)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The connecting curve is periodic.");
                return;
            }

            List<ModuleConnector> startConnectors = new List<ModuleConnector>();
            List<ModuleConnector> endConnectors = new List<ModuleConnector>();
            foreach (Module module in modules)
            {
                startConnectors.AddRange(
                    module.GetConnectorsContainingPoint(curve.PointAtStart)
                   );
                endConnectors.AddRange(
                    module.GetConnectorsContainingPoint(curve.PointAtEnd)
                   );
            }

            if (startConnectors.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                                  "The curve does not start at any module connector.");
            }

            if (endConnectors.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                                  "The curve does not end at any module connector.");
            }

            foreach (ModuleConnector startConnector in startConnectors)
            {
                foreach (ModuleConnector endConnector in endConnectors)
                {
                    if (endConnector.Direction.IsOpposite(startConnector.Direction))
                    {
                        rules.Add(
                            new Rule(
                                startConnector.ModuleName,
                                startConnector.ConnectorIndex,
                                endConnector.ModuleName,
                                endConnector.ConnectorIndex
                                )
                            );
                    }
                    else
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                                          "The curve connects non-opposing connectors.");
                    }
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.C;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("119E048F-D0D0-49E6-ABE2-76C4B7ECE492");
    }
}
