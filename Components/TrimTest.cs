using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using Rhino.Geometry;

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
                                          "G",
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

            var geometryClean = geometryRaw
                .Where(goo => goo != null)
                .Select(ghGeo => {
                    var geo = ghGeo.Duplicate();
                    return GH_Convert.ToGeometryBase(geo);
                }).ToList();

            if (!geometryClean.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Failed to collect any geometry.");
                return;
            }

            var geometryBoxes = geometryClean.Select(geometry => geometry.GetBoundingBox(true)).ToList();

            var unionBox = BoundingBox.Empty;
            foreach (var geometryBox in geometryBoxes) {
                unionBox.Union(geometryBox);
            }

            var outputGeometry = new List<GeometryBase>();
            for (var z = Math.Floor(unionBox.Min.Z); z < Math.Ceiling(unionBox.Max.Z); z++) {
                for (var y = Math.Floor(unionBox.Min.Y); y < Math.Ceiling(unionBox.Max.Y); y++) {
                    for (var x = Math.Floor(unionBox.Min.X); x < Math.Ceiling(unionBox.Max.X); x++) {
                        var boundingBox = new BoundingBox(x, y, z, x + 1, y + 1, z + 1);
                        var boxBrep = boundingBox.ToBrep();
                        var meshBox = Mesh.CreateFromBox(boundingBox, 1, 1, 1);
                        for (var i = 0; i < geometryClean.Count; i++) {
                            var geometry = geometryClean[i];
                            var geometryBox = geometryBoxes[i];
                            if (boundingBox.Contains(geometryBox)) {
                                outputGeometry.Add(geometry);
                                continue;
                            }
                            if (geometry.HasBrepForm) {
                                var brep = (Brep)geometry;
                                var pieces = brep.Trim(boxBrep, Config.EPSILON);
                                outputGeometry.AddRange(pieces);
                            }
                            if (geometry.ObjectType == ObjectType.Mesh) {
                                var mesh = (Mesh)geometry;
                                var pieces = mesh.Split(meshBox);
                                foreach (var piece in pieces) {
                                    var currentMeshBox = piece.GetBoundingBox(true);
                                    if (boundingBox.Contains(currentMeshBox.Center)) {
                                        outputGeometry.Add(piece);
                                    }
                                }
                            }
                            if (geometry.ObjectType == ObjectType.Curve) {
                                var curve = (Curve)geometry;
                                var pieces = curve.Split(boxBrep, Config.EPSILON * 1000, Config.EPSILON * 1000);
                                foreach (var piece in pieces) {
                                    var curveBox = piece.GetBoundingBox(true);
                                    if (boundingBox.Contains(curveBox.Center)) {
                                        outputGeometry.Add(piece);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            DA.SetDataList(0, outputGeometry);
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
