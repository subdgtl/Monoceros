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

        public override Guid ComponentGuid => new Guid("97FF0AFD-0FA8-42ED-82C2-6D14B7A629CE");

        protected override System.Drawing.Bitmap Icon => Properties.Resources.solver;

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
            pManager.AddIntegerParameter("Max Observations",
                                         "O",
                                         "Maximum Number of Solver Observations per Attempt (leave default for virtually unlimited)",
                                         GH_ParamAccess.item,
                                         Int32.MaxValue);
            pManager.AddIntegerParameter("Max Time",
                                         "T",
                                         "Maximum Time spent with Attempts (milliseconds). Negative and 0 = infinity",
                                         GH_ParamAccess.item,
                                         0);
            pManager.AddBooleanParameter("Use Shannon Entropy",
                                         "E",
                                         "Whether to use Shannon Entropy instead of the simpler linear entropy calculations",
                                         GH_ParamAccess.item,
                                         false);
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
            pManager.AddBooleanParameter("Deterministic",
                                         "OK",
                                         "Did the Monoceros WFC Solver find a solution that can be Materialized?",
                                         GH_ParamAccess.item);
            pManager.AddBooleanParameter("Contradictory",
                                         "C",
                                         "Did the Monoceros WFC Solver end with a contradictory solution?",
                                         GH_ParamAccess.item);
            pManager.AddIntegerParameter("Seed",
                                         "S",
                                         "Random seed of the solution",
                                         GH_ParamAccess.item);
            pManager.AddIntegerParameter("Attempts",
                                         "A",
                                         "Number of attempts needed to find the solution",
                                         GH_ParamAccess.item);
            pManager.AddIntegerParameter("Observations",
                                         "O",
                                         "Number of observations needed to find the solution",
                                         GH_ParamAccess.item);
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
            var maxTime = 0;
            bool useShannonEntropy = false;
            var maxObservations = 0;

            // Due to many early return branches it is easier to set and the re-set the output pin
            DA.SetData(3, false);
            DA.SetData(4, true);

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

            if (!DA.GetData(5, ref maxObservations)) {
                return;
            }

            if (!DA.GetData(6, ref maxTime)) {
                return;
            }

            if (!DA.GetData(7, ref useShannonEntropy)) {
                return;
            }

            if (maxObservations < 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Maximum number of " +
                    "observations must be 0 or more.");
                return;
            }

            if (maxAttempts < 1) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Maximum number of attempts must " +
                    "be 1 or more.");
                return;
            }

            Entropy entropy = Entropy.Linear;
            if (useShannonEntropy) {
                entropy = Entropy.Shannon;
            }

            if (slotsRaw.Any(slot => slot == null || !slot.IsValid)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or more Slot are null or invalid.");
            }
            var slotsValid = slotsRaw.Where(slot => slot != null || slot.IsValid);
            var anySlotAllowsAll = slotsValid.Any(slot => slot.AllowsAnyModule);

            if (rulesRaw.Any(rule => rule == null || !rule.IsValid)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or more Rule are null or invalid.");
            }
            var rulesValid = rulesRaw.Where(rule => rule != null || rule.IsValid);


            if (modulesRaw.Any(module => module == null || !module.IsValid)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or more Modules are null or invalid.");
            }
            var modulesValid = modulesRaw.Where(module => module != null || module.IsValid);

            if (!anySlotAllowsAll) {
                foreach (var module in modulesValid) {
                    if (!slotsValid.Any(slot => slot.AllowedModuleNames.Contains(module.Name))) {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        "Module \"" + module.Name + "\" will be excluded from the " +
                        "solution because it is not allowed in any Slot.");
                    }
                }
            }

            // TODO: Consider removing this because it may be redundant with the check if all connectors are described by any Rule
            foreach (var module in modulesValid) {
                if (!rulesValid.Any(rule => rule.UsesModule(module.Name))) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Module \"" + module.Name + "\" will be excluded from the " +
                    "solution because it is not described by any Rule.");
                }
            }

            var modulesClean = anySlotAllowsAll
                ? modulesValid
                : modulesValid.Where(module =>
                    slotsValid.Any(slot => slot.AllowedModuleNames.Contains(module.Name))
                    && rulesValid.Any(rule => rule.UsesModule(module.Name)));

            if (!modulesClean.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Modules collected.");
                return;
            }

            var rulesClean = rulesValid
                .Where(rule => rule.UsesModule(Config.OUTER_MODULE_NAME) || rule.IsValidWithModules(modulesClean));


            // TODO: Consider telling the user which ones are skipped
            if (rulesValid.Any(rule => !rule.UsesModule(Config.OUTER_MODULE_NAME) && !rule.IsValidWithModules(modulesClean))) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "One or more Rules will be excluded from the solution because " +
                        "they do not refer to any existing Module.");
            }

            if (!rulesClean.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Modules collected.");
                return;
            }

            // TODO: Consider smarter compatibility check or non-uniform scaling
            if (!Slot.AreSlotDiagonalsCompatible(slotsValid)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slots are not defined with the same diagonal.");
                return;
            }
            var diagonal = slotsValid.First().Diagonal;

            // TODO: Consider smarter compatibility check or base plane conversion
            if (!Slot.AreSlotBasePlanesCompatible(slotsValid)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slots are not defined with the same base plane.");
                return;
            }
            var slotBasePlane = slotsValid.First().BasePlane;

            if (!Slot.AreSlotLocationsUnique(slotsValid)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot centers are not unique.");
                return;
            }

            var moduleDiagonal = modulesClean.First().PartDiagonal;

            if (modulesClean.Any(module => module.PartDiagonal != moduleDiagonal)) {
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

            var slotsClean = new List<Slot>();
            foreach (var slot in slotsValid) {
                if (slot.AllowedModuleNames.All(moduleName =>
                moduleName == Config.OUTER_MODULE_NAME
                || modulesUsable.Any(module => module.Name == moduleName))) {
                    slotsClean.Add(slot);
                } else {
                    var existingModuleNames = slot.AllowedModuleNames.Where(moduleName =>
                    moduleName == Config.OUTER_MODULE_NAME
                    || modulesUsable.Any(module => module.Name == moduleName)).ToList();
                    if (existingModuleNames.Any()) {
                        slotsClean.Add(new Slot(slot.BasePlane,
                                                slot.RelativeCenter,
                                                slot.Diagonal,
                                                slot.AllowsAnyModule,
                                                existingModuleNames,
                                                new List<string>(),
                                                0));
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Slot refers to a non-existent Module.");
                    } else {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot allows no Module to be placed.");
                        return;
                    }
                }
            }

            if (!slotsClean.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Slots collected.");
                return;
            }

            var allPartsCount = modulesUsable
                .Aggregate(0, (sum, module) => sum + module.PartCenters.Count);

            if (allPartsCount > Config.MAX_PARTS) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "The Modules occupy too many Slots: " + allPartsCount + ". Maximum allowed :" +
                    Config.MAX_PARTS + ".");
                return;
            }

            Module.GenerateEmptySingleModule(Config.OUTER_MODULE_NAME,
                                             Config.INDIFFERENT_TAG,
                                             new Rhino.Geometry.Vector3d(1, 1, 1),
                                             out var moduleOut,
                                             out var rulesOutTyped);
            var rulesAtBoundary = rulesOutTyped.Select(ruleTyped => new Rule(ruleTyped));
            rulesClean = rulesClean.Concat(rulesAtBoundary);

            // Convert AllowEverything slots into an explicit list of allowed modules (except Out)
            var allModuleNames = modulesUsable.Select(module => module.Name).ToList();
            var slotsUnwrapped = slotsClean.Select(slot =>
                slot.AllowsAnyModule ?
                    slot.DuplicateWithModuleNames(allModuleNames) :
                    slot
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
                                             allPartsCount);
                }
            }

            // Unwrap module names to part names for all slots
            var worldForSolver = worldSlots.Select(slot => {
                if (slot.AllowedPartNames.Count != 0) {
                    return slot.DuplicateWithPartsCount(allPartsCount);
                }

                var partNames = new List<string>();
                foreach (var moduleName in slot.AllowedModuleNames) {
                    var module = modulesUsable.Find(m => m.Name == moduleName);
                    partNames.AddRange(module.PartNames);
                }
                return slot.DuplicateWithPartsCountAndNames(allPartsCount, partNames);
            });

            foreach (var slot in worldForSolver) {
                if (!slot.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, slot.IsValidWhyNot);
                    return;
                }

                if (slot.IsContradictory) {
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

            uint maxObservationsUint = maxObservations == Int32.MaxValue
                ? UInt32.MaxValue
                : (uint)maxObservations;

            // SOLVER
            var stats = Solve(rulesForSolver.Distinct().ToList(),
                                worldSize,
                                worldForSolver.ToList(),
                                randomSeed,
                                maxAttempts,
                                maxTime,
                                maxObservationsUint,
                                entropy,
                                out var solvedSlotPartsTree);

            if (stats.contradictory) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, stats.report);
            }

            // Remember module name for each part name
            var partToModuleName = new Dictionary<string, string>();
            foreach (var module in modulesUsable) {
                foreach (var partName in module.PartNames) {
                    partToModuleName.Add(partName, module.Name);
                }
            }

            // Sort slots into the same order as they were input
            var slotsSolved = slotOrder.Select(index => {
                if (solvedSlotPartsTree.Count <= index) {
                    return worldSlots[index];
                }
                var allowedParts = solvedSlotPartsTree[index];
                var allowedModules = allowedParts
                    .Select(allowedPart => partToModuleName[allowedPart])
                    .Distinct()
                    .ToList();
                // Convert world from solver format into slots
                return new Slot(slotBasePlane,
                                From1DTo3D(index, worldMin, worldMax),
                                diagonal,
                                false,
                                allowedModules,
                                allowedParts,
                                allPartsCount);
            });

            if (!stats.deterministic) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Current solution is partial. " +
                    "A complete valid solution is not guaranteed!");
            }

            DA.SetData(0, stats.ToString());
            DA.SetDataList(1, slotsSolved);
            DA.SetData(2, stats.deterministic);
            DA.SetData(3, stats.contradictory);
            DA.SetData(4, stats.seed);
            DA.SetData(5, stats.solveAttempts);
            DA.SetData(6, stats.observations);
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

        private bool AreModuleNamesUnique(IEnumerable<Module> modules) {
            var moduleNameGroupLengths = modules
                .GroupBy(module => module.Name)
                .Where(g => g.Count() > 1);
            return !moduleNameGroupLengths.Any();
        }

        private Stats Solve(List<RuleForSolver> rules,
                           Point3i worldSize,
                           List<Slot> slots,
                           int randomSeed,
                           int maxAttemptsInt,
                           int maxTime,
                           uint maxObservations,
                           Entropy entropy,
                           out List<List<string>> worldSlotPartsTree) {
            worldSlotPartsTree = new List<List<string>>();

            var stats = new Stats();
            stats.worldNotCanonical = true;
            stats.contradictory = true;
            stats.deterministic = false;
            stats.observationLimit = maxObservations;
            stats.averageObservations = 0;
            stats.observations = 0;
            stats.partCount = 0;
            stats.slotCount = (uint)slots.Count;
            stats.report = "";
            stats.solveAttempts = 0;
            stats.ruleCount = (uint)rules.Count;
            stats.entropy = entropy;

            var limitTime = maxTime > 0;

            //
            // -- Adjacency rules --
            //

            // Check ahead of time, if there are at most maxModuleCount modules
            // altogether in the input.
            uint maxModuleCount = Native.wfc_max_module_count_get();
            {
                var allParts = new HashSet<string>();

                foreach (var rule in rules) {
                    allParts.Add(rule.LowerPartName);
                    allParts.Add(rule.HigherPartName);
                }

                if (allParts.Count > maxModuleCount) {
                    stats.report = "Too many modules. Maximum allowed is " + maxModuleCount;
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, stats.report);
                    return stats;
                }
            }

            byte nextPart = 0;
            var nameToPart = new Dictionary<string, byte>();
            var partToName = new Dictionary<byte, string>();
            var adjacencyRules = new AdjacencyRule[rules.Count];

            for (var i = 0; i < rules.Count; ++i) {
                var lowStr = rules[i].LowerPartName;
                var highStr = rules[i].HigherPartName;
                var kind = rules[i].Axis;

                byte low;
                if (nameToPart.ContainsKey(lowStr)) {
                    nameToPart.TryGetValue(lowStr, out low);
                } else {
                    low = nextPart;
                    nameToPart.Add(lowStr, low);
                    partToName.Add(low, lowStr);
                    nextPart++;
                    Debug.Assert(nextPart < maxModuleCount);
                }

                byte high;
                if (nameToPart.ContainsKey(highStr)) {
                    nameToPart.TryGetValue(highStr, out high);
                } else {
                    high = nextPart;
                    nameToPart.Add(highStr, high);
                    partToName.Add(high, highStr);
                    nextPart++;
                    Debug.Assert(nextPart < maxModuleCount);
                }

                AdjacencyRule rule;
                rule.kind = kind;
                rule.module_low = low;
                rule.module_high = high;
                adjacencyRules[i] = rule;
            }

            stats.partCount = (uint)partToName.Count;

            //
            // -- World dimensions --
            //

            ushort worldX = (ushort)worldSize.X;
            ushort worldY = (ushort)worldSize.Y;
            ushort worldZ = (ushort)worldSize.Z;
            uint worldDimensions = (uint)worldX * worldY * worldZ;
            uint worldSlotsPerLayer = (uint)worldX * worldY;
            uint worldSlotsPerRow = worldX;

            //
            // -- World slot positions and modules --
            //

            // This array is re-used for both input and output (if input world
            // state was provided). This is ok, because
            // wfc_world_state_slots_get does clear it to zero before writing to
            // it.
            var worldStateSlots = new SlotState[worldDimensions];

            // ... WE do need to clear it to zero, however. C# does not
            // initialize slot_state for us!
            for (var i = 0; i < worldStateSlots.Length; ++i) {
                unsafe {
                    worldStateSlots[i].slot_state[0] = 0;
                    worldStateSlots[i].slot_state[1] = 0;
                    worldStateSlots[i].slot_state[2] = 0;
                    worldStateSlots[i].slot_state[3] = 0;
                }
            }

            for (var slotIndex = 0; slotIndex < slots.Count; ++slotIndex) {
                var partStrings = slots[slotIndex].AllowedPartNames;
                foreach (var partString in partStrings) {
                    if (nameToPart.TryGetValue(partString, out var partByte)) {
                        Debug.Assert(slotIndex < worldStateSlots.Length);

                        byte blkIndex = (byte)(partByte / 64u);
                        byte bitIndex = (byte)(partByte % 64u);

                        Debug.Assert(blkIndex < 4);
                        unsafe {
                            worldStateSlots[slotIndex].slot_state[blkIndex] |= 1ul << bitIndex;
                        }
                    }
                }
            }

            uint attempts = 0;
            WfcObserveResult observationResult = WfcObserveResult.Contradiction;
            uint spentObservations;
            uint maxAttempts = (uint)maxAttemptsInt;

            var wfcRngStateHandle = IntPtr.Zero;
            var wfcWorldStateHandle = IntPtr.Zero;
            var wfcWorldStateHandleBackup = IntPtr.Zero;

            var randomMajor = new Random(randomSeed);
            var firstAttempt = true;

            var timeStart = DateTime.UtcNow;

            while (true) {

                //
                // -- Random seed --
                //
                // wfc_rng_state_init needs 128 bits worth of random seed, but that
                // is tricky to provide from GH.  We let GH provide an int, use it
                // to seed a C# Random, get 16 bytes of data from that and copy
                // those into two u64's.

                int currentSeed;

                if (firstAttempt) {
                    currentSeed = randomSeed;
                    firstAttempt = false;
                } else {
                    currentSeed = randomMajor.Next();
                }

                stats.seed = currentSeed;

                var random = new Random(currentSeed);
                byte[] rngSeedLowArr = new byte[8];
                byte[] rngSeedHighArr = new byte[8];
                random.NextBytes(rngSeedLowArr);
                random.NextBytes(rngSeedHighArr);

                if (!BitConverter.IsLittleEndian) {
                    // If we are running on a BE machine, we need to reverse the bytes,
                    // because low and high are defined to always be LE.
                    Array.Reverse(rngSeedLowArr);
                    Array.Reverse(rngSeedHighArr);
                }

                ulong rngSeedLow = BitConverter.ToUInt64(rngSeedLowArr, 0);
                ulong rngSeedHigh = BitConverter.ToUInt64(rngSeedHighArr, 0);


                //
                // -- Run the thing and **pray** --
                //


                unsafe {
                    Native.wfc_rng_state_init(&wfcRngStateHandle, rngSeedLow, rngSeedHigh);

                    fixed (AdjacencyRule* adjacencyRulesPtr = &adjacencyRules[0]) {
                        var result = Native.wfc_world_state_init(&wfcWorldStateHandle,
                                                                 adjacencyRulesPtr,
                                                                 (UIntPtr)adjacencyRules.Length,
                                                                 worldX,
                                                                 worldY,
                                                                 worldZ,
                                                                 entropy);

                        switch (result) {
                            case WfcWorldStateInitResult.Ok:
                                // All good
                                break;
                            case WfcWorldStateInitResult.ErrTooManyModules:
                                stats.report = "Monoceros Solver failed: Rules refer to Modules occupying " +
                                    "too many Slots.";
                                return stats;
                            case WfcWorldStateInitResult.ErrWorldDimensionsZero:
                                stats.report = "Monoceros Solver failed: World dimensions are zero.";
                                return stats;
                            default:
                                stats.report = "Monoceros Solver failed with unknown error.";
                                return stats;
                        }
                    }

                    fixed (SlotState* worldStateSlotsPtr = &worldStateSlots[0]) {
                        var result = Native.wfc_world_state_slots_set(wfcWorldStateHandle,
                                                                      worldStateSlotsPtr,
                                                                      (UIntPtr)worldStateSlots.Length);
                        switch (result) {
                            case WfcWorldStateSlotsSetResult.Ok:
                                // All good
                                stats.worldNotCanonical = false;
                                break;
                            case WfcWorldStateSlotsSetResult.OkWorldNotCanonical:
                                // All good, but we the slots we gave were not
                                // canonical. wfc_world_state_slots_set fixed that for us.
                                stats.worldNotCanonical = true;
                                break;
                            case WfcWorldStateSlotsSetResult.ErrWorldContradictory:
                                stats.report = "Monoceros Solver failed: World state is contradictory. " +
                                    "Try changing Slots, Modules, Rules or add boundary Rules. Changing " +
                                    "random seed or max attempts will not help.";
                                return stats;
                        }
                    }

                    Native.wfc_world_state_init_from(&wfcWorldStateHandleBackup, wfcWorldStateHandle);
                }

                spentObservations = 0;
                stats.averageObservations = 0;

                unsafe {

                    observationResult = Native.wfc_observe(wfcWorldStateHandle,
                                                    wfcRngStateHandle,
                                                    maxObservations,
                                                    &spentObservations);
                    attempts++;
                    stats.averageObservations += spentObservations;

                    if (observationResult == WfcObserveResult.Deterministic
                        || observationResult == WfcObserveResult.Nondeterministic
                        || attempts == maxAttempts
                        || (limitTime && ((DateTime.UtcNow - timeStart).TotalMilliseconds > maxTime))) {
                        break;
                    }
                    Native.wfc_world_state_clone_from(wfcWorldStateHandle, wfcWorldStateHandleBackup);
                }
            }

            if (observationResult == WfcObserveResult.Deterministic) {
                stats.deterministic = true;
                stats.contradictory = false;
            }
            if (observationResult == WfcObserveResult.Nondeterministic) {
                stats.deterministic = false;
                stats.contradictory = false;
            }
            if (observationResult == WfcObserveResult.Contradiction) {
                stats.deterministic = false;
                stats.contradictory = true;
            }

            stats.observations = spentObservations;
            stats.averageObservations /= attempts == 0 ? 1 : attempts;

            stats.solveAttempts = attempts;

            if (stats.contradictory) {
                if (attempts == maxAttempts) {
                    stats.report = "WFC solver failed to find solution within " + maxAttempts + " attempts";
                } else if (limitTime && attempts < maxAttempts) {
                    stats.report = "WFC solver failed to find solution within time limit " + maxTime + " milliseconds";
                } else {
                    stats.report = "WFC solver failed to find solution for unknown reason. Please report this error, including screenshots, Rhino file and Grasshopper file at monoceros@sub.digital. Thank you!";
                }
            }

            unsafe {
                fixed (SlotState* worldStateSlotsPtr = &worldStateSlots[0]) {
                    Native.wfc_world_state_slots_get(wfcWorldStateHandle,
                                                     worldStateSlotsPtr,
                                                     (UIntPtr)worldStateSlots.Length);
                }
            }

            Native.wfc_world_state_free(wfcWorldStateHandle);
            Native.wfc_rng_state_free(wfcRngStateHandle);

            // -- Output: World state --
            for (var i = 0; i < worldStateSlots.Length; ++i) {
                var partShorts = new List<short>();
                for (int blkIndex = 0; blkIndex < 4; ++blkIndex) {
                    for (int bitIndex = 0; bitIndex < 64; ++bitIndex) {
                        unsafe {
                            if ((worldStateSlots[i].slot_state[blkIndex] & (1ul << bitIndex)) != 0) {
                                partShorts.Add((short)(64 * blkIndex + bitIndex));
                            }
                        }
                    }
                }

                var currentWorldSlotParts = new List<string>();
                foreach (var part in partShorts) {
                    Debug.Assert(part >= 0);
                    Debug.Assert(part <= byte.MaxValue);
                    Debug.Assert(part < maxModuleCount);
                    var valid = partToName.TryGetValue((byte)part, out var partStr);
                    if (valid) {
                        currentWorldSlotParts.Add(partStr);
                    } else {
                        stats.report = "Monoceros WFC Solver returned a non-existing Module Part.";
                        return stats;
                    }
                }
                worldSlotPartsTree.Add(currentWorldSlotParts);
            }

            if (stats.deterministic) {
                stats.report = "Monoceros WFC Solver found a solution.";
            }
            if (!stats.deterministic && !stats.contradictory) {
                stats.report = "Monoceros WFC Solver found a partial solution. Increase number " +
                    "of observations or run another Solver with the output Slots from this Solver. " +
                    "WARNING: There is no guarantee that the current setup can be eventually " +
                    "solved even though the partial solution exists.";
            }

            return stats;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AdjacencyRule {
        public Axis kind;
        public byte module_low;
        public byte module_high;
    }

    internal enum Entropy : uint {
        Linear = 0,
        Shannon = 1,
    }

    internal enum WfcWorldStateInitResult : uint {
        Ok = 0,
        ErrTooManyModules = 1,
        ErrWorldDimensionsZero = 2,
    }

    internal enum WfcWorldStateSlotsSetResult : uint {
        Ok = 0,
        OkWorldNotCanonical = 1,
        ErrWorldContradictory = 2,
    }

    internal enum WfcObserveResult : uint {
        Deterministic = 0,
        Contradiction = 1,
        Nondeterministic = 2,
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
        public uint partCount;
        public uint slotCount;
        public uint solveAttempts;
        public int seed;
        public uint observationLimit;
        public uint observations;
        public uint averageObservations;
        public bool deterministic;
        public bool contradictory;
        public bool worldNotCanonical;
        public Entropy entropy;
        public string report;

        public override string ToString( ) {
            var b = new StringBuilder(1024);

            b.Append(report);
            b.AppendLine();

            b.Append("Rule count: ");
            b.Append(ruleCount);
            b.AppendLine();
            b.Append("Part count: ");
            b.Append(partCount);
            b.AppendLine();
            b.Append("Slot count (including outer Slots): ");
            b.Append(slotCount);
            b.AppendLine();
            b.Append("Solution random seed: ");
            b.Append(seed);
            b.AppendLine();
            b.Append("Solve attempts: ");
            b.Append(solveAttempts);
            b.AppendLine();
            b.Append("Approach to entropy calculation: ");
            if (entropy == Entropy.Linear) {
                b.Append("linear");
            }
            if (entropy == Entropy.Shannon) {
                b.Append("Shannon weighted");
            }
            b.AppendLine();
            b.Append("Solver observations limited to: ");
            b.Append(observationLimit);
            b.AppendLine();
            b.Append("Observations required to find the solution: ");
            b.Append(observations);
            b.AppendLine();
            if (solveAttempts > 1) {
                b.Append("Average observation count per attempt: ");
                b.Append(averageObservations);
                b.AppendLine();
            }

            if (contradictory) {
                b.AppendLine(
                    "The solution is contradictory - some Slots are not allowed to contain any Module.");
            } else {

                if (deterministic) {
                    b.AppendLine(
                        "The solution is deterministic - each Slot allows placement of exactly one " +
                        "Module Part.");
                } else {
                    b.AppendLine(
                        "The solution is non-deterministic - at least some Slots allow placement of " +
                        "more than one Module Part.");
                }
                b.AppendLine();


                if (worldNotCanonical) {
                    b.AppendLine(
                        "Initial world state is not canonical according to the original WFC standards.");
                }
            }
            b.AppendLine();

            return b.ToString();
        }
    }

    internal class Native {
        [DllImport("monoceros-wfc-0.2.0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint wfc_max_module_count_get( );

        [DllImport("monoceros-wfc-0.2.0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static unsafe extern WfcWorldStateInitResult wfc_world_state_init(IntPtr* wfc_world_state_handle_ptr,
                                                                                   AdjacencyRule* adjacency_rules_ptr,
                                                                                   UIntPtr adjacency_rules_len,
                                                                                   ushort world_x,
                                                                                   ushort world_y,
                                                                                   ushort world_z,
                                                                                   Entropy entropy);

        [DllImport("monoceros-wfc-0.2.0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static unsafe extern void wfc_world_state_init_from(IntPtr* wfc_world_state_handle_ptr,
                                                                     IntPtr source_wfc_world_state_handle);

        [DllImport("monoceros-wfc-0.2.0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static unsafe extern void wfc_world_state_clone_from(IntPtr destination_wfc_world_state_handle,
                                                                      IntPtr source_wfc_world_state_handle);

        [DllImport("monoceros-wfc-0.2.0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern void wfc_world_state_free(IntPtr wfc_world_state_handle);

        [DllImport("monoceros-wfc-0.2.0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static unsafe extern WfcWorldStateSlotsSetResult wfc_world_state_slots_set(IntPtr wfc_world_state_handle,
                                                                                            SlotState* slots_ptr,
                                                                                            UIntPtr slots_len);

        [DllImport("monoceros-wfc-0.2.0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static unsafe extern void wfc_world_state_slots_get(IntPtr wfc_world_state_handle,
                                                                     SlotState* slots_ptr,
                                                                     UIntPtr slots_len);

        [DllImport("monoceros-wfc-0.2.0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static unsafe extern void wfc_rng_state_init(IntPtr* wfc_rng_state_handle_ptr,
                                                              ulong rng_seed_low,
                                                              ulong rng_seed_high);

        [DllImport("monoceros-wfc-0.2.0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern void wfc_rng_state_free(IntPtr wfc_rng_state_handle);

        [DllImport("monoceros-wfc-0.2.0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static unsafe extern WfcObserveResult wfc_observe(IntPtr wfc_world_state_handle,
                                                            IntPtr wfc_rng_state_handle,
                                                            uint max_observations,
                                                            uint* spent_observations);
    }

}
