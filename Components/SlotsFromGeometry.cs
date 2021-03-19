using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;

namespace Monoceros {
    public class ComponentSlotsFromgeometry : GH_Component {
        public ComponentSlotsFromgeometry( ) : base("Slots from Geometry",
                                                  "SlotsFromGeometry",
                                                  "Identify Module geometry and construct Slots containing it. Ignores connection to boundary.",
                                                  "Monoceros", "Slot") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddGeometryParameter("Geometry",
                                          "G",
                                          "Geometry to scan and identify Modules",
                                          GH_ParamAccess.list);
            pManager.AddParameter(new ModuleParameter(),
                                 "Module",
                                 "M",
                                 "Monoceros Module",
                                 GH_ParamAccess.item);
            pManager.AddPlaneParameter("Base Plane",
                                       "B",
                                       "Grid space base plane of the scanned geometry. Defines orientation of the grid.",
                                       GH_ParamAccess.item,
                                       Plane.WorldXY);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new SlotParameter(), "Slots", "S", "Monoceros Slots", GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var geometryRaw = new List<IGH_GeometricGoo>();
            var module = new Module();
            var basePlane = new Plane();

            if (!DA.GetDataList(0, geometryRaw)) {
                return;
            }

            if (!DA.GetData(1, ref module)) {
                return;
            }

            if (!DA.GetData(2, ref basePlane)) {
                return;
            }

            if (module == null || !module.IsValid) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module is null or invalid.");
                return;
            }

