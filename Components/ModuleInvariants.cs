using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Monoceros {
    public class ComponentModuleInvariants : GH_Component {
        public ComponentModuleInvariants( ) : base("Generate Module invariants",
                                                   "ModuleInvar",
                                                   "Generate all orthogonal rotations and mirror images of the module",
                                                   "Monoceros",
                                                   "Module") {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleParameter(),
                                  "Module",
                                  "M",
                                  "Monoceros Module",
                                  GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new ModuleParameter(),
                                  "Modules",
                                  "M",
                                  "Collection of Monoceros Modules",
                                  GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA) {
            var module = new Module();

            if (!DA.GetData(0, ref module)) {
                return;
            }

            if (module == null) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The is null or invalid.");
                return;
            }

            if (!module.IsValid) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The module is invalid.");
            }

            var outModules = new List<Module>();

            var unitBoundingBox = BoundingBox.Empty;
            foreach (var relativeCenter in module.PartCenters) {
                unitBoundingBox.Union(relativeCenter.ToPoint3d());
            }
            var dimX = (unitBoundingBox.Diagonal.X + 1) * module.PartDiagonal.X;
            var dimY = (unitBoundingBox.Diagonal.Y + 1) * module.PartDiagonal.Y;
            var dimZ = (unitBoundingBox.Diagonal.Z + 1) * module.PartDiagonal.Z;
            var maxDim = Math.Max(dimX, Math.Max(dimY, dimZ));
            var step = maxDim * 2;



            DA.SetDataList(0, outModules);
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.module_deconstruct;

        public override Guid ComponentGuid => new Guid("519F18D4-98A8-412B-8FC8-3225D92BF7F9");
    }
}
