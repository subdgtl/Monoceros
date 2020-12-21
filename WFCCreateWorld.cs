using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Messaging;

namespace WFCTools {

    // TODO: Obsolete, the world shoudl now be a list of slots. They will be wrapped later, in the solver
    public class WFCCreateWorld : GH_Component {
        public WFCCreateWorld() : base("WFC Create World", "WFCWorld",
            "Create initial state of the world for the WFC solver",
            "WaveFunctionCollapse", "Tools") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddGeometryParameter("Envelope Geometry", "G", "World envelope geometry", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Base Plane", "B", "Grid base plane", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddVectorParameter(
               "Grid Slot Diagonal",
               "D",
               "World grid slot diagonal vector specifying single grid slot dimension in base-plane-aligned XYZ axes",
               GH_ParamAccess.item,
               new Vector3d(10.0, 10.0, 10.0)
               );
            pManager.AddNumberParameter("Precision", "P", "Calculation precision. Higher = better & slower", GH_ParamAccess.item, 4.0);
            pManager.AddBooleanParameter("Fill", "F", "Fill solid BRep voids", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Strict", "S", "Only use slots entirely inside the BRep voids", GH_ParamAccess.item, true);
            pManager.AddTextParameter("Module Names", "N", "All (sub)module names (except '" + WFCUtilities.EMPTY_MODULE_NAME + "' and '" + WFCUtilities.OUTER_MODULE_NAME + "')", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddVectorParameter("World Diagonal", "D", "World diagonal (dimensions) in integer grid units", GH_ParamAccess.item);
            pManager.AddPointParameter("World Slot Coordinates", "S", "Coordinates of world slots in integer grid units", GH_ParamAccess.list);
            pManager.AddTextParameter("Allowed Modules", "M", "Allowed modules in respective world slots. This list is parallel to S", GH_ParamAccess.list);
            pManager.AddBoxParameter("Inner Slots", "IB", "Inner slot that will be filled with modules by the WFC solver", GH_ParamAccess.list);
            pManager.AddBoxParameter("Outer Slots", "OB", "Outer slot that will remain empty", GH_ParamAccess.list);
            pManager.AddPlaneParameter("World Base Plane", "B", "Grid base plane", GH_ParamAccess.item);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            List<IGH_GeometricGoo> envelopeGHGeometry = new List<IGH_GeometricGoo>();
            Plane basePlane = Plane.WorldXY;
            Vector3d slotDiagonal = new Vector3d();
            double precision = 1.0;
            bool fillVoids = true;
            bool strict = true;
            List<string> submoduleNames = new List<string>();

            if (!DA.GetDataList(0, envelopeGHGeometry)) return;
            if (!DA.GetData(1, ref basePlane)) return;
            if (!DA.GetData(2, ref slotDiagonal)) return;
            if (!DA.GetData(3, ref precision)) return;
            if (!DA.GetData(4, ref fillVoids)) return;
            if (!DA.GetData(5, ref strict)) return;
            if (!DA.GetDataList(6, submoduleNames)) return;

            if (submoduleNames.Any(name => name == WFCUtilities.EMPTY_MODULE_NAME || name == WFCUtilities.OUTER_MODULE_NAME)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The megamodule name cannot be " +
                    "'" + WFCUtilities.EMPTY_MODULE_NAME + "'" +
                    " or " +
                    "'" + WFCUtilities.OUTER_MODULE_NAME + "'" +
                    " because it is reserved by WFC.");
                return;
            }

            int submoduleCount = submoduleNames.Count;
            if (submoduleCount == 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No megamodule names have been specified.");
                return;
            }
            submoduleNames.Add(WFCUtilities.EMPTY_MODULE_NAME);
            submoduleCount++;

            if (slotDiagonal.X <= 0.0 || slotDiagonal.Y <= 0.0 || slotDiagonal.Z <= 0.0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module dimensions in each direction must be greater than 0.0.");
                return;
            }

            double maxCageDimension = slotDiagonal.Length;
            double divisionLength = maxCageDimension / precision;

            IEnumerable<GeometryBase> envelopeGeometry = envelopeGHGeometry.Select(goo => GH_Convert.ToGeometryBase(goo));


            List<BoundingBox> planeAlignedBoundingBoxes = envelopeGeometry.Select(goo => goo.GetBoundingBox(basePlane)).ToList();
            BoundingBox planeAlignedUnionBox = new BoundingBox();
            foreach (BoundingBox bBox in planeAlignedBoundingBoxes) {
                planeAlignedUnionBox.Union(bBox);
            }

            List<BoundingBox> planeAlignedSlots = WFCUtilities.SubdivideBoundingBox(slotDiagonal, planeAlignedUnionBox);
            List<bool> patternSlotsWithGeometry = Enumerable.Repeat(false, planeAlignedSlots.Count).ToList();
            List<bool> patternGeometryEntirelyInSingleSlot = Enumerable.Repeat(false, planeAlignedBoundingBoxes.Count).ToList();

            WFCUtilities.AreEntireGeometriesInsideModuleCages(
                planeAlignedBoundingBoxes,
                planeAlignedSlots,
                ref patternGeometryEntirelyInSingleSlot,
                ref patternSlotsWithGeometry
                );

            // Only transform geometry that spans over more slots
            IEnumerable<GeometryBase> planeAlignedGeometry = envelopeGeometry
                .Where((goo, i) => !patternGeometryEntirelyInSingleSlot[i])
                .Select(goo => {
                    GeometryBase planeAlignedGoo = goo.Duplicate();
                    planeAlignedGoo.Transform(Transform.PlaneToPlane(basePlane, Plane.WorldXY));
                    return planeAlignedGoo;
                });

            if (!strict) {
                // Fill the surface of the geometry with points, then test them for inclusion in the cages.
                IEnumerable<Point3d> planeAlignedPopulatePoints = planeAlignedGeometry
                    .SelectMany(goo => {
                        var populatePoints = WFCUtilities.PopulateGeometry(divisionLength, goo);
                        if (populatePoints == null) {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to perform conversion of geometry" + goo.ObjectType + ".");
                        }
                        return populatePoints;
                    });

                // Check if the cage contains input geometry points
                for (int moduleCageI = 0; moduleCageI < planeAlignedSlots.Count; moduleCageI++) {
                    if (!patternSlotsWithGeometry[moduleCageI]) {
                        foreach (Point3d point in planeAlignedPopulatePoints) {
                            if (planeAlignedSlots[moduleCageI].Contains(point)) {
                                patternSlotsWithGeometry[moduleCageI] = true;
                                break;
                            }
                        }
                    }
                }
            }

            // Check if the cage is inside input Brep geometry
            if (fillVoids) {
                IEnumerable<Brep> planeAlignedSolidBreps = planeAlignedGeometry
                    .Where(goo => goo.HasBrepForm)
                    .Select(goo => (Brep)goo)
                    .Where(brep => brep.IsSolid);
                WFCUtilities.AreModulesInsideSolidBreps(
                    planeAlignedSlots,
                    planeAlignedSolidBreps,
                    false,
                    ref patternSlotsWithGeometry
                    );
            }

            List<BoundingBox> planeAlignedTightSlots = planeAlignedSlots
                .Where((_, i) => patternSlotsWithGeometry[i])
                .ToList();

            if (planeAlignedTightSlots.Count == 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No slots meet the requirements to be internal and contain modules.");
            }

            IEnumerable<Point3d> planeAlignedSlotCentersCartesian = planeAlignedTightSlots.Select(slot => slot.Center);
            BoundingBox tightBoundingBoxCartesian = new BoundingBox(planeAlignedSlotCentersCartesian);
            Point3d worldOrigin = tightBoundingBoxCartesian.Min - slotDiagonal;
            worldOrigin.Transform(Transform.PlaneToPlane(Plane.WorldXY, basePlane));
            Plane worldBasePlane = basePlane.Clone();
            worldBasePlane.Origin = worldOrigin;
            IEnumerable<Point3d> slotCentersGrid = planeAlignedSlotCentersCartesian.Select(cartesian => new Point3d((cartesian.X - slotDiagonal.X / 2) / slotDiagonal.X,
                                                                                                        (cartesian.Y - slotDiagonal.Y / 2) / slotDiagonal.Y,
                                                                                                        (cartesian.Z - slotDiagonal.Z / 2) / slotDiagonal.Z));
            BoundingBox tightBoundingBoxGrid = new BoundingBox(slotCentersGrid);
            Point3d minPoint = tightBoundingBoxGrid.Min - new Vector3d(1, 1, 1);
            Point3d maxPoint = tightBoundingBoxGrid.Max + new Vector3d(1, 1, 1);
            Vector3d shiftVector = new Vector3d(minPoint);
            Vector3d worldDiagonalGrid = maxPoint - minPoint + new Vector3d(1, 1, 1);

            Dictionary<Point3d, bool> innerSlotMap = new Dictionary<Point3d, bool>();
            for (int z = (int)tightBoundingBoxGrid.Min.Z - 1; z <= tightBoundingBoxGrid.Max.Z + 1; z++) {
                for (int y = (int)tightBoundingBoxGrid.Min.Y - 1; y <= tightBoundingBoxGrid.Max.Y + 1; y++) {
                    for (int x = (int)tightBoundingBoxGrid.Min.X - 1; x <= tightBoundingBoxGrid.Max.X + 1; x++) {
                        Point3d currentPoint = new Point3d(x, y, z);
                        innerSlotMap.Add(currentPoint, false);
                    }
                }
            }

            foreach (Point3d innerCenter in slotCentersGrid) {
                Point3d shiftedInnerCenter = innerCenter;
                Point3d integerCeter = new Point3d((int)shiftedInnerCenter.X, (int)shiftedInnerCenter.Y, (int)shiftedInnerCenter.Z);
                if (innerSlotMap.ContainsKey(integerCeter)) {
                    innerSlotMap[integerCeter] = true;
                } else {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                      "Slot center grid center: " +
                                      shiftedInnerCenter +
                                      " was cast to an integer vector: " +
                                      integerCeter +
                                      ", which may be incorrect.");
                }
            }

