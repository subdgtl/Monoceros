using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace WFCPlugin {
    // TODO: Think about baking blocks. Override baking output geometry.
    // https://github.com/scotttd/developer-rhino3d-com-branches/blob/92de0fdec892b5bdc0ad9167673c003a43f9d19d/_samples/rhinocommon/addnestedblock.md
    // TODO: Think about how to bake contradictory and non-deterministic slots.

    // TODO: Add (and possibly replace this with) MaterializeModule (Module is Item access). 
    // That will generate a block from the module geometry in place and orient it to the 
    // respective slot pivots. Also return transform data, so that it is possible to orient 
    // the module geometry manually. 
    public class ComponentMaterializeModule : GH_Component {
        public ComponentMaterializeModule( ) : base("WFC Materialize Module",
                                             "WFCMaterializeModule",
                                             "WFC Materialize Module into given Slots.",
                                             "WaveFunctionCollapse",
                                             "Main") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleParameter(),
                                  "Module",
                                  "M",
                                  "All WFC Modules",
                                  GH_ParamAccess.item);
            pManager.AddParameter(new SlotParameter(),
                                  "Slots",
                                  "S",
                                  "WFC Slots",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddGeometryParameter("Geometry",
                                          "G",
                                          "Geometry placed into WFC Slot",
                                          GH_ParamAccess.tree);
            pManager.AddTransformParameter("Transform",
                                           "X",
                                           "Transformation data",
                                           GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var module = new Module();
            var slots = new List<Slot>();

            if (!DA.GetData(0, ref module)) {
                return;
            }

            if (!DA.GetDataList(1, slots)) {
                return;
            }

            var transforms = new List<Transform>();
            var geometry = new DataTree<GeometryBase>();

            for (var branch = 0; branch < slots.Count; branch++) {
                var slot = slots[branch];
                if (slot.AllowedSubmoduleNames.Count == 1 &&
                    slot.AllowedSubmoduleNames[0] == module.PivotSubmoduleName) {
                    var transform = Transform.PlaneToPlane(module.Pivot, slot.Pivot);
                    transforms.Add(transform);
                    var slotGeometry = module.Geometry.Select(geo => {
                        var placedGeometry = geo.Duplicate();
                        placedGeometry.Transform(transform);
                        return placedGeometry;
                    });
                    geometry.AddRange(slotGeometry, new Grasshopper.Kernel.Data.GH_Path(branch));
                }
            }

            DA.SetDataTree(0, geometry);
            DA.SetDataList(1, transforms);
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to
        /// septenary), each of which can be combined with the
        /// GH_Exposure.obscure flag, which ensures the component will only be
        /// visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override Bitmap Icon => Properties.Resources.WFC;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("1E4C296E-8E3D-4979-AF34-C1DDFB73ED47");
    }
}
