using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace Monoceros {
    class ConnectorIndexParameter : GH_PersistentParam<ConnectorIndex> {
        public ConnectorIndexParameter( ) : base("Connector Index",
                                             "CI",
                                             "Contains a collection of Monoceros Module Connector indices.",
                                             "Monoceros",
                                             "Parameters") { }

        public override Guid ComponentGuid => new Guid("318FECF6-B28C-442A-8ED6-AD229F4645E5");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.connector_index_parameter;

        protected override GH_GetterResult Prompt_Plural(ref List<ConnectorIndex> values) {
            values = new List<ConnectorIndex>();
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Singular(ref ConnectorIndex value) {
            value = new ConnectorIndex();
            return GH_GetterResult.success;
        }
    }
}
