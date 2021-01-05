using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WFCPlugin
{
    public class ComponentPreviewRules : GH_Component, IGH_PreviewObject, IGH_BakeAwareObject
    {
        private IEnumerable<ExplicitLine> _explicitLines;
        private IEnumerable<TypedLine> _typedLines;

        public ComponentPreviewRules()
            : base("WFC Preview Rules",
                   "WFCRulePreview",
                   "Preview rules as lines connecting individual connectors. Does not display " +
                  "connections to reserved modules " + Config.RESERVED_TO_STRING + ".",
                   "WaveFunctionCollapse",
                   "Preview")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ModuleParameter(),
                                  "Module",
                                  "M",
                                  "WFC module for indifferent rule generation",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "All existing WFC rules",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Module> modules = new List<Module>();
            List<Rule> rules = new List<Rule>();

            if (!DA.GetDataList(0, modules))
            {
                return;
            }

            if (!DA.GetDataList(1, rules))
            {
                return;
            }

            double minimumSlotDimension = 1.0;
            Vector3d averageSlotDiagonal = new Vector3d();

            foreach (Module module in modules)
            {
                double minSize = module.SlotDiagonal.MinimumCoordinate;
                if (minSize < minimumSlotDimension)
                {
                    minimumSlotDimension = minSize;
                }
                averageSlotDiagonal += module.SlotDiagonal;
            }

            averageSlotDiagonal /= modules.Count;

            List<ExplicitLine> explicitLines = new List<ExplicitLine>();
            List<TypedLine> typedLines = new List<TypedLine>();

            IEnumerable<RuleExplicit> rulesExplicit = rules.Where(rule => rule.IsExplicit()).Select(rule => rule.Explicit);
            IEnumerable<RuleTyped> rulesTyped = rules.Where(rule => rule.IsTyped()).Select(rule => rule.Typed);

            foreach (RuleExplicit ruleExplicit in rulesExplicit)
            {
                // TODO: Consider displaying Out connections somehow too
                if (!Config.RESERVED_NAMES.Contains(ruleExplicit.SourceModuleName) &&
                    !Config.RESERVED_NAMES.Contains(ruleExplicit.TargetModuleName))
                {
                    GetLinesFromExplicitRule(modules,
                                             ruleExplicit,
                                             out List<Line> linesX,
                                             out List<Line> linesY,
                                             out List<Line> linesZ);

                    explicitLines.AddRange(
                        linesX.Select(line => new ExplicitLine(line, Config.X_COLOR))
                        );
                    explicitLines.AddRange(
                        linesY.Select(line => new ExplicitLine(line, Config.Y_COLOR))
                        );
                    explicitLines.AddRange(
                        linesZ.Select(line => new ExplicitLine(line, Config.Z_COLOR))
                        );
                }
            }

            foreach (RuleTyped ruleTyped in rulesTyped)
            {
                List<RuleExplicit> rulesExplicitComputed = ruleTyped.ToRuleExplicit(rulesTyped, modules);

                foreach (RuleExplicit ruleExplicit in rulesExplicitComputed)
                {
                    // TODO: Consider displaying Out connections somehow too
                    if (!Config.RESERVED_NAMES.Contains(ruleExplicit.SourceModuleName) &&
                        !Config.RESERVED_NAMES.Contains(ruleExplicit.TargetModuleName))
                    {
                        GetLinesFromExplicitRule(modules,
                                                 ruleExplicit,
                                                 out List<Line> linesX,
                                                 out List<Line> linesY,
                                                 out List<Line> linesZ);
                        typedLines.AddRange(
                            linesX.Select(line => new TypedLine(line,
                                                                Config.X_COLOR,
                                                                ruleTyped.ConnectorType))
                        );
                        typedLines.AddRange(
                            linesY.Select(line => new TypedLine(line,
                                                                Config.Y_COLOR,
                                                                ruleTyped.ConnectorType))
                        );
                        typedLines.AddRange(
                            linesZ.Select(line => new TypedLine(line,
                                                                Config.Z_COLOR,
                                                                ruleTyped.ConnectorType))
                        );
                    }
                }
            }

            _explicitLines = explicitLines.Distinct();
            _typedLines = typedLines.Distinct();
        }

        private void GetLinesFromExplicitRule(
            List<Module> modules,
            RuleExplicit ruleExplicit,
            out List<Line> linesX,
            out List<Line> linesY,
            out List<Line> linesZ
            )
        {
            linesX = new List<Line>();
            linesY = new List<Line>();
            linesZ = new List<Line>();
            IEnumerable<Module> sourceModules = modules
                .Where(module => module.Name == ruleExplicit.SourceModuleName);
            IEnumerable<ModuleConnector> sourceConnectors = sourceModules
                .SelectMany(module => module.Connectors)
                .Where(connector => connector.ConnectorIndex == ruleExplicit.SourceConnectorIndex);
            IEnumerable<Module> targetModules = modules
                .Where(module => module.Name == ruleExplicit.TargetModuleName);
            IEnumerable<ModuleConnector> targetConnectors = targetModules
                .SelectMany(module => module.Connectors)
                .Where(connector => connector.ConnectorIndex == ruleExplicit.TargetConnectorIndex);

            foreach (ModuleConnector sourceConnector in sourceConnectors)
            {
                foreach (ModuleConnector targetConnector in targetConnectors)
                {
                    if (targetConnector.Direction.IsOpposite(sourceConnector.Direction))
                    {
                        switch (sourceConnector.Direction._axis)
                        {
                            case Axis.X:
                                linesX.Add(new Line(sourceConnector.AnchorPlane.Origin,
                                                    targetConnector.AnchorPlane.Origin));
                                break;
                            case Axis.Y:
                                linesY.Add(new Line(sourceConnector.AnchorPlane.Origin,
                                                    targetConnector.AnchorPlane.Origin));
                                break;
                            case Axis.Z:
                                linesZ.Add(new Line(sourceConnector.AnchorPlane.Origin,
                                                    targetConnector.AnchorPlane.Origin));
                                break;
                        }
                    }
                }
            }
        }

        public new bool Hidden => false;

        public bool GetIsPreviewCapable()
        {
            return true;
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            foreach (ExplicitLine line in _explicitLines)
            {
                args.Display.DrawLine(line.Line, line.Color, 2);
            }

            foreach (TypedLine line in _typedLines)
            {
                args.Display.DrawLine(line.Line, line.Color, 2);

                Point3d middlePoint = (line.Line.To + line.Line.From) / 2;
                args.Display.DrawDot(middlePoint, line.Type, Config.POSITIVE_COLOR, line.Color);
            }
        }

        public override bool IsBakeCapable => true;

        public override void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            BakeGeometry(doc, new ObjectAttributes(), obj_ids);
        }

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            if (att == null)
            {
                att = doc.CreateDefaultAttributes();
            }
            int layerIndex = doc.Layers.CurrentLayerIndex;

            foreach (ExplicitLine line in _explicitLines)
            {
                ObjectAttributes lineAttributes = att.Duplicate();
                lineAttributes.ObjectColor = line.Color;
                lineAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                lineAttributes.LayerIndex = layerIndex;
                obj_ids.Add(doc.Objects.AddLine(line.Line, lineAttributes));
            }

            foreach (TypedLine line in _typedLines)
            {
                ObjectAttributes lineAttributes = att.Duplicate();
                lineAttributes.ObjectColor = line.Color;
                lineAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                lineAttributes.LayerIndex = layerIndex;
                obj_ids.Add(doc.Objects.AddLine(line.Line, lineAttributes));

                Point3d middlePoint = (line.Line.To + line.Line.From) / 2;

                ObjectAttributes dotAttributes = att.Duplicate();
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
    internal struct ExplicitLine
    {
        public readonly Line Line;
        public readonly Color Color;

        public ExplicitLine(Line line, Color color)
        {
            Line = line;
            Color = color;
        }

        public override bool Equals(object obj)
        {
            Line flipped = Line;
            flipped.Flip();
            return obj is ExplicitLine line &&
                   (Line.Equals(line.Line) || flipped.Equals(line.Line)) &&
                   EqualityComparer<Color>.Default.Equals(Color, line.Color);
        }

        public override int GetHashCode()
        {
            int hashCode = -5646795;
            hashCode = hashCode * -1521134295 + Line.GetHashCode();
            hashCode = hashCode * -1521134295 + Color.GetHashCode();
            return hashCode;
        }
    }
    internal struct TypedLine
    {
        public readonly Line Line;
        public readonly Color Color;
        public readonly string Type;

        public TypedLine(Line line, Color color, string type)
        {
            Line = line;
            Color = color;
            Type = type;
        }

        public override bool Equals(object obj)
        {
            Line flipped = Line;
            flipped.Flip();
            return obj is TypedLine line &&
                   (Line.Equals(line.Line) || flipped.Equals(line.Line)) &&
                   EqualityComparer<Color>.Default.Equals(Color, line.Color) &&
                   Type == line.Type;
        }

        public override int GetHashCode()
        {
            int hashCode = -551534709;
            hashCode = hashCode * -1521134295 + Line.GetHashCode();
            hashCode = hashCode * -1521134295 + Color.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Type);
            return hashCode;
        }
    }
}
