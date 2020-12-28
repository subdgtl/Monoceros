// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace WFCToolset
{
    /// <summary>
    /// WFC Module type containing name, geometry and submodule and connector info, 
    /// including internal rules holding the module together.
    /// </summary>
    public class Module : IGH_Goo, IGH_PreviewData, IGH_BakeAwareObject
    {

        /// <summary>
        /// Module name.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Geometry contained in the module.
        /// </summary>
        public readonly List<GeometryBase> Geometry;

        /// <summary>
        /// Base plane of the module's coordinate system.
        /// </summary>
        public readonly Plane BasePlane;

        /// <summary>
        /// Centers of submodules for module reconstruction.
        /// </summary>
        public readonly List<Point3i> SubmoduleCenters;

        /// <summary>
        /// Source plane for geometry placement. The Pivot is located in 
        /// the center of the first submodule and oriented so that 
        /// the geometry can be Oriented from the Pivot to the target 
        /// slot center plane.
        /// </summary>
        public readonly Plane Pivot;

        /// <summary>
        /// Name of the submodule containing the Pivot. The entire contained
        /// geometry will be placed onto the center plane of a slot containing
        /// PivotSubmoduleName.
        /// </summary>
        public readonly string PivotSubmoduleName;

        /// <summary>
        /// Dimensions of a single slot. Slots are boxes encapsulating the module geometry.
        /// The filed's purpose is to check compatibility of various modules with the world they should populate.
        /// In the future the modules could be squished to fit the world's slot dimensions.
        /// </summary>
        public readonly Vector3d SlotDiagonal;

        /// <summary>
        /// Information about module connectors: 
        /// submodule name, connector direction, valence (internal / external), 
        /// anchor point (face center) position
        /// Connector numbering convention: 
        /// (submoduleIndex * 6) + faceIndex, where faceIndex is X=0, Y=1, Z=2, -X=3, -Y=4, -Z=5
        /// </summary>
        public readonly List<ModuleConnector> Connectors;

        /// <summary>
        /// Explicit rules holding the module's submodules together.
        /// </summary>
        public readonly List<RuleExplicit> InternalRules;

        /// <summary>
        /// Check if the module submodules create a continuous blob. 
        /// If the module contains islands, then it is not continuous and the module will never hold together. 
        /// </summary>
        public readonly bool Continuous;

        /// <summary>
        /// WFC Module constructor.
        /// </summary>
        /// <param name="geometry">Contains geometry to be placed into the module. 
        /// The geometry is not related to the module's submodules, which means it does not have 
        /// to respect the module boundaries, nor fill all submodules.</param>
        /// <param name="basePlane">The base plane of the module, defining its coordinate system. 
        /// It will be used to display submodule cages and to orient 
        /// the geometry into the WFC world slots.</param>
        /// <param name="submoduleCenters">Centers of the submodules in integer coordinate system. 
        /// Each unit represents one slot. The coordinate system origin 
        /// and orientation is defined by the basePlane. submoduleCenters are the only source of 
        /// information about the module's dimensions and occupied submodules.</param>
        /// <param name="slotDiagonal">Dimension of a single world slot.</param>
        public Module(string name,
                      IEnumerable<GeometryBase> geometry,
                      Plane basePlane,
                      List<Point3i> submoduleCenters,
                      Vector3d slotDiagonal)
        {
            // Check if any submodule centers are defined
            if (submoduleCenters.Count == 0)
            {
                throw new Exception("Submodule centers list is empty");
            }

            // Check if all the submodules are unique
            for (var i = 0; i < submoduleCenters.Count - 1; i++)
            {
                var center = submoduleCenters[i];
                for (var j = i + 1; j < submoduleCenters.Count; j++)
                {
                    var other = submoduleCenters[j];
                    if (other.Equals(center))
                    {
                        throw new Exception("Submodule centers are repetitive");
                    }
                }
            }

            if (slotDiagonal.X <= 0 || slotDiagonal.Y <= 0 || slotDiagonal.Z <= 0)
            {
                throw new Exception("One or more slot dimensions are not larger than 0");
            }

            SlotDiagonal = slotDiagonal;

            Continuous = true;

            // TODO: Make a proper crawler to test the continuity
            if (submoduleCenters.Count > 1)
            {
                foreach (var current in submoduleCenters)
                {
                    if (!submoduleCenters.Any(other =>
                    (Math.Abs(current._x - other._x) == 1 && current._y == other._y && current._z == other._z) ||
                    (Math.Abs(current._y - other._y) == 1 && current._x == other._x && current._z == other._z) ||
                    (Math.Abs(current._z - other._z) == 1 && current._y == other._y && current._x == other._x)
                    ))
                    {
                        Continuous = false;
                        break;
                    }
                }
            }

            Name = name.ToLower() ?? throw new ArgumentNullException(nameof(name));
            Geometry = geometry.ToList() ?? throw new ArgumentNullException(nameof(geometry));

            BasePlane = basePlane.Clone();
            SubmoduleCenters = submoduleCenters;

            // Place the pivot into the first submodule and orient is according to the base plane 
            Pivot = basePlane.Clone();
            Pivot.Origin = new Point3d(submoduleCenters[0]._x * slotDiagonal.X,
                                       submoduleCenters[0]._y * slotDiagonal.Y,
                                       submoduleCenters[0]._z * slotDiagonal.Z);
            // The name of the first submodule which should trigger the geometry placement
            PivotSubmoduleName = Name + 0;

            Connectors = ComputeModuleConnectors(submoduleCenters, slotDiagonal, basePlane);

            InternalRules = ComputeInternalRules(submoduleCenters);
        }

        public Module()
        {
        }

        private List<ModuleConnector> ComputeModuleConnectors(List<Point3i> submoduleCenters,
                                                              Vector3d slotDiagonal,
                                                              Plane basePlane)
        {
            var moduleConnectors = new List<ModuleConnector>(submoduleCenters.Count * 6);

            var directionXPositive = new Direction { _axis = Axis.X, _orientation = Orientation.Positive };
            var directionYPositive = new Direction { _axis = Axis.Y, _orientation = Orientation.Positive };
            var directionZPositive = new Direction { _axis = Axis.Z, _orientation = Orientation.Positive };
            var directionXNegative = new Direction { _axis = Axis.X, _orientation = Orientation.Negative };
            var directionYNegative = new Direction { _axis = Axis.Y, _orientation = Orientation.Negative };
            var directionZNegative = new Direction { _axis = Axis.Z, _orientation = Orientation.Negative };

            var xPositiveVectorUnit = directionXPositive.ToVector();
            var yPositiveVectorUnit = directionYPositive.ToVector();
            var zPositiveVectorUnit = directionZPositive.ToVector();
            var xNegativeVectorUnit = directionXNegative.ToVector();
            var yNegativeVectorUnit = directionYNegative.ToVector();
            var zNegativeVectorUnit = directionZNegative.ToVector();

            // Orient to the base coordinate system
            var baseAlignmentTransform = Transform.PlaneToPlane(Plane.WorldXY, basePlane);
            // Scale up to slot size
            var scalingTransform = Transform.Scale(basePlane, slotDiagonal.X, slotDiagonal.Y, slotDiagonal.Z);

            // Connector numbering convention: (submoduleIndex * 6) + faceIndex, where faceIndex is X=0, Y=1, Z=2, -X=3, -Y=4, -Z=5

            for (var submoduleIndex = 0; submoduleIndex < submoduleCenters.Count; submoduleIndex++)
            {
                var center = submoduleCenters[submoduleIndex];
                var submoduleName = Name + submoduleIndex;
                var submoduleCenter = new Point3d(center._x, center._y, center._z);

                var faceCenterXPositive = submoduleCenter + xPositiveVectorUnit * 0.5;
                faceCenterXPositive.Transform(baseAlignmentTransform);
                faceCenterXPositive.Transform(scalingTransform);
                var planeXPositive = new Plane(faceCenterXPositive, basePlane.YAxis, basePlane.ZAxis);
                var faceXPositive = new Rectangle3d(
                    planeXPositive,
                    new Interval(slotDiagonal.Y * (-0.5), slotDiagonal.Y * 0.5),
                    new Interval(slotDiagonal.Z * (-0.5), slotDiagonal.Z * 0.5));
                var valenceXPositive = submoduleCenters.Any(o => center._x - o._x == -1 && center._y == o._y && center._z == o._z) ?
                    ModuleConnectorValence.Internal :
                    ModuleConnectorValence.External;
                var connectorXPositive = new ModuleConnector(
                    Name,
                    submoduleName,
                    submoduleIndex * 6 + 0,
                    directionXPositive,
                    valenceXPositive,
                    planeXPositive,
                    faceXPositive);
                moduleConnectors.Add(connectorXPositive);

                var faceCenterYPositive = submoduleCenter + yPositiveVectorUnit * 0.5;
                faceCenterYPositive.Transform(baseAlignmentTransform);
                faceCenterYPositive.Transform(scalingTransform);
                var planeYPositive = new Plane(faceCenterYPositive, basePlane.XAxis * (-1), basePlane.ZAxis);
                var faceYPositive = new Rectangle3d(
                    planeYPositive,
                    new Interval(slotDiagonal.X * (-0.5), slotDiagonal.X * 0.5),
                    new Interval(slotDiagonal.Z * (-0.5), slotDiagonal.Z * 0.5));
                var valenceYPositive = submoduleCenters.Any(o => center._y - o._y == -1 && center._x == o._x && center._z == o._z) ?
                    ModuleConnectorValence.Internal :
                    ModuleConnectorValence.External;
                var connectorYPositive = new ModuleConnector(
                    Name,
                    submoduleName,
                    submoduleIndex * 6 + 1,
                    directionYPositive,
                    valenceYPositive,
                    planeYPositive,
                    faceYPositive);
                moduleConnectors.Add(connectorYPositive);

                var faceCenterZPositive = submoduleCenter + zPositiveVectorUnit * 0.5;
                faceCenterZPositive.Transform(baseAlignmentTransform);
                faceCenterZPositive.Transform(scalingTransform);
                var planeZPositive = new Plane(faceCenterZPositive, basePlane.XAxis, basePlane.YAxis);
                var faceZPositive = new Rectangle3d(
                    planeZPositive,
                    new Interval(slotDiagonal.X * (-0.5), slotDiagonal.X * 0.5),
                    new Interval(slotDiagonal.Y * (-0.5), slotDiagonal.Y * 0.5));
                var valenceZPositive = submoduleCenters.Any(o => center._z - o._z == -1 && center._x == o._x && center._y == o._y) ?
                    ModuleConnectorValence.Internal :
                    ModuleConnectorValence.External;
                var connectorZPositive = new ModuleConnector(
                    Name,
                    submoduleName,
                    submoduleIndex * 6 + 2,
                    directionZPositive,
                    valenceZPositive,
                    planeZPositive,
                    faceZPositive);
                moduleConnectors.Add(connectorZPositive);

                var faceCenterXNegative = submoduleCenter + xNegativeVectorUnit * 0.5;
                faceCenterXNegative.Transform(baseAlignmentTransform);
                faceCenterXNegative.Transform(scalingTransform);
                var planeXNegative = new Plane(faceCenterXNegative, basePlane.YAxis * (-1), basePlane.ZAxis);
                var faceXNegative = new Rectangle3d(
                    planeXNegative,
                    new Interval(slotDiagonal.Y * (-0.5), slotDiagonal.Y * 0.5),
                    new Interval(slotDiagonal.Z * (-0.5), slotDiagonal.Z * 0.5));
                var valenceXNegative = submoduleCenters.Any(o => center._x - o._x == 1 && center._y == o._y && center._z == o._z) ?
                    ModuleConnectorValence.Internal :
                    ModuleConnectorValence.External;
                var connectorXNegative = new ModuleConnector(
                    Name,
                    submoduleName,
                    submoduleIndex * 6 + 3,
                    directionXNegative,
                    valenceXNegative,
                    planeXNegative,
                    faceXNegative);
                moduleConnectors.Add(connectorXNegative);

                var faceCenterYNegative = submoduleCenter + yNegativeVectorUnit * 0.5;
                faceCenterYNegative.Transform(baseAlignmentTransform);
                faceCenterYNegative.Transform(scalingTransform);
                var planeYNegative = new Plane(faceCenterYNegative, basePlane.XAxis, basePlane.ZAxis);
                var faceYNegative = new Rectangle3d(
                    planeYNegative,
                    new Interval(slotDiagonal.X * (-0.5), slotDiagonal.X * 0.5),
                    new Interval(slotDiagonal.Z * (-0.5), slotDiagonal.Z * 0.5));
                var valenceYNegative = submoduleCenters.Any(o => center._y - o._y == 1 && center._x == o._x && center._z == o._z) ?
                    ModuleConnectorValence.Internal :
                    ModuleConnectorValence.External;
                var connectorYNegative = new ModuleConnector(
                    Name,
                    submoduleName,
                    submoduleIndex * 6 + 4,
                    directionYNegative,
                    valenceYNegative,
                    planeYNegative,
                    faceYNegative);
                moduleConnectors.Add(connectorYNegative);

                var faceCenterZNegative = submoduleCenter + zNegativeVectorUnit * 0.5;
                faceCenterZNegative.Transform(baseAlignmentTransform);
                faceCenterZNegative.Transform(scalingTransform);
                var planeZNegative = new Plane(faceCenterZNegative, basePlane.XAxis * (-1), basePlane.YAxis);
                var faceZNegative = new Rectangle3d(
                    planeZNegative,
                    new Interval(slotDiagonal.X * (-0.5), slotDiagonal.X * 0.5),
                    new Interval(slotDiagonal.Y * (-0.5), slotDiagonal.Y * 0.5));
                var valenceZNegative = submoduleCenters.Any(o => center._z - o._z == 1 && center._x == o._x && center._x == o._x) ?
                    ModuleConnectorValence.Internal :
                    ModuleConnectorValence.External;
                var connectorZNegative = new ModuleConnector(
                    Name,
                    submoduleName,
                    submoduleIndex * 6 + 5,
                    directionZNegative,
                    valenceZNegative,
                    planeZNegative,
                    faceZNegative);
                moduleConnectors.Add(connectorZNegative);
            }

            return moduleConnectors;
        }

        private List<RuleExplicit> ComputeInternalRules(List<Point3i> submoduleCenters)
        {
            var rulesInternal = new List<RuleExplicit>();

            for (var thisIndex = 0; thisIndex < submoduleCenters.Count; thisIndex++)
            {
                var center = submoduleCenters[thisIndex];

                // Connector numbering convention: (submoduleIndex * 6) + faceIndex, where faceIndex is X=0, Y=1, Z=2, -X=3, -Y=4, -Z=5
                var otherIndexXPositive = submoduleCenters.FindIndex(o => center._x - o._x == -1 && center._y == o._y && center._z == o._z);
                if (otherIndexXPositive != -1)
                {
                    rulesInternal.Add(new RuleExplicit(Name, thisIndex * 6 + 0, Name, otherIndexXPositive * 6 + 3));
                    continue;
                }

                var otherIndexYPositive = submoduleCenters.FindIndex(o => center._y - o._y == -1 && center._x == o._x && center._z == o._z);
                if (otherIndexYPositive != -1)
                {
                    rulesInternal.Add(new RuleExplicit(Name, thisIndex * 6 + 1, Name, otherIndexYPositive * 6 + 4));
                    continue;
                }

                var otherIndexZPositive = submoduleCenters.FindIndex(o => center._z - o._z == -1 && center._x == o._x && center._y == o._y);
                if (otherIndexZPositive != -1)
                {
                    rulesInternal.Add(new RuleExplicit(Name, thisIndex * 6 + 2, Name, otherIndexZPositive * 6 + 5));
                    continue;
                }
            }
            return rulesInternal;
        }

        public IEnumerable<ModuleConnector> GetExternalConnectors() => Connectors.Where(c => c._valence == ModuleConnectorValence.External);

        public IEnumerable<ModuleConnector> GetExternalConnectorsContainingPoint(Point3d point) =>
            GetExternalConnectors().Where(connector =>
                connector._anchorPlane.DistanceTo(point) < RhinoMath.SqrtEpsilon &&
                connector._face.Contains(point) == PointContainment.Inside
            );

        public bool IsValid =>
            Connectors != null &&
            Geometry != null &&
            InternalRules != null &&
            Name != null &&
            Pivot != null &&
            PivotSubmoduleName != null &&
            Connectors.Count > 0 &&
            Continuous;

        // TODO: check why this doesn't say it is invalid
        public string IsValidWhyNot
        {
            get
            {
                if (!Continuous)
                {
                    return "The module is not continuous and therefore will not hold together.";
                }
                return "Some of the fields are null.";
            }
        }

        public string TypeName => "WFCModule";

        public string TypeDescription => "WFC Module data.";

        BoundingBox IGH_PreviewData.ClippingBox
        {
            get
            {
                var unionBox = BoundingBox.Empty;
                foreach (var connector in Connectors)
                {
                    unionBox.Union(connector._face.BoundingBox);
                }
                return unionBox;
            }
        }

        public bool CastFrom(object source) => false;

        public bool CastTo<T>(out T target)
        {
            // TODO: This is overridden by ToString. Not sure this could ever work
            if (IsValid && typeof(T) == typeof(string))
            {
                var obj = Name.Clone();
                target = (T)obj;
                return true;
            }
            target = default;
            return false;
        }

        public IGH_Goo Duplicate() => (IGH_Goo)MemberwiseClone();

        // TODO: Find out what this is
        public IGH_GooProxy EmitProxy() => null;

        // TODO: Do this for real
        public bool Read(GH_IReader reader) => true;
        public bool Write(GH_IWriter writer) => true;

        public object ScriptVariable() => this;

        public override string ToString() =>
            "Module " + Name + " occupies " +
            Connectors.Count / 6 + " slots and has " +
            Connectors.Count(c => c._valence == ModuleConnectorValence.External) + " connectors." +
            (Continuous ?
            "The module is continuous." :
            "WARNING: The module is not continuous and therefore will not hold together.");


        // This is to generate Out and Empty modules with Indifferent connector type
        public static void GenerateNamedEmptySingleModule(string name,
                                                          string connectorType,
                                                          Vector3d slotDiagonal,
                                                          out Module module,
                                                          out List<RuleTyped> rulesExternal)
        {
            module = new Module(
                name,
                new List<GeometryBase>(),
                Plane.WorldXY,
                new List<Point3i> { new Point3i(0, 0, 0) },
                slotDiagonal
                );
            rulesExternal = new List<RuleTyped>(6);
            for (var i = 0; i < 6; i++)
            {
                rulesExternal.Add(new RuleTyped(name, i, connectorType));
            }
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            foreach (var geo in Geometry)
            {
                if (geo.ObjectType == ObjectType.Point)
                {
                    args.Pipeline.DrawPoint(((Point)geo).Location, args.Color);
                }
                if (geo.ObjectType == ObjectType.Curve)
                {
                    args.Pipeline.DrawCurve((Curve)geo, args.Color);
                }
            }
            foreach (var externalConnector in GetExternalConnectors())
            {
                args.Pipeline.DrawPolyline(externalConnector._face.ToPolyline(), Configuration.CAGE_COLOR);
                var anchorPosition = externalConnector._anchorPlane.Origin;
                var dotColor = Configuration.ColorBackgroundFromDirection(externalConnector._direction);
                var textColor = Configuration.ColorForegroundFromDirection(externalConnector._direction);
                args.Pipeline.DrawDot(anchorPosition, externalConnector._connectorIndex.ToString(), dotColor, textColor);
            }
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            foreach (var geo in Geometry)
            {
                if (geo.ObjectType == ObjectType.Brep)
                {
                    args.Pipeline.DrawBrepShaded((Brep)geo, args.Material);
                }
                if (geo.ObjectType == ObjectType.Mesh)
                {
                    args.Pipeline.DrawMeshShaded((Mesh)geo, args.Material);
                }
            }
        }

        public bool IsBakeCapable => IsValid;

        public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            BakeGeometry(doc, new ObjectAttributes(), obj_ids);
        }

        public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            if (att == null)
            {
                att = doc.CreateDefaultAttributes();
            }

            var groupCagesId = doc.Groups.Add(Name + "-cages");
            var groupConnectorsId = doc.Groups.Add(Name + "-connectors");

            foreach (var connector in Connectors)
            {
                if (connector._valence == ModuleConnectorValence.External)
                {
                    var cageAttributes = att.Duplicate();
                    cageAttributes.ObjectColor = Configuration.CAGE_COLOR;
                    cageAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                    var faceId = doc.Objects.AddRectangle(connector._face, cageAttributes);
                    doc.Groups.AddToGroup(groupCagesId, faceId);
                    obj_ids.Add(faceId);
                    var dotAttributes = att.Duplicate();
                    dotAttributes.ObjectColor = Configuration.ColorBackgroundFromDirection(connector._direction);
                    dotAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                    var connectorId = doc.Objects.AddTextDot(connector._connectorIndex.ToString(),
                                                             connector._anchorPlane.Origin,
                                                             dotAttributes);
                    doc.Groups.AddToGroup(groupConnectorsId, connectorId);
                    obj_ids.Add(connectorId);
                }
            }
        }
    }

    public enum ModuleConnectorValence
    {
        Internal,
        External
    }

    public struct ModuleConnector
    {
        public string _moduleName;
        public string _submoduleName;
        public int _connectorIndex;
        public Direction _direction;
        public ModuleConnectorValence _valence;
        public Plane _anchorPlane;
        public Rectangle3d _face;

        public ModuleConnector(string moduleName,
                               string submoduleName,
                               int connectorIndex,
                               Direction direction,
                               ModuleConnectorValence valence,
                               Plane anchorPlane,
                               Rectangle3d face)
        {
            _moduleName = moduleName;
            _submoduleName = submoduleName;
            _connectorIndex = connectorIndex;
            _direction = direction;
            _valence = valence;
            _anchorPlane = anchorPlane;
            _face = face;
        }

        public override bool Equals(object obj)
        {
            return obj is ModuleConnector connector &&
                   _submoduleName == connector._submoduleName &&
                   _connectorIndex == connector._connectorIndex &&
                   EqualityComparer<Direction>.Default.Equals(_direction, connector._direction) &&
                   _valence == connector._valence &&
                   _anchorPlane.Equals(connector._anchorPlane) &&
                   EqualityComparer<Rectangle3d>.Default.Equals(_face, connector._face);
        }

        public override int GetHashCode()
        {
            var hashCode = -855668167;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_submoduleName);
            hashCode = hashCode * -1521134295 + _connectorIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + _direction.GetHashCode();
            hashCode = hashCode * -1521134295 + _valence.GetHashCode();
            hashCode = hashCode * -1521134295 + _anchorPlane.GetHashCode();
            hashCode = hashCode * -1521134295 + _face.GetHashCode();
            return hashCode;
        }
    }

    public struct Point3i
    {
        public int _x;
        public int _y;
        public int _z;

        public Point3i(int item1, int item2, int item3)
        {
            _x = item1;
            _y = item2;
            _z = item3;
        }

        public override bool Equals(object obj)
        {
            return obj is Point3i other &&
                   _x == other._x &&
                   _y == other._y &&
                   _z == other._z;
        }

        public override int GetHashCode()
        {
            var hashCode = 341329424;
            hashCode = hashCode * -1521134295 + _x.GetHashCode();
            hashCode = hashCode * -1521134295 + _y.GetHashCode();
            hashCode = hashCode * -1521134295 + _z.GetHashCode();
            return hashCode;
        }

        public void Deconstruct(out int item1, out int item2, out int item3)
        {
            item1 = _x;
            item2 = _y;
            item3 = _z;
        }
    }
}
