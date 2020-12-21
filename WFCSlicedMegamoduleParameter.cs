using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace WFCTools {

    // TODO: Obsolete
    class WFCSlicedMegamoduleParameter : GH_PersistentParam<WFCSlicedMegamodule> {
        public WFCSlicedMegamoduleParameter()
      : base("WFC Sliced Megamodule", "WFCSMM", "Sliced geometry, names and cages of the megamodules", "WaveFunctionCollapse", "Params") { }
        public override Guid ComponentGuid => new Guid("7C8448F3-135A-40DD-8270-B5EA5B40284B");

        protected override GH_GetterResult Prompt_Plural(ref List<WFCSlicedMegamodule> values) {
            values = new List<WFCSlicedMegamodule>();
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Singular(ref WFCSlicedMegamodule value) {
            value = new WFCSlicedMegamodule();
            return GH_GetterResult.success;
        }
    }
}
