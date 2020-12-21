using Grasshopper.GUI;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WFCTools {

    // TODO: Obsolete
    public struct WFCConnectionOld {
        public char Axis;
        public string Lower;
        public string Higher;
    }

    // TODO: Obsolete

    public struct FaceConnection {
        public SubmoduleFace Lower;
        public SubmoduleFace Higher;
        public bool IsValid;

        public override bool Equals(object obj) {
            return obj is FaceConnection connection &&
                   EqualityComparer<SubmoduleFace>.Default.Equals(Lower, connection.Lower) &&
                   EqualityComparer<SubmoduleFace>.Default.Equals(Higher, connection.Higher) &&
                   IsValid == connection.IsValid;
        }

        public override int GetHashCode() {
            int hashCode = 1331502617;
            hashCode = hashCode * -1521134295 + Lower.GetHashCode();
            hashCode = hashCode * -1521134295 + Higher.GetHashCode();
            hashCode = hashCode * -1521134295 + IsValid.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(FaceConnection left, FaceConnection right) {
            return left.Equals(right);
        }

        public static bool operator !=(FaceConnection left, FaceConnection right) {
            return !(left == right);
        }
    }

    // TODO: Obsolete, replace with multiple various rule generators
    public class WFCComputeConnections : GH_Component {
        public WFCComputeConnections() : base("WFC Compute connections", "WFCConnectionOlds",
            "Compute allowed and disallowed connections",
            "WaveFunctionCollapse", "Tools") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(
                new WFCSlicedMegamoduleParameter(),
                "Sliced Megamodules",
                "SMM",
                "Megamodules sliced into submodules",
                GH_ParamAccess.list
                );
            pManager.AddCurveParameter(
                "Allowed Connections",
                "AC",
                "Curves connecting megamodule external faces marking allowed connection.",
                GH_ParamAccess.list
                );
            pManager.AddCurveParameter(
                "Disallowed Connections",
                "DC",
                "Curves connecting megamodule external faces marking disallowed connection. Applies last.",
                GH_ParamAccess.list
                );
            pManager.AddBooleanParameter(
                "Auto Indifferent",
                "I",
                "Automatically mark faces without explicit connections indifferent. " +
                "Indifferent face can connect to any other indifferent face of the opposite orientation.",
                GH_ParamAccess.item,
                true
                );
            pManager.AddPointParameter(
                "Allowed Indifferent Faces",
                "AI",
                "Points on megamodule external faces marking the face indifferent even though there is another explicit connection allowed.",
                GH_ParamAccess.list
                );
            pManager.AddPointParameter(
                "Disallowed Indifferent Faces",
                "DI",
                "Points on megamodule external faces marking the face not indifferent even though the face should be indifferent " +
                "(either has no explicit connection or is marked indifferent). " +
                "Applies after allowing indifferent connections.",
                GH_ParamAccess.list
                );
            pManager.AddBooleanParameter(
                "Allow Indifferent Empty Module",
                "E",
                "Allow existence of an empty module indifferent in all directions.",
                GH_ParamAccess.item,
                false
                );
            pManager.AddPointParameter(
                "Allowed Empty Neighbor",
                "AE",
                "Points on megamodule external faces marking the face to be potential neighbor with an empty module even though " +
                "there is another explicit connection allowed.",
                GH_ParamAccess.list
                );
            pManager.AddPointParameter(
                "Disallowed Empty Neighbor",
                "DE",
                "Points on megamodule external faces marking the face disallowed to have an empty module as a neighbor even though " +
                "it should be possible " +
                "(either has no explicit connection and therefore is indifferent just like the empty module " +
                "or is marked to be a potential neighbor with an empty module). " +
                "Applies after allowing indifferent connections and connections with empty.",
                GH_ParamAccess.list
                );
            pManager.AddPointParameter(
                "Allowed Outer Neighbor",
                "AO",
                "Points on megamodule external faces marking the face to be potential neighbor with an outer module even though " +
                "there is another explicit connection allowed.",
                GH_ParamAccess.list
                );
            pManager.AddPointParameter(
                "Disallowed Outer Neighbor",
                "DO",
                "Points on megamodule external faces marking the face disallowed to have an outer module as a neighbor even though " +
                "it should be possible " +
                "(either has no explicit connection and therefore is indifferent just like hte outer module " +
                "or is marked to be a potential neighbor with an outer module). " +
                "Applies after allowing indifferent connections and connections with outer.",
                GH_ParamAccess.list
                );
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[7].Optional = true;
            pManager[8].Optional = true;
            pManager[9].Optional = true;
            pManager[10].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddTextParameter("Ruleset", "R", "Ruluest in CSV strings", GH_ParamAccess.list);
            pManager.AddTextParameter("Module Names", "N", "All module names (except '" + WFCUtilities.EMPTY_MODULE_NAME + "' and '" + WFCUtilities.OUTER_MODULE_NAME + "')", GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            List<WFCSlicedMegamodule> megamodules = new List<WFCSlicedMegamodule>();
            List<Curve> allowedConnectors = new List<Curve>();
            List<Curve> disallowedConnectors = new List<Curve>();
            bool autoIndifferent = false;
            List<Point3d> allowedIndifferent = new List<Point3d>();
            List<Point3d> disallowedIndifferent = new List<Point3d>();
            bool allowIndifferentEmpty = false;
            List<Point3d> allowedEmpty = new List<Point3d>();
            List<Point3d> disallowedEmpty = new List<Point3d>();
            List<Point3d> allowedOuter = new List<Point3d>();
            List<Point3d> disallowedOuter = new List<Point3d>();


            if (!DA.GetDataList(0, megamodules)) return;
            DA.GetDataList(1, allowedConnectors);
            DA.GetDataList(2, disallowedConnectors);
            if (!DA.GetData(3, ref autoIndifferent)) return;
            DA.GetDataList(4, allowedIndifferent);
            DA.GetDataList(5, disallowedIndifferent);
            if (!DA.GetData(6, ref allowIndifferentEmpty)) return;
            DA.GetDataList(7, allowedEmpty);
            DA.GetDataList(8, disallowedEmpty);
            DA.GetDataList(9, allowedOuter);
            DA.GetDataList(10, disallowedOuter);

            IEnumerable<SubmoduleFace> externalFaces = megamodules.SelectMany(
                megamodule => megamodule.Submodules.SelectMany(
                    submodule => submodule.WorldAlignedSubmoduleFaces.Where(
                        face => face.GridRelation == Relation.ExternalFace
                    )
                )
            );

            IEnumerable<SubmoduleFace> emptyFaces = GenerateFauxMegamodule(WFCUtilities.EMPTY_MODULE_NAME).Submodules.SelectMany(submodule => submodule.WorldAlignedSubmoduleFaces);
            IEnumerable<SubmoduleFace> outerFaces = GenerateFauxMegamodule(WFCUtilities.OUTER_MODULE_NAME).Submodules.SelectMany(submodule => submodule.WorldAlignedSubmoduleFaces);

            IEnumerable<IEnumerable<SubmoduleFace>> internalFacesPerMegamodule = megamodules.Select(
                megamodule => megamodule.Submodules.SelectMany(
                    submodule => submodule.WorldAlignedSubmoduleFaces.Where(
                        face => face.GridRelation == Relation.InternalFace
                    )
                )
            );

            IEnumerable<FaceConnection> internalConnections = internalFacesPerMegamodule.SelectMany(
                megamoduleFaces => megamoduleFaces.SelectMany(
                    firstFace =>
                        megamoduleFaces.Where(
                            otherFace => otherFace.GridDirection.IsOpposite(firstFace.GridDirection)
                        ).Select(
                            secondFace => {
                                if (firstFace.GridDirection.Orientation == Orientation.Positive) {
                                    return new FaceConnection {
                                        Lower = firstFace,
                                        Higher = secondFace,
                                        IsValid = true
                                    };
                                } else {
                                    return new FaceConnection {
                                        Lower = secondFace,
                                        Higher = firstFace,
                                        IsValid = true
                                    };
                                }
                            })
                )
            );

            IEnumerable<FaceConnection> explicitAllowedConnections =
                allowedConnectors.SelectMany(
                    connector => ExtractFaceConnectionsFromConnector(connector, externalFaces)
            );
            IEnumerable<FaceConnection> explicitDisallowedConnections =
                disallowedConnectors.SelectMany(
                    connector => ExtractFaceConnectionsFromConnector(connector, externalFaces)
            );

            IEnumerable<FaceConnection> explicitConnections = explicitAllowedConnections.Where(
                connection => !explicitDisallowedConnections.Any(disallowedConnection => disallowedConnection == connection)
            );

            IEnumerable<SubmoduleFace> indiffectentFaces = autoIndifferent
                ? externalFaces.Where(
                    face => !(
                        explicitAllowedConnections.Any(connection => connection.Lower == face)
                        || explicitAllowedConnections.Any(connection => connection.Higher == face)
                    )
                )
                : Enumerable.Empty<SubmoduleFace>();

            IEnumerable<SubmoduleFace> allowedIndifferentFaces = allowedIndifferent.SelectMany(
                tag => ExtractFacesFromTag(tag, externalFaces)
            );
            IEnumerable<SubmoduleFace> disallowedIndifferentFaces = disallowedIndifferent.SelectMany(
                tag => ExtractFacesFromTag(tag, externalFaces)
            );

            IEnumerable<SubmoduleFace> cleanIndifferentFaces = indiffectentFaces
                .Concat(allowedIndifferentFaces)
                .Concat(allowIndifferentEmpty ? emptyFaces : Enumerable.Empty<SubmoduleFace>())
                .Concat(outerFaces)
                .Where(
                    face => !disallowedIndifferentFaces.Any(otherFace => face == otherFace)
                );


            IEnumerable<SubmoduleFace> xPositiveIndifferentFaces = cleanIndifferentFaces.Where(
                face => face.GridDirection.Axis == Axis.X
                && face.GridDirection.Orientation == Orientation.Positive
            );
            IEnumerable<SubmoduleFace> xNegativeIndifferentFaces = cleanIndifferentFaces.Where(
                face => face.GridDirection.Axis == Axis.X
                && face.GridDirection.Orientation == Orientation.Negative
            );
            IEnumerable<SubmoduleFace> yPositiveIndifferentFaces = cleanIndifferentFaces.Where(
                face => face.GridDirection.Axis == Axis.Y
                && face.GridDirection.Orientation == Orientation.Positive
            );
            IEnumerable<SubmoduleFace> yNegativeIndifferentFaces = cleanIndifferentFaces.Where(
                face => face.GridDirection.Axis == Axis.Y
                && face.GridDirection.Orientation == Orientation.Negative
            );
            IEnumerable<SubmoduleFace> zPositiveIndifferentFaces = cleanIndifferentFaces.Where(
                face => face.GridDirection.Axis == Axis.Z
                && face.GridDirection.Orientation == Orientation.Positive
            );
            IEnumerable<SubmoduleFace> zNegativeIndifferentFaces = cleanIndifferentFaces.Where(
                face => face.GridDirection.Axis == Axis.Z
                && face.GridDirection.Orientation == Orientation.Negative
            );

            IEnumerable<FaceConnection> indifferentConnections = cleanIndifferentFaces.SelectMany(
                firstFace => {
                    switch (firstFace.GridDirection.Axis) {
                        case Axis.X when firstFace.GridDirection.Orientation == Orientation.Positive: {
                                return xNegativeIndifferentFaces.Select(
                                    secondFace => new FaceConnection {
                                        Lower = firstFace,
                                        Higher = secondFace,
                                        IsValid = true
                                    }
                                );
                            }

                        case Axis.X when firstFace.GridDirection.Orientation == Orientation.Negative: {
                                return xPositiveIndifferentFaces.Select(
                                    secondFace => new FaceConnection {
                                        Lower = secondFace,
                                        Higher = firstFace,
                                        IsValid = true
                                    }
                                );
                            }

                        case Axis.Y when firstFace.GridDirection.Orientation == Orientation.Positive: {
                                return yNegativeIndifferentFaces.Select(
                                    secondFace => new FaceConnection {
                                        Lower = firstFace,
                                        Higher = secondFace,
                                        IsValid = true
                                    }
                                );
                            }

                        case Axis.Y when firstFace.GridDirection.Orientation == Orientation.Negative: {
                                return yPositiveIndifferentFaces.Select(
                                    secondFace => new FaceConnection {
                                        Lower = secondFace,
                                        Higher = firstFace,
                                        IsValid = true
                                    }
                                );
                            }

                        case Axis.Z when firstFace.GridDirection.Orientation == Orientation.Positive: {
                                return zNegativeIndifferentFaces.Select(
                                    secondFace => new FaceConnection {
                                        Lower = firstFace,
                                        Higher = secondFace,
                                        IsValid = true
                                    }
                                );
                            }

                        case Axis.Z when firstFace.GridDirection.Orientation == Orientation.Negative: {
                                return zPositiveIndifferentFaces.Select(
                                    secondFace => new FaceConnection {
                                        Lower = secondFace,
                                        Higher = firstFace,
                                        IsValid = true
                                    }
                                );
                            }
                        default:
                            // Never
                            return Enumerable.Empty<FaceConnection>();
                    }
                }
            );

            IEnumerable<SubmoduleFace> allowedEmptyFaces = allowedEmpty.SelectMany(
                tag => ExtractFacesFromTag(tag, externalFaces)
            );
            IEnumerable<SubmoduleFace> disallowedEmptyFaces = disallowedEmpty.SelectMany(
                tag => ExtractFacesFromTag(tag, externalFaces)
            );

            IEnumerable<SubmoduleFace> allowedOuterFaces = allowedOuter.SelectMany(
                tag => ExtractFacesFromTag(tag, externalFaces)
            );
            IEnumerable<SubmoduleFace> disallowedOuterFaces = disallowedOuter.SelectMany(
                tag => ExtractFacesFromTag(tag, externalFaces)
            );

            IEnumerable<FaceConnection> connections = explicitConnections
                .Concat(indifferentConnections)
                .Concat(allowedEmptyFaces.Select(
                    face => {
                        if (face.GridDirection.Orientation == Orientation.Positive) {
                            return new FaceConnection() {
                                Lower = face,
                                Higher = emptyFaces.First(empty => empty.GridDirection.IsOpposite(face.GridDirection)),
                                IsValid = true
                            };
                        } else {
                            return new FaceConnection() {
                                Lower = emptyFaces.First(empty => empty.GridDirection.IsOpposite(face.GridDirection)),
                                Higher = face,
                                IsValid = true
                            };
                        }
                    }
                ))
                .Where(
                    connection =>
                        !(
                        emptyFaces.Any(empty => empty == connection.Lower || empty == connection.Higher)
                        && disallowedEmptyFaces.Any(disallowed => disallowed == connection.Lower || disallowed == connection.Higher)
                        )
                )
                .Concat(allowedOuterFaces.Select(
                    face => {
                        if (face.GridDirection.Orientation == Orientation.Positive) {
                            return new FaceConnection() {
                                Lower = face,
                                Higher = outerFaces.First(outer => outer.GridDirection.IsOpposite(face.GridDirection)),
                                IsValid = true
                            };
                        } else {
                            return new FaceConnection() {
                                Lower = outerFaces.First(outer => outer.GridDirection.IsOpposite(face.GridDirection)),
                                Higher = face,
                                IsValid = true
                            };
                        }
                    }
                ))
                .Where(
                    connection =>
                        !(
                        outerFaces.Any(outer => outer == connection.Lower || outer == connection.Higher)
                        && disallowedOuterFaces.Any(disallowed => disallowed == connection.Lower || disallowed == connection.Higher)
                        )
                )
                .Concat(internalConnections)
                .Distinct();

            // TODO make a proper GH Parameter with a ToString implementation.
            IEnumerable<WFCConnectionOld> wfcConnections = connections.Select(
                connection => new WFCConnectionOld() {
                    Axis = AxisToChar(connection.Lower.GridDirection.Axis),
                    Lower = connection.Lower.ParentSubmoduleName,
                    Higher = connection.Higher.ParentSubmoduleName,
                }
            );

            IEnumerable<string> ruleset = wfcConnections.Select(connection => "" + connection.Axis + ": " + connection.Lower + " -> " + connection.Higher);

            IEnumerable<string> allModuleNames = connections.SelectMany(
                connection => new List<string>() {
                    connection.Lower.ParentSubmoduleName,
                    connection.Higher.ParentSubmoduleName
                })
                .Distinct()
                .Where(name => name != WFCUtilities.EMPTY_MODULE_NAME && name != WFCUtilities.OUTER_MODULE_NAME);

            DA.SetDataList(0, ruleset);
            DA.SetDataList(1, allModuleNames);
        }

        private WFCSlicedMegamodule GenerateFauxMegamodule(string name) {
            Box box = new Box(Plane.WorldXY, new Interval(-0.5, 0.5), new Interval(-0.5, 0.5), new Interval(-0.5, 0.5));
            Plane pivot = Plane.WorldXY;
            return new WFCSlicedMegamodule() {
                WorldAlignedSimpleGeometry = new List<GeometryBase>(),
                WorldAlignedProductionGeometry = new List<GeometryBase>(),
                Name = name + " megamodule",
                WorldAlignedPivot = pivot,
                Colour = Color.Black,
                Submodules = new List<Submodule>() {
                    new Submodule() {
                        Name = name,
                        PlaneAlignedBoundingBox = box.BoundingBox,
                        WorldAlignedBox = box,
                        WorldAlignedPivot = pivot,
                        WorldAlignedSubmoduleFaces = box.ToBrep().Faces.Select(
                            face => new SubmoduleFace() {
                                        Center = new Point3d(),
                                        Face = face,
                                        GridDirection = Direction.FromDirectionVector(
                                            face.PointAt(face.Domain(0).Mid, face.Domain(1).Mid) - pivot.Origin, pivot
                                            ),
                                        ParentSubmoduleName = name
                                }
                                ).ToList()
                    }
                }
            };
        }

        private static char AxisToChar(Axis a) {
            switch (a) {
                case Axis.X:
                    return 'x';
                case Axis.Y:
                    return 'y';
                case Axis.Z:
                    return 'z';
                default:
                    return '0';
            }
        }

        private static IEnumerable<FaceConnection> ExtractFaceConnectionsFromConnector(Curve connector, IEnumerable<SubmoduleFace> faces) {
            IEnumerable<SubmoduleFace> touchedFaces = faces.Where(
                                face => face.Face.PullPointsToFace(new[] { connector.PointAtStart }, Rhino.RhinoMath.SqrtEpsilon)[0]
                                .EpsilonEquals(connector.PointAtStart, Rhino.RhinoMath.SqrtEpsilon)
                                || face.Face.PullPointsToFace(new[] { connector.PointAtEnd }, Rhino.RhinoMath.SqrtEpsilon)[0]
                                .EpsilonEquals(connector.PointAtEnd, Rhino.RhinoMath.SqrtEpsilon)
                                );
            return touchedFaces.SelectMany(
                firstFace => touchedFaces.Select(secondFace => {
                    if (firstFace.GridDirection.IsOpposite(secondFace.GridDirection)) {
                        if (firstFace.GridDirection.Orientation == Orientation.Positive) {
                            return new FaceConnection {
                                Lower = firstFace,
                                Higher = secondFace,
                                IsValid = true
                            };
                        } else {
                            return new FaceConnection {
                                Lower = secondFace,
                                Higher = firstFace,
                                IsValid = true
                            };
                        }
                    } else {
                        return new FaceConnection {
                            Lower = firstFace,
                            Higher = secondFace,
                            IsValid = false
                        };
                    }
                })
             ).Where(connection => connection.IsValid);
        }

        private static IEnumerable<SubmoduleFace> ExtractFacesFromTag(Point3d tag, IEnumerable<SubmoduleFace> faces) {
            return faces.Where(
                face => face.Face.PullPointsToFace(new[] { tag }, Rhino.RhinoMath.SqrtEpsilon)[0]
                .EpsilonEquals(tag, Rhino.RhinoMath.SqrtEpsilon)
            );
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
                WFCTools.Properties.Resources.C;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("A7746094-86B3-4CA7-AEB8-32CA3E73766F");
    }
}