using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WFCPlugin
{
    public class ComponentRuleIndifferentFromPoint : GH_Component
    {
        public ComponentRuleIndifferentFromPoint()
            : base("WFC Create Indifferent Type Rule From Point Tag",
                   "WFCRuleIndifferentPt",
                   "Allow the connector to connect to any opposite indifferent connector.",
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
                                       "PT",
                                       "Point marking a connector",
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
            string type = Config.INDIFFERENT_TAG;

            if (!DA.GetDataList(0, modules))
            {
                return;
            }

            if (!DA.GetData(1, ref point))
            {
                return;
            }

            List<Rule> rules = new List<Rule>();

            foreach (Module module in modules)
            {
                IEnumerable<Rule> moduleRules = module
                    .GetConnectorsContainingPoint(point)
                    .Select(connector => new Rule(connector.ModuleName,
                                                  connector.ConnectorIndex,
                                                  type));
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
        public override GH_Exposure Exposure => GH_Exposure.quarternary;

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
        public override Guid ComponentGuid => new Guid("E86B433F-B55A-4A0F-9A07-69D09E171804");
    }
}
