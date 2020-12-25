// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;

namespace WFCToolset
{

    /// <summary>
    /// Supplemental static methods for the WFC Tools. 
    /// </summary>
    public class Populate
    {
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
        public static IEnumerable<Point3d> PopulateGeometry(double distance, GeometryBase goo)
        {
            var type = goo.ObjectType;
            switch (type)
            {
                case Rhino.DocObjects.ObjectType.Point:
                    var point = (Point)goo;
                    return Enumerable.Repeat(point.Location, 1);
                case Rhino.DocObjects.ObjectType.Curve:
                    var curve = (Curve)goo;
                    return PopulateCurve(distance, curve);
                case Rhino.DocObjects.ObjectType.Mesh:
                    var mesh = (Mesh)goo;
                    var surfacePoints = PopulateMeshSurface(distance, mesh);
                    var volumePoints = PopulateMeshVolume(distance, mesh);
                    var points = surfacePoints.Concat(volumePoints);
                    return points;
            }
            if (goo.HasBrepForm)
            {
                var meshingParameters = MeshingParameters.FastRenderMesh;
                var meshes = Mesh.CreateFromBrep(Brep.TryConvertBrep(goo), meshingParameters);
                var points = new List<Point3d>();
                foreach (var mesh in meshes)
                {
                    var surfacePoints = PopulateMeshSurface(distance, mesh);
                    var volumePoints = PopulateMeshVolume(distance, mesh);
                    points.AddRange(surfacePoints);
                    points.AddRange(volumePoints);
                }
                return points;
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
        public static IEnumerable<Point3d> PopulateMeshSurface(double distance, Mesh mesh)
        {
            var pointsOnMesh = new List<Point3d>();
            foreach (var face in mesh.Faces)
            {
                pointsOnMesh.AddRange(
                    PopulateTriangle(
                        distance,
                        mesh.Vertices[face.A],
                        mesh.Vertices[face.B],
                        mesh.Vertices[face.C]
                    )
                );
                if (!face.IsTriangle)
                {
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
        public static IEnumerable<Point3d> PopulateCurve(double distance, Curve curve)
        {
            // Curve division calcualtion is bearably fast, therefore it can be more precise
            var preciseDistance = distance * 0.25;
            var divisionPoints = curve.DivideByLength(preciseDistance, true);
            if (divisionPoints != null)
            {
                return divisionPoints.Select(t => curve.PointAt(t));
            }
            else
            {
                return new List<Point3d>() { curve.PointAtStart, curve.PointAtEnd };
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
        public static IEnumerable<Point3d> PopulateBrepSurface(double distance, Brep bRep)
        {
            // TODO: Investigate the unprecise results
            var pointsOnBrepSurfaces = new List<Point3d>();
            var faces = bRep.Faces;
            foreach (var face in faces)
            {
                face.GetSurfaceSize(out var width, out var height);
                var divisions = (int)Math.Ceiling(Math.Max(width, height) / distance);
                var domainU = face.Domain(0);
                var domainV = face.Domain(1);
                for (var vCounter = 0; vCounter <= divisions; vCounter++)
                {
                    for (var uCounter = 0; uCounter <= divisions; uCounter++)
                    {
                        var u = domainU.Length * uCounter / divisions + domainU.Min;
                        var v = domainV.Length * vCounter / divisions + domainV.Min;
                        if (face.IsSurface)
                        {
                            pointsOnBrepSurfaces.Add(face.PointAt(u, v));
                        }
                        else
                        {
                            // If the face is a trimmed surface, then check if the generated point is inside.
                            var pointFaceRelation = face.IsPointOnFace(u, v);
                            if (pointFaceRelation == PointFaceRelation.Interior || pointFaceRelation == PointFaceRelation.Boundary)
                            {
                                pointsOnBrepSurfaces.Add(face.PointAt(u, v));
                            }
                        }
                    }
                }
            }
            return pointsOnBrepSurfaces;
        }

        public static IEnumerable<Point3d> PopulateBrepVolume(double distance, Brep bRep)
        {
            // TODO: Investigate the unprecise results
            var pointsInsideBrep = new List<Point3d>();
            var boundingBox = bRep.GetBoundingBox(false);
            for (var z = boundingBox.Min.Z - distance; z < boundingBox.Max.Z + distance; z += distance)
            {
                for (var y = boundingBox.Min.Y - distance; y < boundingBox.Max.Y + distance; y += distance)
                {
                    for (var x = boundingBox.Min.X - distance; x < boundingBox.Max.X + distance; x += distance)
                    {
                        var testPoint = new Point3d(x, y, z);
                        if (bRep.IsPointInside(testPoint, Rhino.RhinoMath.SqrtEpsilon, true))
                        {
                            pointsInsideBrep.Add(testPoint);
                        }
                    }

                }
            }
            return pointsInsideBrep;
        }

        public static IEnumerable<Point3d> PopulateMeshVolume(double distance, Mesh mesh)
        {
            var pointsInsideMesh = new List<Point3d>();
            var boundingBox = mesh.GetBoundingBox(false);
            for (var z = boundingBox.Min.Z - distance; z < boundingBox.Max.Z + distance; z += distance)
            {
                for (var y = boundingBox.Min.Y - distance; y < boundingBox.Max.Y + distance; y += distance)
                {
                    for (var x = boundingBox.Min.X - distance; x < boundingBox.Max.X + distance; x += distance)
                    {
                        var testPoint = new Point3d(x, y, z);
                        if (mesh.IsPointInside(testPoint, Rhino.RhinoMath.SqrtEpsilon, false))
                        {
                            pointsInsideMesh.Add(testPoint);
                        }
                    }

                }
            }
            return pointsInsideMesh;
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
        private static IEnumerable<Point3d> PopulateTriangle(double distance, Point3d aPoint, Point3d bPoint, Point3d cPoint)
        {
            // Compute the density of points on the respective face.
            var abDistanceSq = aPoint.DistanceToSquared(bPoint);
            var bcDistanceSq = bPoint.DistanceToSquared(cPoint);
            var caDistanceSq = cPoint.DistanceToSquared(aPoint);
            var longestEdgeLen = Math.Sqrt(Math.Max(abDistanceSq, Math.Max(bcDistanceSq, caDistanceSq)));

            // Number of face divisions (points) in each direction.
            var divisions = Math.Ceiling(longestEdgeLen / distance);

            var points = new List<Point3d>((int)Math.Sqrt(divisions + 1));

            for (var ui = 0; ui < divisions; ui++)
            {
                for (var wi = 0; wi < divisions; wi++)
                {
                    var uNormalized = ui / divisions;
                    var wNormalized = wi / divisions;
                    var vNormalized = 1.0 - uNormalized - wNormalized;
                    if (vNormalized >= 0.0)
                    {
                        var barycentric =
                            new Point3d(uNormalized, vNormalized, wNormalized);
                        var cartesian = BarycentricToCartesian(
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
        )
        {
            return new Point3d(
                barycentricCoords.X * aPoint +
                barycentricCoords.Y * bPoint +
                barycentricCoords.Z * cPoint
            );
        }
    }
}
