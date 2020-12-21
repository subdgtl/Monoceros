using System;
using System.Collections.Generic;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace WFCToolset
{

    /// <summary>
    /// WFC Module type containing name, geometry and submodule and connector info, 
    /// including internal rules holding the module together.
    /// </summary>
    public class Module : IGH_Goo, IGH_PreviewData
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
        /// Boxes representing the submodules of the module.
        /// Mainly for visualization purposes.
        /// </summary>
        public readonly List<Box> Cages;

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
        public Module(string name, List<GeometryBase> geometry, Plane basePlane, List<Point3i> submoduleCenters, Vector3d slotDiagonal)
        {
            // TODO: Fix basePlane mess-up

            // Check if any submodule centers are defined
            if (submoduleCenters.Count == 0)
            {
                throw new Exception("Submodule centers list is empty");
            }

            // Check if all the submodules are unique
            for (int i = 0; i < submoduleCenters.Count - 1; i++)
            {
                var center = submoduleCenters[i];
                for (int j = i + 1; j < submoduleCenters.Count; j++)
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

            Continuous = true;

            // TODO: Make a crawler to test the continuity
            if (submoduleCenters.Count > 1)
            {
                foreach (var current in submoduleCenters)
                {
                    if (!submoduleCenters.Any(other =>
                    (Math.Abs(current.X - other.X) == 1 && current.Y == other.Y && current.Z == other.Z) ||
                    (Math.Abs(current.Y - other.Y) == 1 && current.X == other.X && current.Z == other.Z) ||
                    (Math.Abs(current.Z - other.Z) == 1 && current.Y == other.Y && current.X == other.X)
                    ))
                    {
                        Continuous = false;
                        break;
                    }
                }
            }

            Name = name.ToLower() ?? throw new ArgumentNullException(nameof(name));
            Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));

            // Place the pivot into the first submodule and orient is according to the base plane 
            Pivot = basePlane.Clone();
            Pivot.Origin = new Point3d(submoduleCenters[0].X * slotDiagonal.X, submoduleCenters[0].Y * slotDiagonal.Y, submoduleCenters[0].Z * slotDiagonal.Z);
            // The name of the first submodule which should trigger the geometry placement
            PivotSubmoduleName = Name + 0;

            Connectors = ComputeModuleConnectors(submoduleCenters, slotDiagonal, basePlane);
            Cages = ComputeCages(submoduleCenters, slotDiagonal, basePlane);

            InternalRules = ComputeInternalRules(submoduleCenters);
        }

        public Module()
        {
        }

        private List<Box> ComputeCages(List<Point3i> submoduleCenters, Vector3d slotDiagonal, Plane basePlane)
        {
            var cages = new List<Box>(submoduleCenters.Count);

            var size = new Interval(-0.5, 0.5);
            // Orient to the base coordinate system
            Transform baseAlignmentTransform = Transform.PlaneToPlane(Plane.WorldXY, basePlane);
            // Scale up to slot size
            Transform scalingTransform = Transform.Scale(basePlane, slotDiagonal.X, slotDiagonal.Y, slotDiagonal.Z);

            foreach (var center in submoduleCenters)
            {
                var submoduleCenter = new Point3d(center.X, center.Y, center.Z);
                var cagePlane = Plane.WorldXY;
                cagePlane.Origin = submoduleCenter;
                var cage = new Box(cagePlane, size, size, size);
                cage.Transform(baseAlignmentTransform);
                cage.Transform(scalingTransform);
                cages.Add(cage);
            }

            return cages;
        }

        private List<ModuleConnector> ComputeModuleConnectors(List<Point3i> submoduleCenters, Vector3d slotDiagonal, Plane basePlane)
        {
            var moduleConnectors = new List<ModuleConnector>(submoduleCenters.Count * 6);

            var directionXPositive = new Direction { Axis = Axis.X, Orientation = Orientation.Positive };
            var directionYPositive = new Direction { Axis = Axis.Y, Orientation = Orientation.Positive };
            var directionZPositive = new Direction { Axis = Axis.Z, Orientation = Orientation.Positive };
            var directionXNegative = new Direction { Axis = Axis.X, Orientation = Orientation.Negative };
            var directionYNegative = new Direction { Axis = Axis.Y, Orientation = Orientation.Negative };
            var directionZNegative = new Direction { Axis = Axis.Z, Orientation = Orientation.Negative };

            directionXPositive.ToVector(out var xPositiveVectorUnit);
            directionYPositive.ToVector(out var yPositiveVectorUnit);
            directionZPositive.ToVector(out var zPositiveVectorUnit);
            directionXNegative.ToVector(out var xNegativeVectorUnit);
            directionYNegative.ToVector(out var yNegativeVectorUnit);
            directionZNegative.ToVector(out var zNegativeVectorUnit);

            // Orient to the base coordinate system
            Transform baseAlignmentTransform = Transform.PlaneToPlane(Plane.WorldXY, basePlane);
            // Scale up to slot size
            Transform scalingTransform = Transform.Scale(basePlane, slotDiagonal.X, slotDiagonal.Y, slotDiagonal.Z);

            // Connector numbering convention: (submoduleIndex * 6) + faceIndex, where faceIndex is X=0, Y=1, Z=2, -X=3, -Y=4, -Z=5

            for (int submoduleIndex = 0; submoduleIndex < submoduleCenters.Count; submoduleIndex++)
            {
                var center = submoduleCenters[submoduleIndex];
                var submoduleName = Name + submoduleIndex;
                var submoduleCenter = new Point3d(center.X, center.Y, center.Z);

                var faceCenterXPositive = submoduleCenter + xPositiveVectorUnit * 0.5;
                faceCenterXPositive.Transform(baseAlignmentTransform);
                faceCenterXPositive.Transform(scalingTransform);
                if (submoduleCenters.Any(o => center.X - o.X == -1 && center.Y == o.Y && center.Z == o.Z))
                {
                    moduleConnectors.Add(new ModuleConnector(submoduleName, submoduleIndex * 6 + 0, directionXPositive, ModuleConnectorValence.Internal, faceCenterXPositive));
                }
                else
                {
                    moduleConnectors.Add(new ModuleConnector(submoduleName, submoduleIndex * 6 + 0, directionXPositive, ModuleConnectorValence.External, faceCenterXPositive));
                }

                var faceCenterYPositive = submoduleCenter + yPositiveVectorUnit * 0.5;
                faceCenterYPositive.Transform(baseAlignmentTransform);
                faceCenterYPositive.Transform(scalingTransform);
                if (submoduleCenters.Any(o => center.Y - o.Y == -1 && center.X == o.X && center.Z == o.Z))
                {
                    moduleConnectors.Add(new ModuleConnector(submoduleName, submoduleIndex * 6 + 1, directionYPositive, ModuleConnectorValence.Internal, faceCenterYPositive));
                }
                else
                {
                    moduleConnectors.Add(new ModuleConnector(submoduleName, submoduleIndex * 6 + 1, directionYPositive, ModuleConnectorValence.External, faceCenterYPositive));
                }

                var faceCenterZPositive = submoduleCenter + zPositiveVectorUnit * 0.5;
                faceCenterZPositive.Transform(baseAlignmentTransform);
                faceCenterZPositive.Transform(scalingTransform);
                if (submoduleCenters.Any(o => center.Z - o.Z == -1 && center.X == o.X && center.Y == o.Y))
                {
                    moduleConnectors.Add(new ModuleConnector(submoduleName, submoduleIndex * 6 + 2, directionZPositive, ModuleConnectorValence.Internal, faceCenterZPositive));
                }
                else
                {
                    moduleConnectors.Add(new ModuleConnector(submoduleName, submoduleIndex * 6 + 2, directionZPositive, ModuleConnectorValence.External, faceCenterZPositive));
                }

                var faceCenterXNegative = submoduleCenter + xNegativeVectorUnit * 0.5;
                faceCenterXNegative.Transform(baseAlignmentTransform);
                faceCenterXNegative.Transform(scalingTransform);
                if (submoduleCenters.Any(o => center.X - o.X == 1 && center.Y == o.Y && center.Z == o.Z))
                {
                    moduleConnectors.Add(new ModuleConnector(submoduleName, submoduleIndex * 6 + 3, directionXNegative, ModuleConnectorValence.Internal, faceCenterXNegative));
                }
                else
                {
                    moduleConnectors.Add(new ModuleConnector(submoduleName, submoduleIndex * 6 + 3, directionXNegative, ModuleConnectorValence.External, faceCenterXNegative));
                }

                var faceCenterYNegative = submoduleCenter + yNegativeVectorUnit * 0.5;
                faceCenterYNegative.Transform(baseAlignmentTransform);
                faceCenterYNegative.Transform(scalingTransform);
                if (submoduleCenters.Any(o => center.Y - o.Y == 1 && center.X == o.X && center.Z == o.Z))
                {
                    moduleConnectors.Add(new ModuleConnector(submoduleName, submoduleIndex * 6 + 4, directionYNegative, ModuleConnectorValence.Internal, faceCenterYNegative));
                }
                else
                {
                    moduleConnectors.Add(new ModuleConnector(submoduleName, submoduleIndex * 6 + 4, directionYNegative, ModuleConnectorValence.External, faceCenterYNegative));
                }

                var faceCenterZNegative = submoduleCenter + zNegativeVectorUnit * 0.5;
                faceCenterZNegative.Transform(baseAlignmentTransform);
                faceCenterZNegative.Transform(scalingTransform);
                if (submoduleCenters.Any(o => center.Z - o.Z == 1 && center.X == o.X && center.X == o.X))
                {
                    moduleConnectors.Add(new ModuleConnector(submoduleName, submoduleIndex * 6 + 5, directionZNegative, ModuleConnectorValence.Internal, faceCenterZNegative));
                }
                else
                {
                    moduleConnectors.Add(new ModuleConnector(submoduleName, submoduleIndex * 6 + 5, directionZNegative, ModuleConnectorValence.External, faceCenterZNegative));
                }
            }

            return moduleConnectors;
        }

        private List<RuleExplicit> ComputeInternalRules(List<Point3i> submoduleCenters)
        {
            var rulesInternal = new List<RuleExplicit>();

            for (int thisIndex = 0; thisIndex < submoduleCenters.Count; thisIndex++)
            {
                var center = submoduleCenters[thisIndex];

                // Connector numbering convention: (submoduleIndex * 6) + faceIndex, where faceIndex is X=0, Y=1, Z=2, -X=3, -Y=4, -Z=5
                var otherIndexXPositive = submoduleCenters.FindIndex(o => center.X - o.X == -1 && center.Y == o.Y && center.Z == o.Z);
                if (otherIndexXPositive != -1)
                {
                    rulesInternal.Add(new RuleExplicit(Name, thisIndex * 6 + 0, Name, otherIndexXPositive * 6 + 3));
                    continue;
                }

                var otherIndexYPositive = submoduleCenters.FindIndex(o => center.Y - o.Y == -1 && center.X == o.X && center.Z == o.Z);
                if (otherIndexYPositive != -1)
                {
                    rulesInternal.Add(new RuleExplicit(Name, thisIndex * 6 + 1, Name, otherIndexYPositive * 6 + 4));
                    continue;
                }

                var otherIndexZPositive = submoduleCenters.FindIndex(o => center.Z - o.Z == -1 && center.X == o.X && center.Y == o.Y);
                if (otherIndexZPositive != -1)
                {
                    rulesInternal.Add(new RuleExplicit(Name, thisIndex * 6 + 2, Name, otherIndexZPositive * 6 + 5));
                    continue;
                }
            }
            return rulesInternal;
        }

        public IEnumerable<ModuleConnector> GetExternalConnectors()
        {
            return Connectors.Where(c => c.Valence == ModuleConnectorValence.External);
        }

        // TODO: check why this says it is valid even if it's not
        public bool IsValid =>
            Cages != null &&
            Connectors != null &&
            Geometry != null &&
            InternalRules != null &&
            Name != null &&
            Pivot != null &&
            PivotSubmoduleName != null &&
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
                var bBoxes = Cages.Select(cage => cage.BoundingBox);
                var unionBox = BoundingBox.Empty;
                foreach (var bBox in bBoxes)
                {
                    unionBox.Union(bBox);
                }
                return unionBox;
            }
        }

        public bool CastFrom(object source) => false;

        public bool CastTo<T>(out T target)
        {
            target = default;
            return false;
        }

        public IGH_Goo Duplicate()
        {
            return (IGH_Goo)this.MemberwiseClone();

        }

        public IGH_GooProxy EmitProxy()
        {
            return (IGH_GooProxy)null;
        }

        // TODO: Do this for real
        public bool Read(GH_IReader reader) => true;
        public bool Write(GH_IWriter writer) => true;

        public object ScriptVariable() => this;

        public override string ToString() =>
            "Module " + Name + " occupies " + Cages.Count + " slots. " +
            (Continuous ?
            "The module is continous." :
            "WARNING: The module is not continuous and therefore will not hold together.");


        // This is to generate Out and Empty modules with Indifferent connector type
        public static void GenerateNamedEmptySingleModule(string name, string connectorType, Vector3d slotDiagonal, out Module module, out List<RuleTyped> rulesExternal)
        {
            module = new Module(
                name,
                new List<GeometryBase>(),
                Plane.WorldXY,
                new List<Point3i> { new Point3i(0, 0, 0) },
                slotDiagonal
                );
            rulesExternal = new List<RuleTyped>(6);
            for (int i = 0; i < 6; i++)
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
            foreach (var box in Cages)
            {
                args.Pipeline.DrawBrepWires(box.ToBrep(), System.Drawing.Color.FromArgb(64, 255, 255, 255), 0);
            }
            foreach (var connector in Connectors)
            {
                if (connector.Valence == ModuleConnectorValence.External)
                {
                    Point3d anchorPosition = connector.AnchorPosition;
                    args.Pipeline.DrawDot(anchorPosition, connector.ConnectorIndex.ToString(), System.Drawing.Color.FromArgb(64, 255, 255, 255), System.Drawing.Color.Black);
                }
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
    }

    public enum ModuleConnectorValence
    {
        Internal,
        External
    }

    public struct ModuleConnector
    {
        public string SubmoduleName;
        public int ConnectorIndex;
        public Direction Direction;
        public ModuleConnectorValence Valence;
        public Point3d AnchorPosition;

        public ModuleConnector(string submoduleName, int connectorIndex, Direction direction, ModuleConnectorValence valence, Point3d anchorPosition)
        {
            SubmoduleName = submoduleName;
            ConnectorIndex = connectorIndex;
            Direction = direction;
            Valence = valence;
            AnchorPosition = anchorPosition;
        }

        public override bool Equals(object obj)
        {
            return obj is ModuleConnector connector &&
                   SubmoduleName == connector.SubmoduleName &&
                   ConnectorIndex == connector.ConnectorIndex &&
                   EqualityComparer<Direction>.Default.Equals(Direction, connector.Direction) &&
                   Valence == connector.Valence &&
                   AnchorPosition.Equals(connector.AnchorPosition);
        }

        public override int GetHashCode()
        {
            int hashCode = 1363960438;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SubmoduleName);
            hashCode = hashCode * -1521134295 + ConnectorIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + Direction.GetHashCode();
            hashCode = hashCode * -1521134295 + Valence.GetHashCode();
            hashCode = hashCode * -1521134295 + AnchorPosition.GetHashCode();
            return hashCode;
        }
    }

    public struct Point3i
    {
        public int X;
        public int Y;
        public int Z;

        public Point3i(int item1, int item2, int item3)
        {
            X = item1;
            Y = item2;
            Z = item3;
        }

        public override bool Equals(object obj)
        {
            return obj is Point3i other &&
                   X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }

        public override int GetHashCode()
        {
            int hashCode = 341329424;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + Z.GetHashCode();
            return hashCode;
        }

        public void Deconstruct(out int item1, out int item2, out int item3)
        {
            item1 = X;
            item2 = Y;
            item3 = Z;
        }
    }
}
