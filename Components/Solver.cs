using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace WFCPlugin {
    public class ComponentSolver : GH_Component {

        private const int IN_PARAM_RULE_AXIS = 0;
        private const int IN_PARAM_RULE_LOW = 1;
        private const int IN_PARAM_RULE_HIGH = 2;
        private const int IN_PARAM_WORLD_SIZE = 3;
        private const int IN_PARAM_WORLD_SLOT_POSITION = 4;
        private const int IN_PARAM_WORLD_SLOT_MODULE = 5;
        private const int IN_PARAM_RANDOM_SEED = 6;
        private const int IN_PARAM_MAX_ATTEMPTS = 7;

        private const int OUT_PARAM_DEBUG_OUTPUT = 0;
        private const int OUT_PARAM_WORLD_SLOT_POSITION = 1;
        private const int OUT_PARAM_WORLD_SLOT_MODULE = 2;

        public ComponentSolver( ) : base("WFC Solver",
                                         "WFC",
                                         "Solver for the Wave Function Collapse",
                                         "WaveFunctionCollapse",
                                         "Main") {
        }

        public override Guid ComponentGuid => new Guid("DD1A1FA6-ACD4-4202-8B1A-9840949644B3");

        protected override System.Drawing.Bitmap Icon => Properties.Resources.WFC;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new SlotParameter(),
                                  "Slots",
                                  "S",
                                  "All WFC Slots",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new ModuleParameter(),
                                  "Modules",
                                  "M",
                                  "All WFC Modules",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "All WFC rules",
                                  GH_ParamAccess.list);
            pManager.AddIntegerParameter("Random Seed",
                                         "S",
                                         "Random Seed",
                                         GH_ParamAccess.item,
                                         42);
            pManager.AddIntegerParameter("Max Attempts",
                                         "A",
                                         "Maximum Number of Solver Attempts",
                                         GH_ParamAccess.item,
                                         10);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddTextParameter("out", "out", "Solver report", GH_ParamAccess.item);
            pManager.AddParameter(new SlotParameter(),
                                  "Slots",
                                  "S",
                                  "Solved WFC Slots",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var slotsRaw = new List<Slot>();
            var modules = new List<Module>();
            var rulesRaw = new List<Rule>();
            var randomSeed = 42;
            var maxAttempts = 10;

            if (!DA.GetDataList(0, slotsRaw)) {
                return;
            }

            if (!DA.GetDataList(1, modules)) {
                return;
            }

            if (!DA.GetDataList(2, rulesRaw)) {
                return;
            }

            if (!DA.GetData(3, ref randomSeed)) {
                return;
            }

            if (!DA.GetData(4, ref maxAttempts)) {
                return;
            }

            // Check if there are any slots to define the world
            if (slotsRaw.Count == 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No slots to define world.");
                return;
            }

            var diagonal = slotsRaw.First().Diagonal;

            // Check if all slots have the same slot diagonal
            if (slotsRaw.Any(slot => slot.Diagonal != diagonal)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slots are not defined with the same diagonal.");
                return;
            }

            // Check if all slots have the same base plane 
            // TODO: Consider smarter compatibility check or base plane conversion
            var slotBasePlane = slotsRaw.First().BasePlane;
            if (slotsRaw.Any(slot => slot.BasePlane != slotBasePlane)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slots are not defined with the same base plane.");
                return;
            }

            if (!AreSlotLocationsUnique(slotsRaw)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot centers are not unique.");
                return;
            }

            if (slotsRaw.Any(slot => slot.AllowedNothing)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Some slots allow no modules to be placed.");
                return;
            }

            if (modules.Count == 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No modules to populate world.");
                return;
            }

            var moduleDiagonal = modules.First().SlotDiagonal;

            if (modules.Any(module => module.SlotDiagonal != moduleDiagonal)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Modules are not defined with the same diagonal.");
                return;
            }

            if (!AreModuleNamesUnique(modules)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module names are not unique.");
                return;
            }

            var allSubmodulesCount = modules
                .Aggregate(0, (sum, module) => sum + module.SubmoduleCenters.Count);

            if (moduleDiagonal != diagonal) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Modules and slots are not defined with the same diagonal.");
                return;
            }

            // Generate Out module
            Module.GenerateEmptySingleModule(Config.OUTER_MODULE_NAME,
                                             Config.INDIFFERENT_TAG,
                                             new Rhino.Geometry.Vector3d(1, 1, 1),
                                             out var moduleOut,
                                             out var rulesOutTyped);
            var rulesOut = rulesOutTyped.Select(ruleTyped => new Rule(ruleTyped));
            rulesRaw.AddRange(rulesOut);

            // Convert AllowEverything slots into an explicit list of allowed modules (except Out)
            var allModuleNames = modules.Select(module => module.Name).ToList();
            var slotsUnwrapped = slotsRaw.Select(slotRaw =>
                slotRaw.AllowAnyModule ?
                    slotRaw.DuplicateWithModuleNames(allModuleNames) :
                    slotRaw
            );

            modules.Add(moduleOut);

            var rulesDistinct = rulesRaw.Distinct();

            // Unwrap typed rules
            var rulesTyped = rulesDistinct.Where(rule => rule.IsTyped()).Select(rule => rule.Typed);
            var rulesTypedUnwrappedToExplicit = rulesTyped
                .SelectMany(ruleTyped => ruleTyped.ToRuleExplicit(rulesTyped, modules));

            var rulesExplicit = rulesDistinct
                .Where(rule => rule.IsExplicit())
                .Select(rule => rule.Explicit);

            // Deduplicate rules again
            var rulesUnwrappedExplicit = rulesExplicit.Concat(rulesTypedUnwrappedToExplicit).Distinct();

            // Filter out invalid rules (not connecting the same connectors && connecting opposing connectors)
            var rulesValid = rulesUnwrappedExplicit.Where(rule => rule.IsValidWithGivenModules(modules));

            // Convert rules to solver format
            var rulesForSolver = new List<RuleForSolver>();
            foreach (var rule in rulesValid) {
                if (rule.ToWFCRuleSolver(modules, out var ruleForSolver)) {
                    rulesForSolver.Add(ruleForSolver);
                }
            }

            // Add internal rules to the main rule set
            foreach (var module in modules) {
                rulesForSolver.AddRange(module.InternalRules);
            }

            var slotOrder = new List<int>(slotsRaw.Count);
            // Define world space (slots bounding box + 1 layer padding)
            ComputeWorldRelativeBounds(slotsUnwrapped, out var worldMin, out var worldMax);
            var worldLength = ComputeWorldLength(worldMin, worldMax);
            var worldSlots = Enumerable.Repeat<Slot>(null, worldLength).ToList();
            foreach (var slot in slotsUnwrapped) {
                var index = From3DTo1D(slot.RelativeCenter - worldMin, worldMin, worldMax);
                worldSlots[index] = slot;
                slotOrder.Add(index);
            }

            // Fill unused world slots with Out modules
            for (var i = 0; i < worldSlots.Count; i++) {
                var slot = worldSlots[i];
                var relativeCenter = From1DTo3D(i, worldMin, worldMax) + worldMin;
                if (slot == null) {
                    worldSlots[i] = new Slot(slotBasePlane,
                                             relativeCenter,
                                             diagonal,
                                             false,
                                             new List<string>() { moduleOut.Name },
                                             new List<string>(),
                                             allSubmodulesCount);
                }
            }

            // Unwrap module names to submodule names for all slots
            var worldPreprocessed = worldSlots.Select(slotRaw => {
                if (slotRaw.AllowedSubmoduleNames.Count != 0 && !slotRaw.AllowAnyModule) {
                    return slotRaw.DuplicateWithSubmodulesCount(allSubmodulesCount);
                }

                var submoduleNames = new List<string>();
                foreach (var moduleName in slotRaw.AllowedModuleNames) {
                    var module = modules.Find(m => m.Name == moduleName);
                    submoduleNames.AddRange(module.SubmoduleNames);
                }
                if (submoduleNames.Count == 0) {
                    throw new Exception("Slot is empty in spite of previous checks.");
                }
                return slotRaw.DuplicateWithSubmodulesCountAndNames(allSubmodulesCount,
                                                                    submoduleNames);
            });

            // SOLVER
            // Scan all slots, pick one submodule for each
            // The solution may contain more than one value as an output: 
            // useful for Step Solver and for post-processor tuning

            var rulesForSolverDistinct = rulesForSolver.Distinct();

            var adjacencyRulesAxis = rulesForSolverDistinct
                .Select(rule => rule.AxialDirection)
                .ToList();
            var adjacencyRulesSubmoduleLow = rulesForSolverDistinct
                .Select(rule => rule.LowerSubmoduleName)
                .ToList();
            var adjacencyRulesSubmoduleHigh = rulesForSolverDistinct
                .Select(rule => rule.HigherSubmoduleName)
                .ToList();
            var worldsSizeP3i = (worldMax - worldMin);
            var worldSize = worldsSizeP3i.ToVector3d();
            var worldSlotsPositions = worldPreprocessed.SelectMany((slot, index) =>
                Enumerable.Repeat(
                    From1DTo3D(index, worldMin, worldMax).ToPoint3d(),
                    slot.AllowedSubmoduleNames.Count
                    )
            ).ToList();
            var worldSlotSubmodules = worldPreprocessed
                .SelectMany(slot => slot.AllowedSubmoduleNames)
                .ToList();

            var success = Solve(adjacencyRulesAxis,
                                adjacencyRulesSubmoduleLow,
                                adjacencyRulesSubmoduleHigh,
                                worldSize,
                                randomSeed,
                                maxAttempts,
                                ref worldSlotsPositions,
                                ref worldSlotSubmodules,
                                out var report);

            if (!success) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, report);
                DA.SetData(0, report);
                return;
            }

            var solution = new List<List<string>>();
            for (var i = 0; i < worldLength; i++) {
                solution.Add(new List<string>());
            }

            for (var i = 0; i < worldSlotsPositions.Count; i++) {
                var position = worldSlotsPositions[i];
                var submoduleName = worldSlotSubmodules[i];
                var position1D = From3DTo1D(new Point3i(position), worldsSizeP3i);
                solution[position1D].Add(submoduleName);
            }

            // Remember module name for each submodule name
            var submoduleToModuleName = new Dictionary<string, string>();
            foreach (var module in modules) {
                foreach (var submoduleName in module.SubmoduleNames) {
                    submoduleToModuleName.Add(submoduleName, module.Name);
                }
            }

            // Sort slots into the same order as they were input
            var slotsSolved = slotOrder.Select(index => {
                var allowedSubmodules = solution[index];
                var allowedModules = allowedSubmodules
                    .Select(submoduleName => submoduleToModuleName[submoduleName])
                    .Distinct()
                    .ToList();
                // Convert world from solver format into slots 
                return new Slot(slotBasePlane,
                                From1DTo3D(index, worldMin, worldMax) + worldMin,
                                diagonal,
                                false,
                                allowedModules,
                                allowedSubmodules,
                                allSubmodulesCount);
            });

            DA.SetData(0, report);
            DA.SetDataList(1, slotsSolved);
        }

        private static void ComputeWorldRelativeBounds(IEnumerable<Slot> slots,
                                                       out Point3i min,
                                                       out Point3i max) {
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var minZ = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;
            var maxZ = int.MinValue;

            foreach (var slot in slots) {
                var center = slot.RelativeCenter;
                minX = Math.Min(minX, center.X);
                minY = Math.Min(minY, center.Y);
                minZ = Math.Min(minZ, center.Z);
                maxX = Math.Max(maxX, center.X);
                maxY = Math.Max(maxY, center.Y);
                maxZ = Math.Max(maxZ, center.Z);
            }

            minX -= 1;
            minY -= 1;
            minZ -= 1;

            maxX += 1;
            maxY += 1;
            maxZ += 1;

            min = new Point3i(minX, minY, minZ);
            max = new Point3i(maxX, maxY, maxZ);
        }

        private static int ComputeWorldLength(Point3i min, Point3i max) {
            var lengthX = max.X - min.X;
            var lengthY = max.Y - min.Y;
            var lengthZ = max.Z - min.Z;

            return (lengthX * lengthY * lengthZ);
        }

        private static int From3DTo1D(Point3i p, Point3i min, Point3i max) {
            var lengthX = max.X - min.X;
            var lengthY = max.Y - min.Y;

            return (lengthX * lengthY * p.Z) + lengthX * p.Y + p.X;
        }

        private static int From3DTo1D(Point3i p, Point3i max) {
            return (max.X * max.Y * p.Z) + max.X * p.Y + p.X;
        }

        private static Point3i From1DTo3D(int index, Point3i min, Point3i max) {
            var lengthX = max.X - min.X;
            var lengthY = max.Y - min.Y;

            var lengthXY = lengthX * lengthY;
            var z = index / lengthXY;
            var y = (index % lengthXY) / lengthX;
            var x = index % lengthX;

            return new Point3i(x, y, z);
        }

        private bool AreModuleNamesUnique(List<Module> modules) {
            for (var i = 0; i < modules.Count; i++) {
                for (var j = i + 1; j < modules.Count; j++) {
                    if (modules[i].Name == modules[j].Name) {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool AreSlotLocationsUnique(List<Slot> slots) {
            for (var i = 0; i < slots.Count; i++) {
                for (var j = i + 1; j < slots.Count; j++) {
                    if (slots[i].RelativeCenter.Equals(slots[j].RelativeCenter)) {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool Solve(List<string> adjacencyRulesAxis,
                           List<string> adjacencyRulesSubmoduleLow,
                           List<string> adjacencyRulesSubmoduleHigh,
                           Vector3d worldSize,
                           int randomSeed,
                           int maxAttemptsInt,
                           // Note: This list will be cleared and re-used for output.
                           ref List<Point3d> worldSlotPositions,
                           // Note: This list will be cleared and re-used for output.
                           ref List<string> worldSlotSubmodules,
                           out string report) {
            var stats = new Stats();

            // -- Adjacency rules --
            //
            // The rules come in three lists of the same length. The first contains texts
            // representing the axis/kind (x/y/z).
            // Second and third list contain unique textual identifiers of the modules.
            // This importer replaces those string names with generated u32 numbers,
            // starting with 0.

            if (adjacencyRulesAxis.Count != adjacencyRulesSubmoduleLow.Count ||
                adjacencyRulesAxis.Count != adjacencyRulesSubmoduleHigh.Count) {
                report = "Unequal rule components count.";
                return false;
            }

            // We need to check ahead of time, if there are at most 256 modules
            // altogether in the input, otherwise the `nextModule` variable will
            // overflow and cause a dictionary error.
            {
                var allModules = new HashSet<string>();

                for (var i = 0; i < adjacencyRulesAxis.Count; ++i) {
                    allModules.Add(adjacencyRulesSubmoduleLow[i]);
                    allModules.Add(adjacencyRulesSubmoduleHigh[i]);
                }

                // TODO: Move to caller
                if (allModules.Count > 256) {
                    report = "The modules occupy too many slots. Maximum allowed is 256, current is " +
                        allModules.Count + ".";
                    return false;
                }
            }

            byte nextSubmodule = 0;
            var nameToSubmodule = new Dictionary<string, byte>();
            var submoduleToName = new Dictionary<byte, string>();
            var adjacencyRules = new AdjacencyRule[adjacencyRulesAxis.Count];

            for (var i = 0; i < adjacencyRulesAxis.Count; ++i) {
                var axisStr = adjacencyRulesAxis[i];
                var lowStr = adjacencyRulesSubmoduleLow[i];
                var highStr = adjacencyRulesSubmoduleHigh[i];

                AdjacencyRuleKind kind;
                switch (axisStr) {
                    case "x":
                    case "X":
                        kind = AdjacencyRuleKind.X;
                        break;
                    case "y":
                    case "Y":
                        kind = AdjacencyRuleKind.Y;
                        break;
                    case "z":
                    case "Z":
                        kind = AdjacencyRuleKind.Z;
                        break;
                    default:
                        report = "Invalid world state.";
                        return false;
                }

                byte low = 0;
                if (nameToSubmodule.ContainsKey(lowStr)) {
                    nameToSubmodule.TryGetValue(lowStr, out low);
                } else {
                    low = nextSubmodule;
                    nameToSubmodule.Add(lowStr, low);
                    submoduleToName.Add(low, lowStr);
                    nextSubmodule++;
                }

                byte high = 0;
                if (nameToSubmodule.ContainsKey(highStr)) {
                    nameToSubmodule.TryGetValue(highStr, out high);
                } else {
                    high = nextSubmodule;
                    nameToSubmodule.Add(highStr, high);
                    submoduleToName.Add(high, highStr);
                    nextSubmodule++;
                }

                AdjacencyRule rule;
                rule.kind = kind;
                rule.module_low = low;
                rule.module_high = high;
                adjacencyRules[i] = rule;
            }

            //
            // -- World dimensions --
            //

            var worldXInt = (int)Math.Round(worldSize.X);
            var worldYInt = (int)Math.Round(worldSize.Y);
            var worldZInt = (int)Math.Round(worldSize.Z);

            if (worldXInt <= 0) {
                report = "World X must be a positive integer";
                return false;
            }
            if (worldYInt <= 0) {
                report = "World Y must be a positive integer";
                return false;
            }
            if (worldZInt <= 0) {
                report = "World Z must be a positive integer";
                return false;
            }

            var worldX = (ushort)worldXInt;
            var worldY = (ushort)worldYInt;
            var worldZ = (ushort)worldZInt;
            var worldDimensions = (uint)worldX * worldY * worldZ;
            var worldSlotsPerLayer = (uint)worldX * worldY;
            uint worldSlotsPerRow = worldX;

            //
            // -- World slot positions and modules --
            //

            // This array is re-used for both input and output (if input world state was provided).
            // This is ok, because wfc_world_state_get does clear it to zero before writing to it.
            var worldState = new SlotState[worldDimensions];

            // ... WE do need to clear it to zero, however. C# does not initialize slot_state for us!
            for (var i = 0; i < worldState.Length; ++i) {
                unsafe {
                    worldState[i].slot_state[0] = 0;
                    worldState[i].slot_state[1] = 0;
                    worldState[i].slot_state[2] = 0;
                    worldState[i].slot_state[3] = 0;
                }
            }

            var worldSlotMinCount = Math.Min(worldSlotPositions.Count, worldSlotSubmodules.Count);
            for (var i = 0; i < worldSlotMinCount; ++i) {
                var position = worldSlotPositions[i];
                var moduleStr = worldSlotSubmodules[i];

                var slotXInt = (int)Math.Round(position.X);
                var slotYInt = (int)Math.Round(position.Y);
                var slotZInt = (int)Math.Round(position.Z);

                if (slotXInt < 0) {
                    report = "Slot X must be a nonnegative integer";
                    return false;
                }
                if (slotYInt < 0) {
                    report = "Slot Y must be a nonnegative integer";
                    return false;
                }
                if (slotZInt < 0) {
                    report = "World Z must be a nonnegative integer";
                    return false;
                }

                var slotX = (uint)slotXInt;
                var slotY = (uint)slotYInt;
                var slotZ = (uint)slotZInt;

                if (nameToSubmodule.TryGetValue(moduleStr.ToString(), out var module)) {
                    var slotIndex = slotX + slotY * worldSlotsPerRow + slotZ * worldSlotsPerLayer;
                    Debug.Assert(slotIndex < worldState.Length);

                    var blkIndex = (byte)(module / 64u);
                    var bitIndex = (byte)(module % 64u);
                    var mask = 1ul << bitIndex;

                    Debug.Assert(blkIndex < 4);
                    unsafe {
                        worldState[slotIndex].slot_state[blkIndex] |= mask;
                    }
                } else {
                    report = "Slot submodule list (SM) contains submodule not found in " +
                                      "the ruleset: " + moduleStr;
                    return false;
                }
            }

            //
            // -- Random seed --
            //
            // wfc_init needs 128 bits worth of random seed, but that is tricky to provide from GH.
            // We let GH provide an int, use it to seed a C# Random, get 16 bytes of data from that
            // and copy those into two u64's.

            var random = new Random(randomSeed);
            var rngSeedLowArr = new byte[8];
            var rngSeedHighArr = new byte[8];
            random.NextBytes(rngSeedLowArr);
            random.NextBytes(rngSeedHighArr);

            if (!BitConverter.IsLittleEndian) {
                // If we are running on a BE machine, we need to reverse the bytes,
                // because low and high are defined to always be LE.
                Array.Reverse(rngSeedLowArr);
                Array.Reverse(rngSeedHighArr);
            }

            var rngSeedLow = BitConverter.ToUInt64(rngSeedLowArr, 0);
            var rngSeedHigh = BitConverter.ToUInt64(rngSeedHighArr, 0);

            //
            // -- Max attempts --
            //

            var maxAttempts = (uint)maxAttemptsInt;

            //
            // -- Run the thing and **pray** --
            //

            var wfc = IntPtr.Zero;
            unsafe {
                fixed (AdjacencyRule* adjacencyRulesPtr = &adjacencyRules[0]) {
                    var result = Native.wfc_init(&wfc,
                                                 adjacencyRulesPtr,
                                                 (UIntPtr)adjacencyRules.Length,
                                                 worldX,
                                                 worldY,
                                                 worldZ,
                                                 rngSeedLow,
                                                 rngSeedHigh);

                    switch (result) {
                        case WfcInitResult.Ok:
                            // All good
                            break;
                        case WfcInitResult.TooManyModules:
                            report = "WFC solver failed: Adjacency rules contained too many modules";
                            return false;
                        case WfcInitResult.WorldDimensionsZero:
                            report = "WFC solver failed: World dimensions are zero";
                            return false;
                        default:
                            report = "WFC solver failed with unknown error";
                            return false;
                    }
                }

                fixed (SlotState* worldStatePtr = &worldState[0]) {
                    var result = Native.wfc_world_state_set(wfc,
                                                            worldStatePtr,
                                                            (UIntPtr)worldState.Length);
                    switch (result) {
                        case WfcWorldStateSetResult.Ok:
                            // All good
                            stats.worldNotCanonical = false;
                            break;
                        case WfcWorldStateSetResult.OkNotCanonical:
                            // All good, but we had to fix some things
                            stats.worldNotCanonical = true;
                            break;
                        case WfcWorldStateSetResult.WorldContradictory:
                            report = "WFC solver failed: World state is contradictory";
                            return false;
                    }
                }
            }

            var attempts = Native.wfc_observe(wfc, maxAttempts);
            if (attempts == 0) {
                report = "WFC solver failed to find solution within " + maxAttempts + " attempts";
                return false;
            }

            unsafe {
                fixed (SlotState* worldStatePtr = &worldState[0]) {
                    Native.wfc_world_state_get(wfc, worldStatePtr, (UIntPtr)worldState.Length);
                }
            }

            Native.wfc_free(wfc);

            //
            // -- Output: World state --
            //
            // The resulting world state is in the flat bit-vector format. Since we
            // early-out on nondeterministic results, we can assume exactly one bit
            // being set here and can therefore produce a flat list on output.

            worldSlotPositions.Clear();
            worldSlotSubmodules.Clear();
            for (var i = 0; i < worldState.Length; ++i) {
                // Assume the result is deterministic and only take the first set bit
                var submodule = short.MinValue;
                for (var blkIndex = 0; blkIndex < 4 && submodule == short.MinValue; ++blkIndex) {
                    for (var bitIndex = 0; bitIndex < 64 && submodule == short.MinValue; ++bitIndex) {
                        var mask = 1ul << bitIndex;
                        unsafe {
                            if ((worldState[i].slot_state[blkIndex] & mask) != 0) {
                                submodule = (short)(64 * blkIndex + bitIndex);
                            }
                        }
                    }
                }

                var submoduleStr = "<unknown>";
                if (submodule >= 0) {
                    Debug.Assert(submodule <= byte.MaxValue);
                    submoduleToName.TryGetValue((byte)submodule, out submoduleStr);
                }

                var slotX = i % worldSlotsPerLayer % worldSlotsPerRow;
                var slotY = i % worldSlotsPerLayer / worldSlotsPerRow;
                var slotZ = i / worldSlotsPerLayer;

                worldSlotPositions.Add(new Point3d(slotX, slotY, slotZ));
                worldSlotSubmodules.Add(submoduleStr);
            }

            stats.ruleCount = (uint)adjacencyRulesAxis.Count;
            stats.submoduleCount = (uint)submoduleToName.Count;
            stats.solveAttempts = attempts;

            report = stats.ToString();
            return true;
        }
    }

    internal enum AdjacencyRuleKind : uint {
        X = 0,
        Y = 1,
        Z = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AdjacencyRule {
        public AdjacencyRuleKind kind;
        public byte module_low;
        public byte module_high;
    }

    internal enum WfcInitResult : uint {
        Ok = 0,
        TooManyModules = 1,
        WorldDimensionsZero = 2,
    }

    internal enum WfcWorldStateSetResult : uint {
        Ok = 0,
        OkNotCanonical = 1,
        WorldContradictory = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SlotState {
        public fixed ulong slot_state[4];

        public override string ToString( ) {
            var b = new StringBuilder("Slot state { ", 64);

            b.Append("[");
            b.Append(slot_state[0]);
            b.Append("][");
            b.Append(slot_state[1]);
            b.Append("][");
            b.Append(slot_state[2]);
            b.Append("][");
            b.Append(slot_state[3]);
            b.Append("] }");

            return b.ToString();
        }
    }

    internal struct Stats {
        public uint ruleCount;
        public uint submoduleCount;
        public uint solveAttempts;
        public bool worldNotCanonical;

        public override string ToString( ) {
            var b = new StringBuilder(128);

            b.Append("Rule count: ");
            b.Append(ruleCount);
            b.AppendLine();
            b.Append("Submodule count: ");
            b.Append(submoduleCount);
            b.AppendLine();
            b.Append("Solve attempts: ");
            b.Append(solveAttempts);
            b.AppendLine();

            if (worldNotCanonical) {
                b.AppendLine("Warning: Initial world state is not canonical");
            }

            return b.ToString();
        }
    }

    internal class Native {
        [DllImport("wfc", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe WfcInitResult wfc_init(IntPtr* wfc_ptr,
                                                             AdjacencyRule* adjacency_rules_ptr,
                                                             UIntPtr adjacency_rules_len,
                                                             ushort world_x,
                                                             ushort world_y,
                                                             ushort world_z,
                                                             ulong rngSeedLow,
                                                             ulong rngSeedHigh);

        [DllImport("wfc", CallingConvention = CallingConvention.StdCall)]
        internal static extern void wfc_free(IntPtr wfc);

        [DllImport("wfc", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint wfc_observe(IntPtr wfc, uint max_attempts);

        [DllImport("wfc", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe WfcWorldStateSetResult wfc_world_state_set(IntPtr wfc,
                                                                                 SlotState* world_state_ptr,
                                                                                 UIntPtr world_state_len);

        [DllImport("wfc", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe void wfc_world_state_get(IntPtr wfc,
                                                               SlotState* world_state_ptr,
                                                               UIntPtr world_state_len);
    }

}