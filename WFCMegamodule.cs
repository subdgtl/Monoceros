using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WFCTools {
    
    // TODO: Obsolete
    public class WFCMegamodule: IGH_Goo {

        public string Name;
        public List<GeometryBase> SimpleGeometry;
        public List<GeometryBase> ProductionGeometry;
        public Plane BasePlane;
        public Color Colour;

        public bool IsValid => true;

        public string IsValidWhyNot => "Dunno.";

        public string TypeName => "WFCMegamodule";

        public string TypeDescription => "Megamodule input data.";

        public bool CastFrom(object source) => false;

        public bool CastTo<T>(out T target) {
            target = default;
            return false;
        }

        public IGH_Goo Duplicate() {
            return (IGH_Goo)this.MemberwiseClone();

        }

        public IGH_GooProxy EmitProxy() {
            return (IGH_GooProxy)null;
        }

        public bool Read(GH_IReader reader) => true;

        public object ScriptVariable() => this;

        public override string ToString() {
            return SimpleGeometry
                .Aggregate(
                Name + System.Environment.NewLine,
                (str, geo) => str + "- " + geo.ToString() + System.Environment.NewLine
                );
        }

        public bool Write(GH_IWriter writer) => true;
    }
}
