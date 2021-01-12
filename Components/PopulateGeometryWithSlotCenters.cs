using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace WFCPlugin {
    public class ComponentPopulateGeometryWithSlotCenters : GH_Component {
        public ComponentPopulateGeometryWithSlotCenters( )
            : base("Populate Geometry With Slot Centers",
                   "PopSlots",
                   "Populate geometry with points ready to be " +
                   "used as Monoceros Slot and Monoceros Module centers. Supports Point, Curve, " +
                   "Brep, Mesh.",
                   "Monoceros",
                   "Main") {
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
            pManager.AddIntegerParameter("Fill",
                                         "F",
                                         "0 = only wrap geometry surface, " +
                                         "1 = only fill geometry volume, " +
                                         "2 = wrap surface and fill volume",
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
                                       "Points ready to be used as Monoceros Slot centers",
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

            var submoduleCentersNormalized = new List<Point3i>();
            var geometryOnBoundary = false;

            foreach (var goo in geometryClean) {
                var geometry = goo.Duplicate();
                if (geometry.Transform(normalizationTransform) &&
                    geometry.Transform(worldAlignmentTransform)) {
                    if (method == 0 || method == 2) {
                        var populatePoints = PopulateSurface(precision, geometry);
                        foreach (var geometryPoint in populatePoints) {
                            if (IsOnEdge(geometryPoint, diagonal)) {
                                geometryOnBoundary = true;
                            } else {
                                // Round point locations
                                // Slot dimension is for the sake of this calculation 1,1,1
                                var slotCenterPoint = new Point3i(geometryPoint);
                                // Deduplicate
                                if (!submoduleCentersNormalized.Contains(slotCenterPoint)) {
                                    submoduleCentersNormalized.Add(slotCenterPoint);
                                }
                            }
                        }
                    }
                    if ((method == 1 || method == 2) &&
                        goo.ObjectType == Rhino.DocObjects.ObjectType.Mesh) {
                        var mesh = (Mesh)geometry;
                        var pointsInsideMesh = PopulateMeshVolumeUnit(mesh);
                        foreach (var geometryPoint in pointsInsideMesh) {
                            if (IsOnEdge(geometryPoint, diagonal)) {
                                geometryOnBoundary = true;
                            } else {
                                // Round point locations
                                // Slot dimension is for the sake of this calculation 1,1,1
                                var slotCenterPoint = new Point3i(geometryPoint);
                                // Deduplicate
                                if (!submoduleCentersNormalized.Contains(slotCenterPoint)) {
                                    submoduleCentersNormalized.Add(slotCenterPoint);
                                }
                            }
                        }
                    }
                }
            }

            if (!submoduleCentersNormalized.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Failed to collect any Slot centers from the given geometry.");
                return;
            }

            if (geometryOnBoundary) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                                  "Some geometry touches the boundary of a Slot.");
            }

            var baseAlignmentTransform = Transform.PlaneToPlane(Plane.WorldXY, basePlane);
            var scalingTransform = Transform.Scale(basePlane, diagonal.X, diagonal.Y, diagonal.Z);

            var submoduleCenters = submoduleCentersNormalized
                .Select(centerNormalized => {
                    var center = centerNormalized.ToPoint3d();
                    center.Transform(baseAlignmentTransform);
                    center.Transform(scalingTransform);
                    return center;
                });



            DA.SetDataList(0, submoduleCenters);
        }

        private static bool IsOnEdge(Point3d geometryPoint, Vector3d diagonal) {
            return Math.Abs(geometryPoint.X % diagonal.X - (diagonal.X / 2)) <= Rhino.RhinoMath.SqrtEpsilon
                || Math.Abs(geometryPoint.Y % diagonal.Y - (diagonal.Y / 2)) <= Rhino.RhinoMath.SqrtEpsilon
                || Math.Abs(geometryPoint.Z % diagonal.Z - (diagonal.Z / 2)) <= Rhino.RhinoMath.SqrtEpsilon;
        }

        /// <summary>
        /// Populates the surface of various geometries with points.
        /// </summary>
        /// <param name="distance">The distance.</param>
        /// <param name="goo">The goo.</param>
        /// <returns>A list of Point3ds.</returns>
        private static IEnumerable<Point3d> PopulateSurface(double distance, GeometryBase goo) {
            var type = goo.ObjectType;
            switch (type) {
                case Rhino.DocObjects.ObjectType.Point:
                    var point = (Point)goo;
                    return Enumerable.Repeat(point.Location, 1);
                case Rhino.DocObjects.ObjectType.Curve:
                    var curve = (Curve)goo;
                    return PopulateCurve(distance, curve);
                case Rhino.DocObjects.ObjectType.Mesh:
                    var mesh = (Mesh)goo;
                    return PopulateMeshSurface(distance, mesh);
                default:
                    return Enumerable.Empty<Point3d>();
            }
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
                if (!face.IsTriangle) {
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
        private static List<Point3d> PopulateMeshVolumeUnit(Mesh mesh) {
            var pointsInsideMesh = new List<Point3d>();
            var boundingBox = mesh.GetBoundingBox(false);
            var slotInterval = new Interval(-0.5, 0.5);
            for (var z = Math.Round(boundingBox.Min.Z - 1); z < Math.Round(boundingBox.Max.Z + 1); z++) {
                for (var y = Math.Round(boundingBox.Min.Y - 1); y < Math.Round(boundingBox.Max.Y + 1); y++) {
                    for (var x = Math.Round(boundingBox.Min.X - 1); x < Math.Round(boundingBox.Max.X + 1); x++) {
                        var testSlotCenter = new Point3d(x, y, z);
                        var plane = Plane.WorldXY;
                        plane.Origin = testSlotCenter;
                        var box = new Box(plane, slotInterval, slotInterval, slotInterval);
                        var boxPoints = box.GetCorners();
                        if (boxPoints.All(point => mesh.IsPointInside(point, Rhino.RhinoMath.SqrtEpsilon, false))) {
                            pointsInsideMesh.Add(testSlotCenter);
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
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.W;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("F4CB7062-F85C-4E92-8215-034C4CC3941C");
    }
}
