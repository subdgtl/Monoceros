﻿using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Monoceros {
    public class ComponentRuleGuesser : GH_Component {
        public ComponentRuleGuesser( )
            : base("Suggest Rules from Geometry",
                   "RuleSuggest",
                   "Suggest Rules based on naked geometry at connectors. " +
                   "The two Modules described by the generated Rule can " +
                   "be joined or welded. Supports Curves, BReps and Meshes.",
                   "Monoceros",
                   "Rule") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleParameter(),
                                  "Modules",
                                  "M",
                                  "All available Monoceros Modules",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "Suggested Monoceros Rules",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules Indifferent",
                                  "RI",
                                  "Suggested Indifferent Monoceros Rules",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var modules = new List<Module>();

            if (!DA.GetDataList(0, modules)) {
                return;
            }

            var rules = new List<Rule>();
            var rulesIndifferent = new List<Rule>();

            var connectorGeometries = new List<ConnectorGeometry>();

            foreach (var module in modules) {
                var precision = module.PartDiagonal.Length / 100;
                if (module == null || !module.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The Module is null or invalid.");
                    continue;
                }

                var allGeometry = module.Geometry.Concat(module.ReferencedGeometry);

                var nakedPolylinesBrep = allGeometry
                    .Where(geo => geo.HasBrepForm)
                    .SelectMany(geo => Brep.TryConvertBrep(geo).DuplicateNakedEdgeCurves(true, false))
                    .Select(curve => curve.ToPolyline(0, 0, 0.05, 0.1, 0, precision, precision * 2, 0, true).ToPolyline());
                var nakedPolylinesMesh = allGeometry
                    .Where(geo => geo.ObjectType == ObjectType.Mesh)
                    .SelectMany(geo => ((Mesh)geo).GetNakedEdges() ?? new Polyline[0]);
                var curveEndPoints = allGeometry
                    .Where(geo => geo.ObjectType == ObjectType.Curve)
                    .Select(geo => (Curve)geo)
                    .Where(curve => !curve.IsPeriodic && !curve.IsClosed)
                    .SelectMany(curve => new Point3d[] { curve.PointAtEnd, curve.PointAtStart });

                for (var connectorIndex = 0; connectorIndex < module.Connectors.Count; connectorIndex++) {
                    var connector = module.Connectors[connectorIndex];
                    var transform = Transform.PlaneToPlane(connector.AnchorPlane, Plane.WorldXY);

                    ConnectorGeometry connectorGeometry;
                    connectorGeometry.ModuleName = module.Name;
                    connectorGeometry.ConnectorIndex = (uint)connectorIndex;
                    connectorGeometry.ConnectorDirection = connector.Direction;
                    connectorGeometry.BrepNakedEdgePoints = new List<Point3d>();
                    connectorGeometry.MeshNakedEdgePoints = new List<Point3d>();
                    connectorGeometry.CurveEndPoints = new List<Point3d>();

                        connectorGeometry.BrepNakedEdgePoints = nakedPolylinesBrep
                            .Where(polyline => polyline.All(point => connector.ContainsPoint(point)))
                            .SelectMany(polyline => polyline.Distinct().ToArray())
                            .Select(point => {
                                var transformedPoint = point;
                                transformedPoint.Transform(transform);
                                var roundedPoint = new Point3d(Math.Round(transformedPoint.X, Config.DECIMAL_PRECISION),
                                                               Math.Round(transformedPoint.Y, Config.DECIMAL_PRECISION),
                                                               Math.Round(transformedPoint.Z, Config.DECIMAL_PRECISION));
                                return roundedPoint;
                            }).ToList();
                        connectorGeometry.BrepNakedEdgePoints.Sort();

                        connectorGeometry.MeshNakedEdgePoints = nakedPolylinesMesh
                        .Where(polyline => polyline.All(point => connector.ContainsPoint(point)))
                        .SelectMany(polyline => polyline.Distinct())
                        .Select(point => {
                            var transformedPoint = point;
                            transformedPoint.Transform(transform);
                            var roundedPoint = new Point3d(Math.Round(transformedPoint.X, Config.DECIMAL_PRECISION),
                                                           Math.Round(transformedPoint.Y, Config.DECIMAL_PRECISION),
                                                           Math.Round(transformedPoint.Z, Config.DECIMAL_PRECISION));
                            return roundedPoint;
                        }).ToList();
                        connectorGeometry.MeshNakedEdgePoints.Sort();

                        connectorGeometry.CurveEndPoints = curveEndPoints
                        .Where(point => connector.ContainsPoint(point))
                        .Select(point => {
                            var transformedPoint = point;
                            transformedPoint.Transform(transform);
                            var roundedPoint = new Point3d(Math.Round(transformedPoint.X, Config.DECIMAL_PRECISION),
                                                           Math.Round(transformedPoint.Y, Config.DECIMAL_PRECISION),
                                                           Math.Round(transformedPoint.Z, Config.DECIMAL_PRECISION));
                            return roundedPoint;
                        }).ToList();
                        connectorGeometry.CurveEndPoints.Sort();

                    if (connectorGeometry.BrepNakedEdgePoints.Any()
                        || connectorGeometry.MeshNakedEdgePoints.Any()
                        || connectorGeometry.CurveEndPoints.Any()) {
                        connectorGeometries.Add(connectorGeometry);
                    } else {
                        rulesIndifferent.Add(new Rule(module.Name, (uint)connectorIndex, Config.INDIFFERENT_TAG));
                    }
                }
            }

            foreach (var current in connectorGeometries) {
                if (current.ConnectorDirection.Orientation == Orientation.Positive) {
                    foreach (var other in connectorGeometries) {
                        if (other.ConnectorDirection.IsOpposite(current.ConnectorDirection)
                            && ArePointListsEpsilonEqual(current.BrepNakedEdgePoints, other.BrepNakedEdgePoints)
                            && ArePointListsEpsilonEqual(current.MeshNakedEdgePoints, other.MeshNakedEdgePoints)
                            && ArePointListsEpsilonEqual(current.CurveEndPoints, other.CurveEndPoints)) {
                            rules.Add(new Rule(current.ModuleName,
                                               current.ConnectorIndex,
                                               other.ModuleName,
                                               other.ConnectorIndex));
                        }
                    }
                }
            }

            if (!rules.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                                  "Could not identify any matching naked geometry.");
            }

            foreach (var rule in rules) {
                if (!rule.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, rule.IsValidWhyNot);
                }
            }

            var outRules = rules.Distinct().ToList();
            outRules.Sort();
            var outIndifferentRules = rulesIndifferent.Distinct().ToList();
            outIndifferentRules.Sort();
            DA.SetDataList(0, outRules);
            DA.SetDataList(1, outIndifferentRules);
        }

        private static bool ArePointListsEpsilonEqual(List<Point3d> a, List<Point3d> b) {
            if (a.Count != b.Count) {
                return false;
            }
            for (var i = 0; i < a.Count; i++) {
                var pointCurrent = a[i];
                var pointOther = b[i];
                if (!pointCurrent.EpsilonEquals(pointOther, Config.EPSILON)) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to
        /// septenary), each of which can be combined with the
        /// GH_Exposure.obscure flag, which ensures the component will only be
        /// visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.quinary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.rule_suggest;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("2E45553C-62DE-4649-9D6B-F3F9A148A10E");
    }

    internal struct ConnectorGeometry {
        public string ModuleName;
        public uint ConnectorIndex;
        public Direction ConnectorDirection;
        public List<Point3d> BrepNakedEdgePoints;
        public List<Point3d> MeshNakedEdgePoints;
        public List<Point3d> CurveEndPoints;
    }

}
