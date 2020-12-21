using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace WFCTools
{

    public class WFCRuleParameter : GH_PersistentParam<WFCRule>
    {
        public WFCRuleParameter()
      : base("WFC Rule", "WFC-R", "Rule definition.", "WaveFunctionCollapse", "Parameters") { }
        public override Guid ComponentGuid => new Guid("9804D786-20FA-4DEF-A68E-7A5D47D8A61D");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon =>
               Properties.Resources.C;

        public bool Hidden { get => true; set { } }

        protected override GH_GetterResult Prompt_Plural(ref List<WFCRule> values)
        {
            values = new List<WFCRule>();
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Singular(ref WFCRule value)
        {
            value = new WFCRule();
            return GH_GetterResult.success;
        }
    }
}
