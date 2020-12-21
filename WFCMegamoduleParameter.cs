using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace WFCTools {

    // TODO: Obsolete
    class WFCMegamoduleParameter : GH_PersistentParam<WFCMegamodule> {
        public WFCMegamoduleParameter()
      : base("WFC Megamodule", "WFCMM", "Geometry, name, plane and colour of a megamodule.", "WaveFunctionCollapse", "Params") { }
        public override Guid ComponentGuid => new Guid("3B8E557B-E200-4CEE-906E-75C99DAFA792");

        protected override GH_GetterResult Prompt_Plural(ref List<WFCMegamodule> values) {
            values = new List<WFCMegamodule>();
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Singular(ref WFCMegamodule value) {
            value = new WFCMegamodule();
            return GH_GetterResult.success;
        }
    }
}
