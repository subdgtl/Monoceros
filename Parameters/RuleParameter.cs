using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace WFCToolset
{

    public class RuleParameter : GH_PersistentParam<Rule>
    {
        public RuleParameter()
      : base("WFC Rule", "WFC-R", "Rule definition.", "WaveFunctionCollapse", "Parameters") { }
        public override Guid ComponentGuid => new Guid("9804D786-20FA-4DEF-A68E-7A5D47D8A61D");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon =>
               Properties.Resources.C;

        public bool Hidden { get => true; set { } }

        protected override GH_GetterResult Prompt_Plural(ref List<Rule> values)
        {
            values = new List<Rule>();
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Singular(ref Rule value)
        {
            value = new Rule();
            return GH_GetterResult.success;
        }
    }
}
