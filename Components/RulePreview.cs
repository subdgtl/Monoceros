// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace WFCToolset
{
    public class ComponentPreviewRules : GH_Component, IGH_PreviewObject, IGH_BakeAwareObject
    {
        private IEnumerable<ExplicitLine> _explicitLines;
        private IEnumerable<TypedLine> _typedLines;

        public ComponentPreviewRules() : base("WFC Preview Rules", "WFCRulePreview",
            "Preview rules as lines connecting individual connectors. Does not display connections to reserved modules " + Configuration.RESERVED_TO_STRING + ".",
            "WaveFunctionCollapse", "Preview")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new ModuleParameter(), "Module", "M", "WFC module for indifferent rule generation", GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(), "Rules", "R", "All existing WFC rules", GH_ParamAccess.list);
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
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var modules = new List<Module>();
            var rules = new List<Rule>();

            if (!DA.GetDataList(0, modules))
            {
                return;
            }

            if (!DA.GetDataList(1, rules))
            {
                return;
            }

            var minimumSlotDimension = 1.0;
            var averageSlotDiagonal = new Vector3d();

            foreach (var module in modules)
            {
                var minSize = module.SlotDiagonal.MinimumCoordinate;
                if (minSize < minimumSlotDimension)
                {
                    minimumSlotDimension = minSize;
                }
                averageSlotDiagonal += module.SlotDiagonal;
            }

            averageSlotDiagonal /= modules.Count;

            var explicitLines = new List<ExplicitLine>();
            var typedLines = new List<TypedLine>();

            var rulesExplicit = rules.Where(rule => rule.IsExplicit()).Select(rule => rule.Explicit);
            var rulesTyped = rules.Where(rule => rule.IsTyped()).Select(rule => rule.Typed);

            foreach (var ruleExplicit in rulesExplicit)
            {
                // TODO: Consider displaying Out connections somehow too
                if (!Configuration.RESERVED_NAMES.Contains(ruleExplicit.SourceModuleName) &&
                    !Configuration.RESERVED_NAMES.Contains(ruleExplicit.TargetModuleName))
                {
                    GetLinesFromExplicitRule(modules, ruleExplicit, out var linesX, out var linesY, out var linesZ);

                    explicitLines.AddRange(
                        linesX.Select(line => new ExplicitLine(line, Configuration.X_COLOR))
                        );
                    explicitLines.AddRange(
                        linesY.Select(line => new ExplicitLine(line, Configuration.Y_COLOR))
                        );
                    explicitLines.AddRange(
                        linesZ.Select(line => new ExplicitLine(line, Configuration.Z_COLOR))
                        );
                }
            }

            foreach (var ruleTyped in rulesTyped)
            {
                var rulesExplicitComputed = ruleTyped.ToRuleExplicit(rulesTyped, modules);

                foreach (var ruleExplicit in rulesExplicitComputed)
                {
                    // TODO: Consider displaying Out connections somehow too
                    if (!Configuration.RESERVED_NAMES.Contains(ruleExplicit.SourceModuleName) &&
                        !Configuration.RESERVED_NAMES.Contains(ruleExplicit.TargetModuleName))
                    {
                        GetLinesFromExplicitRule(modules, ruleExplicit, out var linesX, out var linesY, out var linesZ);
                        typedLines.AddRange(
                            linesX.Select(line => new TypedLine(line, Configuration.X_COLOR, ruleTyped.ConnectorType))
                        );
                        typedLines.AddRange(
                            linesY.Select(line => new TypedLine(line, Configuration.Y_COLOR, ruleTyped.ConnectorType))
                        );
                        typedLines.AddRange(
                            linesZ.Select(line => new TypedLine(line, Configuration.Z_COLOR, ruleTyped.ConnectorType))
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
            var sourceModules = modules.Where(module => module.Name == ruleExplicit.SourceModuleName);
            var sourceConnectors = sourceModules
                .SelectMany(module => module.Connectors)
                .Where(connector => connector.ConnectorIndex == ruleExplicit.SourceConnectorIndex);
            var targetModules = modules.Where(module => module.Name == ruleExplicit.TargetModuleName);
            var targetConnectors = targetModules
                .SelectMany(module => module.Connectors)
                .Where(connector => connector.ConnectorIndex == ruleExplicit.TargetConnectorIndex);

            foreach (var sourceConnector in sourceConnectors)
            {
                foreach (var targetConnector in targetConnectors)
                {
                    if (targetConnector.Direction.IsOpposite(sourceConnector.Direction))
                    {
                        switch (sourceConnector.Direction._axis)
                        {
                            case Axis.X:
                                linesX.Add(new Line(sourceConnector.AnchorPlane.Origin, targetConnector.AnchorPlane.Origin));
                                break;
                            case Axis.Y:
                                linesY.Add(new Line(sourceConnector.AnchorPlane.Origin, targetConnector.AnchorPlane.Origin));
                                break;
                            case Axis.Z:
                                linesZ.Add(new Line(sourceConnector.AnchorPlane.Origin, targetConnector.AnchorPlane.Origin));
                                break;
                        }
                    }
                }
            }
        }

        public new bool Hidden => false;

        public bool GetIsPreviewCapable() => true;

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            foreach (var line in _explicitLines)
            {
                args.Display.DrawLine(line._line, line._color, 2);
            }

            foreach (var line in _typedLines)
            {
                args.Display.DrawLine(line._line, line._color, 2);

                var middlePoint = (line._line.To + line._line.From) / 2;
                args.Display.DrawDot(middlePoint, line._type, Configuration.POSITIVE_COLOR, line._color);
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
            var layerIndex = doc.Layers.CurrentLayerIndex;

            foreach (var line in _explicitLines)
            {
                var lineAttributes = att.Duplicate();
                lineAttributes.ObjectColor = line._color;
                lineAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                lineAttributes.LayerIndex = layerIndex;
                obj_ids.Add(doc.Objects.AddLine(line._line, lineAttributes));
            }

            foreach (var line in _typedLines)
            {
                var lineAttributes = att.Duplicate();
                lineAttributes.ObjectColor = line._color;
                lineAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                lineAttributes.LayerIndex = layerIndex;
                obj_ids.Add(doc.Objects.AddLine(line._line, lineAttributes));

                var middlePoint = (line._line.To + line._line.From) / 2;

                var dotAttributes = att.Duplicate();
                dotAttributes.ObjectColor = line._color;
                dotAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                dotAttributes.LayerIndex = layerIndex;
                obj_ids.Add(doc.Objects.AddTextDot(line._type, middlePoint, dotAttributes));
            }
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
        protected override Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                Properties.Resources.W;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("CD5D3078-2F06-4BCD-9267-2B828177DFDB");
    }
    internal struct ExplicitLine
    {
        public Line _line;
        public Color _color;

        public ExplicitLine(Line line, Color color)
        {
            _line = line;
            _color = color;
        }

        public override bool Equals(object obj)
        {
            var flipped = _line;
            flipped.Flip();
            return obj is ExplicitLine line &&
                   (_line.Equals(line._line) || flipped.Equals(line._line)) &&
                   EqualityComparer<Color>.Default.Equals(_color, line._color);
        }

        public override int GetHashCode()
        {
            var hashCode = -5646795;
            hashCode = hashCode * -1521134295 + _line.GetHashCode();
            hashCode = hashCode * -1521134295 + _color.GetHashCode();
            return hashCode;
        }
    }
    internal struct TypedLine
    {
        public Line _line;
        public Color _color;
        public string _type;

        public TypedLine(Line line, Color color, string type)
        {
            _line = line;
            _color = color;
            _type = type;
        }

        public override bool Equals(object obj)
        {
            var flipped = _line;
            flipped.Flip();
            return obj is TypedLine line &&
                   (_line.Equals(line._line) || flipped.Equals(line._line)) &&
                   EqualityComparer<Color>.Default.Equals(_color, line._color) &&
                   _type == line._type;
        }

        public override int GetHashCode()
        {
            var hashCode = -551534709;
            hashCode = hashCode * -1521134295 + _line.GetHashCode();
            hashCode = hashCode * -1521134295 + _color.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_type);
            return hashCode;
        }
    }
}
