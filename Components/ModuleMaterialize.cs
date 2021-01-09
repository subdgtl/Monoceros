using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace WFCPlugin {
    public class ComponentMaterializeModule : GH_Component, IGH_BakeAwareObject {

        private List<List<GeometryBase>> _moduleGeometry;
        private List<Point3d> _moduleOrigins;
        private List<string> _moduleNames;
        private List<List<Transform>> _slotTransforms;
        public ComponentMaterializeModule( ) : base("Materialize Module",
                                             "Materialize",
                                             "Materialize WFC Module into given WFC Slots.",
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
                                  GH_ParamAccess.list);
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
            var modules = new List<Module>();
            var slots = new List<Slot>();

            if (!DA.GetDataList(0, modules)) {
                return;
            }

            if (!DA.GetDataList(1, slots)) {
                return;
            }

            var transforms = new List<Transform>();
            var geometry = new DataTree<GeometryBase>();

            _moduleGeometry = new List<List<GeometryBase>>();
            _moduleOrigins = new List<Point3d>();
            _moduleNames = new List<string>();
            _slotTransforms = new List<List<Transform>>();

            for (var moduleIndex = 0; moduleIndex < modules.Count; moduleIndex++) {
                var module = modules[moduleIndex];
                var currentTransforms = new List<Transform>();
                IEnumerable<GeometryBase> slotGeometry;
                for (var slotIndex = 0; slotIndex < slots.Count; slotIndex++) {
                    var slot = slots[slotIndex];
                    // TODO: Think about how to display bake contradictory and non-deterministic slots.
                    if (slot.AllowedSubmoduleNames.Count == 1 &&
                        slot.AllowedSubmoduleNames[0] == module.PivotSubmoduleName) {
                        var transform = Transform.PlaneToPlane(module.Pivot, slot.Pivot);
                        transforms.Add(transform);
                        currentTransforms.Add(transform);
                        slotGeometry = module.Geometry.Select(geo => {
                            var placedGeometry = geo.Duplicate();
                            placedGeometry.Transform(transform);
                            return placedGeometry;
                        });
                    } else {
                        slotGeometry = Enumerable.Empty<GeometryBase>();
                    }
                    geometry.AddRange(slotGeometry, new GH_Path(new int[] { moduleIndex, slotIndex }));

                    _moduleGeometry.Add(module.Geometry);
                    _moduleOrigins.Add(module.Pivot.Origin);
                    _slotTransforms.Add(currentTransforms);
                    _moduleNames.Add(module.Name);
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

        public override bool IsBakeCapable => true;

        public override void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids) {
            BakeGeometry(doc, new ObjectAttributes(), obj_ids);
        }

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids) {
            // Bake as blocks to save memory, file size and make it possible to edit all at once
            for (var i = 0; i < _moduleGeometry.Count; i++) {
                var geometry = _moduleGeometry[i];
                var origin = _moduleOrigins[i];
                var name = _moduleNames[i];
                var transforms = _slotTransforms[i];

                // Only bake if the module appears in any slots
                if (transforms.Count > 0) {
                    var instanceIndex = doc.InstanceDefinitions.Add(name,
                                                                    "Geometry of module " + name,
                                                                    origin,
                                                                    geometry);
                    foreach (var transfrom in transforms) {
                        obj_ids.Add(
                            doc.Objects.AddInstanceObject(instanceIndex, transfrom)
                            );
                    }
                }

            }

        }
    }
}
