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

namespace Monoceros {
    public class ComponentMaterializeSlots : GH_Component, IGH_BakeAwareObject {

        private List<List<GeometryBase>> _moduleGeometry;
        private List<List<Guid>> _moduleGuids;
        private List<string> _moduleNames;
        private List<List<Transform>> _moduleTransforms;
        public ComponentMaterializeSlots( ) : base("Materialize Slots",
                                                   "Materialize",
                                                   "Materialize Monoceros Modules into Monoceros Slots.",
                                                   "Monoceros",
                                                   "Main") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleParameter(),
                                  "Modules",
                                  "M",
                                  "All Monoceros Modules",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new SlotParameter(),
                                  "Slots",
                                  "S",
                                  "Monoceros Slots",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddGeometryParameter("Geometry",
                                          "G",
                                          "Geometry placed into Monoceros Slot",
                                          GH_ParamAccess.tree);
            pManager.AddTransformParameter("Transform",
                                           "X",
                                           "Transformation data",
                                           GH_ParamAccess.tree);
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

            var transforms = new DataTree<Transform>();
            var geometry = new DataTree<GeometryBase>();

            var invalidSlotCount = slots.RemoveAll(slot => slot == null || !slot.IsValid);

            if (invalidSlotCount > 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  invalidSlotCount + " Slots are null or invalid and were removed.");
            }

            if (!slots.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Slots collected.");
                return;
            }

            var invalidModuleCount = modules.RemoveAll(module => module == null || !module.IsValid);

            if (invalidModuleCount > 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  invalidModuleCount + " Modules are null or invalid and were removed.");
            }

            if (!modules.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Modules collected.");
                return;
            }

            _moduleGeometry = new List<List<GeometryBase>>();
            _moduleGuids = new List<List<Guid>>();
            _moduleNames = new List<string>();
            _moduleTransforms = new List<List<Transform>>();

            for (var moduleIndex = 0; moduleIndex < modules.Count; moduleIndex++) {
                var module = modules[moduleIndex];
                var currentModuleTransforms = new List<Transform>();
                var allModuleGeometry = module.Geometry.Concat(module.ReferencedGeometry);
                for (var slotIndex = 0; slotIndex < slots.Count; slotIndex++) {
                    var slot = slots[slotIndex];
                    // TODO: Think about how to display and bake contradictory and non-deterministic slots.
                    if (slot.AllowedPartNames.Count == 1 &&
                        slot.AllowedPartNames[0] == module.PivotPartName) {
                        var transform = Transform.PlaneToPlane(module.Pivot, slot.Pivot);
                        var slotGeometry = allModuleGeometry
                            .Select(geo => {
                                var placedGeometry = geo.Duplicate();
                                placedGeometry.Transform(transform);
                                return placedGeometry;
                            });
                        currentModuleTransforms.Add(transform);
                        geometry.AddRange(slotGeometry, new GH_Path(new int[] { moduleIndex, slotIndex }));
                    }
                }
                transforms.AddRange(currentModuleTransforms, new GH_Path(new int[] { moduleIndex }));
                _moduleGeometry.Add(module.Geometry);
                _moduleGuids.Add(module.ReferencedGeometryGuids);
                _moduleTransforms.Add(currentModuleTransforms);
                _moduleNames.Add(module.Name);
            }

            DA.SetDataTree(0, geometry);
            DA.SetDataTree(1, transforms);
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to
        /// septenary), each of which can be combined with the
        /// GH_Exposure.obscure flag, which ensures the component will only be
        /// visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override Bitmap Icon => Properties.Resources.materialize;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("1E4C296E-8E3D-4979-AF34-C1DDFB73ED47");

        public override bool IsBakeCapable => IsInstantiated;

        public override void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids) {
            BakeGeometry(doc, new ObjectAttributes(), obj_ids);
        }

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids) {
            // Bake as blocks to save memory, file size and make it possible to edit all at once
            for (var i = 0; i < _moduleGeometry.Count; i++) {
                var directGeometry = _moduleGeometry[i];
                var directAttributes = Enumerable.Repeat(att.Duplicate(), directGeometry.Count);
                var referencedObjects = _moduleGuids[i].Select(guid => doc.Objects.FindId(guid)).Where(obj => obj != null);
                var referencedGeometry = referencedObjects.Select(obj => obj.Geometry);
                var referencedAttributes = referencedObjects.Select(obj => obj.Attributes);
                var referencedNewAttributes = referencedAttributes.Select(originalAttributes => {
                    var mainAttributesDuplicate = att.Duplicate();
                    mainAttributesDuplicate.ObjectColor = originalAttributes.ColorSource == ObjectColorSource.ColorFromObject
                    ? originalAttributes.ObjectColor
                    : doc.Layers[originalAttributes.LayerIndex].Color;
                    mainAttributesDuplicate.ColorSource = ObjectColorSource.ColorFromObject;
                    mainAttributesDuplicate.MaterialIndex = originalAttributes.MaterialSource == ObjectMaterialSource.MaterialFromObject
                    ? originalAttributes.MaterialIndex
                    : doc.Layers[originalAttributes.LayerIndex].RenderMaterialIndex;
                    mainAttributesDuplicate.MaterialSource = ObjectMaterialSource.MaterialFromObject;
                    mainAttributesDuplicate.LinetypeIndex = originalAttributes.LinetypeSource == ObjectLinetypeSource.LinetypeFromObject ?
                    originalAttributes.LinetypeIndex
                    : doc.Layers[originalAttributes.LayerIndex].LinetypeIndex;
                    mainAttributesDuplicate.LinetypeSource = ObjectLinetypeSource.LinetypeFromObject;
                    mainAttributesDuplicate.LayerIndex = originalAttributes.LayerIndex;
                    return mainAttributesDuplicate;
                });
                var geometry = directGeometry.Concat(referencedGeometry).ToList();
                var attributes = directAttributes.Concat(referencedNewAttributes).ToList();
                var name = _moduleNames[i];
                var transforms = _moduleTransforms[i];
                // Only bake if the module appears in any slots
                if (transforms.Count > 0) {
                    var newName = name;
                    while (doc.InstanceDefinitions.Any(inst => inst.Name == newName)) {
                        newName += "_1";
                    }

                    var instanceIndex = doc.InstanceDefinitions.Add(newName,
                                                                    "Geometry of module " + name,
                                                                    Point3d.Origin,
                                                                    geometry,
                                                                    attributes);
                    var blockAttributes = att.Duplicate();
                    blockAttributes.LayerIndex = doc.Layers.CurrentLayerIndex;
                    foreach (var transfrom in transforms) {
                        obj_ids.Add(
                            doc.Objects.AddInstanceObject(instanceIndex, transfrom, blockAttributes)
                            );
                    }
                }
            }


        }

        private bool IsInstantiated => _moduleGeometry != null
                                       && _moduleGuids != null
                                       && _moduleNames != null
                                       && _moduleTransforms != null
                                       && _moduleGeometry.All(x => x != null)
                                       && _moduleGuids.All(x => x != null)
                                       && _moduleNames.All(x => x != null)
                                       && _moduleTransforms.All(x => x != null);
    }
}
