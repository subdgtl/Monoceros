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
    public class ComponentAssembleRule : GH_Component {

        public ComponentAssembleRule( ) : base("Assemble Rule into Slots",
                                               "AssembleRule",
                                               "Materialize Monoceros Rule.",
                                               "Monoceros",
                                               "Slot") {
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
            pManager.AddPlaneParameter("Source Module Pivot Plane",
                                       "P",
                                       "Plane to put the Source Module's Pivot onto",
                                       GH_ParamAccess.item,
                                       Plane.WorldXY);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new SlotParameter(),
                                          "Source Slots",
                                          "SS",
                                          "Slots generated from Monoceros Module described by the " +
                                          "Monoceros Rule as source.",
                                          GH_ParamAccess.list);
            pManager.AddParameter(new SlotParameter(),
                                          "Target Slots",
                                          "TS",
                                          "Slots generated from Monoceros Module described by the " +
                                          "Monoceros Rule as target.",
                                          GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var modules = new List<Module>();
            var rule = new Rule();
            var basePlane = new Plane();

            if (!DA.GetDataList(0, modules)) {
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
                                  "Rule Assemble works only with Monoceros Explicit Rules to " +
                                  "make sure a single Rule generate a single assembly. " +
                                  "Unwrap Monoceros Rules first.");
                return;
            }

            //if (rule.Explicit.SourceModuleName == Config.OUTER_MODULE_NAME
            //    || rule.Explicit.TargetModuleName == Config.OUTER_MODULE_NAME
            //    || rule.Explicit.SourceModuleName == Config.EMPTY_MODULE_NAME
            //    || rule.Explicit.TargetModuleName == Config.EMPTY_MODULE_NAME) {
            //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
            //                      "Catalog cannot display a Monoceros Rule describing a connection " +
            //                      "to an outer or empty Monoceros Module.");
            //    return;
            //}

            var invalidModuleCount = modules.RemoveAll(module => module == null || !module.IsValid);

            if (invalidModuleCount > 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  invalidModuleCount + " Modules are null or invalid and were removed.");
            }

            if (!modules.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Modules collected.");
                return;
            }

            if (!rule.IsValidWithModules(modules)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "The Monoceros Rule is not valid with the given Monoceros Modules.");
                return;
            }

            var sourceModule = modules.Find(module => module.Name == rule.Explicit.SourceModuleName);
            var sourceConnector = sourceModule.Connectors[rule.Explicit.SourceConnectorIndex];
            var targetModule = modules.Find(module => module.Name == rule.Explicit.TargetModuleName);
            var targetConnector = targetModule.Connectors[rule.Explicit.TargetConnectorIndex];

            if (sourceModule.BasePlane != targetModule.BasePlane) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "The source and target Modules are not defined with the same Base Plane. " +
                    "The resulting Slots would be incompatible.");
                return;
            }

            if (sourceModule.PartDiagonal != targetModule.PartDiagonal) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "The source and target Module Part Diagonals are not identical. " +
                    "The Modules are incompatible.");
                return;
            }

            var allPartsCount = sourceModule.PartCenters.Count + targetModule.PartCenters.Count;

            var sourcePivotOffset = Point3i.FromCartesian(sourceModule.Pivot.Origin,
                                                          sourceModule.BasePlane,
                                                          sourceModule.PartDiagonal);

            var sourceSlots = new List<Slot>();
            for (var i = 0; i < sourceModule.PartNames.Count; i++) {
                var sourceModulePartCenter = sourceModule.PartCenters[i] - sourcePivotOffset;
                var sourceModulePartName = sourceModule.PartNames[i];
                var slot = new Slot(basePlane,
                                    sourceModulePartCenter,
                                    sourceModule.PartDiagonal,
                                    false,
                                    new List<string> { sourceModule.Name },
                                    new List<string> { sourceModulePartName },
                                    allPartsCount);
                sourceSlots.Add(slot);
            }

            var targetToSourceTransfrom = Transform.PlaneToPlane(targetConnector.AnchorPlane, sourceConnector.AnchorPlane);
            var targetPartCenters = targetModule.PartCenters.Select(center => center.ToCartesian(targetModule.BasePlane, targetModule.PartDiagonal));

            var targetSlots = new List<Slot>();
            for (var i = 0; i < targetModule.PartNames.Count; i++) {
                var targetModulePartCenterCartesian = targetModule
                    .PartCenters[i]
                    .ToCartesian(targetModule.BasePlane, targetModule.PartDiagonal);
                targetModulePartCenterCartesian.Transform(targetToSourceTransfrom);
                var targetModulePartCenter = Point3i.FromCartesian(targetModulePartCenterCartesian,
                                                                   sourceModule.BasePlane,
                                                                   sourceModule.PartDiagonal) - sourcePivotOffset;
                var targetModulePartName = targetModule.PartNames[i];
                var slot = new Slot(basePlane,
                                    targetModulePartCenter,
                                    targetModule.PartDiagonal,
                                    false,
                                    new List<string> { targetModule.Name },
                                    new List<string> { targetModulePartName },
                                    allPartsCount);
                targetSlots.Add(slot);
            }

            DA.SetDataList(0, sourceSlots);
            DA.SetDataList(1, targetSlots);
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to
        /// septenary), each of which can be combined with the
        /// GH_Exposure.obscure flag, which ensures the component will only be
        /// visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.quarternary;

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
        public override Guid ComponentGuid => new Guid("569D505A-5A26-4C29-AE59-AFA06A6B41DD");
    }
}