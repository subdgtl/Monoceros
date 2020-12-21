using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WFCTools {
    
    // TODO: Obsolete
    public class WFCMegamoduleSlicer : GH_Component {
        public WFCMegamoduleSlicer() : base("WFC Megamodule Slicer", "WFCSlicer",
            "Construct plane-aligned box cages for submodules of a specified megamodule." +
            "Only Breps can be filled.",
            "WaveFunctionCollapse", "Tools") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new WFCMegamoduleParameter(), "Megamodules", "MM", "Megamodules", GH_ParamAccess.list);
            pManager.AddVectorParameter(
                "Submodule Diagonal",
                "D",
                "Module diagonal vector specifying module dimension in base-plane-aligned XYZ axes",
                GH_ParamAccess.item,
                new Vector3d(10.0, 10.0, 10.0)
                );
            pManager.AddNumberParameter("Precision", "P", "Calculation precision. Higher = better & slower", GH_ParamAccess.item, 4.0);
            pManager.AddBooleanParameter("Fill", "F", "Fill solid BRep voids (slow)", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new WFCSlicedMegamoduleParameter(), "Sliced Megamodules", "SMM", "Megamodules sliced into submodules", GH_ParamAccess.list);
            pManager.AddPointParameter("External Anchors", "A", "Connector helper: Points on external faces of submodules", GH_ParamAccess.list);
            pManager.AddSurfaceParameter("External Faces", "F", "Connector helper: External faces of submodules", GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            List<WFCMegamodule> megamoduleInputs = new List<WFCMegamodule>();
            Vector3d submoduleDiagonal = new Vector3d();
            double precision = 1.0;
            bool fillVoids = false;

            if (!DA.GetDataList(0, megamoduleInputs)) return;
            if (!DA.GetData(1, ref submoduleDiagonal)) return;
            if (!DA.GetData(2, ref precision)) return;
            if (!DA.GetData(3, ref fillVoids)) return;

            List<string> justNames = megamoduleInputs.Select(mG => mG.Name).ToList(); ;
            if (justNames.Count != justNames.Distinct().Count()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Megamodule names are not unique.");
                return;
            }

            if (submoduleDiagonal.X <= 0.0 || submoduleDiagonal.Y <= 0.0 || submoduleDiagonal.Z <= 0.0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module dimensions in each direction must be greater than 0.0.");
                return;
            }
            if (precision <= 0.0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Precision must be greater than 0.0.");
                return;
            }

            double maxCageDimension = submoduleDiagonal.Length;
            double divisionLength = maxCageDimension / precision;

            IEnumerable<WFCSlicedMegamodule> megamodules = megamoduleInputs.Select((megamoduleInput, geometrySetIndex) => {
                WFCSlicedMegamodule megamodule = new WFCSlicedMegamodule {
                    WorldAlignedSimpleGeometry = megamoduleInput.SimpleGeometry,
                    WorldAlignedProductionGeometry = megamoduleInput.ProductionGeometry,
                    Name = megamoduleInput.Name,
                    WorldAlignedPivot = megamoduleInput.BasePlane,
                    Colour = megamoduleInput.Colour
                };

                List<BoundingBox> planeAlignedBoundingBoxes = megamodule.WorldAlignedSimpleGeometry.Select(goo => goo.GetBoundingBox(megamodule.WorldAlignedPivot)).ToList();
                BoundingBox planeAlignedUnionBox = new BoundingBox();
                foreach (BoundingBox bBox in planeAlignedBoundingBoxes) {
                    planeAlignedUnionBox.Union(bBox);
                }

                List<BoundingBox> planeAlignedModuleCages = WFCUtilities.SubdivideBoundingBox(submoduleDiagonal, planeAlignedUnionBox);
                List<bool> patternModuleCagesWithGeometry = Enumerable.Repeat(false, planeAlignedModuleCages.Count).ToList();
                List<bool> patternGeometryEntirelyInSingleModuleCage = Enumerable.Repeat(false, planeAlignedBoundingBoxes.Count).ToList();

                WFCUtilities.AreEntireGeometriesInsideModuleCages(
                    planeAlignedBoundingBoxes,
                    planeAlignedModuleCages,
                    ref patternGeometryEntirelyInSingleModuleCage,
                    ref patternModuleCagesWithGeometry
                    );

                // Only transform geometry that spans over more modules
                IEnumerable<GeometryBase> planeAlignedGeometry = megamodule.WorldAlignedSimpleGeometry
                    .Where((goo, i) => !patternGeometryEntirelyInSingleModuleCage[i])
                    .Select(goo => {
                        GeometryBase planeAlignedGoo = goo.Duplicate();
                        planeAlignedGoo.Transform(Transform.PlaneToPlane(megamodule.WorldAlignedPivot, Plane.WorldXY));
                        return planeAlignedGoo;
                    });

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
                for (int moduleCageI = 0; moduleCageI < planeAlignedModuleCages.Count; moduleCageI++) {
                    if (!patternModuleCagesWithGeometry[moduleCageI]) {
                        foreach (Point3d point in planeAlignedPopulatePoints) {
                            if (planeAlignedModuleCages[moduleCageI].Contains(point)) {
                                patternModuleCagesWithGeometry[moduleCageI] = true;
                                break;
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
                        planeAlignedModuleCages,
                        planeAlignedSolidBreps,
                        false,
                        ref patternModuleCagesWithGeometry
                        );
                }

                IEnumerable<BoundingBox> planeAlignedTightModuleCages = planeAlignedModuleCages
                    .Where((_, i) => patternModuleCagesWithGeometry[i]);

                IEnumerable<Point3d> worldAlignedFaceCenters = planeAlignedTightModuleCages.SelectMany(cage =>
                    cage.ToBrep().Faces.Select(face => face.PointAt(face.Domain(0).Mid, face.Domain(1).Mid)).Select(point => {
                        point.Transform(Transform.PlaneToPlane(Plane.WorldXY, megamodule.WorldAlignedPivot));
                        return point;
                    })
                );

                megamodule.Submodules = planeAlignedTightModuleCages
                    .Select((planeAlignedCage, i) => {
                        Submodule submodule = new Submodule {
                            Name = megamodule.Name + "_" + i,
                            PlaneAlignedBoundingBox = planeAlignedCage,
                            WorldAlignedBox = new Box(planeAlignedCage),
                            WorldAlignedPivot = megamodule.WorldAlignedPivot.Clone(),
                            SlicedMegamodule = megamodule
                        };
                        submodule.WorldAlignedBox.Transform(Transform.PlaneToPlane(Plane.WorldXY, megamodule.WorldAlignedPivot));
                        submodule.WorldAlignedPivot.Origin = submodule.WorldAlignedBox.Center;
                        submodule.WorldAlignedSubmoduleFaces = submodule.WorldAlignedBox.ToBrep().Faces.Select(bRepFace => {
                            Point3d brepFaceCenter = bRepFace.PointAt(bRepFace.Domain(0).Mid, bRepFace.Domain(1).Mid);
                            Vector3d worldAlignedDirectionVector = brepFaceCenter - submodule.WorldAlignedPivot.Origin;
                            SubmoduleFace submoduleFace = new SubmoduleFace {
                                Face = bRepFace,
                                Center = brepFaceCenter,
                                GridDirection = Direction.FromDirectionVector(worldAlignedDirectionVector, submodule.WorldAlignedPivot),
                                ParentSubmoduleName = submodule.Name
                            };
                            int valence = worldAlignedFaceCenters.Count(c => c.EpsilonEquals(submoduleFace.Center, Rhino.RhinoMath.SqrtEpsilon));
                            switch (valence) {
                                case 1:
                                    submoduleFace.GridRelation = Relation.ExternalFace;
                                    break;
                                case 2:
                                    submoduleFace.GridRelation = Relation.InternalFace;
                                    break;
                                default:
                                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to identify submodule face relation.");
                                    break;
                            }
                            return submoduleFace;
                        }).ToList();
                        return submodule;
                    }).ToList();
                return megamodule;
            });

            IEnumerable<Point3d> externalAnchors = megamodules.SelectMany(
                megamodule => megamodule.Submodules.SelectMany(
                    submodule => submodule.WorldAlignedSubmoduleFaces
                    .Where(
                        submoduleFace => submoduleFace.GridRelation == Relation.ExternalFace
                    ).Select(
                        submoduleFace => submoduleFace.Center
                        )
                    )
            );

            IEnumerable<NurbsSurface> externalFaces = megamodules.SelectMany(
                megamodule => megamodule.Submodules.SelectMany(
                    submodule => submodule.WorldAlignedSubmoduleFaces
                    .Where(
                        submoduleFace => submoduleFace.GridRelation == Relation.ExternalFace
                    ).Select(
                        submoduleFace => submoduleFace.Face.ToNurbsSurface()
                        )
                    )
            );

            DA.SetDataList(0, megamodules);
            DA.SetDataList(1, externalAnchors);
            DA.SetDataList(2, externalFaces);
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
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                WFCTools.Properties.Resources.S;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("fca388ce-e1e5-481f-a545-85bec35797fc");
    }
}