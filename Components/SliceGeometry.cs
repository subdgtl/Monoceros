using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Monoceros {
    public class ComponentPopulateGeometryWithSlotCenters : GH_Component {
        public ComponentPopulateGeometryWithSlotCenters( )
            : base("Slice Geometry",
                   "Slice",
                   "Populate geometry with points ready to be " +
                   "used as Monoceros Slot and Monoceros Module centers. Supports Point, Curve, " +
                   "Brep, Mesh.",
                   "Monoceros",
                   "Postprocess") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddGeometryParameter("Geometry",
                                          "G",
                                          "Geometry to populate with slots",
                                          GH_ParamAccess.list);
            pManager.AddPlaneParameter("Base plane",
                                       "B",
                                       "Grid space base plane. Defines orientation of the grid.",
                                       GH_ParamAccess.item,
                                       Plane.WorldXY);
            pManager.AddVectorParameter(
               "Grid Slot Diagonal",
               "D",
               "World grid slot diagonal vector specifying single grid slot dimension " +
               "in base-plane-aligned XYZ axes",
               GH_ParamAccess.item,
               new Vector3d(1.0, 1.0, 1.0)
               );
            pManager.AddIntegerParameter("Fill Method",
                                         "F",
                                         "0 = wrap geometry surface, " +
                                         "1 = fill geometry volume, " +
                                         "2 = wrap surface and fill volume, " +
                                         "3 = wrap geometry surface (experimental)",
                                         GH_ParamAccess.item,
                                         2);
            pManager.AddNumberParameter("Precision",
                                        "P",
                                        "Geometry surface populate precision (lower = more precise & slower)",
                                        GH_ParamAccess.item,
                                        0.5);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddPointParameter("Slot Centers",
                                       "Pt",
                                       "Points ready to be used as Monoceros Slot centers or Module Part centers",
                                       GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var geometryRaw = new List<IGH_GeometricGoo>();
            var basePlane = new Plane();
            var diagonal = new Vector3d();
            var method = 2;
            var precision = 0.5;

            if (!DA.GetDataList(0, geometryRaw)) {
                return;
            }

            if (!DA.GetData(1, ref basePlane)) {
                return;
            }

            if (!DA.GetData(2, ref diagonal)) {
                return;
            }

            if (!DA.GetData(3, ref method)) {
                return;
            }

            if (!DA.GetData(4, ref precision)) {
                return;
            }


            if (diagonal.X <= 0 || diagonal.Y <= 0 || diagonal.Z <= 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "One or more slot dimensions are not larger than 0.");
                return;
            }

            if (method < 0 || method > 3) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Unknown Fill Method (F).");
                return;
            }

            var geometryClean = geometryRaw
               .Where(goo => goo != null)
               .Select(goo => {
                   var geo = goo.Duplicate();
                   var geometry = GH_Convert.ToGeometryBase(geo);
                   // Transformation of BReps sometimes does not work as expected
                   // For example non uniform scaling of a sphere results in a sphere
                   // Mesh scaling is safe and populating is fast(er)
                   if (geometry.HasBrepForm) {
                       var meshingParameters = MeshingParameters.FastRenderMesh;
                       var brep = Brep.TryConvertBrep(geometry);
                       var meshes = Mesh.CreateFromBrep(brep, meshingParameters);
                       var mesh = new Mesh();
                       foreach (var meshFace in meshes) {
                           mesh.Append(meshFace);
                       }
                       mesh.Weld(Math.PI / 8);
                       return mesh;
                   } else {
                       return geometry;
                   }
               }
               );

            if (!geometryClean.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Failed to collect any valid geometry.");
                return;
            }

            // Scale down to unit size
            var normalizationTransform = Transform.Scale(basePlane,
                                                         1 / diagonal.X,
                                                         1 / diagonal.Y,
                                                         1 / diagonal.Z);

            // Orient to the world coordinate system
            var worldAlignmentTransform = Transform.PlaneToPlane(basePlane, Plane.WorldXY);

            var centersNormalized = new List<Point3i>();
            var ambiguityWarning = false;
            foreach (var goo in geometryClean) {
                var geometry = goo.Duplicate();
                if (geometry.Transform(normalizationTransform) &&
                    geometry.Transform(worldAlignmentTransform)) {
                    var objectType = goo.ObjectType;
                    var isMesh = objectType == ObjectType.Mesh;
                    if ((method == 0 || method == 2 || method == 3) && objectType == ObjectType.Point) {
                        var p = ((Point)geometry).Location;
                        if (!IsOnEdgeUnitized(p)) {
                            centersNormalized.Add(new Point3i(p));
                        } else {
                            ambiguityWarning = true;
                        }
                    }
                    if ((method == 0 || method == 2 || method == 3) && objectType == ObjectType.Curve) {
                        centersNormalized.AddRange(PopulateCurve(precision, (Curve)geometry)
                                                    .Where(p => !IsOnEdgeUnitized(p))
                                                    .Select(p => new Point3i(p))
                                                    .Distinct());
                        ambiguityWarning = true;
                    }
                    if (objectType == ObjectType.Mesh
                        && (method == 0
                            || ((method == 2 || method == 3)
                                && !((Mesh)geometry).IsClosed))) {
                        centersNormalized.AddRange(PopulateMeshSurface(precision, (Mesh)geometry)
                                                    .Where(p => !IsOnEdgeUnitized(p))
                                                    .Select(p => new Point3i(p))
                                                    .Distinct());
                        ambiguityWarning = true;
                    }
                    if (method == 1 && objectType == ObjectType.Mesh && ((Mesh)geometry).IsClosed) {
                        centersNormalized.AddRange(CentersFromMeshVolume((Mesh)geometry));
                    }
                    if (method == 2 && objectType == ObjectType.Mesh && ((Mesh)geometry).IsClosed) {
                        centersNormalized.AddRange(CentersFromMeshVolumeAndSurface((Mesh)geometry));
                    }
                    if (method == 3 && objectType == ObjectType.Mesh && ((Mesh)geometry).IsClosed) {
                        centersNormalized.AddRange(CentersFromMeshSurface((Mesh)geometry));
                    }
                }
            }

            if (ambiguityWarning) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                                      "Test points matching the Slot grid may have been skipped " +
                                      "due to ambiguity.");
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                                      "Slightly move, scale or remodel the geometry where Slot " +
                                      "centers are missing.");
            }

            if (!centersNormalized.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Failed to collect any Slot centers from the given geometry.");
                return;
            }

            var centers = centersNormalized
                .Distinct()
                .Select(centerNormalized => centerNormalized.ToCartesian(basePlane, diagonal));

            DA.SetDataList(0, centers);
        }

        private static bool IsOnEdgeUnitized(Point3d geometryPoint) {
            return Math.Abs(Math.Abs(geometryPoint.X % 1) - 0.5) <= RhinoMath.SqrtEpsilon
                || Math.Abs(Math.Abs(geometryPoint.Y % 1) - 0.5) <= RhinoMath.SqrtEpsilon
                || Math.Abs(Math.Abs(geometryPoint.Z % 1) - 0.5) <= RhinoMath.SqrtEpsilon;
        }

        /// <summary>
        /// Populate mesh surface with evenly distributed points in specified
        /// distances.
        /// </summary>
        /// <param name="distance">Rough maximum distance between the points. 
        ///     This behaves differently for various geometry type and for
        ///     various circumstances.  The distance is never higher and often
        ///     is significantly lower.</param>
        /// <param name="mesh">Mesh geometry, which surface should to be
        ///     populated with points.</param>
        private static IEnumerable<Point3d> PopulateMeshSurface(double distance, Mesh mesh) {
            return mesh.Faces.SelectMany(face => {
                var firstTriangle = PopulateTriangle(distance,
                                                     mesh.Vertices[face.A],
                                                     mesh.Vertices[face.B],
                                                     mesh.Vertices[face.C]);
                if (face.IsTriangle) {
                    return firstTriangle;

                } else {
                    var secondTriangle = PopulateTriangle(distance,
                                                          mesh.Vertices[face.A],
                                                          mesh.Vertices[face.C],
                                                          mesh.Vertices[face.D]);
                    return firstTriangle.Concat(secondTriangle);
                }
            });
        }

        /// <summary>
        /// Populate curve with evenly distributed points in specified
        /// distances.
        /// </summary>
        /// <param name="distance">Rough maximum distance between the points. 
        ///     This behaves differently for various geometry type and for
        ///     various circumstances.  The distance is never higher and often
        ///     is significantly lower.</param>
        /// <param name="mesh">Curve (incl. Line, Polyline, Arc, Circle), which
        ///     should to be populated with points.</param>
        private static IEnumerable<Point3d> PopulateCurve(double distance, Curve curve) {
            // Curve division calculation is bearably fast, therefore it can be more precise
            var preciseDistance = distance * 0.25;
            var divisionPoints = curve.DivideByLength(preciseDistance, true);
            if (divisionPoints != null) {
                return divisionPoints.Select(t => curve.PointAt(t));
            } else {
                return new List<Point3d>() { curve.PointAtStart, curve.PointAtEnd };
            }
        }

        /// <summary>
        /// Populates the mesh volume with unit boxes.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <returns>A list of Point3ds representing unit boxes centers that are
        ///     entirely inside mesh volume.</returns>
        private static List<Point3i> CentersFromMeshVolume(Mesh mesh) {
            var pointsInsideMesh = new List<Point3i>();
            var boundingBox = mesh.GetBoundingBox(false);
            var slotInterval = new Interval(-0.5, 0.5);
            for (var z = Math.Floor(boundingBox.Min.Z - 1); z < Math.Ceiling(boundingBox.Max.Z + 1); z++) {
                for (var y = Math.Floor(boundingBox.Min.Y - 1); y < Math.Ceiling(boundingBox.Max.Y + 1); y++) {
                    for (var x = Math.Floor(boundingBox.Min.X - 1); x < Math.Ceiling(boundingBox.Max.X + 1); x++) {
                        var testSlotCenter = new Point3d(x, y, z);
                        var plane = Plane.WorldXY;
                        plane.Origin = testSlotCenter;
                        var box = new Box(plane, slotInterval, slotInterval, slotInterval);
                        var boxPoints = box.GetCorners();
                        if (boxPoints.All(point => mesh.IsPointInside(point, RhinoMath.SqrtEpsilon, false))) {
                            pointsInsideMesh.Add(new Point3i(testSlotCenter));
                        }
                    }

                }
            }
            return pointsInsideMesh;
        }


        /// <summary>
        /// Populates the mesh volume and surface with unit boxes.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <returns>A list of Point3ds representing unit boxes centers that are
        ///     entirely inside mesh volume.</returns>
        private static List<Point3i> CentersFromMeshVolumeAndSurface(Mesh mesh) {
            var pointsInsideMesh = new List<Point3i>();
            var boundingBox = mesh.GetBoundingBox(false);
            var slotInterval = new Interval(-0.5, 0.5);
            for (var z = Math.Floor(boundingBox.Min.Z - 1); z < Math.Ceiling(boundingBox.Max.Z + 1); z++) {
                for (var y = Math.Floor(boundingBox.Min.Y - 1); y < Math.Ceiling(boundingBox.Max.Y + 1); y++) {
                    for (var x = Math.Floor(boundingBox.Min.X - 1); x < Math.Ceiling(boundingBox.Max.X + 1); x++) {
                        var testSlotCenter = new Point3d(x, y, z);
                        var plane = Plane.WorldXY;
                        plane.Origin = testSlotCenter;
                        var box = new Box(plane, slotInterval, slotInterval, slotInterval);
                        var boxPoints = box.GetCorners();
                        if (boxPoints.Any(point => mesh.IsPointInside(point, Rhino.RhinoMath.SqrtEpsilon, false))) {
                            pointsInsideMesh.Add(new Point3i(testSlotCenter));
                        }
                    }

                }
            }
            return pointsInsideMesh;
        }

        /// <summary>
        /// Populates the mesh surface with unit boxes.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <returns>A list of Point3ds representing unit boxes centers that are
        ///     entirely inside mesh volume.</returns>
        private static List<Point3i> CentersFromMeshSurface(Mesh mesh) {
            var pointsInsideMesh = new List<Point3i>();
            var boundingBox = mesh.GetBoundingBox(false);
            var slotInterval = new Interval(-0.5, 0.5);
            for (var z = Math.Floor(boundingBox.Min.Z - 1); z < Math.Ceiling(boundingBox.Max.Z + 1); z++) {
                for (var y = Math.Floor(boundingBox.Min.Y - 1); y < Math.Ceiling(boundingBox.Max.Y + 1); y++) {
                    for (var x = Math.Floor(boundingBox.Min.X - 1); x < Math.Ceiling(boundingBox.Max.X + 1); x++) {
                        var testSlotCenter = new Point3d(x, y, z);
                        var plane = Plane.WorldXY;
                        plane.Origin = testSlotCenter;
                        var box = new Box(plane, slotInterval, slotInterval, slotInterval);
                        var boxPoints = box.GetCorners();
                        var boxcornersInside = boxPoints
                            .Count(point => mesh.IsPointInside(point, RhinoMath.SqrtEpsilon, false));
                        if (boxcornersInside > 0 && boxcornersInside < 6) {
                            pointsInsideMesh.Add(new Point3i(testSlotCenter));
                        }
                    }

                }
            }
            return pointsInsideMesh;
        }

        /// <summary>
        /// Evenly populate a triangle with points.
        /// </summary>
        /// <param name="aPoint">The first point of the triangle in world
        ///     (Cartesian) coordinates.</param>
        /// <param name="bPoint">The second point of the triangle in world
        ///     (Cartesian) coordinates.</param>
        /// <param name="cPoint">The third point of the triangle in world
        ///     (Cartesian) coordinates.</param>
        /// <param name="distance">Rough maximum distance between the points. 
        ///     This behaves differently for various geometry type and for
        ///     various circumstances.  The distance is never higher and often
        ///     is significantly lower.</param>
        private static List<Point3d> PopulateTriangle(double distance,
                                                      Point3d aPoint,
                                                      Point3d bPoint,
                                                      Point3d cPoint) {
            // Compute the density of points on the respective face.
            var abDistanceSq = aPoint.DistanceToSquared(bPoint);
            var bcDistanceSq = bPoint.DistanceToSquared(cPoint);
            var caDistanceSq = cPoint.DistanceToSquared(aPoint);
            var longestEdgeLen = Math.Sqrt(
                Math.Max(abDistanceSq,
                         Math.Max(bcDistanceSq, caDistanceSq))
                );

            // Number of face divisions (points) in each direction.
            var divisions = Math.Ceiling(longestEdgeLen / distance);

            var points = new List<Point3d>(Convert.ToInt32(Math.Pow((divisions + 1), 2)));

            for (var ui = 0; ui < divisions; ui++) {
                for (var wi = 0; wi < divisions; wi++) {
                    var uNormalized = ui / divisions;
                    var wNormalized = wi / divisions;
                    var vNormalized = 1.0 - uNormalized - wNormalized;
                    if (vNormalized >= 0.0) {
                        var barycentric =
                            new Point3d(uNormalized, vNormalized, wNormalized);
                        var cartesian = BarycentricToCartesian(barycentric,
                                                               aPoint,
                                                               bPoint,
                                                               cPoint);
                        points.Add(cartesian);
                    }
                }
            }
            return points;
        }

        /// <summary>
        /// Convert barycentric coordinates into Cartesian.
        /// </summary>
        /// <param name="barycentricCoords">A point on the triangle specified in
        ///     barycentric coordinates.</param>
        /// <param name="aPoint">The first point of the triangle in world
        ///     (Cartesian) coordinates.</param>
        /// <param name="bPoint">The second point of the triangle in world
        ///     (Cartesian) coordinates.</param>
        /// <param name="cPoint">The third point of the triangle in world
        ///     (Cartesian) coordinates.</param>
        private static Point3d BarycentricToCartesian(
            Point3d barycentricCoords,
            Point3d aPoint,
            Point3d bPoint,
            Point3d cPoint
        ) {
            return new Point3d((barycentricCoords.X * aPoint)
                + (barycentricCoords.Y * bPoint)
                + (barycentricCoords.Z * cPoint));
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.populate;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("F4CB7062-F85C-4E92-8215-034C4CC3941C");
    }
}
