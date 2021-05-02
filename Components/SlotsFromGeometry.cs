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

            // Transform the geometry to be oriented to world XYZ fore easier scanning
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

            var moduleGeometry = module.Geometry
                .Concat(module.ReferencedGeometry);
            if (!moduleGeometry.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Module \"" + module.Name + "\" contains " +
                    "no geometry and therefore will be skipped.");
                return;
            }

            var moduleGeometryBBoxes = moduleGeometry.Select(geo => geo.GetBoundingBox(true));

            var bBoxUnionModule = BoundingBox.Empty;
            bBoxUnionModule.Union(module.Pivot.Origin);
            foreach (var moduleBBox in moduleGeometryBBoxes) {
                bBoxUnionModule.Union(moduleBBox);
            }
            var moduleDimensionSafetyBuffer = new Point3i(
                (int)Math.Ceiling(bBoxUnionModule.Diagonal.X / module.PartDiagonal.X) + 1,
                (int)Math.Ceiling(bBoxUnionModule.Diagonal.Y / module.PartDiagonal.Y) + 1,
                (int)Math.Ceiling(bBoxUnionModule.Diagonal.Z / module.PartDiagonal.Z) + 1);

            var geometryBBoxes = geometryClean.Select(geo => geo.GetBoundingBox(true)).ToList();
            var bBoxUnionGeometry = BoundingBox.Empty;
            foreach (var bBox in geometryBBoxes) {
                bBoxUnionGeometry.Union(bBox);
            }

            var slots = new List<Slot>();

            for (int z = (int)Math.Floor(bBoxUnionGeometry.Min.Z / module.PartDiagonal.Z) - moduleDimensionSafetyBuffer.Z;
                z < Math.Ceiling(bBoxUnionGeometry.Max.Z / module.PartDiagonal.Z) + moduleDimensionSafetyBuffer.Z;
                z++) {
                for (int y = (int)Math.Floor(bBoxUnionGeometry.Min.Y / module.PartDiagonal.Y) - moduleDimensionSafetyBuffer.Y;
                    y < Math.Ceiling(bBoxUnionGeometry.Max.Y / module.PartDiagonal.Y) + moduleDimensionSafetyBuffer.Y;
                    y++) {
                    for (int x = (int)Math.Floor(bBoxUnionGeometry.Min.X / module.PartDiagonal.X) - moduleDimensionSafetyBuffer.X;
                        x < Math.Ceiling(bBoxUnionGeometry.Max.X / module.PartDiagonal.X) + moduleDimensionSafetyBuffer.X;
                        x++) {
                        var currentRelativePosition = new Point3i(x, y, z);
                        var currentPivot = Plane.WorldXY;
                        currentPivot.Origin = currentRelativePosition.ToCartesian(Plane.WorldXY, module.PartDiagonal);

                        var transformModuleToCurrentPivot = Transform.PlaneToPlane(module.Pivot, currentPivot);
                        var moduleGeometryBBoxesAtCurrentPivot = moduleGeometryBBoxes.Select(bBox => {
                            var transformedBBox = bBox;
                            transformedBBox.Transform(transformModuleToCurrentPivot);
                            return transformedBBox;
                        });

                        var indicesOfSimilarBBoxes = moduleGeometryBBoxesAtCurrentPivot.Select(moduleGeometryBBox =>
                            geometryBBoxes.Select((geometryBBox, index) => {
                                // TODO: Find a universal way to compare two lists of values
                                var moduleCorners = moduleGeometryBBox.GetCorners().Distinct().ToList();
                                moduleCorners.Sort();
                                var geometryCorners = geometryBBox.GetCorners().Distinct().ToList();
                                geometryCorners.Sort();
                                if (moduleCorners.Count != geometryCorners.Count) {
                                    return -1;
                                }
                                var equalityPattern = moduleCorners.Zip(geometryCorners, (moduleCorner, geometryCorner) => moduleCorner.EpsilonEquals(geometryCorner, Config.EPSILON));
                                if (equalityPattern.All(equal => equal)) {
                                    return index;
                                } else {
                                    return -1;
                                }
                            }).Where(index => index != -1)
                        );

                        // If any module geometry doesn't have a bbox similar to any geometry, then early continue
                        if (!indicesOfSimilarBBoxes.All(similarBBoxes => similarBBoxes.Any())) {
                            continue;
                        }

                        // Heavy calculations

                        var transformedModuleGeometry = moduleGeometry.Select(geo => {
                            var transformedGeometry = geo.Duplicate();
                            transformedGeometry.Transform(transformModuleToCurrentPivot);
                            return transformedGeometry;
                        });

                        var geometriesToCheck = indicesOfSimilarBBoxes.Select(indices => indices.Select(index => geometryClean[index]));

                        var geometryEqualityPattern = transformedModuleGeometry
                            .Zip(geometriesToCheck, (current, others) => others.Any(other =>
                            // TODO: when the original geometry is moved, the meshes become non-equal
                            // TODO: replace with visual similarity check (pull random points to geometry)
                            // TODO: check if the two are of the same type first
                                GeometryBase.GeometryEquals(current, other)
                            ));

                        if (geometryEqualityPattern.All(equal => equal)) {
                            var firstModulePartRelativeCenter = module.PartCenters[0];
                            var modulePartsRelativeCentersRelativeToModulePivot = module.PartCenters.Select(center => center - firstModulePartRelativeCenter);

                            var currentModuleSlots = modulePartsRelativeCentersRelativeToModulePivot
                                .Zip(
                                module.PartNames,
                                (partCenter, partName) => new Slot(basePlane,
                                                               currentRelativePosition + partCenter,
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
