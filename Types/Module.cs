using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Monoceros {
    /// <summary>
    /// <para>
    /// Monoceros Module type.
    /// </para>
    /// <para>
    /// The Module is a collection of cuboid parts that should be placed into
    /// <see cref="Slot"/>s of the world, complying the specified
    /// <see cref="Rule"/>s. The existence of parts is hidden from the
    /// Grasshopper API. Instead, the Module appears to be a collection of
    /// geometries (0, 1 or n) wrapped into cuboid voxels. 
    /// </para>
    /// <para>
    /// In practice, the geometry and the parts are not related and can
    /// significantly differ and do not have to occupy the same space.
    /// </para>
    /// <para>
    /// The class consists of input data, computed data, which will be later
    /// used by <see cref="Rule"/> generators and parsers, the
    /// <see cref="ComponentSolver"/> and the
    /// <see cref="ComponentMaterializeSlot"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The class is designed as immutable, therefore all fields are read only. 
    /// </remarks>
    [Serializable]
    public class Module : IGH_Goo, IGH_PreviewData, IGH_BakeAwareObject {
        /// <summary>
        /// Module name, used as a unique module identifier.
        /// </summary>
        public string Name;

        /// <summary>
        /// Geometry to be placed into the corresponding world
        /// <see cref="Slot"/>s.
        /// </summary>
        public List<GeometryBase> Geometry;

        /// <summary>
        /// Geometry to be placed into the corresponding world
        /// <see cref="Slot"/>s.
        /// </summary>
        public List<GeometryBase> ReferencedGeometry;

        /// <summary>
        /// Guids of referenced geometry to be placed into the corresponding
        /// world <see cref="Slot"/>s.
        /// </summary>
        public List<Guid> ReferencedGeometryGuids;

        /// <summary>
        /// Base plane defining the module's coordinate system.
        /// </summary>
        public Plane BasePlane;

        /// <summary>
        /// Computed centers of parts for module deconstruction and
        /// reconstruction.
        /// </summary>
        public List<Point3i> PartCenters;

        /// <summary>
        /// Computed part names for the purposes of
        /// <see cref="ComponentSolver"/> and
        /// <see cref="ComponentMaterializeSlot"/>.
        /// </summary>
        public List<string> PartNames;

        /// <summary>
        /// Computed source plane for geometry placement. The Pivot is located
        /// in the center of the first part and oriented so that the geometry
        /// can be Oriented from the Pivot to the target <see cref="Slot"/>
        /// center plane.
        /// </summary>
        public Plane Pivot;

        /// <summary>
        /// Computed name of the part containing the Pivot. The entire contained
        /// geometry will be placed onto the center plane of a
        /// <see cref="Slot"/>, which allows the placement of solely the
        /// PivotpartName.
        /// </summary>
        public string PivotPartName;

        // TODO: Consider squishing the modules to fit the world's slot dimensions.
        /// <summary>
        /// Dimensions of a single part - a cuboid voxel encapsulating the
        /// module. The filed's purpose is to check compatibility of various
        /// modules with the world they should populate.
        /// </summary>
        public Vector3d PartDiagonal;

        /// <summary>
        /// Computed information about module connectors - those of the 6 faces
        /// of each part that are external. The <see cref="Module"/>s may
        /// connect to each other via the <see cref="Connectors"/>. 
        /// </summary>
        /// <remarks>
        /// The connectors appear to belong to the main module.  The concept of
        /// parts is not revealed in the Grasshopper API. 
        /// </remarks>
        public List<ModuleConnector> Connectors;

        /// <summary>
        /// Rules in the solver format describing connections of parts through
        /// their internal connectors (faces).  The internal rules hold the
        /// module's parts together. The existence of the internal rules is
        /// hidden from the Grasshopper API. 
        /// </summary>
        public List<RuleForSolver> InternalRules;

        /// <summary>
        /// Initializes a new instance of the <see cref="Module"/> class.
        /// </summary>
        /// <remarks>
        /// Required by Grasshopper; generates an invalid instance.
        /// </remarks>
        public Module( ) {
        }

        /// <summary>
        /// Monoceros <see cref="Module"/> constructor.
        /// </summary>
        /// <param name="name">Module name to be used as its unique identifier.
        ///     </param>
        /// <param name="geometry">Contains geometry to be placed into the
        ///     module.  The geometry is not related to the module's parts,
        ///     which means it does not have to respect the module boundaries,
        ///     nor fill all parts.</param>
        /// <param name="basePlane">The base plane of the module, defining its
        ///     coordinate system.  It will be used to display part cages and to
        ///     orient the geometry into the Monoceros world slots.</param>
        /// <param name="partCenters">Centers of the parts in integer coordinate
        ///     system.  Each unit represents one slot. The coordinate system
        ///     origin and orientation is defined by the basePlane. partCenters
        ///     are the only source of information about the module's dimensions
        ///     and occupied parts.</param>
        /// <param name="partDiagonal">Dimension of a single world slot.</param>
        public Module(string name,
                      IEnumerable<GeometryBase> geometry,
                      IEnumerable<GeometryBase> referencedGeometry,
                      IEnumerable<Guid> geometryGuids,
                      Plane basePlane,
                      List<Point3i> partCenters,
                      Vector3d partDiagonal) {
            // Check if any part centers are defined
            if (!partCenters.Any()) {
                throw new Exception("part centers list is empty");
            }

            // Check if all the parts are unique
            if (partCenters.Count != partCenters.Distinct().ToList().Count) {
                throw new Exception("Part centers are repetitive");
            }

            // Check if the slot diagonal is valid for the purposes of Monoceros
            if (partDiagonal.X <= 0 || partDiagonal.Y <= 0 || partDiagonal.Z <= 0) {
                throw new Exception("One or more slot dimensions are not larger than 0");
            }

            PartDiagonal = partDiagonal;

            Name = name.ToLower();
            Geometry = geometry.ToList();
            ReferencedGeometry = referencedGeometry.ToList();
            ReferencedGeometryGuids = geometryGuids.ToList();
            BasePlane = basePlane.Clone();
            PartCenters = partCenters;

            // Generate part names to be used as module names by the Monoceros solver
            PartNames = new List<string>();
            for (var i = 0; i < partCenters.Count; i++) {
                PartNames.Add(name + i);
            }

            // Place the pivot into the first part and orient is according to the base plane 
            var pivot = BasePlane.Clone();

            pivot.Origin = PartCenters[0].ToCartesian(BasePlane, PartDiagonal);
            Pivot = pivot;
            // The name of the first part which should trigger the geometry placement
            PivotPartName = Name + 0;

            // The connectors describe faces of parts and their relation to the entire module
            Connectors = ComputeModuleConnectors(PartCenters,
                                                 PartNames,
                                                 Name,
                                                 PartDiagonal,
                                                 BasePlane);

            // Generates internal rules holding the module (its parts) together
            InternalRules = ComputeInternalRules(PartCenters, PartNames);
        }

        private bool CheckConsistency(List<Point3i> centers) {
            var visited = Enumerable.Repeat(false, centers.Count).ToList();
            var stack = new List<Point3i>() { centers[0] };
            visited[0] = true;
            var i = 0;
            while (i < stack.Count) {
                var current = stack[i];
                for (var j = 0; j < centers.Count; j++) {
                    var other = centers[j];
                    if (current.IsNeighbor(other) && !visited[j]) {
                        stack.Add(other);
                        visited[j] = true;
                    }
                }
                i++;
            }
            return visited.All(wasVisited => wasVisited);
        }

        /// <summary>
        /// Computes the module connectors.
        /// </summary>
        /// <param name="partCenters">The part centers.</param>
        /// <param name="partNames">The part names.</param>
        /// <param name="moduleName">The main module name.</param>
        /// <param name="partDiagonal">The slot diagonal.</param>
        /// <param name="basePlane">Base plane defining the module's coordinate
        ///     system.</param>
        /// <returns>A list of ModuleConnectors.</returns>
        private List<ModuleConnector> ComputeModuleConnectors(List<Point3i> partCenters,
                                                              List<string> partNames,
                                                              string moduleName,
                                                              Vector3d partDiagonal,
                                                              Plane basePlane) {
            var moduleConnectors = new List<ModuleConnector>(partCenters.Count * 6);

            // Precompute reusable values
            var directionXPositive = new Direction(Axis.X, Orientation.Positive);
            var directionYPositive = new Direction(Axis.Y, Orientation.Positive);
            var directionZPositive = new Direction(Axis.Z, Orientation.Positive);
            var directionXNegative = new Direction(Axis.X, Orientation.Negative);
            var directionYNegative = new Direction(Axis.Y, Orientation.Negative);
            var directionZNegative = new Direction(Axis.Z, Orientation.Negative);

            var scalingTransform = Transform.Scale(basePlane,
                                                   partDiagonal.X,
                                                   partDiagonal.Y,
                                                   partDiagonal.Z);

            // Precompute reusable values
            var xPositiveVectorUnit = directionXPositive.ToVector();
            var yPositiveVectorUnit = directionYPositive.ToVector();
            var zPositiveVectorUnit = directionZPositive.ToVector();
            var xNegativeVectorUnit = directionXNegative.ToVector();
            var yNegativeVectorUnit = directionYNegative.ToVector();
            var zNegativeVectorUnit = directionZNegative.ToVector();

            xPositiveVectorUnit.Transform(scalingTransform);
            yPositiveVectorUnit.Transform(scalingTransform);
            zPositiveVectorUnit.Transform(scalingTransform);
            xNegativeVectorUnit.Transform(scalingTransform);
            yNegativeVectorUnit.Transform(scalingTransform);
            zNegativeVectorUnit.Transform(scalingTransform);

            // Orient to the base coordinate system
            var baseAlignmentTransform = Transform.PlaneToPlane(Plane.WorldXY, basePlane);
            // Scale up to slot size
            // Connector numbering convention: 
            // (partIndex * 6) + faceIndex, where faceIndex is X=0, Y=1, Z=2, -X=3, -Y=4, -Z=5
            // For each of the 6 part faces manually assign values
            for (var partIndex = 0; partIndex < partCenters.Count; partIndex++) {
                var center = partCenters[partIndex];
                var partName = partNames[partIndex];
                var partCenter = center.ToCartesian(basePlane, partDiagonal);

                // Compute values for part face in the positive X direction

                // Determines whether the connector is internal (touches another connector of 
                // the same module) or external (ready to touch a different instance of the same 
                // or different module).
                // Only store if external.
                var isInternalXPositive = partCenters
                    .Any(o => center.X - o.X == -1 && center.Y == o.Y && center.Z == o.Z);
                if (!isInternalXPositive) {
                    var faceCenterXPositive = partCenter + xPositiveVectorUnit * 0.5;
                    // A plane oriented as the part face, placed in the face center
                    var planeXPositive = new Plane(faceCenterXPositive,
                                                   basePlane.YAxis,
                                                   basePlane.ZAxis);
                    // Rectangle around the part face. 
                    // To be used for point tag detection and to be displayed in the viewport.
                    var faceXPositive = new Rectangle3d(
                        planeXPositive,
                        new Interval(partDiagonal.Y * (-0.5), partDiagonal.Y * 0.5),
                        new Interval(partDiagonal.Z * (-0.5), partDiagonal.Z * 0.5));
                    // Construct the actual connector
                    var connectorXPositive = new ModuleConnector(
                        moduleName,
                        partName,
                        directionXPositive,
                        planeXPositive,
                        faceXPositive);
                    // And add it to the collection
                    moduleConnectors.Add(connectorXPositive);
                }

                // Continue with the remaining 5 connectors
                var isInternalYPositive = partCenters
                    .Any(o => center.Y - o.Y == -1 && center.X == o.X && center.Z == o.Z);
                if (!isInternalYPositive) {
                    var faceCenterYPositive = partCenter + yPositiveVectorUnit * 0.5;
                    var planeYPositive = new Plane(faceCenterYPositive,
                                                   basePlane.XAxis * (-1),
                                                   basePlane.ZAxis);
                    var faceYPositive = new Rectangle3d(
                        planeYPositive,
                        new Interval(partDiagonal.X * (-0.5), partDiagonal.X * 0.5),
                        new Interval(partDiagonal.Z * (-0.5), partDiagonal.Z * 0.5));
                    var connectorYPositive = new ModuleConnector(
                        moduleName,
                        partName,
                        directionYPositive,
                        planeYPositive,
                        faceYPositive);
                    moduleConnectors.Add(connectorYPositive);
                }


                var isInternalZPositive = partCenters
                    .Any(o => center.Z - o.Z == -1 && center.X == o.X && center.Y == o.Y);
                if (!isInternalZPositive) {
                    var faceCenterZPositive = partCenter + zPositiveVectorUnit * 0.5;
                    var planeZPositive = new Plane(faceCenterZPositive,
                                                   basePlane.XAxis,
                                                   basePlane.YAxis);
                    var faceZPositive = new Rectangle3d(
                        planeZPositive,
                        new Interval(partDiagonal.X * (-0.5), partDiagonal.X * 0.5),
                        new Interval(partDiagonal.Y * (-0.5), partDiagonal.Y * 0.5));
                    var connectorZPositive = new ModuleConnector(
                        moduleName,
                        partName,
                        directionZPositive,
                        planeZPositive,
                        faceZPositive);
                    moduleConnectors.Add(connectorZPositive);
                }


                var isInternalXNegative = partCenters
                    .Any(o => center.X - o.X == 1 && center.Y == o.Y && center.Z == o.Z);
                if (!isInternalXNegative) {
                    var faceCenterXNegative = partCenter + xNegativeVectorUnit * 0.5;
                    var planeXNegative = new Plane(faceCenterXNegative,
                                                   basePlane.YAxis * (-1),
                                                   basePlane.ZAxis);
                    var faceXNegative = new Rectangle3d(
                        planeXNegative,
                        new Interval(partDiagonal.Y * (-0.5), partDiagonal.Y * 0.5),
                        new Interval(partDiagonal.Z * (-0.5), partDiagonal.Z * 0.5));
                    var connectorXNegative = new ModuleConnector(
                        moduleName,
                        partName,
                        directionXNegative,
                        planeXNegative,
                        faceXNegative);
                    moduleConnectors.Add(connectorXNegative);
                }


                var isInternalYNegative = partCenters
                    .Any(o => center.Y - o.Y == 1 && center.X == o.X && center.Z == o.Z);
                if (!isInternalYNegative) {
                    var faceCenterYNegative = partCenter + yNegativeVectorUnit * 0.5;
                    var planeYNegative = new Plane(faceCenterYNegative,
                                                   basePlane.XAxis,
                                                   basePlane.ZAxis);
                    var faceYNegative = new Rectangle3d(
                        planeYNegative,
                        new Interval(partDiagonal.X * (-0.5), partDiagonal.X * 0.5),
                        new Interval(partDiagonal.Z * (-0.5), partDiagonal.Z * 0.5));
                    var connectorYNegative = new ModuleConnector(
                        moduleName,
                        partName,
                        directionYNegative,
                        planeYNegative,
                        faceYNegative);
                    moduleConnectors.Add(connectorYNegative);
                }

                var isInternalZNegative = partCenters
                    .Any(o => center.Z - o.Z == 1 && center.X == o.X && center.Y == o.Y);
                if (!isInternalZNegative) {
                    var faceCenterZNegative = partCenter + zNegativeVectorUnit * 0.5;
                    var planeZNegative = new Plane(faceCenterZNegative,
                                                   basePlane.XAxis * (-1),
                                                   basePlane.YAxis);
                    var faceZNegative = new Rectangle3d(
                        planeZNegative,
                        new Interval(partDiagonal.X * (-0.5), partDiagonal.X * 0.5),
                        new Interval(partDiagonal.Y * (-0.5), partDiagonal.Y * 0.5));
                    var connectorZNegative = new ModuleConnector(
                        moduleName,
                        partName,
                        directionZNegative,
                        planeZNegative,
                        faceZNegative);
                    moduleConnectors.Add(connectorZNegative);
                }
            }

            return moduleConnectors;
        }

        /// <summary>
        /// Computes the internal rules that hold the parts of a module
        /// together.
        /// </summary>
        /// <param name="partCenters">The part centers.</param>
        /// <returns>A list of RuleExplicits to be used only in the
        ///     <see cref="ComponentSolver"/>.</returns>
        private List<RuleForSolver> ComputeInternalRules(List<Point3i> partCenters,
                                                         List<string> partNames) {
            var rulesInternal = new List<RuleForSolver>();

            // For each part
            for (var thisIndex = 0; thisIndex < partCenters.Count; thisIndex++) {
                var center = partCenters[thisIndex];

                // Positive X neighbor
                // Check if there is a part in the positive X direction from the current part
                var otherIndexXPositive = partCenters
                    .FindIndex(o => center.X - o.X == -1 && center.Y == o.Y && center.Z == o.Z);
                // Ff there is, then the current connector is internal
                if (otherIndexXPositive != -1) {
                    // Add internal rule, that the current part connects to the neighbor 
                    // through its positive X connector (face)
                    rulesInternal.Add(new RuleForSolver(Axis.X,
                                                        partNames[thisIndex],
                                                        partNames[otherIndexXPositive]));
                }

                // Positive Y neighbor
                var otherIndexYPositive = partCenters
                    .FindIndex(o => center.Y - o.Y == -1 && center.X == o.X && center.Z == o.Z);
                if (otherIndexYPositive != -1) {
                    rulesInternal.Add(new RuleForSolver(Axis.Y,
                                                        partNames[thisIndex],
                                                        partNames[otherIndexYPositive]));
                }

                // Positive Z neighbor
                var otherIndexZPositive = partCenters
                    .FindIndex(o => center.Z - o.Z == -1 && center.X == o.X && center.Y == o.Y);
                if (otherIndexZPositive != -1) {
                    rulesInternal.Add(new RuleForSolver(Axis.Z,
                                                        partNames[thisIndex],
                                                        partNames[otherIndexZPositive]));
                }
            }
            // No need to check neighbors in the negative orientation because 
            // all cases should be already covered by the positive orientation.
            return rulesInternal;
        }

        /// <summary>
        /// Gets a value indicating whether the module is valid. Required by
        /// Grasshopper.
        /// </summary>
        public bool IsValid =>
            Connectors != null &&
            Geometry != null &&
            ReferencedGeometry != null &&
            ReferencedGeometryGuids != null &&
            InternalRules != null &&
            Name != null &&
            Pivot != null &&
            PivotPartName != null &&
            Connectors.Count > 0 &&
            PartCenters.Count <= Config.MAX_PARTS &&
            Compact;

        /// <summary>
        /// Indicates why is the module not valid. Required by Grasshopper.
        /// </summary>
        public string IsValidWhyNot {
            get {
                if (!Compact) {
                    return "Module \"" + Name + "\" is not compact, contains islands and therefore " +
                        "will not hold together.";
                }
                if (PartCenters.Count > Config.MAX_PARTS) {
                    return "Module \"" + Name + "\" alone occupies " + PartCenters.Count +
                           " Slots. The Monoceros WFC Solver supports Modules occupying " +
                           "up to " + Config.MAX_PARTS + " Slots altogether.";
                }
                return "Some of the fields are null.";
            }
        }

        /// <summary>
        /// Gets the type name. Required by Grasshopper.
        /// </summary>
        public string TypeName => "Monoceros Module";

        /// <summary>
        /// Gets the type description. Required by Grasshopper.
        /// </summary>
        public string TypeDescription => "Monoceros Module for Wave Function Collapse solution.";

        /// <summary>
        /// Gets the clipping box for the module. Required by Grasshopper for
        /// viewport display and baking.
        /// </summary>
        BoundingBox IGH_PreviewData.ClippingBox {
            get {
                var unionBox = BoundingBox.Empty;
                foreach (var connector in Connectors) {
                    unionBox.Union(connector.Face.BoundingBox);
                }
                return unionBox;
            }
        }

        /// <summary>
        /// Does not cast any other type to Module. Required by Grasshopper.
        /// </summary>
        /// <param name="source">The source object.</param>
        /// <returns>A bool if the cast was successful.</returns>
        public bool CastFrom(object source) {
            return false;
        }

        /// <summary>
        /// Casts module to <see cref="ModuleName"/>. This is useful when
        /// reconstructing a module or defining a <see cref="Rule"/> because the
        /// Module itself can be used instead of its name.  Required by
        /// Grasshopper.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>A bool if the cast was successful.</returns>
        public bool CastTo<T>(out T target) {
            if (IsValid && typeof(T) == typeof(ModuleName)) {
                var moduleName = new ModuleName(Name);
                target = (T)moduleName.Duplicate();
                return true;
            }
            target = default;
            return false;
        }

        /// <summary>
        /// Duplicates the module. Required by Grasshopper.
        /// </summary>
        /// <returns>An IGH_Goo.</returns>
        public IGH_Goo Duplicate( ) {
            return (IGH_Goo)MemberwiseClone();
        }

        /// <summary>
        /// De-serialization. Required by Grasshopper for data internalization.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>True when successful.</returns>
        public bool Read(GH_IReader reader) {
            var formatter = new BinaryFormatter();

            if (reader.ItemExists("ModuleData")) {
                var moduleData = reader.GetByteArray("ModuleData");
                using (var moduleDataStream = new System.IO.MemoryStream(moduleData)) {
                    try {
                        var module = (Module)formatter.Deserialize(moduleDataStream);
                        Name = module.Name;
                        Geometry = module.Geometry;
                        BasePlane = module.BasePlane;
                        PartCenters = module.PartCenters;
                        Pivot = module.Pivot;
                        PivotPartName = module.PivotPartName;
                        PartDiagonal = module.PartDiagonal;
                        Connectors = module.Connectors;
                        InternalRules = module.InternalRules;
                        return true;
                    } catch (SerializationException e) {
                        throw e;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Serialization. Required by Grasshopper for data internalization.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <returns>True when successful.</returns>
        public bool Write(GH_IWriter writer) {
            var formatter = new BinaryFormatter();
            var module = Duplicate();
            using (var dataStream = new System.IO.MemoryStream()) {
                try {
                    formatter.Serialize(dataStream, module);
                    writer.SetByteArray("ModuleData", dataStream.ToArray());
                    return true;
                } catch (SerializationException e) {
                    throw e;
                }
            }
        }

        /// <summary>
        /// Returns the script variable. Required by Grasshopper.
        /// </summary>
        /// <returns>An object.</returns>
        public object ScriptVariable( ) {
            return this;
        }

        /// <summary>
        /// Compiles a text describing the current module. Required by
        /// Grasshopper for data peeking.
        /// </summary>
        /// <remarks>
        /// This overrides any casting to string. Therefore it interferes with
        /// automatic casting of a module to its name. The trade-off was either
        /// simple use of the Module type data as its reference (as its name
        /// that serves as an UID) or having an informative description.
        /// Therefore there was a new data type introduced:
        /// <see cref="ModuleName"/>. The module automatically casts to the
        /// ModuleName, which automatically casts to and from string. Therefore
        /// it is possible to use the module as its name and having a detailed
        /// description at the same time.
        /// </remarks>
        /// <returns>A string description of the module.</returns>
        public override string ToString( ) {
            var partCount = PartCenters.Count;
            var diagonal = new GH_Vector(PartDiagonal);
            return "Module \"" + Name + "\" has " + Connectors.Count + " connectors and " +
                    "has " + partCount + (partCount == 1 ? " part" : " parts") +
                    " with dimensions " + diagonal + ". " +
                    (Compact ?
                    "The module is compact." :
                    "WARNING: The module is not compact, contains islands and therefore " +
                    "will not hold together.");
        }

        /// <summary>
        /// Generates a named empty module with a single part. It is useful to
        /// generate reserved special modules with no geometry, such as Out and
        /// Empty.
        /// </summary>
        /// <param name="name">Module name.</param>
        /// <param name="connectorType">The connector type.</param>
        /// <param name="slotDiagonal">The slot diagonal.</param>
        /// <param name="module">Outputs the generated module.</param>
        /// <param name="rulesExternal">Outputs typed rules of the module.
        ///     </param>
        public static void GenerateEmptySingleModule(string name,
                                                     string connectorType,
                                                     Vector3d slotDiagonal,
                                                     out Module module,
                                                     out List<RuleTyped> rulesExternal) {
            GenerateEmptySingleModuleWithBasePlane(name,
                                                   connectorType,
                                                   Plane.WorldXY,
                                                   slotDiagonal,
                                                   out module,
                                                   out rulesExternal);
        }

        /// <summary>
        /// Generates a named empty module with a single part. It is useful to
        /// generate reserved special modules with no geometry, such as Out and
        /// Empty.
        /// </summary>
        /// <param name="name">Module name.</param>
        /// <param name="connectorType">The connector type.</param>
        /// <param name="basePlane">Module's base plane.</param>
        /// <param name="slotDiagonal">The slot diagonal.</param>
        /// <param name="module">Outputs the generated module.</param>
        /// <param name="rulesExternal">Outputs typed rules of the module.
        ///     </param>
        public static void GenerateEmptySingleModuleWithBasePlane(string name,
                                                                  string connectorType,
                                                                  Plane basePlane,
                                                                  Vector3d slotDiagonal,
                                                                  out Module module,
                                                                  out List<RuleTyped> rulesExternal) {
            module = new Module(
                name,
                new List<GeometryBase>(),
                new List<GeometryBase>(),
                new List<Guid>(),
                basePlane,
                new List<Point3i> { new Point3i(0, 0, 0) },
                slotDiagonal
                );
            rulesExternal = new List<RuleTyped>(6);
            for (var i = 0; i < 6; i++) {
                rulesExternal.Add(new RuleTyped(name, i, connectorType));
            }
        }

        /// <summary>
        /// Draws wired geometry representing the module to the viewport.
        /// Required by Grasshopper for data preview.
        /// </summary>
        /// <param name="args">Viewport arguments.</param>
        public void DrawViewportWires(GH_PreviewWireArgs args) {
            var geometry = Geometry.Concat(ReferencedGeometry);
            foreach (var geo in geometry) {
                if (geo.ObjectType == ObjectType.Point) {
                    // Draw those geometries of the module that are a point
                    args.Pipeline.DrawPoint(((Point)geo).Location, args.Color);
                }
                if (geo.ObjectType == ObjectType.Curve) {
                    // Draw those geometries of the module that are a curve
                    args.Pipeline.DrawCurve((Curve)geo, args.Color, args.Thickness);
                }
            }
            for (var connectorIndex = 0; connectorIndex < Connectors.Count; connectorIndex++) {
                var connector = Connectors[connectorIndex];
                // Draw connectors (together they look like a cage around the module)
                var cageColor = IsValid ? Config.CAGE_COLOR : Config.CAGE_ERROR_COLOR;
                args.Pipeline.DrawPolyline(connector.Face.ToPolyline(), cageColor);
                var anchorPosition = connector.AnchorPlane.Origin;
                var dotColor = Config.ColorFromAxis(connector.Direction.Axis);
                var textColor = Config.ColorFromOrientation(connector.Direction.Orientation);
                // Display connector index in the viewport
                args.Pipeline.DrawDot(new TextDot(connectorIndex.ToString(), anchorPosition),
                                      dotColor,
                                      textColor,
                                      dotColor);
            }

            var namePosition = Pivot.Origin;
            var nameColor = IsValid ? Config.CAGE_ONE_COLOR : Config.CAGE_ERROR_COLOR;
            args.Pipeline.Draw2dText(Name,
                                     nameColor,
                                     namePosition,
                                     true,
                                     Config.MODULE_NAME_FONT_HEIGHT,
                                     Config.FONT_FACE);
        }

        /// <summary>
        /// Draws simply shaded geometry representing the module to the
        /// viewport. Required by Grasshopper for data preview.
        /// </summary>
        /// <param name="args">Viewport arguments.</param>
        public void DrawViewportMeshes(GH_PreviewMeshArgs args) {
            var geometry = Geometry.Concat(ReferencedGeometry);
            foreach (var geo in geometry) {
                if (geo.ObjectType == ObjectType.Brep) {
                    args.Pipeline.DrawBrepShaded((Brep)geo, args.Material);
                }
                if (geo.ObjectType == ObjectType.Mesh) {
                    args.Pipeline.DrawMeshShaded((Mesh)geo, args.Material);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the module is bake-capable. Required
        /// by Grasshopper for baking.
        /// </summary>
        public bool IsBakeCapable => IsValid;

        /// <summary>
        /// Check if the module parts create a continuous compact blob.  If the
        /// module contains islands, then it is not compact and the module will
        /// not hold together. 
        /// </summary>
        public bool Compact => CheckConsistency(PartCenters);

        /// <summary>
        /// Bakes the module helpers (cages and connector anchors). Required by
        /// Grasshopper for baking.
        /// </summary>
        /// <param name="doc">The Rhino doc.</param>
        /// <param name="obj_ids">The Guids of the baked objects.</param>
        public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids) {
            BakeGeometry(doc, new ObjectAttributes(), obj_ids);
        }

        /// <summary>
        /// Bakes the module helpers (cages and connector anchors). Required by
        /// Grasshopper for baking.
        /// </summary>
        /// <param name="doc">The Rhino doc.</param>
        /// <param name="att">Attributes of the baked objects.</param>
        /// <param name="obj_ids">The Guids of the baked objects.</param>
        public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids) {
            if (att == null) {
                att = doc.CreateDefaultAttributes();
            }

            // Put all cages into a Rhino group
            var groupCagesId = doc.Groups.Add(Name + "-cages");
            // Put all connector anchors into a Rhino group
            var groupConnectorsId = doc.Groups.Add(Name + "-connectors");

            for (var connectorIndex = 0; connectorIndex < Connectors.Count; connectorIndex++) {
                var connector = Connectors[connectorIndex];
                var cageAttributes = att.Duplicate();
                var cageColor = IsValid ? Config.CAGE_COLOR : Config.CAGE_ERROR_COLOR;
                cageAttributes.ObjectColor = cageColor;
                cageAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                var faceId = doc.Objects.AddRectangle(connector.Face, cageAttributes);
                doc.Groups.AddToGroup(groupCagesId, faceId);
                obj_ids.Add(faceId);
                var dotAttributes = att.Duplicate();
                // Following the 3D modeling convention, the connectors in 
                // direction X are Red, in Y are Green, in Z are Blue
                dotAttributes.ObjectColor = Config.ColorFromAxis(connector.Direction.Axis);
                dotAttributes.ColorSource = ObjectColorSource.ColorFromObject;
                var connectorId = doc.Objects.AddTextDot(connectorIndex.ToString(),
                                                         connector.AnchorPlane.Origin,
                                                         dotAttributes);
                doc.Groups.AddToGroup(groupConnectorsId, connectorId);
                obj_ids.Add(connectorId);
            }

            var pivotPosition = Pivot.Origin;
            var pivotDotAttributes = att.Duplicate();
            pivotDotAttributes.ObjectColor = Config.POSITIVE_COLOR;
            pivotDotAttributes.ColorSource = ObjectColorSource.ColorFromObject;
            doc.Objects.AddTextDot(Name, pivotPosition, pivotDotAttributes);
        }

        IGH_GooProxy IGH_Goo.EmitProxy( ) {
            return null;
        }
    }

    /// <summary>
    /// The <see cref="Module"/>s and their parts exist in a discrete 3D grid -
    /// the World.  The connectors are rectangular faces of a part cage. The
    /// connectors align with rectangular divisors of the World grid.
    /// <see cref="ModuleConnector"/> is a descriptor of a connector, that is
    /// able to connect (touch, became a neighbor of) to another connector.  It
    /// contains data to be used in <see cref="Rule"/> creation and parsing.
    /// <list type="bullet">
    ///     <item>
    ///         <term><see cref="ModuleName"/></term>
    ///         <description>Name of the parent module containing the connector
    ///             </description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="partName"/></term>
    ///         <description>Name of a part containing the connector
    ///             </description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="ConnectorIndex"/></term>
    ///         <description>Index of the current connector in the list of all
    ///             module's connectors</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Direction"/></term>
    ///         <description>Direction of the current connector in the
    ///             orthogonal coordinate system (defined by
    ///             <see cref="Module.BasePlane"/>).</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Valence"/></term>
    ///         <description>Determines whether the module is internal or
    ///             external</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="AnchorPlane"/></term>
    ///         <description>A plane in the center of the connector oriented in
    ///             the direction of the connector (in world Cartesian
    ///             coordinates)</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Face"/></term>
    ///         <description>A rectangle around the connector - a face of the
    ///             cuboid representing the part</description>
    ///     </item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct ModuleConnector {
        /// <summary>
        /// >Name of a part containing the connector
        /// </summary>
        public string PartName;
        /// <summary>
        /// Direction of the current connector in the orthogonal coordinate
        /// system
        /// </summary>
        public Direction Direction;
        /// <summary>
        /// A plane in the center of the connector oriented in the direction of
        /// the connector (in world Cartesian coordinates)
        /// </summary>
        public Plane AnchorPlane;
        /// <summary>
        /// A rectangle around the connector - a face of the cuboid representing
        /// the part
        /// </summary>
        public Rectangle3d Face;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleConnector"/>
        /// class.
        /// </summary>
        /// <param name="partName">The current part name.</param>
        /// <param name="direction">The connector direction.</param>
        /// <param name="anchorPlane">The connector anchor plane.</param>
        /// <param name="face">The connector face rectangle.</param>
        public ModuleConnector(string moduleName,
                               string partName,
                               Direction direction,
                               Plane anchorPlane,
                               Rectangle3d face) {
            PartName = partName;
            Direction = direction;
            AnchorPlane = anchorPlane;
            Face = face;
        }

        /// <summary>
        /// Checks whether the connector contains point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>True if contains.</returns>
        public bool ContaininsPoint(Point3d point) {
            return Math.Abs(AnchorPlane.DistanceTo(point)) < RhinoMath.SqrtEpsilon &&
                Face.Contains(point) == PointContainment.Inside;
        }

        /// <summary>
        /// Equals the.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj) {
            return obj is ModuleConnector connector &&
                   PartName == connector.PartName &&
                   EqualityComparer<Direction>.Default.Equals(Direction, connector.Direction) &&
                   AnchorPlane.Equals(connector.AnchorPlane) &&
                   EqualityComparer<Rectangle3d>.Default.Equals(Face, connector.Face);
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>An int.</returns>
        public override int GetHashCode( ) {
            var hashCode = -855668167;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PartName);
            hashCode = hashCode * -1521134295 + Direction.GetHashCode();
            hashCode = hashCode * -1521134295 + AnchorPlane.GetHashCode();
            hashCode = hashCode * -1521134295 + Face.GetHashCode();
            return hashCode;
        }
    }

}
