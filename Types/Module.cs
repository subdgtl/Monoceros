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
    /// <para>
    /// WFC Module type.
    /// </para>
    /// <para>
    /// The Module is a collection of cuboid submodules that should be 
    /// placed into <see cref="Slot"/>s of the world, complying the 
    /// specified <see cref="Rule"/>s.
    /// The existence of submodules is hidden from the Grasshopper API.
    /// Instead, the Module appears to be a collection of geometries 
    /// (0, 1 or n) wrapped into cuboid voxels. 
    /// </para>
    /// <para>
    /// In practice, the geometry and the submodules are not related 
    /// and can significantly differ and do not have to occupy 
    /// the same space.
    /// </para>
    /// <para>
    /// The class consists of input data, computed data, which will be later used by 
    /// <see cref="Rule"/> generators and parsers, the <see cref="ComponentFauxSolver"/> 
    /// and the <see cref="ComponentPostprocessor"/>.
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <term><see cref="Name"/></term>
    /// <description>Module name, used as a unique module identifier</description>
    /// </item>
    /// <item>
    /// <term><see cref="Geometry"/></term>
    /// <description>Geometry to be placed into the corresponding world <see cref="Slot"/>s</description>
    /// </item>
    /// <item>
    /// <term><see cref="BasePlane"/></term>
    /// <description>Base plane defining the module's coordinate system</description>
    /// </item>
    /// <item>
    /// <term><see cref="SubmoduleCenters"/></term>
    /// <description>Computed centers of submodules for module deconstruction and reconstruction</description>
    /// </item>
    /// <item>
    /// <term><see cref="SubmoduleNames"/></term>
    /// <description>Computed submodule names for the purposes of <see cref="ComponentFauxSolver"/> 
    /// and <see cref="ComponentPostprocessor"/></description>
    /// </item>
    /// <item>
    /// <term><see cref="Pivot"/></term>
    /// <description>Computed source plane for geometry placement. The Pivot is located in 
    /// the center of the first submodule and oriented so that 
    /// the geometry can be Oriented from the Pivot to the target 
    /// <see cref="Slot"/> center plane.</description>
    /// </item>
    /// <item>
    /// <term><see cref="PivotSubmoduleName"/></term>
    /// <description>Computed name of the submodule containing the Pivot. The entire contained
    /// geometry will be placed onto the center plane of a <see cref="Slot"/> allowing to contain 
    /// solely the PivotSubmoduleName.</description>
    /// </item>
    /// <item>
    /// <term><see cref="SlotDiagonal"/></term>
    /// <description>Dimensions of a single submodule - a cuboid voxel encapsulating the module.
    /// The filed's purpose is to check compatibility of various modules with the world they 
    /// should populate.</description>
    /// </item>
    /// <item>
    /// <term><see cref="Connectors"/></term>
    /// <description>Computed information about module connectors - 6 faces of each submodule, 
    /// through which the submodules may connect to each other.
    /// The connectors appear to belong to the main module. 
    /// The concept of submodules is not revealed in the Grasshopper API.</description>
    /// </item>
    /// <item>
    /// <term><see cref="InternalRules"/></term>
    /// <description>Explicit rules describing connections of submodules through their 
    /// internal connectors (faces). The internal rules hold the module's submodules together.
    /// The existence of the internal rules is hidden from the Grasshopper API. </description>
    /// </item>
    /// <item>
    /// <term><see cref="Continuous"/></term>
    /// <description>Check if the module submodules create a continuous blob. 
    /// If the module contains islands, then it is not continuous and the module will 
    /// never hold together.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// The class is designed as immutable, therefore all fields are read only. 
    /// </remarks>
    public class Module : IGH_Goo, IGH_PreviewData, IGH_BakeAwareObject
    {
        /// <summary>
        /// Module name, used as a unique module identifier.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Geometry to be placed into the corresponding world <see cref="Slot"/>s.
        /// </summary>
        public readonly List<GeometryBase> Geometry;

        /// <summary>
        /// Base plane defining the module's coordinate system.
        /// </summary>
        public readonly Plane BasePlane;

        /// <summary>
        /// Computed centers of submodules for module deconstruction and reconstruction.
        /// </summary>
        public readonly List<Point3i> SubmoduleCenters;

        /// <summary>
        /// Computed submodule names for the purposes of <see cref="ComponentFauxSolver"/> 
        /// and <see cref="ComponentPostprocessor"/>.
        /// </summary>
        public readonly List<string> SubmoduleNames;

        /// <summary>
        /// Computed source plane for geometry placement. The Pivot is located in 
        /// the center of the first submodule and oriented so that 
        /// the geometry can be Oriented from the Pivot to the target 
        /// <see cref="Slot"/>  center plane.
        /// </summary>
        public readonly Plane Pivot;

        /// <summary>
        /// Computed name of the submodule containing the Pivot. The entire contained
        /// geometry will be placed onto the center plane of a <see cref="Slot"/>, which allows
        /// the placement of solely the PivotSubmoduleName.
        /// </summary>
        public readonly string PivotSubmoduleName;

        // TODO: Consider squishing the modules to fit the world's slot dimensions.
        /// <summary>
        /// Dimensions of a single submodule - a cuboid voxel encapsulating the module.
        /// The filed's purpose is to check compatibility of various modules with the world they should populate.
        /// </summary>
        public readonly Vector3d SlotDiagonal;

        /// <summary>
        /// Computed information about module connectors - 6 faces of each submodule, 
        /// through which the submodules may connect to each other. 
        /// </summary>
        /// <remarks>
        /// The connectors appear to belong to the main module. 
        /// The concept of submodules is not revealed in the Grasshopper API. 
        /// </remarks>
        public readonly List<ModuleConnector> Connectors;

        /// <summary>
        /// Explicit rules describing connections of submodules through their internal connectors (faces). 
        /// The internal rules hold the module's submodules together.
        /// The existence of the internal rules is hidden from the Grasshopper API. 
        /// </summary>
        public readonly List<RuleExplicit> InternalRules;

        /// <summary>
        /// Check if the module submodules create a continuous blob. 
        /// If the module contains islands, then it is not continuous and the module will never hold together. 
        /// </summary>
        public readonly bool Continuous;

        /// <summary>
        /// Initializes a new instance of the <see cref="Module"/> class.
        /// </summary>
        /// <remarks>Required by Grasshopper; generates an invalid instance.</remarks>
        public Module()
        {
        }

        /// <summary>
        /// WFC <see cref="Module"/> constructor.
        /// </summary>
        /// <param name="name">Module name to be used as its unique identifier.</param>
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

            // Check if the slot diagonal is valid for the purposes of WFC
            if (slotDiagonal.X <= 0 || slotDiagonal.Y <= 0 || slotDiagonal.Z <= 0)
            {
                throw new Exception("One or more slot dimensions are not larger than 0");
            }

            SlotDiagonal = slotDiagonal;

            // TODO: Make a proper crawler to test the continuity
            Continuous = true;

            Name = name.ToLower() ?? throw new ArgumentNullException(nameof(name));
            Geometry = geometry.ToList() ?? throw new ArgumentNullException(nameof(geometry));
            BasePlane = basePlane.Clone();
            SubmoduleCenters = submoduleCenters;

            // Generate submodule names to be used as module names by the WFC solver
            SubmoduleNames = new List<string>();
            for (var i = 0; i < submoduleCenters.Count; i++)
            {
                SubmoduleNames.Add(name + i);
            }

            // Place the pivot into the first submodule and orient is according to the base plane 
            Pivot = basePlane.Clone();
            Pivot.Origin = new Point3d(submoduleCenters[0].X * slotDiagonal.X,
                                       submoduleCenters[0].Y * slotDiagonal.Y,
                                       submoduleCenters[0].Z * slotDiagonal.Z);

            // The name of the first submodule which should trigger the geometry placement
            PivotSubmoduleName = Name + 0;

            // The connectors describe faces of submodules and their relation to the entire module
            Connectors = ComputeModuleConnectors(submoduleCenters, SubmoduleNames, name, slotDiagonal, basePlane);

            // Generates internal rules holding the module (its submodules) together in the right order
            InternalRules = ComputeInternalRules(submoduleCenters);
        }

        /// <summary>
        /// Computes the module connectors.
        /// </summary>
        /// <param name="submoduleCenters">The submodule centers.</param>
        /// <param name="submoduleNames">The submodule names.</param>
        /// <param name="moduleName">The main module name.</param>
        /// <param name="slotDiagonal">The slot diagonal.</param>
        /// <param name="basePlane">Base plane defining the module's coordinate system.</param>
        /// <returns>A list of ModuleConnectors.</returns>
        private List<ModuleConnector> ComputeModuleConnectors(List<Point3i> submoduleCenters,
                                                              List<string> submoduleNames,
                                                              string moduleName,
                                                              Vector3d slotDiagonal,
                                                              Plane basePlane)
        {
            var moduleConnectors = new List<ModuleConnector>(submoduleCenters.Count * 6);

            // Precompute reusable values
            var directionXPositive = new Direction { _axis = Axis.X, _orientation = Orientation.Positive };
            var directionYPositive = new Direction { _axis = Axis.Y, _orientation = Orientation.Positive };
            var directionZPositive = new Direction { _axis = Axis.Z, _orientation = Orientation.Positive };
            var directionXNegative = new Direction { _axis = Axis.X, _orientation = Orientation.Negative };
            var directionYNegative = new Direction { _axis = Axis.Y, _orientation = Orientation.Negative };
            var directionZNegative = new Direction { _axis = Axis.Z, _orientation = Orientation.Negative };

            // Precompute reusable values
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
            // For each of the 6 submodule faces manually assign values
            for (var submoduleIndex = 0; submoduleIndex < submoduleCenters.Count; submoduleIndex++)
            {
                var center = submoduleCenters[submoduleIndex];
                var submoduleName = submoduleNames[submoduleIndex];
                var submoduleCenter = center.ToPoint3d();

                // Compute values for submodule face in the positive X direction
                var faceCenterXPositive = submoduleCenter + xPositiveVectorUnit * 0.5;
                faceCenterXPositive.Transform(baseAlignmentTransform);
                faceCenterXPositive.Transform(scalingTransform);
                // A plane oriented as the submodule face, placed in the face center
                var planeXPositive = new Plane(faceCenterXPositive, basePlane.YAxis, basePlane.ZAxis);
                // Rectangle around the submodule face. 
                // To be used for point tag detection and to be displayed in the viewport.
                var faceXPositive = new Rectangle3d(
                    planeXPositive,
                    new Interval(slotDiagonal.Y * (-0.5), slotDiagonal.Y * 0.5),
                    new Interval(slotDiagonal.Z * (-0.5), slotDiagonal.Z * 0.5));
                // Determines whether the connector is internal (touches another connector of the same module) 
                // or external (ready to touch a different instance of the same or different module).
                var valenceXPositive = submoduleCenters.Any(o => center.X - o.X == -1 && center.Y == o.Y && center.Z == o.Z) ?
                    ModuleConnectorValence.Internal :
                    ModuleConnectorValence.External;
                // Construct the actual connector
                var connectorXPositive = new ModuleConnector(
                    moduleName,
                    submoduleName,
                    submoduleIndex * 6 + directionXPositive.ToConnectorIndex(),
                    directionXPositive,
                    valenceXPositive,
                    planeXPositive,
                    faceXPositive);
                // And add it to the collection
                moduleConnectors.Add(connectorXPositive);

                // Continue with the remaining 5 connectors
                var faceCenterYPositive = submoduleCenter + yPositiveVectorUnit * 0.5;
                faceCenterYPositive.Transform(baseAlignmentTransform);
                faceCenterYPositive.Transform(scalingTransform);
                var planeYPositive = new Plane(faceCenterYPositive, basePlane.XAxis * (-1), basePlane.ZAxis);
                var faceYPositive = new Rectangle3d(
                    planeYPositive,
                    new Interval(slotDiagonal.X * (-0.5), slotDiagonal.X * 0.5),
                    new Interval(slotDiagonal.Z * (-0.5), slotDiagonal.Z * 0.5));
                var valenceYPositive = submoduleCenters.Any(o => center.Y - o.Y == -1 && center.X == o.X && center.Z == o.Z) ?
                    ModuleConnectorValence.Internal :
                    ModuleConnectorValence.External;
                var connectorYPositive = new ModuleConnector(
                    moduleName,
                    submoduleName,
                    submoduleIndex * 6 + directionYPositive.ToConnectorIndex(),
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
                var valenceZPositive = submoduleCenters.Any(o => center.Z - o.Z == -1 && center.X == o.X && center.Y == o.Y) ?
                    ModuleConnectorValence.Internal :
                    ModuleConnectorValence.External;
                var connectorZPositive = new ModuleConnector(
                    moduleName,
                    submoduleName,
                    submoduleIndex * 6 + directionZPositive.ToConnectorIndex(),
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
                var valenceXNegative = submoduleCenters.Any(o => center.X - o.X == 1 && center.Y == o.Y && center.Z == o.Z) ?
                    ModuleConnectorValence.Internal :
                    ModuleConnectorValence.External;
                var connectorXNegative = new ModuleConnector(
                    moduleName,
                    submoduleName,
                    submoduleIndex * 6 + directionXNegative.ToConnectorIndex(),
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
                var valenceYNegative = submoduleCenters.Any(o => center.Y - o.Y == 1 && center.X == o.X && center.Z == o.Z) ?
                    ModuleConnectorValence.Internal :
                    ModuleConnectorValence.External;
                var connectorYNegative = new ModuleConnector(
                    moduleName,
                    submoduleName,
                    submoduleIndex * 6 + directionYNegative.ToConnectorIndex(),
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
                var valenceZNegative = submoduleCenters.Any(o => center.Z - o.Z == 1 && center.X == o.X && center.X == o.X) ?
                    ModuleConnectorValence.Internal :
                    ModuleConnectorValence.External;
                var connectorZNegative = new ModuleConnector(
                    moduleName,
                    submoduleName,
                    submoduleIndex * 6 + directionZNegative.ToConnectorIndex(),
                    directionZNegative,
                    valenceZNegative,
                    planeZNegative,
                    faceZNegative);
                moduleConnectors.Add(connectorZNegative);
            }

            return moduleConnectors;
        }

        /// <summary>
        /// Computes the internal rules that hold the submodules of a module together.
        /// </summary>
        /// <param name="submoduleCenters">The submodule centers.</param>
        /// <returns>A list of RuleExplicits to be used only in the <see cref="ComponentFauxSolver"/>.</returns>
        private List<RuleExplicit> ComputeInternalRules(List<Point3i> submoduleCenters)
        {
            var rulesInternal = new List<RuleExplicit>();

            // For each submodule
            for (var thisIndex = 0; thisIndex < submoduleCenters.Count; thisIndex++)
            {
                var center = submoduleCenters[thisIndex];

                // Positive X neighbor
                // Check if there is a submodule in the positive X direction from the current submodule
                var otherIndexXPositive = submoduleCenters.FindIndex(o => center.X - o.X == -1 && center.Y == o.Y && center.Z == o.Z);
                // Ff there is, then the current connector is internal
                if (otherIndexXPositive != -1)
                {
                    // Add internal rule, that the current submodule connects to the neighbor through its positive X connector (face)
                    // Connector numbering convention: (submoduleIndex * 6) + faceIndex, where faceIndex is X=0, Y=1, Z=2, -X=3, -Y=4, -Z=5
                    rulesInternal.Add(new RuleExplicit(Name, thisIndex * 6 + 0, Name, otherIndexXPositive * 6 + 3));
                }

                // Positive Y neighbor
                var otherIndexYPositive = submoduleCenters.FindIndex(o => center.Y - o.Y == -1 && center.X == o.X && center.Z == o.Z);
                if (otherIndexYPositive != -1)
                {
                    rulesInternal.Add(new RuleExplicit(Name, thisIndex * 6 + 1, Name, otherIndexYPositive * 6 + 4));
                }

                // Positive Z neighbor
                var otherIndexZPositive = submoduleCenters.FindIndex(o => center.Z - o.Z == -1 && center.X == o.X && center.Y == o.Y);
                if (otherIndexZPositive != -1)
                {
                    rulesInternal.Add(new RuleExplicit(Name, thisIndex * 6 + 2, Name, otherIndexZPositive * 6 + 5));
                }
            }
            // No need to check neighbors in the negative orientation because 
            // all cases should be already covered by the positive orientation.
            return rulesInternal;
        }

        /// <summary>
        /// Returns only those connectors that are external. 
        /// The internal connectors are not revealed in the Grasshopper API.
        /// </summary>
        /// <returns>A list of ModuleConnectors.</returns>
        public IEnumerable<ModuleConnector> ExternalConnectors => Connectors.Where(c => c.Valence == ModuleConnectorValence.External);

        /// <summary>
        /// Check whether the point lies inside external connectors.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>A list of ModuleConnectors encompassing the point.</returns>
        public IEnumerable<ModuleConnector> GetExternalConnectorsContainingPoint(Point3d point) =>
            ExternalConnectors.Where(connector =>
                connector.AnchorPlane.DistanceTo(point) < RhinoMath.SqrtEpsilon &&
                connector.Face.Contains(point) == PointContainment.Inside
            );

        /// <summary>
        /// Gets a value indicating whether the module is valid.
        /// Required by Grasshopper.
        /// </summary>
        public bool IsValid =>
            Connectors != null &&
            Geometry != null &&
            InternalRules != null &&
            Name != null &&
            Pivot != null &&
            PivotSubmoduleName != null &&
            Connectors.Count > 0 &&
            Continuous;

        // TODO: Check whether this works
        /// <summary>
        /// Indicates why is the module not valid.
        /// Required by Grasshopper.
        /// </summary>
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

        /// <summary>
        /// Gets the type name.
        /// Required by Grasshopper.
        /// </summary>
        public string TypeName => "WFC Module";

        /// <summary>
        /// Gets the type description.
        /// Required by Grasshopper.
        /// </summary>
        public string TypeDescription => "WFC Module data.";

        /// <summary>
        /// Gets the clipping box for the module.
        /// Required by Grasshopper for viewport display and baking.
        /// </summary>
        BoundingBox IGH_PreviewData.ClippingBox
        {
            get
            {
                var unionBox = BoundingBox.Empty;
                foreach (var connector in Connectors)
                {
                    unionBox.Union(connector.Face.BoundingBox);
                }
                return unionBox;
            }
        }

        /// <summary>
        /// Does not cast any other type to Module.
        /// Required by Grasshopper.
        /// </summary>
        /// <param name="source">The source object.</param>
        /// <returns>A bool if the cast was successful.</returns>
        public bool CastFrom(object source) => false;

        /// <summary>
        /// Casts module to <see cref="ModuleName"/>.
        /// This is useful when reconstructing a module or defining a <see cref="Rule"/> 
        /// because the Module itself can be used instead of its name. 
        /// Required by Grasshopper.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>A bool if the cast was successful.</returns>
        public bool CastTo<T>(out T target)
        {
            if (IsValid && typeof(T) == typeof(ModuleName))
            {
                var moduleName = new ModuleName(Name);
                target = (T)moduleName.Duplicate();
                return true;
            }
            target = default;
            return false;
        }

        /// <summary>
        /// Duplicates the module.
        /// Required by Grasshopper.
        /// </summary>
        /// <returns>An IGH_Goo.</returns>
        public IGH_Goo Duplicate() => (IGH_Goo)MemberwiseClone();

        // TODO: Find out what this is
        /// <summary>
        /// Emits the proxy.
        /// Required by Grasshopper.
        /// </summary>
        /// <returns>An IGH_GooProxy.</returns>
        public IGH_GooProxy EmitProxy() => null;

        // TODO: Do this for real
        /// <summary>
        /// De-serialization.
        /// Required by Grasshopper for data internalization.
        /// </summary>
        /// <remarks>Not implemented yet.</remarks>
        /// <param name="reader">The reader.</param>
        /// <returns>A bool when successful.</returns>
        public bool Read(GH_IReader reader) => true;

        // TODO: Do this for real
        /// <summary>
        /// Serialization.
        /// Required by Grasshopper for data internalization.
        /// </summary>
        /// <remarks>Not implemented yet.</remarks>
        /// <param name="writer">The writer.</param>
        /// <returns>A bool when successful.</returns>
        public bool Write(GH_IWriter writer) => true;

        // TODO: Find out what this is and what should be done here
        /// <summary>
        /// Returns the script variable.
        /// Required by Grasshopper.
        /// </summary>
        /// <returns>An object.</returns>
        public object ScriptVariable() => this;

        /// <summary>
        /// Compiles a text describing the current module.
        /// Required by Grasshopper for data peeking.
        /// </summary>
        /// <remarks>This overrides any casting to string. Therefore it interferes 
        /// with automatic casting of a module to its name. The trade-off was either 
        /// simple use of the Module type data as its reference (as its name that serves
        /// as an UID) or having an informative description. Therefore there was a new 
        /// data type introduced: <see cref="ModuleName"/>. The module automatically casts
        /// to the ModuleName, which automatically casts to and from string. Therefore it is
        /// possible to use the module as its name and having a detailed description at 
        /// the same time.</remarks>
        /// <returns>A string description of the module.</returns>
        public override string ToString() =>
            "Module " + Name + " occupies " +
            Connectors.Count / 6 + " slots and has " +
            // TODO: Consider precomputing this number
            ExternalConnectors.ToList().Count + " connectors. " +
            (Continuous ?
            "The module is continuous." :
            "WARNING: The module is not continuous and therefore will not hold together.");

        /// <summary>
        /// Generates a named empty module with a single submodule.
        /// It is useful to generate reserved special modules with no geometry, 
        /// such as Out and Empty.
        /// </summary>
        /// <param name="name">Module name.</param>
        /// <param name="connectorType">The connector type.</param>
        /// <param name="slotDiagonal">The slot diagonal.</param>
        /// <param name="module">Outputs the generated module.</param>
        /// <param name="rulesExternal">Outputs typed rules of the module.</param>
        public static void GenerateNamedEmptySingleModule(string name,
                                                          string connectorType,
                                                          Vector3d slotDiagonal,
                                                          out Module module,
                                                          out List<RuleTyped> rulesExternal)
        {
            GenerateNamedEmptySingleModuleWithBasePlane(name,
                                                        connectorType,
                                                        Plane.WorldXY,
                                                        slotDiagonal,
                                                        out module,
                                                        out rulesExternal);
        }

        /// <summary>
        /// Generates a named empty module with a single submodule.
        /// It is useful to generate reserved special modules with no geometry, 
        /// such as Out and Empty.
        /// </summary>
        /// <param name="name">Module name.</param>
        /// <param name="connectorType">The connector type.</param>
        /// <param name="basePlane">Module's base plane.</param>
        /// <param name="slotDiagonal">The slot diagonal.</param>
        /// <param name="module">Outputs the generated module.</param>
        /// <param name="rulesExternal">Outputs typed rules of the module.</param>
        public static void GenerateNamedEmptySingleModuleWithBasePlane(string name,
                                                                       string connectorType,
                                                                       Plane basePlane,
                                                                       Vector3d slotDiagonal,
                                                                       out Module module,
                                                                       out List<RuleTyped> rulesExternal)
        {
            module = new Module(
                name,
                new List<GeometryBase>(),
                basePlane,
                new List<Point3i> { new Point3i(0, 0, 0) },
                slotDiagonal
                );
            rulesExternal = new List<RuleTyped>(6);
            for (var i = 0; i < 6; i++)
            {
                rulesExternal.Add(new RuleTyped(name, i, connectorType));
            }
        }

        /// <summary>
        /// Draws wired geometry representing the module to the viewport.
        /// Required by Grasshopper for data preview.
        /// </summary>
        /// <param name="args">Viewport arguments.</param>
        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            foreach (var geo in Geometry)
            {
                if (geo.ObjectType == ObjectType.Point)
                {
                    // Draw those geometries of the module that are a point
                    args.Pipeline.DrawPoint(((Point)geo).Location, args.Color);
                }
                if (geo.ObjectType == ObjectType.Curve)
                {
                    // Draw those geometries of the module that are a curve
                    args.Pipeline.DrawCurve((Curve)geo, args.Color);
                }
            }
            foreach (var externalConnector in ExternalConnectors)
            {
                // Draw connectors (together they look like a cage around the module)
                args.Pipeline.DrawPolyline(externalConnector.Face.ToPolyline(), Configuration.CAGE_COLOR);
                var anchorPosition = externalConnector.AnchorPlane.Origin;
                var dotColor = Configuration.ColorBackgroundFromDirection(externalConnector.Direction);
                var textColor = Configuration.ColorForegroundFromDirection(externalConnector.Direction);
                // Display connector index in the viewport
                args.Pipeline.DrawDot(anchorPosition, externalConnector.ConnectorIndex.ToString(), dotColor, textColor);
            }
        }

        /// <summary>
        /// Draws simply shaded geometry representing the module to the viewport.
        /// Required by Grasshopper for data preview.
        /// </summary>
        /// <param name="args">Viewport arguments.</param>
        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            foreach (var geo in Geometry)
            {
                if (geo.ObjectType == ObjectType.Brep)
                {
                    // Draw those geometries of the module that are a brep
                    args.Pipeline.DrawBrepShaded((Brep)geo, args.Material);
                }
                if (geo.ObjectType == ObjectType.Mesh)
                {
                    // Draw those geometries of the module that are a mesh
                    args.Pipeline.DrawMeshShaded((Mesh)geo, args.Material);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the module is bake-capable.
        /// Required by Grasshopper for baking.
        /// </summary>
        public bool IsBakeCapable => IsValid;

        /// <summary>
        /// Bakes the module helpers (cages and connector anchors).
        /// Required by Grasshopper for baking.
        /// </summary>
        /// <param name="doc">The Rhino doc.</param>
        /// <param name="obj_ids">The Guids of the baked objects.</param>
        public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            BakeGeometry(doc, new ObjectAttributes(), obj_ids);
        }

        /// <summary>
        /// Bakes the module helpers (cages and connector anchors).
        /// Required by Grasshopper for baking.
        /// </summary>
        /// <param name="doc">The Rhino doc.</param>
        /// <param name="att">Attributes of the baked objects.</param>
        /// <param name="obj_ids">The Guids of the baked objects.</param>
        public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            if (att == null)
            {
                att = doc.CreateDefaultAttributes();
            }

            // Put all cages into a Rhino group
            var groupCagesId = doc.Groups.Add(Name + "-cages");
            // Put all connector anchors into a Rhino group
            var groupConnectorsId = doc.Groups.Add(Name + "-connectors");

            foreach (var connector in Connectors)
            {
                if (connector.Valence == ModuleConnectorValence.External)
                {
                    var cageAttributes = att.Duplicate();
                    cageAttributes.ObjectColor = Configuration.CAGE_COLOR;
                    cageAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                    var faceId = doc.Objects.AddRectangle(connector.Face, cageAttributes);
                    doc.Groups.AddToGroup(groupCagesId, faceId);
                    obj_ids.Add(faceId);
                    var dotAttributes = att.Duplicate();
                    // Following the 3D modeling convention, the connectors in 
                    // direction X are Red, in Y are Green, in Z are Blue
                    dotAttributes.ObjectColor = Configuration.ColorBackgroundFromDirection(connector.Direction);
                    dotAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                    var connectorId = doc.Objects.AddTextDot(connector.ConnectorIndex.ToString(),
                                                             connector.AnchorPlane.Origin,
                                                             dotAttributes);
                    doc.Groups.AddToGroup(groupConnectorsId, connectorId);
                    obj_ids.Add(connectorId);
                }
            }
        }
    }

    /// <summary>
    /// The module connector valence. 
    /// Determines whether the connector has a submodule neighbor from
    /// the same module (internal) or is ready to have a neighbor from 
    /// a different instance or module (external)
    /// </summary>
    public enum ModuleConnectorValence
    {
        /// <summary>
        /// The connector does not have any neighbor from the same module instance.
        /// </summary>
        External,
        /// <summary>
        /// The connector does have a neighbor from the same module instance.
        /// </summary>
        Internal
    }

    /// <summary>
    /// The <see cref="Module"/>s and their submodules exist in a discrete 3D grid - the World. 
    /// The connectors are rectangular faces of a submodule cage.
    /// The connectors align with rectangular divisors of the World grid.
    /// <see cref="ModuleConnector"/> is a descriptor of a connector, that is able to connect
    /// (touch, became a neighbor of) to another connector. 
    /// It contains data to be used in <see cref="Rule"/> creation and parsing.
    /// <list type="bullet">
    /// <item>
    /// <term><see cref="ModuleName"/></term>
    /// <description>Name of the parent module containing the connector</description>
    /// </item>
    /// <item>
    /// <term><see cref="SubmoduleName"/></term>
    /// <description>Name of a submodule containing the connector</description>
    /// </item>
    /// <item>
    /// <term><see cref="ConnectorIndex"/></term>
    /// <description>Index of the current connector in the list of all module's connectors</description>
    /// </item>
    /// <item>
    /// <term><see cref="Direction"/></term>
    /// <description>Direction of the current connector in the orthogonal coordinate system 
    /// (defined by <see cref="Module.BasePlane"/>).</description>
    /// </item>
    /// <item>
    /// <term><see cref="Valence"/></term>
    /// <description>Determines whether the module is internal or external</description>
    /// </item>
    /// <item>
    /// <term><see cref="AnchorPlane"/></term>
    /// <description>A plane in the center of the connector oriented in the direction 
    /// of the connector (in world Cartesian coordinates)</description>
    /// </item>
    /// <item>
    /// <term><see cref="Face"/></term>
    /// <description>A rectangle around the connector - a face of 
    /// the cuboid representing the submodule</description>
    /// </item>
    /// </list>
    /// </summary>
    public struct ModuleConnector
    {
        /// <summary>
        /// Name of the parent module containing the connector
        /// </summary>
        public readonly string ModuleName;
        /// <summary>
        /// >Name of a submodule containing the connector
        /// </summary>
        public readonly string SubmoduleName;
        /// <summary>
        /// Index of the current connector in the list of all module's connectors
        /// </summary>
        public readonly int ConnectorIndex;
        /// <summary>
        /// Direction of the current connector in the orthogonal coordinate system
        /// </summary>
        public readonly Direction Direction;
        /// <summary>
        /// Determines whether the module is internal or external
        /// </summary>
        public readonly ModuleConnectorValence Valence;
        /// <summary>
        /// A plane in the center of the connector oriented in the direction 
        /// of the connector (in world Cartesian coordinates)
        /// </summary>
        public readonly Plane AnchorPlane;
        /// <summary>
        /// A rectangle around the connector - a face of 
        /// the cuboid representing the submodule
        /// </summary>
        public readonly Rectangle3d Face;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleConnector"/> class.
        /// </summary>
        /// <param name="moduleName">The parent module name.</param>
        /// <param name="submoduleName">The current submodule name.</param>
        /// <param name="connectorIndex">The connector index in the module's list of connectors.</param>
        /// <param name="direction">The connector direction.</param>
        /// <param name="valence">The connector valence.</param>
        /// <param name="anchorPlane">The connector anchor plane.</param>
        /// <param name="face">The connector face rectangle.</param>
        public ModuleConnector(string moduleName,
                               string submoduleName,
                               int connectorIndex,
                               Direction direction,
                               ModuleConnectorValence valence,
                               Plane anchorPlane,
                               Rectangle3d face)
        {
            ModuleName = moduleName;
            SubmoduleName = submoduleName;
            ConnectorIndex = connectorIndex;
            Direction = direction;
            Valence = valence;
            AnchorPlane = anchorPlane;
            Face = face;
        }

        /// <summary>
        /// Equals the.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj)
        {
            return obj is ModuleConnector connector &&
                   SubmoduleName == connector.SubmoduleName &&
                   ConnectorIndex == connector.ConnectorIndex &&
                   EqualityComparer<Direction>.Default.Equals(Direction, connector.Direction) &&
                   Valence == connector.Valence &&
                   AnchorPlane.Equals(connector.AnchorPlane) &&
                   EqualityComparer<Rectangle3d>.Default.Equals(Face, connector.Face);
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>An int.</returns>
        public override int GetHashCode()
        {
            var hashCode = -855668167;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SubmoduleName);
            hashCode = hashCode * -1521134295 + ConnectorIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + Direction.GetHashCode();
            hashCode = hashCode * -1521134295 + Valence.GetHashCode();
            hashCode = hashCode * -1521134295 + AnchorPlane.GetHashCode();
            hashCode = hashCode * -1521134295 + Face.GetHashCode();
            return hashCode;
        }
    }
}
