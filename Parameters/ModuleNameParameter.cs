using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace Monoceros {
    /// <summary>
    /// Wraps <see cref="ModuleName"/> type so that it can be used in Grasshopper as an input, output or a floating parameter.
    /// </summary>
    class ModuleNameParameter : GH_PersistentParam<ModuleName> {
        public ModuleNameParameter( ) : base("Module Name",
                                            "MN",
                                            "Contains a collection of Monoceros Module names.",
                                            "Monoceros",
                                            "Parameters") { }

        public override Guid ComponentGuid => new Guid("2206385E-137A-4E4F-B786-642C6A27C126");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.NP;

        protected override GH_GetterResult Prompt_Plural(ref List<ModuleName> values) {
            values = new List<ModuleName>();
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Singular(ref ModuleName value) {
            value = new ModuleName();
            return GH_GetterResult.success;
        }
    }
}
