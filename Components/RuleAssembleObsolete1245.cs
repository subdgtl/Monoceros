using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Monoceros {
    public class ComponentAssembleRuleObsolete1245 : GH_Component, IGH_BakeAwareObject {

        private List<GeometryBase> _sourceModuleGeometry;
        private List<GeometryBase> _sourceModuleReferencedGeometry;
        private List<Guid> _sourceModuleGuids;
        private List<GeometryBase> _targetModuleGeometry;
        private List<GeometryBase> _targetModuleReferencedGeometry;
        private List<Guid> _targetModuleGuids;
        private string _ruleString;

        public ComponentAssembleRuleObsolete1245( ) : base("Assemble Rule",
                                               "AssembleRule",
                                               "Materialize Monoceros Rule.",
                                               "Monoceros",
                                               "Postprocess") {
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
            pManager.AddParameter(new RuleParameter(),
                                  "Rule",
                                  "R",
                                  "Monoceros Explicit Rule",
                                  GH_ParamAccess.item);
            pManager.AddPlaneParameter("Base Plane",
                                       "P",
                                       "Base plane for the first Monoceros Module Pivot",
                                       GH_ParamAccess.item,
                                       Plane.WorldXY);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddGeometryParameter("Source Geometry",
                                          "SG",
                                          "Geometry from Monoceros Module described by the " +
                                          "Monoceros Rule as source",
                                          GH_ParamAccess.list);
            pManager.AddGeometryParameter("Target Geometry",
                                          "TG",
                                          "Geometry from Monoceros Module described by the " +
                                          "Monoceros Rule as target",
                                          GH_ParamAccess.list);
        }
        public override bool Obsolete => true;
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var modulesRaw = new List<Module>();
            var rule = new Rule();
            var basePlane = new Plane();

            if (!DA.GetDataList(0, modulesRaw)) {
                return;
            }

            if (!DA.GetData(1, ref rule)) {
                return;
            }

            if (!DA.GetData(2, ref basePlane)) {
                return;
            }

            var transforms = new DataTree<Transform>();
            var geometry = new DataTree<GeometryBase>();


            if (rule == null || !rule.IsValid) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The rule is null or invalid.");
                return;
            }

            if (!rule.IsExplicit) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Catalog currently works only with Monoceros Explicit Rules. " +
                                  "Unwrap Monoceros Rules first.");
                return;
            }

            if (rule.Explicit.SourceModuleName == Config.OUTER_MODULE_NAME
                || rule.Explicit.TargetModuleName == Config.OUTER_MODULE_NAME
                || rule.Explicit.SourceModuleName == Config.EMPTY_MODULE_NAME
                || rule.Explicit.TargetModuleName == Config.EMPTY_MODULE_NAME) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Catalog cannot display a Monoceros Rule describing a connection " +
                                  "to an outer or empty Monoceros Module.");
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

            if (!rule.IsValidWithModules(modulesClean)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "The Monoceros Rule is not valid with the given Monoceros Modules.");
                return;
            }

            var sourceModule = modulesClean.Find(module => module.Name == rule.Explicit.SourceModuleName);
            var sourceConnector = sourceModule.Connectors[rule.Explicit.SourceConnectorIndex];
            var targetModule = modulesClean.Find(module => module.Name == rule.Explicit.TargetModuleName);
            var targetConnector = targetModule.Connectors[rule.Explicit.TargetConnectorIndex];

            if ((!sourceModule.Geometry.Any()
                && !sourceModule.ReferencedGeometry.Any())
                || (!targetModule.Geometry.Any()
                && !targetModule.ReferencedGeometry.Any())) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "The source or target Monoceros Module does not contain any geometry.");
                return;
            }

            var sourceModuleTransform = Transform.PlaneToPlane(sourceModule.Pivot, basePlane);
            var sourceModuleGeometry = sourceModule.Geometry.Select(geo => {
                var placedGeometry = geo.Duplicate();
                placedGeometry.Transform(sourceModuleTransform);
                return placedGeometry;
            }).ToList();
            var sourceModuleReferencedGeometry = sourceModule.ReferencedGeometry.Select(geo => {
                var placedGeometry = geo.Duplicate();
                placedGeometry.Transform(sourceModuleTransform);
                return placedGeometry;
            }).ToList();
            var allSourceModuleGeometry = sourceModuleGeometry
                .Concat(sourceModuleReferencedGeometry)
                .ToList();

            var transformedSourceConnectorPlane = sourceConnector.AnchorPlane.Clone();
            transformedSourceConnectorPlane.Transform(sourceModuleTransform);
            var targetModuleTransform = Transform.PlaneToPlane(targetConnector.AnchorPlane,
                                                               transformedSourceConnectorPlane);
            var targetModuleGeometry = targetModule.Geometry.Select(geo => {
                var placedGeometry = geo.Duplicate();
                placedGeometry.Transform(targetModuleTransform);
                return placedGeometry;
            }).ToList();
            var targetModuleReferencedGeometry = targetModule.ReferencedGeometry.Select(geo => {
                var placedGeometry = geo.Duplicate();
                placedGeometry.Transform(targetModuleTransform);
                return placedGeometry;
            }).ToList();
            var allTargetModuleGeometry = targetModuleGeometry
                .Concat(targetModuleReferencedGeometry)
                .ToList();

            _sourceModuleGeometry = sourceModuleGeometry;
            _sourceModuleReferencedGeometry = sourceModuleReferencedGeometry;
            _sourceModuleGuids = sourceModule.ReferencedGeometryGuids;

            _targetModuleGeometry = targetModuleGeometry;
            _targetModuleReferencedGeometry = targetModuleReferencedGeometry;
            _targetModuleGuids = targetModule.ReferencedGeometryGuids;

            _ruleString = rule.ToString();

            DA.SetDataList(0, allSourceModuleGeometry);
            DA.SetDataList(1, allTargetModuleGeometry);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override Bitmap Icon => Properties.Resources.rule_assemble;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("6DA78B0E-0328-418B-8A08-5956CC3E51CE");

        public override bool IsBakeCapable => IsInstantiated;

        public override void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids) {
            BakeGeometry(doc, new ObjectAttributes(), obj_ids);
        }

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids) {
            // TODO: Bakes into "Default" layer for some reason
            var sourceReferencedObjects = _sourceModuleGuids
                .Select(guid => doc.Objects.FindId(guid))
                .Where(obj => obj != null);
            var sourceReferencedAttributes = sourceReferencedObjects.Select(obj => obj.Attributes);
            var sourceNewAttributes = sourceReferencedAttributes.Select(originalAttributes => {
                var mainAttributesDuplicate = att.Duplicate();
                mainAttributesDuplicate.ObjectColor = originalAttributes.ObjectColor;
                mainAttributesDuplicate.ColorSource = originalAttributes.ColorSource;
                mainAttributesDuplicate.MaterialIndex = originalAttributes.MaterialIndex;
                mainAttributesDuplicate.MaterialSource = originalAttributes.MaterialSource;
                mainAttributesDuplicate.LinetypeIndex = originalAttributes.LinetypeIndex;
                mainAttributesDuplicate.LinetypeSource = originalAttributes.LinetypeSource;
                return mainAttributesDuplicate;
            });
            var sourceData = _sourceModuleReferencedGeometry
                .Zip(sourceNewAttributes, (geo, attrib) => new { geo, attrib });

            var sourceGroupId = doc.Groups.Add(_ruleString + " Source");
            foreach (var geometry in _sourceModuleGeometry) {
                var geomId = doc.Objects.Add(geometry, att);
                doc.Groups.AddToGroup(sourceGroupId, geomId);
                obj_ids.Add(geomId);
            }

            foreach (var item in sourceData) {
                var geometry = item.geo;
                var attributes = item.attrib;
                var geomId = doc.Objects.Add(geometry, attributes);
                doc.Groups.AddToGroup(sourceGroupId, geomId);
                obj_ids.Add(geomId);
            }

            var targetReferencedObjects = _targetModuleGuids
                .Select(guid => doc.Objects.FindId(guid))
                .Where(obj => obj != null);
            var targetReferencedAttributes = targetReferencedObjects.Select(obj => obj.Attributes);
            var targetNewAttributes = targetReferencedAttributes.Select(originalAttributes => {
                var mainAttributesDuplicate = att.Duplicate();
                mainAttributesDuplicate.ObjectColor = originalAttributes.ObjectColor;
                mainAttributesDuplicate.ColorSource = originalAttributes.ColorSource;
                mainAttributesDuplicate.MaterialIndex = originalAttributes.MaterialIndex;
                mainAttributesDuplicate.MaterialSource = originalAttributes.MaterialSource;
                mainAttributesDuplicate.LinetypeIndex = originalAttributes.LinetypeIndex;
                mainAttributesDuplicate.LinetypeSource = originalAttributes.LinetypeSource;
                return mainAttributesDuplicate;
            });
            var targetData = _targetModuleReferencedGeometry
                .Zip(targetNewAttributes, (geo, attrib) => new { geo, attrib });

            var targetGroupId = doc.Groups.Add(_ruleString + " Target");
            foreach (var geometry in _targetModuleGeometry) {
                var geomId = doc.Objects.Add(geometry, att);
                doc.Groups.AddToGroup(targetGroupId, geomId);
                obj_ids.Add(geomId);
            }

            foreach (var item in targetData) {
                var geometry = item.geo;
                var attributes = item.attrib;
                var geomId = doc.Objects.Add(geometry, attributes);
                doc.Groups.AddToGroup(targetGroupId, geomId);
                obj_ids.Add(geomId);

            }

        }

        private bool IsInstantiated => _sourceModuleGeometry != null
                                       && _sourceModuleGuids != null
                                       && _targetModuleGeometry != null
                                       && _targetModuleGuids != null
                                       && _ruleString != null
                                       && _sourceModuleGeometry.All(x => x != null)
                                       && _sourceModuleGuids.All(x => x != null)
                                       && _targetModuleGeometry.All(x => x != null)
                                       && _targetModuleGuids.All(x => x != null);
    }
}
