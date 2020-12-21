using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace WFCTools {
    // TODO: Obsolete
    class WFCConnectionParameter : GH_PersistentParam<WFCConnection> {
        public WFCConnectionParameter()
      : base("WFC Connection", "WFCC", "Connection rule between two modules in specified direction.", "WaveFunctionCollapse", "Params") { }
        public override Guid ComponentGuid => new Guid("7FDEE608-65F1-4D0B-BAAE-70C015C5892E");

        protected override GH_GetterResult Prompt_Plural(ref List<WFCConnection> values) {
            values = new List<WFCConnection>();
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Singular(ref WFCConnection value) {
            value = new WFCConnection();
            return GH_GetterResult.success;
        }
    }
}