            List<Box> worldAlignedTightInnerSlotBoxes = new List<Box>();
            List<Box> worldAlignedTightOuterSlotBoxes = new List<Box>();

            List<Point3d> slotPoints = new List<Point3d>();
            List<string> slotModules = new List<string>();

            foreach (KeyValuePair<Point3d, bool> slot in innerSlotMap) {
                Point3d slotGridCoordinate = slot.Key;
                bool isInner = slot.Value;
                Point3d centerCartesian = new Point3d((slotGridCoordinate.X * slotDiagonal.X) + (slotDiagonal.X / 2),
                                                      (slotGridCoordinate.Y * slotDiagonal.Y) + (slotDiagonal.Y / 2),
                                                      (slotGridCoordinate.Z * slotDiagonal.Z) + (slotDiagonal.Z / 2));
                centerCartesian.Transform(Transform.PlaneToPlane(Plane.WorldXY, basePlane));
                Plane boxPlane = basePlane.Clone();
                boxPlane.Origin = centerCartesian;
                Box box = new Box(
                    boxPlane,
                    new Interval(-slotDiagonal.X / 2, slotDiagonal.X / 2),
                    new Interval(-slotDiagonal.Y / 2, slotDiagonal.Y / 2),
                    new Interval(-slotDiagonal.Z / 2, slotDiagonal.Z / 2)
                    );
                if (isInner) {
                    worldAlignedTightInnerSlotBoxes.Add(box);
                    slotPoints.AddRange(Enumerable.Repeat(slotGridCoordinate - shiftVector, submoduleCount));
                    slotModules.AddRange(submoduleNames);
                } else {
                    worldAlignedTightOuterSlotBoxes.Add(box);
                    slotPoints.Add(slotGridCoordinate - shiftVector);
                    slotModules.Add(WFCUtilities.OUTER_MODULE_NAME);
                }
            }

            DA.SetData(0, worldDiagonalGrid);
            DA.SetDataList(1, slotPoints);
            DA.SetDataList(2, slotModules);
            DA.SetDataList(3, worldAlignedTightInnerSlotBoxes);
            DA.SetDataList(4, worldAlignedTightOuterSlotBoxes);
            DA.SetData(5, worldBasePlane);
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                WFCTools.Properties.Resources.W;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("3ADDE1B8-F77F-45C4-B261-E3011AA708FE");
    }
}