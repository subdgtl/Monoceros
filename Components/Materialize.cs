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
                                                   "Postprocess") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleParameter(),
                                  "Module",
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
            var modulesRaw = new List<Module>();
            var slotsRaw = new List<Slot>();

            if (!DA.GetDataList(0, modulesRaw)) {
                return;
            }

            if (!DA.GetDataList(1, slotsRaw)) {
                return;
            }

            var transforms = new DataTree<Transform>();
            var geometry = new DataTree<GeometryBase>();

            var slotsClean = new List<Slot>();
            foreach (var slot in slotsRaw) {
                if (slot == null || !slot.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot is null or invalid.");
                } else {
                    slotsClean.Add(slot);
                }
            }

            if (!slotsClean.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Slots collected.");
                return;
            }

            var modulesClean = new List<Module>();
            foreach (var module in modulesRaw) {
                if (module == null || !module.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module is null or invalid.");
                } else {
                    modulesClean.Add(module);
                }
            }

            if (!modulesClean.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Modules collected.");
                return;
            }

            _moduleGeometry = new List<List<GeometryBase>>();
            _moduleGuids = new List<List<Guid>>();
            _moduleNames = new List<string>();
            _moduleTransforms = new List<List<Transform>>();

            for (var moduleIndex = 0; moduleIndex < modulesClean.Count; moduleIndex++) {
                var module = modulesClean[moduleIndex];
                var currentModuleTransforms = new List<Transform>();
                var allModuleGeometry = module.Geometry.Concat(module.ReferencedGeometry);
                for (var slotIndex = 0; slotIndex < slotsClean.Count; slotIndex++) {
                    var slot = slotsClean[slotIndex];
                    // TODO: Think about how to display bake contradictory and non-deterministic slots.
                    if (slot.AllowedSubmoduleNames.Count == 1 &&
                        slot.AllowedSubmoduleNames[0] == module.PivotSubmoduleName) {
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
                _moduleGeometry.Add(module.Geometry.ToList());
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
        public override GH_Exposure Exposure => GH_Exposure.primary;

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
                    mainAttributesDuplicate.ObjectColor = originalAttributes.ObjectColor;
                    mainAttributesDuplicate.ColorSource = originalAttributes.ColorSource;
                    mainAttributesDuplicate.MaterialIndex = originalAttributes.MaterialIndex;
                    mainAttributesDuplicate.MaterialSource = originalAttributes.MaterialSource;
                    mainAttributesDuplicate.LinetypeIndex = originalAttributes.LinetypeIndex;
                    mainAttributesDuplicate.LinetypeSource = originalAttributes.LinetypeSource;
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
                    foreach (var transfrom in transforms) {
                        obj_ids.Add(
                            doc.Objects.AddInstanceObject(instanceIndex, transfrom)
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
