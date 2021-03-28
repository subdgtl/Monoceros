using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace Monoceros {
    public class ComponentTrimTest : GH_Component {
        public ComponentTrimTest( ) : base("Trim Test", "TrimTest", "{1,1,1}", "Monoceros", "Test") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddGeometryParameter("Geometry",
                                          "G",
                                          "Geometry to be converted into Modules and Slots. (Point, Curve, Brep)",
                                          GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddGeometryParameter("Geometry",
                                         "G",
                                         "G",
                                         GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var geometryRaw = new List<IGH_GeometricGoo>();

            if (!DA.GetDataList(0, geometryRaw)) {
                return;
            }

            // Mesh geometry is not supported because the Rhino Mesh library
            // lacks necessary features and shows unexpected (undocumented) behavior:
            // - Mesh split does not work all the time
            // - Mesh split causes excessive and unnecessary triangulation
            // - Mesh split adds unnecessary vertices onto straight edges, splitting them
            // - All above are inconsistent, causing different results where same results would be expected
            // - Mesh Reduce that should simplify the mesh by joining faces that could become one, is erroneous and unpredictable

            var geometryUnwrapped = geometryRaw
                            .Where(goo => goo != null)
                            .Select(ghGeo => {
                                var geo = ghGeo.Duplicate();
                                return GH_Convert.ToGeometryBase(geo);
                            });

            var geometryClean = new List<GeometryBase>();
            var foundUnsupportedGeometry = false;

            foreach (var geo in geometryUnwrapped) {
                if (geo.ObjectType == ObjectType.Point
                                || geo.ObjectType == ObjectType.Curve
                                || geo.HasBrepForm) {
                    geometryClean.Add(geo);
                } else {
                    foundUnsupportedGeometry = true;
                }
            }

            if (foundUnsupportedGeometry) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Found unsupported geometry. Currently supports only Points, Curves and Breps.");
            }

            if (!geometryClean.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Failed to collect any geometry.");
                return;
            }

            var unionBox = BoundingBox.Empty;
            foreach (var geometry in geometryClean) {
                unionBox.Union(geometry.GetBoundingBox(true));
            }

            var minX = Math.Floor(unionBox.Min.X) - 1;
            var maxX = Math.Ceiling(unionBox.Max.X) + 1;
            var minY = Math.Floor(unionBox.Min.Y) - 1;
            var maxY = Math.Ceiling(unionBox.Max.Y) + 1;
            var minZ = Math.Floor(unionBox.Min.Z) - 1;
            var maxZ = Math.Ceiling(unionBox.Max.Z) + 1;

            var toBeChopped = new List<GeometryBase>(geometryClean);
            var alreadyChopped = new List<GeometryBase>();

            for (var z = Math.Floor(unionBox.Min.Z); z < Math.Ceiling(unionBox.Max.Z); z++) {
                var plane = Plane.WorldXY;
                plane.Origin = new Point3d(0, 0, z);
                var surface = Brep.CreateFromCornerPoints(new Point3d(minX, minY, z),
                                                          new Point3d(maxX, minY, z),
                                                          new Point3d(maxX, maxY, z),
                                                          new Point3d(minX, maxY, z),
                                                          Config.EPSILON);
                chop(plane, surface, toBeChopped, ref alreadyChopped, out var nextChopped);
                toBeChopped = nextChopped;
            }

            toBeChopped = toBeChopped.Concat(alreadyChopped).ToList();
            alreadyChopped = new List<GeometryBase>();

            for (var y = Math.Floor(unionBox.Min.Y); y < Math.Ceiling(unionBox.Max.Y); y++) {
                var plane = Plane.WorldZX;
                plane.Origin = new Point3d(0, y, 0);
                var surface = Brep.CreateFromCornerPoints(new Point3d(minX, y, minZ),
                                                          new Point3d(maxX, y, minZ),
                                                          new Point3d(maxX, y, maxZ),
                                                          new Point3d(minX, y, maxZ),
                                                          Config.EPSILON);
                chop(plane, surface, toBeChopped, ref alreadyChopped, out var nextChopped);
                toBeChopped = nextChopped;
            }

            toBeChopped = toBeChopped.Concat(alreadyChopped).ToList();
            alreadyChopped = new List<GeometryBase>();

            for (var x = Math.Floor(unionBox.Min.X); x < Math.Ceiling(unionBox.Max.X); x++) {
                var plane = Plane.WorldYZ;
                plane.Origin = new Point3d(x, 0, 0);

                var surface = Brep.CreateFromCornerPoints(new Point3d(x, minY, minZ),
                                                          new Point3d(x, maxY, minZ),
                                                          new Point3d(x, maxY, maxZ),
                                                          new Point3d(x, minY, maxZ),
                                                          Config.EPSILON);
                chop(plane, surface, toBeChopped, ref alreadyChopped, out var nextChopped);
                toBeChopped = nextChopped;
            }

            var outputGeometry = toBeChopped.Concat(alreadyChopped);

            DA.SetDataList(0, outputGeometry);
        }

        private static void chop(Plane plane, Brep surface, IEnumerable<GeometryBase> toBeChopped, ref List<GeometryBase> alreadyChopped, out List<GeometryBase> nextChopped) {
            var pieces = new List<GeometryBase>();
            foreach (var geometry in toBeChopped) {
                if (geometry.HasBrepForm) {
                    var brep = (Brep)geometry;
                    var cutouts = brep.Split(surface, Config.EPSILON);
                    if (cutouts.Any()) {
                        pieces.AddRange(cutouts);
                    } else {
                        pieces.Add(brep);
                    }
                }
                if (geometry.ObjectType == ObjectType.Curve) {
                    var curve = (Curve)geometry;
                    var intersection = Intersection.CurvePlane(curve, plane, Config.EPSILON);
                    if (intersection != null) {
                        var splittingParams = intersection
                            .Where(intersect => intersect.IsPoint || intersect.IsOverlap)
                            .Select(intersect => intersect.ParameterA);
                        pieces.AddRange(curve.Split(splittingParams));
                    } else {
                        pieces.Add(curve);
                    }
                }
            }
            nextChopped = new List<GeometryBase>();
            foreach (var piece in pieces) {
                var meshBoundingBox = piece.GetBoundingBox(true);
                var distanceToPlane = plane.DistanceTo(meshBoundingBox.Center);
                if (distanceToPlane > 0) {
                    nextChopped.Add(piece);
                } else {
                    alreadyChopped.Add(piece);
                }
            }
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.module_construct;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("1D596AD4-904A-41E6-A1C4-B0663014D441");
    }
}
