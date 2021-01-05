﻿using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WFCPlugin
{
    public class ComponentPopulateGeometryWithSlotCenters : GH_Component
    {
        public ComponentPopulateGeometryWithSlotCenters()
            : base("WFC Populate Geometry With Slot Centers",
                   "WFCPopSlotCen",
                   "Populate geometry with points ready to be " +
                   "used as WFC Slot centers. Supports Point, Curve, " +
                   "Brep, Mesh. Prefer Mesh to BRep.",
                   "WaveFunctionCollapse",
                   "World")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
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
                                        "Module slicer precision (lower = more precise & slower)",
                                        GH_ParamAccess.item,
                                        0.5);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Slot Centers",
                                       "C",
                                       "Points ready to be used as WFC Slot centers",
                                       GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<IGH_GeometricGoo> geometryRaw = new List<IGH_GeometricGoo>();
            Plane basePlane = new Plane();
            Vector3d diagonal = new Vector3d();
            int method = 2;
            double precision = 0.5;

            if (!DA.GetDataList(0, geometryRaw))
            {
                return;
            }

            if (!DA.GetData(1, ref basePlane))
            {
                return;
            }

            if (!DA.GetData(2, ref diagonal))
            {
                return;
            }

            if (!DA.GetData(3, ref method))
            {
                return;
            }

            if (!DA.GetData(4, ref precision))
            {
                return;
            }


            if (diagonal.X <= 0 || diagonal.Y <= 0 || diagonal.Z <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "One or more slot dimensions are not larger than 0.");
                return;
            }

            IEnumerable<GeometryBase> geometryClean = geometryRaw
               .Where(goo => goo != null)
               .Select(goo =>
               {
                   IGH_Goo geo = goo.Duplicate();
                   GeometryBase geometry = GH_Convert.ToGeometryBase(geo);
                   // Transformation of BReps sometimes does not work as expected
                   // For example non uniform scaling of a sphere results in a sphere
                   // Mesh scaling is safe and populating is fast(er)
                   if (geometry.HasBrepForm)
                   {
                       MeshingParameters meshingParameters = MeshingParameters.FastRenderMesh;
                       Brep brep = Brep.TryConvertBrep(geometry);
                       Mesh[] meshes = Mesh.CreateFromBrep(brep, meshingParameters);
                       Mesh mesh = new Mesh();
                       foreach (Mesh meshFace in meshes)
                       {
                           mesh.Append(meshFace);
                       }
                       mesh.Weld(Math.PI / 8);
                       return mesh;
                   }
                   else
                   {
                       return geometry;
                   }
               }
               );

            // Scale down to unit size
            Transform normalizationTransform = Transform.Scale(basePlane,
                                                         1 / diagonal.X,
                                                         1 / diagonal.Y,
                                                         1 / diagonal.Z);
            // Orient to the world coordinate system
            Transform worldAlignmentTransform = Transform.PlaneToPlane(basePlane, Plane.WorldXY);
            List<Point3i> submoduleCentersNormalized = new List<Point3i>();
            IEnumerable<GeometryBase> geometryTransformed = geometryClean.Select(goo =>
            {
                GeometryBase geo = goo.Duplicate();
                geo.Transform(normalizationTransform);
                geo.Transform(worldAlignmentTransform);
                return geo;
            });

            foreach (GeometryBase goo in geometryClean)
            {
                GeometryBase geometry = goo.Duplicate();
                if (geometry.Transform(normalizationTransform) &&
                    geometry.Transform(worldAlignmentTransform))
                {
                    if (method == 0 || method == 2)
                    {
                        IEnumerable<Point3d> populatePoints = PopulateSurface(precision, geometry);
                        foreach (Point3d geometryPoint in populatePoints)
                        {
                            // Round point locations
                            // Slot dimension is for the sake of this calculation 1,1,1
                            Point3i slotCenterPoint = new Point3i(geometryPoint);
                            // Deduplicate
                            if (!submoduleCentersNormalized.Contains(slotCenterPoint))
                            {
                                submoduleCentersNormalized.Add(slotCenterPoint);
                            }
                        }
                    }
                    if ((method == 1 || method == 2) &&
                        goo.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
                    {
                        Mesh mesh = (Mesh)geometry;
                        List<Point3d> populatePoints = PopulateMeshVolume(precision, mesh);
                        foreach (Point3d geometryPoint in populatePoints)
                        {
                            // Round point locations
                            // Slot dimension is for the sake of this calculation 1,1,1
                            Point3i slotCenterPoint = new Point3i(geometryPoint);
                            // Deduplicate
                            if (!submoduleCentersNormalized.Contains(slotCenterPoint))
                            {
                                submoduleCentersNormalized.Add(slotCenterPoint);
                            }
                        }
                    }
                }
            }

            Transform baseAlignmentTransform = Transform.PlaneToPlane(Plane.WorldXY, basePlane);
            Transform scalingTransform = Transform.Scale(basePlane, diagonal.X, diagonal.Y, diagonal.Z);

            IEnumerable<Point3d> submoduleCenters = submoduleCentersNormalized
                .Select(centerNormalized =>
                {
                    Point3d center = centerNormalized.ToPoint3d();
                    center.Transform(baseAlignmentTransform);
                    center.Transform(scalingTransform);
                    return center;
                });

            DA.SetDataList(0, submoduleCenters);
        }

        /// <summary>
        /// Populates the surface of various geometries with points.
        /// </summary>
        /// <param name="distance">The distance.</param>
        /// <param name="goo">The goo.</param>
        /// <returns>A list of Point3ds.</returns>
        private static IEnumerable<Point3d> PopulateSurface(double distance, GeometryBase goo)
        {
            Rhino.DocObjects.ObjectType type = goo.ObjectType;
            switch (type)
            {
                case Rhino.DocObjects.ObjectType.Point:
                    Point point = (Point)goo;
                    return Enumerable.Repeat(point.Location, 1);
                case Rhino.DocObjects.ObjectType.Curve:
                    Curve curve = (Curve)goo;
                    return PopulateCurve(distance, curve);
                case Rhino.DocObjects.ObjectType.Mesh:
                    Mesh mesh = (Mesh)goo;
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
        private static IEnumerable<Point3d> PopulateMeshSurface(double distance, Mesh mesh)
        {
            return mesh.Faces.SelectMany(face =>
            {
                List<Point3d> firstTriangle = PopulateTriangle(distance,
                                                     mesh.Vertices[face.A],
                                                     mesh.Vertices[face.B],
                                                     mesh.Vertices[face.C]);
                if (!face.IsTriangle)
                {
                    return firstTriangle;

                }
                else
                {
                    List<Point3d> secondTriangle = PopulateTriangle(distance,
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
        private static IEnumerable<Point3d> PopulateCurve(double distance, Curve curve)
        {
            // Curve division calculation is bearably fast, therefore it can be more precise
            double preciseDistance = distance * 0.25;
            double[] divisionPoints = curve.DivideByLength(preciseDistance, true);
            if (divisionPoints != null)
            {
                return divisionPoints.Select(t => curve.PointAt(t));
            }
            else
            {
                return new List<Point3d>() { curve.PointAtStart, curve.PointAtEnd };
            }
        }

        /// <summary>
        /// Populates the mesh volume.
        /// </summary>
        /// <param name="step">The distance - precision.</param>
        /// <param name="mesh">The mesh.</param>
        /// <returns>A list of Point3ds.</returns>
        private static List<Point3d> PopulateMeshVolume(double step, Mesh mesh)
        {
            List<Point3d> pointsInsideMesh = new List<Point3d>();
            BoundingBox boundingBox = mesh.GetBoundingBox(false);
            for (double z = boundingBox.Min.Z - step; z < boundingBox.Max.Z + step; z += step)
            {
                for (double y = boundingBox.Min.Y - step; y < boundingBox.Max.Y + step; y += step)
                {
                    for (double x = boundingBox.Min.X - step; x < boundingBox.Max.X + step; x += step)
                    {
                        Point3d testPoint = new Point3d(x, y, z);
                        if (mesh.IsPointInside(testPoint, Rhino.RhinoMath.SqrtEpsilon, false))
                        {
                            pointsInsideMesh.Add(testPoint);
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
                                                      Point3d cPoint)
        {
            // Compute the density of points on the respective face.
            double abDistanceSq = aPoint.DistanceToSquared(bPoint);
            double bcDistanceSq = bPoint.DistanceToSquared(cPoint);
            double caDistanceSq = cPoint.DistanceToSquared(aPoint);
            double longestEdgeLen = Math.Sqrt(
                Math.Max(abDistanceSq,
                         Math.Max(bcDistanceSq, caDistanceSq))
                );

            // Number of face divisions (points) in each direction.
            double divisions = Math.Ceiling(longestEdgeLen / distance);

            List<Point3d> points = new List<Point3d>(Convert.ToInt32(Math.Pow((divisions + 1), 2)));

            for (int ui = 0; ui < divisions; ui++)
            {
                for (int wi = 0; wi < divisions; wi++)
                {
                    double uNormalized = ui / divisions;
                    double wNormalized = wi / divisions;
                    double vNormalized = 1.0 - uNormalized - wNormalized;
                    if (vNormalized >= 0.0)
                    {
                        Point3d barycentric =
                            new Point3d(uNormalized, vNormalized, wNormalized);
                        Point3d cartesian = BarycentricToCartesian(barycentric,
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
        )
        {
            return new Point3d(
                barycentricCoords.X * aPoint +
                barycentricCoords.Y * bPoint +
                barycentricCoords.Z * cPoint
            );
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
