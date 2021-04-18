using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Monoceros {
    public class ComponentRuleGuesser : GH_Component, IGH_PreviewObject {
        private List<Point3d> _points;
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

            var moduleDiagonal = modules.First().PartDiagonal;


            var modulesClean = new List<Module>();
            foreach (var module in modules) {
                if (module == null || !module.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module is null or invalid.");
                    return;
                }
                modulesClean.Add(module);
            }

            if (modulesClean.Any(module => module.PartDiagonal != moduleDiagonal)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Modules are not defined with the same diagonal.");
                return;
            }

            _points = new List<Point3d>();

            var rules = new List<Rule>();
            var rulesIndifferent = new List<Rule>();

            var connectorGeometries = new List<ConnectorGeometry>();

            var precision = moduleDiagonal.Length / 1000;

            foreach (var module in modulesClean) {
                if (module == null || !module.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The Module is null or invalid.");
                    continue;
                }

                var allGeometry = module.Geometry.Concat(module.ReferencedGeometry);

                var nakedPolylinesBrep = allGeometry
                    .Where(geo => geo.HasBrepForm)
                    .SelectMany(geo => Brep.TryConvertBrep(geo).DuplicateNakedEdgeCurves(true, false))
                    .Select(curve => curve.ToPolyline(0, 0, 0.05, 0.1, 0, precision * 10, precision * 20, 0, true).ToPolyline());
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

                    var brepPointsOriginal = nakedPolylinesBrep
                                            .Where(polyline => polyline.All(point => connector.ContainsPoint(point, precision)))
                                            .SelectMany(polyline => polyline.Distinct());
                    _points.AddRange(brepPointsOriginal);
                    connectorGeometry.BrepNakedEdgePoints = brepPointsOriginal
                        .Select(point => {
                            var transformedPoint = point;
                            transformedPoint.Transform(transform);
                            return transformedPoint;
                        }).ToList();
                    connectorGeometry.BrepNakedEdgePoints.Sort();

                    var meshPointsOriginal = nakedPolylinesMesh
                                        .Where(polyline => polyline.All(point => connector.ContainsPoint(point, precision)))
                                        .SelectMany(polyline => polyline.Distinct());
                    _points.AddRange(meshPointsOriginal);
                    connectorGeometry.MeshNakedEdgePoints = meshPointsOriginal
                    .Select(point => {
                        var transformedPoint = point;
                        transformedPoint.Transform(transform);
                        return transformedPoint;
                    }).ToList();
                    connectorGeometry.MeshNakedEdgePoints.Sort();

                    var curvePointsOriginal = curveEndPoints
                                        .Where(point => connector.ContainsPoint(point, precision));
                    _points.AddRange(curvePointsOriginal);
                    connectorGeometry.CurveEndPoints = curvePointsOriginal
                    .Select(point => {
                        var transformedPoint = point;
                        transformedPoint.Transform(transform);
                        return transformedPoint;
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
                            && ArePointListsEpsilonEqual(current.BrepNakedEdgePoints, other.BrepNakedEdgePoints, precision)
                            && ArePointListsEpsilonEqual(current.MeshNakedEdgePoints, other.MeshNakedEdgePoints, precision)
                            && ArePointListsEpsilonEqual(current.CurveEndPoints, other.CurveEndPoints, precision)) {
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

        private static bool ArePointListsEpsilonEqual(List<Point3d> a, List<Point3d> b, double precision) {
            if (a.Count != b.Count) {
                return false;
            }
            var equalityPattern = Enumerable.Repeat(-1, a.Count).ToList();
            ;
            for (var i = 0; i < a.Count; i++) {
                var pointCurrent = a[i];
                for (var j = 0; j < b.Count; j++) {
                    var pointOther = b[j];
                    if (!equalityPattern.Contains(j) && pointCurrent.EpsilonEquals(pointOther, precision)) {
                        equalityPattern[i] = j;
                        break;
                    }
                }
            }
            return !equalityPattern.Any(v => v == -1);
        }

        public override bool IsPreviewCapable => !Hidden && _points != null && !Locked;

        public override void DrawViewportWires(IGH_PreviewArgs args) {
            foreach (var point in _points) {
                args.Display.DrawPoint(point);
            }
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
