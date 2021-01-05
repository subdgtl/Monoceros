using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WFCPlugin
{

    public class ComponentRuleTypedFromPoint : GH_Component
    {
        public ComponentRuleTypedFromPoint()
            : base("WFC Create Typed Rule From Point Tag",
                   "WFCRuleTypPt",
                   "Create a Typed WFC Rule (connector-to-all-same-type-connectors) from " +
                  "a point tag. The connector Type will be converted to lowercase.",
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
            pManager.AddPointParameter("Point Tag",
                                       "Pt",
                                       "Point marking a connector",
                                       GH_ParamAccess.item);
            pManager.AddTextParameter("Connector Type",
                                      "T",
                                      "Type to be assigned to the connector",
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
            Point3d point = new Point3d();
            string type = "";

            if (!DA.GetDataList(0, modules))
            {
                return;
            }

            if (!DA.GetData(1, ref point))
            {
                return;
            }

            if (!DA.GetData(2, ref type))
            {
                return;
            }

            List<Rule> rules = new List<Rule>();

            foreach (Module module in modules)
            {
                IEnumerable<Rule> moduleRules = module
                    .GetConnectorsContainingPoint(point)
                    .Select(connector => new Rule(connector.ModuleName, connector.ConnectorIndex, type));
                rules.AddRange(moduleRules);
            }

            if (rules.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                                  "The point does not mark any module connector.");
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
        public override Guid ComponentGuid => new Guid("6A94F17B-60DF-4CAF-B209-9ABE1068A3EC");
    }
}
