using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Grasshopper.Kernel;

namespace Monoceros {
    public class ComponentSolver : GH_Component {
        public ComponentSolver( ) : base("Monoceros WFC Solver",
                                         "WFC",
                                         "Monoceros Solver for the Wave Function Collapse",
                                         "Monoceros",
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
                                  "All Monoceros Slots",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new ModuleParameter(),
                                  "Modules",
                                  "M",
                                  "All Monoceros Modules",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "All Monoceros rules",
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
                                  "Solved Monoceros Slots",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var slotsRaw = new List<Slot>();
            var modulesRaw = new List<Module>();
            var rulesRaw = new List<Rule>();
            var randomSeed = 42;
            var maxAttempts = 10;

            if (!DA.GetDataList(0, slotsRaw)) {
                return;
            }

            if (!DA.GetDataList(1, modulesRaw)) {
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


            var slotsClean = new List<Slot>();
            foreach (var slot in slotsRaw) {
                if (slot == null || !slot.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot is null or invalid.");
                } else {
                    slotsClean.Add(slot);
                }
            }

            var modulesClean = new List<Module>();
            foreach (var module in modulesRaw) {
                if (module == null || !module.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module is null or invalid.");
                } else {
                    modulesClean.Add(module);
                }
            }

            var rulesClean = new List<Rule>();
            foreach (var rule in rulesRaw) {
                if (rule == null || !rule.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Rule is null or invalid.");
                } else {
                    rulesClean.Add(rule);
                }
            }

            var diagonal = slotsClean.First().Diagonal;

            if (slotsClean.Any(slot => slot.Diagonal != diagonal)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slots are not defined with the same diagonal.");
                return;
            }

            // TODO: Consider smarter compatibility check or base plane conversion
            var slotBasePlane = slotsClean.First().BasePlane;
            if (slotsClean.Any(slot => slot.BasePlane != slotBasePlane)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slots are not defined with the same base plane.");
                return;
            }

            if (!AreSlotLocationsUnique(slotsClean)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot centers are not unique.");
                return;
            }

            var moduleDiagonal = modulesClean.First().SlotDiagonal;

            if (modulesClean.Any(module => module.SlotDiagonal != moduleDiagonal)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Modules are not defined with the same diagonal.");
                return;
            }

            if (!AreModuleNamesUnique(modulesClean)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module names are not unique.");
                return;
            }

            if (moduleDiagonal != diagonal) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Modules and slots are not defined with the same diagonal.");
                return;
            }

            var modulesUsable = new List<Module>();

            foreach (var module in modulesClean) {
                var usedConnectors = Enumerable.Repeat(false, module.Connectors.Count).ToList();
                foreach (var rule in rulesClean) {
                    if (rule.IsExplicit) {
                        if (rule.Explicit.SourceModuleName == module.Name &&
                            rule.Explicit.SourceConnectorIndex < module.Connectors.Count) {
                            usedConnectors[rule.Explicit.SourceConnectorIndex] = true;
                        }
                        if (rule.Explicit.TargetModuleName == module.Name &&
                            rule.Explicit.TargetConnectorIndex < module.Connectors.Count) {
                            usedConnectors[rule.Explicit.TargetConnectorIndex] = true;
                        }
                    }
                    if (rule.IsTyped) {
                        if (rule.Typed.ModuleName == module.Name &&
                            rule.Typed.ConnectorIndex < module.Connectors.Count) {
                            usedConnectors[rule.Typed.ConnectorIndex] = true;
                        }
                    }
                }
                if (usedConnectors.Any(boolean => boolean == false)) {
                    var warningString = "Module \"" + module.Name + "\" will be excluded from the " +
                        "solution. Connectors not described by any Rule: ";
                    for (var i = 0; i < usedConnectors.Count; i++) {
                        if (!usedConnectors[i]) {
                            warningString += i + ", ";
                        }
                    }
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, warningString);
                } else {
                    modulesUsable.Add(module);
                }
            }

            if (!modulesUsable.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "There are no Modules with all connectors described by the " +
                                  "given Rules.");
                return;
            }

            var allSubmodulesCount = modulesUsable
                .Aggregate(0, (sum, module) => sum + module.SubmoduleCenters.Count);

