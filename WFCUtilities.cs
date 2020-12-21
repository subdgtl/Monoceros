using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace WFCTools {

    // TODO: Dissolve into respective files

    /// <summary>
    /// Supplemental static methods for the WFC Tools. 
    /// </summary>
    public class WFCUtilities {
        public const string EMPTY_MODULE_NAME = "empty";
        public const string OUTER_MODULE_NAME = "out";
        // TODO: prevent from using; preferably make an array that contains all reserved strings and prevent it from using
        public const string INDIFFERENT_MODULE_TAG = "Indifferent";

        public static readonly string[] RESERVED_NAMES = { EMPTY_MODULE_NAME, OUTER_MODULE_NAME, INDIFFERENT_MODULE_TAG };

        public static string ReservedNames() {
            return RESERVED_NAMES.Aggregate("", (a, s) => s + a + ",");
        }

        public static string ToString(ConnectionType connectionType) {
            switch (connectionType) {
                case ConnectionType.Explicit:
                    return "Explicit";
                case ConnectionType.Internal:
                    return "Internal";
                case ConnectionType.Tagged:
                    return "Tagged";
                case ConnectionType.Indifferent:
                    return "Indifferent";
                default:
                    return "Unknown";
            }
        }

        public static string ToString(Orientation orientation) {
            switch (orientation) {
                case Orientation.Positive:
                    return "+";
                case Orientation.Negative:
                    return "-";
                default:
                    return "?";
            }
        }

        public static string ToString(Axis axis) {
            switch (axis) {
                case Axis.X:
                    return "X";
                case Axis.Y:
                    return "Y";
                case Axis.Z:
                    return "Z";
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// Check whether entire geometries fit inside their respective module cages. 
        /// </summary>
        /// <param name="geometryBoundingBoxes">
        /// BoundingBoxes of geometry to be checked if inside module cages.
        /// </param>
        /// <param name="patternModuleCagesWithGeometry">
        /// Boolean pattern parallel to moduleCages.
        /// True = module cage contains some geometry or is inside a solid BRep.
        /// False = module cage does not contain any geometry and is not inside any solid BRep (or such case was not detected yet).
        /// </param>
        /// <param name="patternGeometryEntirelyInSingleModuleCage">
        /// Boolean pattern parallel to geometryBoundingBoxes.
        /// True = geometry is entirely inside of a single module cage.
        /// False = geometry is not entirely inside of a single module cage or not at all (or such case was not detected yet).
        /// </param>
        public static void AreEntireGeometriesInsideModuleCages(
            List<BoundingBox> geometryBoundingBoxes,
            List<BoundingBox> moduleCages,
            ref List<bool> patternGeometryEntirelyInSingleModuleCage,
            ref List<bool> patternModuleCagesWithGeometry
            ) {
            // Check if the cage contains entire input geometry
            for (int moduleCageI = 0; moduleCageI < moduleCages.Count; moduleCageI++) {
                if (!patternModuleCagesWithGeometry[moduleCageI]) {
                    for (int geometryI = 0; geometryI < geometryBoundingBoxes.Count; geometryI++) {
                        if (!patternGeometryEntirelyInSingleModuleCage[geometryI] &&
                            moduleCages[moduleCageI].Contains(geometryBoundingBoxes[geometryI])
                        ) {
                            // If the geometry is small enough that it fits a cage, mark both, the cage and the geometry.
                            patternModuleCagesWithGeometry[moduleCageI] = true;
                            patternGeometryEntirelyInSingleModuleCage[geometryI] = true;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if modules are entirely inside provided solid BReps. 
        /// </summary>
        /// <param name="moduleCages">
        /// World-aligned BoundingBoxes to be checked whether they are inside specified solid BReps.
        /// </param>
        /// <param name="solidBreps">
        /// Solid BReps that are being checked for containment of specified bounding boxes.
        /// Doesn't check if the BReps are solid, that check needs to be done before.
        /// </param>
        /// <param name="strict">
        /// True = all 8 module cage corners must be inside the specified solid BReps.
        /// False = only module cage center must be inside the specified solid BReps.
        /// </param>
        /// <param name="patternModuleCagesWithGeometry">
        /// Boolean pattern parallel to moduleCages.
        /// True = module cage contains some geometry or is inside a solid BRep.
        /// False = module cage does not contain any geometry and is not inside any solid BRep (or such case was not detected yet).
        /// </param>
        public static void AreModulesInsideSolidBreps(
            List<BoundingBox> moduleCages,
            IEnumerable<Brep> solidBreps,
            bool strict,
            ref List<bool> patternModuleCagesWithGeometry
            ) {
            for (int moduleCageI = 0; moduleCageI < moduleCages.Count; moduleCageI++) {
                if (!patternModuleCagesWithGeometry[moduleCageI]) {
                    foreach (Brep brep in solidBreps) {
                        bool inside = strict
                            ? moduleCages[moduleCageI].GetCorners().All(point => brep.IsPointInside(point, Rhino.RhinoMath.SqrtEpsilon, true))
                            : brep.IsPointInside(moduleCages[moduleCageI].Center, Rhino.RhinoMath.SqrtEpsilon, true);
                        if (inside) {
                            patternModuleCagesWithGeometry[moduleCageI] = true;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Replace a plane-aligned union bounding box with a regular grid of smaller bounding boxes - module cages. 
        /// The grid origin and directions align with the world origin.
        /// The module cages will be rounded to fit the input union box and match with the grid unit step.
        /// </summary>
        /// <param name="targetDiagonal">
        /// Size of the module cage in the XYZ directions.
        /// </param>
        /// <param name="unionBox">
        /// The union bounding box to be converted into smaller module cages matching the specified grid steps.
        /// </param>
        public static List<BoundingBox> SubdivideBoundingBox(
            Vector3d targetDiagonal,
            BoundingBox unionBox
            ) {
            Point3d minCorner = unionBox.Corner(true, true, true);
            Point3d maxCorner = unionBox.Corner(false, false, false);

            double moduleMinX = Math.Floor(minCorner.X / targetDiagonal.X);
            double moduleMaxX = Math.Ceiling(maxCorner.X / targetDiagonal.X);
            double moduleMinY = Math.Floor(minCorner.Y / targetDiagonal.Y);
            double moduleMaxY = Math.Ceiling(maxCorner.Y / targetDiagonal.Y);
            double moduleMinZ = Math.Floor(minCorner.Z / targetDiagonal.Z);
            double moduleMaxZ = Math.Ceiling(maxCorner.Z / targetDiagonal.Z);

            double moduleCountX = moduleMaxX - moduleMinX;
            double moduleCountY = moduleMaxY - moduleMinY;
            double moduleCountZ = moduleMaxZ - moduleMinZ;
            List<BoundingBox> planeAlignedModuleCages = new List<BoundingBox>((int)(
                Math.Max(1, moduleCountX)
                * Math.Max(1, moduleCountY)
                * Math.Max(1, moduleCountZ)
                ));

            for (int zStep = 0; zStep <= moduleCountZ; zStep++) {
                for (int yStep = 0; yStep <= moduleCountY; yStep++) {
                    for (int xStep = 0; xStep <= moduleCountX; xStep++) {
                        Point3d minPoint = new Point3d(
                            (moduleMinX + xStep) * targetDiagonal.X,
                            (moduleMinY + yStep) * targetDiagonal.Y,
                            (moduleMinZ + zStep) * targetDiagonal.Z
                        );
                        Point3d maxPoint = new Point3d(
                            (moduleMinX + xStep + 1) * targetDiagonal.X,
                            (moduleMinY + yStep + 1) * targetDiagonal.Y,
                            (moduleMinZ + zStep + 1) * targetDiagonal.Z
                        );
                        BoundingBox planeAlignedModuleCage = new BoundingBox(minPoint, maxPoint);
                        planeAlignedModuleCages.Add(planeAlignedModuleCage);
                    }
                }
            }
            return planeAlignedModuleCages;
        }

        /// <summary>
        /// Populate geometry surface with evenly distributed points in specified distances.
        /// Supported geometry types: Point, Curve (incl. Line, Polyline, Arc, Circle), Mesh, BRep (incl. Surface).
        /// </summary>
        /// <param name="distance">
        /// Rough maximum distance between the points. 
        /// This behaves differently for various geometry type and for various circumstances. 
        /// The distance is never higher and often is significantly lower (mainly for Mesh geometry). 
        /// </param>
        /// <param name="goo">
        /// Geometry, which surface should to be populated with points. 
        /// </param>
        public static IEnumerable<Point3d> PopulateGeometry(double distance, GeometryBase goo) {
            Rhino.DocObjects.ObjectType type = goo.ObjectType;
            if (goo.HasBrepForm) {
                Brep bRep = Brep.TryConvertBrep(goo);
                return PopulateBrepSurfaces(distance, bRep);
            }
            switch (type) {
                case Rhino.DocObjects.ObjectType.Point:
                    Point point = (Point)goo;
                    return Enumerable.Repeat(point.Location, 1);
                case Rhino.DocObjects.ObjectType.Curve:
                    Curve curve = (Curve)goo;
                    return PopulateCurve(distance, curve);
                case Rhino.DocObjects.ObjectType.Mesh:
                    Mesh mesh = (Mesh)goo;
                    return PopulateMesh(distance, mesh);
            }
            return null;
        }

        // TODO: Try to avoid memory allocation
        /// <summary>
        /// Populate mesh surface with evenly distributed points in specified distances.
        /// </summary>
        /// <param name="distance">
        /// Rough maximum distance between the points. 
        /// This behaves differently for various geometry type and for various circumstances. 
        /// The distance is never higher and often is significantly lower. 
        /// </param>
        /// <param name="mesh">
        /// Mesh geometry, which surface should to be populated with points. 
        /// </param>
        public static IEnumerable<Point3d> PopulateMesh(double distance, Mesh mesh) {
            List<Point3d> pointsOnMesh = new List<Point3d>();
            foreach (MeshFace face in mesh.Faces) {
                pointsOnMesh.AddRange(
                    PopulateTriangle(
                        distance,
                        mesh.Vertices[face.A],
                        mesh.Vertices[face.B],
                        mesh.Vertices[face.C]
                    )
                );
                if (!face.IsTriangle) {
                    pointsOnMesh.AddRange(
                        PopulateTriangle(
                            distance,
                            mesh.Vertices[face.A],
                            mesh.Vertices[face.B],
                            mesh.Vertices[face.D]
                        )
                    );
                }
            }
            return pointsOnMesh;
        }

        /// <summary>
        /// Populate curve with evenly distributed points in specified distances.
        /// </summary>
        /// <param name="distance">
        /// Rough maximum distance between the points. 
        /// This behaves differently for various geometry type and for various circumstances. 
        /// The distance is never higher and often is significantly lower. 
        /// </param>
        /// <param name="mesh">
        /// Curve (incl. Line, Polyline, Arc, Circle), which should to be populated with points. 
        /// </param>
        public static IEnumerable<Point3d> PopulateCurve(double distance, Curve curve) {
            var divisionPoints = curve.DivideByLength(distance, true);
            if (divisionPoints != null)
            {
                return divisionPoints.Select(t => curve.PointAt(t));
            } else
            {
                return new List<Point3d>(){ curve.PointAtStart, curve.PointAtEnd};
            }
        }

        // TODO: Try to avoid memory allocation
        /// <summary>
        /// Populate Brep surface with evenly distributed points in specified distances.
        /// </summary>
        /// <param name="distance">
        /// Rough maximum distance between the points. 
        /// This behaves differently for various geometry type and for various circumstances. 
        /// The distance is never higher and often is significantly lower. 
        /// </param>
        /// <param name="bRep">
        /// BRep geometry (incl. Surface), which surface should to be populated with points. 
        /// </param>
        public static IEnumerable<Point3d> PopulateBrepSurfaces(double distance, Brep bRep) {
            List<Point3d> pointsOnBrepSurfaces = new List<Point3d>();
            Rhino.Geometry.Collections.BrepFaceList faces = bRep.Faces;
            foreach (BrepFace face in faces) {
                BoundingBox bBox = face.GetBoundingBox(Transform.Identity);
                int divisions = (int)Math.Ceiling(bBox.Diagonal.Length / distance);
                Interval domainU = face.Domain(0);
                Interval domainV = face.Domain(1);
                for (int vCounter = 0; vCounter <= divisions; vCounter++) {
                    for (int uCounter = 0; uCounter <= divisions; uCounter++) {
                        double u = domainU.Length * uCounter / divisions + domainU.Min;
                        double v = domainV.Length * vCounter / divisions + domainV.Min;
                        if (face.IsSurface) {
                            pointsOnBrepSurfaces.Add(face.PointAt(u, v));
                        } else {
                            // If the face is a trimmed surface, then check if the generated point is inside.
                            PointFaceRelation pointFaceRelation = face.IsPointOnFace(u, v);
                            if (pointFaceRelation == PointFaceRelation.Interior || pointFaceRelation == PointFaceRelation.Boundary) {
                                pointsOnBrepSurfaces.Add(face.PointAt(u, v));
                            }
                        }
                    }
                }
            }
            return pointsOnBrepSurfaces;
        }

        // TODO: Try to avoid memory allocation
        /// <summary>
        /// Evenly populate a triangle with points.
        /// </summary>
        /// <param name="aPoint">
        /// The first point of the triangle in world (Cartesian) coordinates. 
        /// </param>
        /// <param name="bPoint">
        /// The second point of the triangle in world (Cartesian) coordinates. 
        /// </param>
        /// <param name="cPoint">
        /// The third point of the triangle in world (Cartesian) coordinates. 
        /// </param>
        /// <param name="distance">
        /// Rough maximum distance between the points. 
        /// This behaves differently for various geometry type and for various circumstances. 
        /// The distance is never higher and often is significantly lower. 
        /// </param>
        private static IEnumerable<Point3d> PopulateTriangle(double distance, Point3d aPoint, Point3d bPoint, Point3d cPoint) {
            // Compute the density of points on the respective face.
            double abDistanceSq = aPoint.DistanceToSquared(bPoint);
            double bcDistanceSq = bPoint.DistanceToSquared(cPoint);
            double caDistanceSq = cPoint.DistanceToSquared(aPoint);
            double longestEdgeLen = Math.Sqrt(Math.Max(abDistanceSq, Math.Max(bcDistanceSq, caDistanceSq)));

            // Number of face divisions (points) in each direction.
            double divisions = Math.Ceiling(longestEdgeLen / distance);

            List<Point3d> points = new List<Point3d>((int)Math.Sqrt(divisions + 1));

            for (int ui = 0; ui < divisions; ui++) {
                for (int wi = 0; wi < divisions; wi++) {
                    double uNormalized = ui / divisions;
                    double wNormalized = wi / divisions;
                    double vNormalized = 1.0 - uNormalized - wNormalized;
                    if (vNormalized >= 0.0) {
                        Point3d barycentric =
                            new Point3d(uNormalized, vNormalized, wNormalized);
                        Point3d cartesian = BarycentricToCartesian(
                            barycentric,
                            aPoint,
                            bPoint,
                            cPoint
                        );
                        points.Add(cartesian);
                    }
                }
            }
            return points;
        }

        /// <summary>
        /// Convert barycentric coordinates into Cartesian.
        /// </summary>
        /// <param name="barycentricCoords">
        /// A point on the triangle specified in barycentric coordinates. 
        /// </param>
        /// <param name="aPoint">
        /// The first point of the triangle in world (Cartesian) coordinates. 
        /// </param>
        /// <param name="bPoint">
        /// The second point of the triangle in world (Cartesian) coordinates. 
        /// </param>
        /// <param name="cPoint">
        /// The third point of the triangle in world (Cartesian) coordinates. 
        /// </param>
        private static Point3d BarycentricToCartesian(
            Point3d barycentricCoords,
            Point3d aPoint,
            Point3d bPoint,
            Point3d cPoint
        ) {
            return new Point3d(
                barycentricCoords.X * aPoint +
                barycentricCoords.Y * bPoint +
                barycentricCoords.Z * cPoint
            );
        }
    }
}