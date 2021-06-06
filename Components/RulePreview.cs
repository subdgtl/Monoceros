using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Monoceros {
    public class ComponentPreviewRules : GH_Component, IGH_PreviewObject, IGH_BakeAwareObject {
        private List<ExplicitLine> _explicitLines;
        private List<TypedLine> _typedLines;
        private List<OutLine> _outLines;

        public ComponentPreviewRules( )
            : base("Preview Rules",
                   "RulePreview",
                   "Preview Monoceros Rules as lines connecting individual Connectors of Monoceros Modules.",
                   "Monoceros",
                   "Rule") {
            _explicitLines = new List<ExplicitLine>();
            _typedLines = new List<TypedLine>();
            _outLines = new List<OutLine>();
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleParameter(),
                                  "Modules",
                                  "M",
                                  "Monoceros Modules",
                                  GH_ParamAccess.list);
            // TODO: Change to tree access and process each branch individually
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "All existing Monoceros Rules",
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
            _explicitLines.Clear();
            _typedLines.Clear();
            _outLines.Clear();

            if (!DA.GetDataList(0, modules)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to collect input Modules.");
                return;
            }

            if (!DA.GetDataList(1, rules)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to collect input Rules.");
                return;
            }

            var invalidModuleCount = modules.RemoveAll(module => module == null || !module.IsValid);

            if (invalidModuleCount > 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  invalidModuleCount + " Modules are null or invalid and were removed.");
            }
            
            
            var invalidRuleCount = rules.RemoveAll(rule => rule == null || !rule.IsValid);

            if (invalidRuleCount > 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  invalidRuleCount + " Rules are null or invalid and were removed.");
            }

            if (!modules.Any() || !rules.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid Modules or Rules provided.");
                return;
            }


            var moduleNames = modules.Select(module => module.Name).ToList();
            if (moduleNames.Count != moduleNames.Distinct().ToList().Count) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                                  "Module names are not unique.");
            }

            var rulesExplicit = rules.Where(rule => rule.IsExplicit).Select(rule => rule.Explicit);
            var rulesTyped = rules.Where(rule => rule.IsTyped).Select(rule => rule.Typed);

            foreach (var ruleExplicit in rulesExplicit) {
                if (ruleExplicit.SourceModuleName != Config.OUTER_MODULE_NAME
                    && ruleExplicit.TargetModuleName != Config.OUTER_MODULE_NAME) {
                    ConstructLineFromExplicitRule(modules,
                                                  ruleExplicit,
                                                  out var line,
                                                  out var axis);
                    _explicitLines.Add(new ExplicitLine(line, Config.ColorFromAxis(axis)));
                }
                if (ruleExplicit.TargetModuleName == Config.OUTER_MODULE_NAME) {
                    ConstructArrowFromOutRule(modules,
                                              ruleExplicit,
                                              out var line,
                                              out var axis);
                    _outLines.Add(new OutLine(line, Config.ColorFromAxis(axis)));
                }
            }

            foreach (var ruleTyped in rulesTyped) {
                var rulesExplicitComputed = ruleTyped.ToRulesExplicit(rulesTyped, modules);

                foreach (var ruleExplicit in rulesExplicitComputed) {
                    if (ruleExplicit.SourceModuleName != Config.OUTER_MODULE_NAME
                        && ruleExplicit.TargetModuleName != Config.OUTER_MODULE_NAME) {
                        ConstructLineFromExplicitRule(modules,
                                                      ruleExplicit,
                                                      out var line,
                                                      out var axis);
                        _typedLines.Add(
                            new TypedLine(line, Config.ColorFromAxis(axis), ruleTyped.ConnectorType)
                        );
                    }
                }
            }

            _explicitLines = _explicitLines.Distinct().ToList();
            _typedLines = _typedLines.Distinct().ToList();
        }

        private bool ConstructLineFromExplicitRule(
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

        private bool ConstructArrowFromOutRule(
            List<Module> modules,
            RuleExplicit ruleExplicit,
            out Line line,
            out Axis axis
            ) {
            line = default;
            axis = default;

            if (ruleExplicit.TargetModuleName != Config.OUTER_MODULE_NAME) {
                return false;
            }

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

            axis = sourceConnector.Direction.Axis;

            var targetPoint = sourceConnector.AnchorPlane.Origin
                + sourceConnector.Direction.ToVector()
                * sourceModule.PartDiagonal[(int)axis]
                / 4;
            line = new Line(sourceConnector.AnchorPlane.Origin,
                            targetPoint);
            return true;

        }

        public override bool IsPreviewCapable => !Hidden && !Locked;

        public override void DrawViewportWires(IGH_PreviewArgs args) {
            foreach (var line in _explicitLines) {
                args.Display.DrawLine(line.Line, line.Color, Config.RULE_PREVIEW_THICKNESS);
            }

            foreach (var line in _typedLines) {
                args.Display.DrawLine(line.Line, line.Color, Config.RULE_PREVIEW_THICKNESS);

                var oneQuarterPoint = line.Line.From + (line.Line.To - line.Line.From) / 4;
                args.Display.DrawDot(oneQuarterPoint, line.Type, Config.POSITIVE_COLOR, line.Color);
            }

            foreach (var line in _outLines) {
                args.Display.DrawArrow(line.Line, line.Color);

                var oneHalfPoint = line.Line.From + (line.Line.To - line.Line.From) / 2;
                args.Display.DrawDot(oneHalfPoint, Config.OUTER_MODULE_NAME, Config.POSITIVE_COLOR, line.Color);
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

                var oneQuarterPoint = line.Line.From + (line.Line.To - line.Line.From) / 4;

                var dotAttributes = att.Duplicate();
                dotAttributes.ObjectColor = line.Color;
                dotAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                dotAttributes.LayerIndex = layerIndex;
                obj_ids.Add(doc.Objects.AddTextDot(line.Type, oneQuarterPoint, dotAttributes));
            }

            foreach (var line in _outLines) {
                var lineAttributes = att.Duplicate();
                lineAttributes.ObjectColor = line.Color;
                lineAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                lineAttributes.LayerIndex = layerIndex;
                obj_ids.Add(doc.Objects.AddLine(line.Line, lineAttributes));

                var dotAttributes = att.Duplicate();
                dotAttributes.ObjectColor = line.Color;
                dotAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                dotAttributes.LayerIndex = layerIndex;
                obj_ids.Add(doc.Objects.AddTextDot(Config.OUTER_MODULE_NAME, line.Line.To, dotAttributes));
            }
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to
        /// septenary), each of which can be combined with the
        /// GH_Exposure.obscure flag, which ensures the component will only be
        /// visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.septenary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override Bitmap Icon => Properties.Resources.rule_general_transparent;

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
    internal struct OutLine {
        public readonly Line Line;
        public readonly Color Color;

        public OutLine(Line line, Color color) {
            Line = line;
            Color = color;
        }

        public override bool Equals(object obj) {
            return obj is OutLine line &&
                   Line.Equals(line.Line) &&
                   EqualityComparer<Color>.Default.Equals(Color, line.Color);
        }

        public override int GetHashCode( ) {
            var hashCode = 1814501463;
            hashCode = hashCode * -1521134295 + Line.GetHashCode();
            hashCode = hashCode * -1521134295 + Color.GetHashCode();
            return hashCode;
        }
    }
}