            if (allSubmodulesCount > Config.MAX_SUBMODULES) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "The Modules occupy too many Slots: " + allSubmodulesCount + ". Maximum allowed :" +
                    Config.MAX_SUBMODULES + ".");
                return;
            }

            Module.GenerateEmptySingleModule(Config.OUTER_MODULE_NAME,
                                             Config.INDIFFERENT_TAG,
                                             new Rhino.Geometry.Vector3d(1, 1, 1),
                                             out var moduleOut,
                                             out var rulesOutTyped);
            var rulesOut = rulesOutTyped.Select(ruleTyped => new Rule(ruleTyped));
            rulesClean.AddRange(rulesOut);

            // Convert AllowEverything slots into an explicit list of allowed modules (except Out)
            var allModuleNames = modulesUsable.Select(module => module.Name).ToList();
            var slotsUnwrapped = slotsClean.Select(slotRaw =>
                slotRaw.AllowsAnyModule ?
                    slotRaw.DuplicateWithModuleNames(allModuleNames) :
                    slotRaw
            );

            modulesUsable.Add(moduleOut);

            // Unwrap typed rules
            var rulesTyped = rulesClean.Where(rule => rule.IsTyped).Select(rule => rule.Typed);
            var rulesTypedUnwrappedToExplicit = rulesTyped
                .SelectMany(ruleTyped => ruleTyped.ToRulesExplicit(rulesTyped, modulesUsable));

            var rulesExplicit = rulesClean
                .Where(rule => rule.IsExplicit)
                .Select(rule => rule.Explicit);

            // Deduplicate rules again
            var rulesExplicitAll = rulesExplicit.Concat(rulesTypedUnwrappedToExplicit).Distinct();

            // Convert rules to solver format
            var rulesForSolver = new List<RuleForSolver>();
            foreach (var rule in rulesExplicitAll) {
                if (rule.ToRuleForSolver(modulesUsable, out var ruleForSolver)) {
                    rulesForSolver.Add(ruleForSolver);
                }
            }

            // Add internal rules to the main rule set
            foreach (var module in modulesUsable) {
                rulesForSolver.AddRange(module.InternalRules);
            }

            var slotOrder = new List<int>(slotsClean.Count);
            // Define world space (slots bounding box + 1 layer padding)
            ComputeWorldRelativeBounds(slotsUnwrapped, out var worldMin, out var worldMax);
            var worldLength = ComputeWorldLength(worldMin, worldMax);
            var worldSlots = Enumerable.Repeat<Slot>(null, worldLength).ToList();
            foreach (var slot in slotsUnwrapped) {
                var index = From3DTo1D(slot.RelativeCenter, worldMin, worldMax);
                worldSlots[index] = slot;
                slotOrder.Add(index);
            }

            // Fill unused world slots with Out modules
            for (var i = 0; i < worldSlots.Count; i++) {
                var slot = worldSlots[i];
                var relativeCenter = From1DTo3D(i, worldMin, worldMax);
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
            var worldForSolver = worldSlots.Select(slotRaw => {
                if (slotRaw.AllowedSubmoduleNames.Count != 0) {
                    return slotRaw.DuplicateWithSubmodulesCount(allSubmodulesCount);
                }

                var submoduleNames = new List<string>();
                foreach (var moduleName in slotRaw.AllowedModuleNames) {
                    var module = modulesUsable.Find(m => m.Name == moduleName);
                    submoduleNames.AddRange(module.SubmoduleNames);
                }
                return slotRaw.DuplicateWithSubmodulesCountAndNames(allSubmodulesCount,
                                                                    submoduleNames);
            });

            foreach (var slot in worldForSolver) {
                if (!slot.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, slot.IsValidWhyNot);
                    return;
                }

                if (slot.AllowsNothing) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slot at " + slot.AbsoluteCenter + "does not allow any Module " +
                                  "to be placed.");
                    return;
                }

                if (slot.AllowsAnyModule) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Unwrapping failed for slot at " + slot.AbsoluteCenter + ".");
                    return;
                }
            }
            var worldSize = (worldMax - worldMin);

            if (!worldSize.FitsUshort()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "The world size exceeds minimum or maximum dimensions: " +
                                  ushort.MinValue + " to " + ushort.MaxValue + "in any direction.");
                return;
            }

            // SOLVER
            var success = Solve(rulesForSolver.Distinct().ToList(),
                                worldSize,
                                worldForSolver.ToList(),
                                randomSeed,
                                maxAttempts,
                                out var solvedSlotSubmodules,
                                out var report);

            if (!success) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, report);
                DA.SetData(0, report);
                return;
            }

            // Remember module name for each submodule name
            var submoduleToModuleName = new Dictionary<string, string>();
            foreach (var module in modulesUsable) {
                foreach (var submoduleName in module.SubmoduleNames) {
                    submoduleToModuleName.Add(submoduleName, module.Name);
                }
            }

            // Sort slots into the same order as they were input
            var slotsSolved = slotOrder.Select(index => {
                var allowedSubmodule = solvedSlotSubmodules[index];
                var allowedModule = submoduleToModuleName[allowedSubmodule];
                // Convert world from solver format into slots 
                return new Slot(slotBasePlane,
                                From1DTo3D(index, worldMin, worldMax),
                                diagonal,
                                false,
                                new List<string>() { allowedModule },
                                new List<string>() { allowedSubmodule },
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

            minX -= 2;
            minY -= 2;
            minZ -= 2;

            maxX += 2;
            maxY += 2;
            maxZ += 2;

            min = new Point3i(minX, minY, minZ);
            max = new Point3i(maxX, maxY, maxZ);
        }

        private static int ComputeWorldLength(Point3i min, Point3i max) {
            var lengthX = max.X - min.X;
            var lengthY = max.Y - min.Y;
            var lengthZ = max.Z - min.Z;

            return (lengthX * lengthY * lengthZ);
        }

        private static int From3DTo1D(Point3i point, Point3i min, Point3i max) {
            var lengthX = max.X - min.X;
            var lengthY = max.Y - min.Y;

            var worldSlotsPerLayer = lengthX * lengthY;
            var worldSlotsPerRow = lengthX;

            var p = point - min;

            var index = p.X + p.Y * worldSlotsPerRow + p.Z * worldSlotsPerLayer;

            return index;
        }

        private static int From3DTo1D(Point3i p, Point3i max) {
            return From3DTo1D(p, new Point3i(0, 0, 0), max);
        }

        private static Point3i From1DTo3D(int index, Point3i min, Point3i max) {
            var lengthX = max.X - min.X;
            var lengthY = max.Y - min.Y;

            var worldSlotsPerLayer = lengthX * lengthY;
            var worldSlotsPerRow = lengthX;

            var x = index % worldSlotsPerLayer % worldSlotsPerRow;
            var y = index % worldSlotsPerLayer / worldSlotsPerRow;
            var z = index / worldSlotsPerLayer;

            return new Point3i(x, y, z) + min;
        }

        private static Point3i From1DTo3D(int index, Point3i max) {
            return From1DTo3D(index, new Point3i(0, 0, 0), max);
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

        private bool Solve(List<RuleForSolver> rules,
                           Point3i worldSize,
                           List<Slot> slots,
                           int randomSeed,
                           int maxAttemptsInt,
                           out List<string> worldSlotSubmodules,
                           out string report) {
            var stats = new Stats();
            worldSlotSubmodules = new List<string>();

            // -- Adjacency rules --
            //
            // Second and third list contain unique textual identifiers of the modules.
            // This importer replaces those string names with generated u32 numbers,
            // starting with 0.

            // We need to check ahead of time, if there are at most 256 modules
            // altogether in the input, otherwise the `nextModule` variable will
            // overflow and cause a dictionary error.
            var allSubmodules = new HashSet<string>();

            foreach (var rule in rules) {
                allSubmodules.Add(rule.LowerSubmoduleName);
                allSubmodules.Add(rule.HigherSubmoduleName);
            }

            byte nextSubmodule = 0;
            var nameToSubmodule = new Dictionary<string, byte>();
            var submoduleToName = new Dictionary<byte, string>();
            var adjacencyRules = new AdjacencyRule[rules.Count];

            for (var i = 0; i < rules.Count; ++i) {
                var lowStr = rules[i].LowerSubmoduleName;
                var highStr = rules[i].HigherSubmoduleName;
                var kind = rules[i].Axis;

                byte low;
                if (nameToSubmodule.ContainsKey(lowStr)) {
                    nameToSubmodule.TryGetValue(lowStr, out low);
                } else {
                    low = nextSubmodule;
                    nameToSubmodule.Add(lowStr, low);
                    submoduleToName.Add(low, lowStr);
                    nextSubmodule++;
                }

                byte high;
                if (nameToSubmodule.ContainsKey(highStr)) {
                    nameToSubmodule.TryGetValue(highStr, out high);
                } else {
                    high = nextSubmodule;
                    nameToSubmodule.Add(highStr, high);
                    submoduleToName.Add(high, highStr);
                    nextSubmodule++;
                }

                var rule = new AdjacencyRule() {
                    kind = kind,
                    module_low = low,
                    module_high = high
                };
                adjacencyRules[i] = rule;
            }

            //
            // -- World dimensions --
            //

            var worldDimensions = worldSize.X * worldSize.Y * worldSize.Z;
            var worldSlotsPerLayer = worldSize.X * worldSize.Y;
            var worldSlotsPerRow = worldSize.X;

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

            for (var slotIndex = 0; slotIndex < slots.Count; ++slotIndex) {
                var submoduleStrings = slots[slotIndex].AllowedSubmoduleNames;
                foreach (var submoduleString in submoduleStrings) {
                    if (nameToSubmodule.TryGetValue(submoduleString, out var submoduleByte)) {
                        Debug.Assert(slotIndex < worldState.Length);

                        var blkIndex = (byte)(submoduleByte / 64u);
                        var bitIndex = (byte)(submoduleByte % 64u);
                        var mask = 1ul << bitIndex;

                        Debug.Assert(blkIndex < 4);
                        unsafe {
                            worldState[slotIndex].slot_state[blkIndex] |= mask;
                        }
                    }
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
                                                 (ushort)worldSize.X,
                                                 (ushort)worldSize.Y,
                                                 (ushort)worldSize.Z,
                                                 rngSeedLow,
                                                 rngSeedHigh);

                    switch (result) {
                        case WfcInitResult.Ok:
                            // All good
                            break;
                        case WfcInitResult.TooManyModules:
                            report = "Monoceros Solver failed: Rules refer to Modules occupying " +
                                "too many Slots.";
                            return false;
                        case WfcInitResult.WorldDimensionsZero:
                            report = "Monoceros Solver failed: World dimensions are zero.";
                            return false;
                        default:
                            report = "Monoceros Solver failed with unknown error.";
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
                            report = "Monoceros Solver failed: World state is contradictory. " +
                                "Try changing Slots, Modules or Rules. Changing random seed or " +
                                "max attempts will not help.";
                            return false;
                    }
                }
            }

            var attempts = Native.wfc_observe(wfc, maxAttempts);
            if (attempts == 0) {
                report = "Monoceros Solver failed to find solution within " + maxAttempts + " attempts";
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

                if (submodule >= 0) {
                    Debug.Assert(submodule <= byte.MaxValue);
                    var valid = submoduleToName.TryGetValue((byte)submodule, out var submoduleStr);
                    if (valid) {
                        worldSlotSubmodules.Add(submoduleStr);
                    } else {
                        report = "Monoceros Solver returned a non-existing submodule.";
                        return false;
                    }
                }
            }

            stats.ruleCount = (uint)rules.Count;
            stats.submoduleCount = (uint)submoduleToName.Count;
            stats.solveAttempts = attempts;

            report = stats.ToString();
            return true;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AdjacencyRule {
        public Axis kind;
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

            //b.Append("Rule count: ");
            //b.Append(ruleCount);
            //b.AppendLine();
            //b.Append("Submodule count: ");
            //b.Append(submoduleCount);
            //b.AppendLine();
            b.Append("Solve attempts: ");
            b.Append(solveAttempts);
            b.AppendLine();

            if (worldNotCanonical) {
                b.AppendLine(
                    "Initial world state is not canonical according to the original WFC standards.");
            }

            return b.ToString();
        }
    }

    internal class Native {
        [DllImport("monoceros-1.0-wfc.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe WfcInitResult wfc_init(IntPtr* wfc_ptr,
                                                             AdjacencyRule* adjacency_rules_ptr,
                                                             UIntPtr adjacency_rules_len,
                                                             ushort world_x,
                                                             ushort world_y,
                                                             ushort world_z,
                                                             ulong rngSeedLow,
                                                             ulong rngSeedHigh);

        [DllImport("monoceros-1.0-wfc.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern void wfc_free(IntPtr wfc);

        [DllImport("monoceros-1.0-wfc.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint wfc_observe(IntPtr wfc, uint max_attempts);

        [DllImport("monoceros-1.0-wfc.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe WfcWorldStateSetResult wfc_world_state_set(IntPtr wfc,
                                                                                 SlotState* world_state_ptr,
                                                                                 UIntPtr world_state_len);

        [DllImport("monoceros-1.0-wfc.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe void wfc_world_state_get(IntPtr wfc,
                                                               SlotState* world_state_ptr,
                                                               UIntPtr world_state_len);
    }

}