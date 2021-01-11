using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace WFCPlugin {
    public class ComponentPreviewRules : GH_Component, IGH_PreviewObject, IGH_BakeAwareObject {
        private IEnumerable<ExplicitLine> _explicitLines;
        private IEnumerable<TypedLine> _typedLines;

        public ComponentPreviewRules( )
            : base("Preview Rules",
                   "RulePreview",
                   "Preview Monoceros Rules as lines connecting individual connectors of Monoceros Modules.",
                   "Monoceros",
                   "Rule") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleParameter(),
                                  "Module",
                                  "M",
                                  "Monoceros module for indifferent rule generation",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "All existing Monoceros rules",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var modules = new List<Module>();
            var rules = new List<Rule>();

            if (!DA.GetDataList(0, modules)) {
                return;
            }

            if (!DA.GetDataList(1, rules)) {
                return;
            }

            if (modules.Count == 0 || rules.Count == 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to collect input data.");
            }

            var minimumSlotDimension = 1.0;
            var averageSlotDiagonal = new Vector3d();

            var moduleNames = modules.Select(module => module.Name);
            if (moduleNames.ToList().Count != moduleNames.Distinct().ToList().Count) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                                  "Module names are not unique.");
            }

            var modulesClean = new List<Module>();
            foreach (var module in modules) {
                if (module == null || !module.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The module is null or invalid.");
                    continue;
                }
                modulesClean.Add(module);
                var minSize = module.SlotDiagonal.MinimumCoordinate;
                if (minSize < minimumSlotDimension) {
                    minimumSlotDimension = minSize;
                }
                averageSlotDiagonal += module.SlotDiagonal;
            }

            averageSlotDiagonal /= modulesClean.Count;

            var explicitLines = new List<ExplicitLine>();
            var typedLines = new List<TypedLine>();

            var rulesClean = new List<Rule>();
            foreach (var rule in rules) {
                if (rule == null || !rule.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The rule is null or invalid.");
                } else {
                    rulesClean.Add(rule);
                }
            }

            var rulesExplicit = rulesClean.Where(rule => rule.IsExplicit).Select(rule => rule.Explicit);
            var rulesTyped = rulesClean.Where(rule => rule.IsTyped).Select(rule => rule.Typed);

            foreach (var ruleExplicit in rulesExplicit) {
                // TODO: Consider displaying Out connections somehow too
                if (!Config.RESERVED_NAMES.Contains(ruleExplicit.SourceModuleName) &&
                    !Config.RESERVED_NAMES.Contains(ruleExplicit.TargetModuleName)) {
                    GetLineFromExplicitRule(modulesClean,
                                            ruleExplicit,
                                            out var line,
                                            out var axis);
                    explicitLines.Add(new ExplicitLine(line, Config.ColorFromAxis(axis)));
                }
            }

            foreach (var ruleTyped in rulesTyped) {
                var rulesExplicitComputed = ruleTyped.ToRulesExplicit(rulesTyped, modulesClean);

                foreach (var ruleExplicit in rulesExplicitComputed) {
                    // TODO: Consider displaying Out connections somehow too
                    if (!Config.RESERVED_NAMES.Contains(ruleExplicit.SourceModuleName) &&
                        !Config.RESERVED_NAMES.Contains(ruleExplicit.TargetModuleName)) {
                        GetLineFromExplicitRule(modulesClean,
                                                ruleExplicit,
                                                out var line,
                                                out var axis);
                        typedLines.Add(
                            new TypedLine(line, Config.ColorFromAxis(axis), ruleTyped.ConnectorType)
                        );
                    }
                }
            }

            _explicitLines = explicitLines.Distinct();
            _typedLines = typedLines.Distinct();
        }

        private bool GetLineFromExplicitRule(
            List<Module> modules,
            RuleExplicit ruleExplicit,
            out Line line,
            out Axis axis
            ) {
            line = default;
            axis = default;

            var sourceModule = modules
                .FirstOrDefault(module => module.Name == ruleExplicit.SourceModuleName);
            if (sourceModule == null) {
                return false;
            }

            var sourceConnector = sourceModule
                .Connectors
                .ElementAtOrDefault(ruleExplicit.SourceConnectorIndex);
            if (sourceConnector.Equals(default(ModuleConnector))) {
                return false;
            }

            var targetModule = modules
                .FirstOrDefault(module => module.Name == ruleExplicit.TargetModuleName);
            if (targetModule == null) {
                return false;
            }

            var targetConnector = targetModule
                .Connectors
                .ElementAtOrDefault(ruleExplicit.TargetConnectorIndex);
            if (targetConnector.Equals(default(ModuleConnector))) {
                return false;
            }

            if (targetConnector.Direction.IsOpposite(sourceConnector.Direction)) {
                line = new Line(sourceConnector.AnchorPlane.Origin,
                                targetConnector.AnchorPlane.Origin);
                axis = sourceConnector.Direction.Axis;
                return true;
            } else {
                return false;
            }
        }

        public new bool Hidden => false;

        public bool GetIsPreviewCapable( ) {
            return true;
        }

        public override void DrawViewportWires(IGH_PreviewArgs args) {
            foreach (var line in _explicitLines) {
                args.Display.DrawLine(line.Line, line.Color, 2);
            }

            foreach (var line in _typedLines) {
                args.Display.DrawLine(line.Line, line.Color, 2);

                var middlePoint = (line.Line.To + line.Line.From) / 2;
                args.Display.DrawDot(middlePoint, line.Type, Config.POSITIVE_COLOR, line.Color);
            }
        }

        public override bool IsBakeCapable => true;

        public override void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids) {
            BakeGeometry(doc, new ObjectAttributes(), obj_ids);
        }

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids) {
            if (att == null) {
                att = doc.CreateDefaultAttributes();
            }
            var layerIndex = doc.Layers.CurrentLayerIndex;

            foreach (var line in _explicitLines) {
                var lineAttributes = att.Duplicate();
                lineAttributes.ObjectColor = line.Color;
                lineAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                lineAttributes.LayerIndex = layerIndex;
                obj_ids.Add(doc.Objects.AddLine(line.Line, lineAttributes));
            }

            foreach (var line in _typedLines) {
                var lineAttributes = att.Duplicate();
                lineAttributes.ObjectColor = line.Color;
                lineAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                lineAttributes.LayerIndex = layerIndex;
                obj_ids.Add(doc.Objects.AddLine(line.Line, lineAttributes));

                var middlePoint = (line.Line.To + line.Line.From) / 2;

                var dotAttributes = att.Duplicate();
                dotAttributes.ObjectColor = line.Color;
                dotAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                dotAttributes.LayerIndex = layerIndex;
                obj_ids.Add(doc.Objects.AddTextDot(line.Type, middlePoint, dotAttributes));
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
        protected override Bitmap Icon => Properties.Resources.W;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("CD5D3078-2F06-4BCD-9267-2B828177DFDB");
    }
    internal struct ExplicitLine {
        public readonly Line Line;
        public readonly Color Color;

        public ExplicitLine(Line line, Color color) {
            Line = line;
            Color = color;
        }

        public override bool Equals(object obj) {
            var flipped = Line;
            flipped.Flip();
            return obj is ExplicitLine line &&
                   (Line.Equals(line.Line) || flipped.Equals(line.Line)) &&
                   EqualityComparer<Color>.Default.Equals(Color, line.Color);
        }

        public override int GetHashCode( ) {
            var hashCode = -5646795;
            hashCode = hashCode * -1521134295 + Line.GetHashCode();
            hashCode = hashCode * -1521134295 + Color.GetHashCode();
            return hashCode;
        }
    }
    internal struct TypedLine {
        public readonly Line Line;
        public readonly Color Color;
        public readonly string Type;

        public TypedLine(Line line, Color color, string type) {
            Line = line;
            Color = color;
            Type = type;
        }

        public override bool Equals(object obj) {
            var flipped = Line;
            flipped.Flip();
            return obj is TypedLine line &&
                   (Line.Equals(line.Line) || flipped.Equals(line.Line)) &&
                   EqualityComparer<Color>.Default.Equals(Color, line.Color) &&
                   Type == line.Type;
        }

        public override int GetHashCode( ) {
            var hashCode = -551534709;
            hashCode = hashCode * -1521134295 + Line.GetHashCode();
            hashCode = hashCode * -1521134295 + Color.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Type);
            return hashCode;
        }
    }
}