            var geometryTransform = Transform.PlaneToPlane(basePlane, Plane.WorldXY);
            var geometryClean = geometryRaw
                .Select(goo => GH_Convert.ToGeometryBase(goo))
                .Where(geo => geo != null)
                .Select(geo => {
                    var transformedGeometry = geo.Duplicate();
                    transformedGeometry.Transform(geometryTransform);
                    return transformedGeometry;
                }).ToList();
            if (!geometryClean.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                                  "Failed to collect any valid geometry to scan.");
                return;
            }

            var moduleTransform = Transform.PlaneToPlane(module.BasePlane, Plane.WorldXY);
            var moduleGeometry = module.Geometry
                .Concat(module.ReferencedGeometry)
                .Select(geo => {
                    var transformedGeometry = geo.Duplicate();
                    transformedGeometry.Transform(moduleTransform);
                    return transformedGeometry;
                });
            var modulePartCenters = module.PartCenters
                .Select(point => {
                    var transformedPoint = point.ToCartesian(module.BasePlane, module.PartDiagonal);
                    transformedPoint.Transform(moduleTransform);
                    return transformedPoint;
                });
            if (!moduleGeometry.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Module \"" + module.Name + "\" contains " +
                    "no geometry and therefore will be skipped.");
                return;
            }

            var moduleBBoxes = moduleGeometry.Select(geo => geo.GetBoundingBox(false));
            var modulePivot = module.Pivot;
            modulePivot.Transform(moduleTransform);
            var modulePivotOrigin = modulePivot.Origin;

            var commonModuleBBox = BoundingBox.Empty;
            commonModuleBBox.Union(modulePivotOrigin);

            foreach (var moduleBBox in moduleBBoxes) {
                commonModuleBBox.Union(moduleBBox);
            }

            var safetyBuffer = new Point3i(
                (int)Math.Ceiling(commonModuleBBox.Diagonal.X / module.PartDiagonal.X) + 1,
                (int)Math.Ceiling(commonModuleBBox.Diagonal.Y / module.PartDiagonal.Y) + 1,
                (int)Math.Ceiling(commonModuleBBox.Diagonal.Z / module.PartDiagonal.Z) + 1);

            var geometryBBoxes = geometryClean.Select(geo => geo.GetBoundingBox(false)).ToList();
            var commonBBox = BoundingBox.Empty;

            foreach (var bBox in geometryBBoxes) {
                commonBBox.Union(bBox);
            }

            var slots = new List<Slot>();

            for (int z = (int)Math.Floor(commonBBox.Min.Z / module.PartDiagonal.Z) - safetyBuffer.Z;
                z < Math.Ceiling(commonBBox.Max.Z / module.PartDiagonal.Z) + safetyBuffer.Z;
                z++) {
                for (int y = (int)Math.Floor(commonBBox.Min.Y / module.PartDiagonal.Y) - safetyBuffer.Y;
                    y < Math.Ceiling(commonBBox.Max.Y / module.PartDiagonal.Y) + safetyBuffer.Y;
                    y++) {
                    for (int x = (int)Math.Floor(commonBBox.Min.X / module.PartDiagonal.X) - safetyBuffer.X;
                        x < Math.Ceiling(commonBBox.Max.X / module.PartDiagonal.X) + safetyBuffer.X;
                        x++) {
                        var currentPivot = Plane.WorldXY;
                        var currentPivotOrigin = new Point3d(x * module.PartDiagonal.X,
                                                             y * module.PartDiagonal.Y,
                                                             z * module.PartDiagonal.Z);
                        currentPivot.Origin = currentPivotOrigin;
                        var currentTransform = Transform.PlaneToPlane(modulePivot, currentPivot);
                        var transformedModuleBBoxes = moduleBBoxes.Select(bBox => {
                            var transformedBBox = bBox;
                            transformedBBox.Transform(currentTransform);
                            return transformedBBox;
                        });

                        var indicesOfSimilarGeometries = new List<int>();
                        var allModuleBoxesFoundSimilarGeometryBox = true;
                        foreach (var currentBBox in transformedModuleBBoxes) {
                            var foundBoxForCurrent = false;
                            var otherIndex = 0;
                            foreach (var otherBBox in geometryBBoxes) {
                                var currentPoints = currentBBox.GetCorners();
                                var otherPoints = otherBBox.GetCorners();
                                for (var i = 0; i < currentPoints.Length; i++) {
                                    var currentPoint = currentPoints[i];
                                    var otherPoint = otherPoints[i];
                                    var equalPoints = currentPoint.EpsilonEquals(otherPoint, RhinoMath.SqrtEpsilon);
                                    if (!equalPoints) {
                                        break;
                                    }
                                    foundBoxForCurrent = true;
                                }
                                if (foundBoxForCurrent) {
                                    indicesOfSimilarGeometries.Add(otherIndex);
                                    break;
                                }
                                otherIndex++;
                            }
                            if (!foundBoxForCurrent) {
                                allModuleBoxesFoundSimilarGeometryBox = false;
                                break;
                            }
                        }

                        if (!allModuleBoxesFoundSimilarGeometryBox) {
                            continue;
                        }

                        // Heavy calculations

                        var transformedModuleGeometry = moduleGeometry.Select(geo => {
                            var transformedGeometry = geo.Duplicate();
                            transformedGeometry.Transform(currentTransform);
                            return transformedGeometry;
                        });

                        var geometriesToCheck = indicesOfSimilarGeometries.Select(index => geometryClean[index]);

                        var geometryEqualityPattern = transformedModuleGeometry
                            .Zip(geometriesToCheck, (current, other) => GeometryBase.GeometryEquals(current, other));

                        if (geometryEqualityPattern.All(equal => equal)) {

                            var transformedModulePartCenters = modulePartCenters.Select(centerPoint => {
                                var transformedPoint = centerPoint;
                                transformedPoint.Transform(currentTransform);
                                return transformedPoint;
                            });

                            var currentPivotCenter = new Point3d(currentPivotOrigin.X / module.PartDiagonal.X,
                                                                currentPivotOrigin.Y / module.PartDiagonal.Y,
                                                                currentPivotOrigin.Z / module.PartDiagonal.Z);
                            var currentPivotRelativeCenter = new Point3i(currentPivotCenter);
                            var firstModulePartRelativeCenter = module.PartCenters[0];
                            var modulePartsRelativeCentersRelativeToModulePivot = module.PartCenters.Select(center => center - firstModulePartRelativeCenter);

                            var currentModuleSlots = modulePartsRelativeCentersRelativeToModulePivot
                                .Zip(
                                module.PartNames,
                                (partCenter, partName) => new Slot(basePlane,
                                                               currentPivotRelativeCenter + partCenter,
                                                               module.PartDiagonal,
                                                               false,
                                                               new List<string>() { module.Name },
                                                               new List<string>() { partName },
                                                               0));
                            slots.AddRange(currentModuleSlots);
                        }
                    }
                }
            }

            if (!Slot.AreSlotLocationsUnique(slots)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Slot centers are not unique.");
            }

            DA.SetDataList(0, slots);
        }


        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to
        /// septenary), each of which can be combined with the
        /// GH_Exposure.obscure flag, which ensures the component will only be
        /// visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.slots_from_geometry;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("725A08EC-60D6-4F72-9EF4-BA82864085D4");
    }
}
