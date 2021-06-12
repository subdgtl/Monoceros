using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new SlotParameter(),
                                  "Slots",
                                  "S",
                                  "All Monoceros Slots as flat list",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new ModuleParameter(),
                                  "Modules",
                                  "M",
                                  "All Monoceros Modules as flat list",
                                  GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "All Monoceros Rules as flat list",
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
                                         "Maximum Time spent with Attempts (milliseconds). Negative or 0 = infinity",
                                         GH_ParamAccess.item,
                                         0);
            pManager.AddBooleanParameter("Use Shannon Entropy",
                                         "E",
                                         "Whether to use Shannon Entropy instead of the simpler linear entropy calculations",
                                         GH_ParamAccess.item,
                                         false);
        }

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

        protected override void SolveInstance(IGH_DataAccess DA) {

            const int IN_PARAM_SLOTS = 0;
            const int IN_PARAM_MODULES = 1;
            const int IN_PARAM_RULES = 2;
            const int IN_PARAM_SEED = 3;
            const int IN_PARAM_MAX_ATTEMPTS = 4;
            const int IN_PARAM_MAX_OBSERVATIONS = 5;
            const int IN_PARAM_MAX_TIME = 6;
            const int IN_PARAM_SHANNON_ENTROPY = 7;

            const int OUT_PARAM_REPORT = 0;
            const int OUT_PARAM_SLOTS = 1;
            const int OUT_PARAM_DETERMINISTIC = 2;
            const int OUT_PARAM_CONTRADICTORY = 3;
            const int OUT_PARAM_SEED = 4;
            const int OUT_PARAM_ATTEMPTS = 5;
            const int OUT_PARAM_OBSERVATIONS = 6;

            var slots = new List<Slot>();
            var modules = new List<Module>();
            var rules = new List<Rule>();
            var randomSeed = 42;
            var maxAttempts = 10;
            var maxTimeMillis = 0;
            bool useShannonEntropy = false;
            var maxObservations = 0;

            if (!DA.GetDataList(IN_PARAM_SLOTS, slots)) {
                return;
            }

            if (!DA.GetDataList(IN_PARAM_MODULES, modules)) {
                return;
            }

            if (!DA.GetDataList(IN_PARAM_RULES, rules)) {
                return;
            }

            if (!DA.GetData(IN_PARAM_SEED, ref randomSeed)) {
                return;
            }

            if (!DA.GetData(IN_PARAM_MAX_ATTEMPTS, ref maxAttempts)) {
                return;
            }

            if (!DA.GetData(IN_PARAM_MAX_OBSERVATIONS, ref maxObservations)) {
                return;
            }

            if (!DA.GetData(IN_PARAM_MAX_TIME, ref maxTimeMillis)) {
                return;
            }

            if (!DA.GetData(IN_PARAM_SHANNON_ENTROPY, ref useShannonEntropy)) {
                return;
            }

            Entropy entropy = Entropy.Linear;
            if (useShannonEntropy) {
                entropy = Entropy.Shannon;
            }

            // The Solver cannot compensate to missing data,
            // therefore it exits early if any input is not fully valid.
            // As this happens often, it checks all scenarios and informs the user about the reason.

            var invalidInputs = false;

            if (maxObservations < 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Maximum number of " +
                    "observations must be 0 or more.");
                invalidInputs = true;
            }

            if (maxAttempts < 1) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Maximum number of attempts must " +
                    "be 1 or more.");
                invalidInputs = true;
            }

            foreach (var slot in slots) {
                if (slot == null || !slot.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Some Slots are null or invalid.");
                    invalidInputs = true;
                    break;
                }
            }

            if (slots.Count == 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to collect any Slots.");
                invalidInputs = true;
            }

            foreach (var rule in rules) {
                if (rule == null || !rule.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Some Rules are null or invalid.");
                    invalidInputs = true;
                    break;
                }
            }

            if (rules.Count == 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to collect any Rules.");
                invalidInputs = true;
            }

            foreach (var module in modules) {
                if (module == null || !module.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Some Modules are null or invalid.");
                    invalidInputs = true;
                    break;
                }
            }

            if (modules.Count == 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to collect any Modules.");
                invalidInputs = true;
            }

            // More than one input streams may be invalid, therefore check all of them before exit.
            if (invalidInputs) {
                DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                return;
            }

            // All diagonals are supposed to be equal. It will be checked later. 
            var moduleDiagonal = modules[0].PartDiagonal;

            // A special Module named OUTER_MODULE_NAME is required to be fed into Slots filling
            // the empty parts of the orthogonal World block required by the Solver.
            Module.GenerateEmptySingleModule(Config.OUTER_MODULE_NAME,
                                             Config.INDIFFERENT_TAG,
                                             moduleDiagonal,
                                             out var outModule,
                                             out var typedRulesOfOutModule);

            modules.Add(outModule);
            foreach (var typedRuleOfOutModule in typedRulesOfOutModule) {
                rules.Add(new Rule(typedRuleOfOutModule));
            }


            // The following couple of pages tries to remove Modules that cannot be used either
            // because they are not referenced by any Rule or are not allowed to be placed by
            // any Slot. The Slots will be then lowered and only usable Modules will be listed as
            // allowed. This may render some Slots contradictory (not allowing anything) and the
            // solver does not have to even try resolving the setup.
            // It is also important to clean up Rules, because they often refer to two Modules at
            // once. If a Rule is removed due to one unused Module, it may disqualify the other one
            // as well as it may not remain described by any Rule.

            // If at least one Slot allows placement of all Modules, then removing Modules that
            // are not allowed to be placed by any Slot becomes irrelevant.

            var anySlotAllowsAll = false;
            foreach (var slot in slots) {
                if (slot.AllowsAnyModule) {
                    anySlotAllowsAll = true;
                    break;
                }
            }

            var moduleNames = new HashSet<string>();
            var modulePartNames = new List<string>();
            var modulePartNameToModuleName = new Dictionary<string, string>();
            var moduleNameToModulePartNames = new Dictionary<string, List<string>>();
            foreach (var module in modules) {
                moduleNames.Add(module.Name);
                modulePartNames.AddRange(module.PartNames);
                foreach (var partName in module.PartNames) {
                    modulePartNameToModuleName.Add(partName, module.Name);
                }
                moduleNameToModulePartNames.Add(module.Name, module.PartNames);
            }

            if (moduleNames.Count != modules.Count) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module Names are not unique.");
                DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                return;
            }

            // Collect Module Names that are listed in Slots
            var slotModuleNames = new HashSet<string> { outModule.Name };
            foreach (var slot in slots) {
                // For a Slot AllowedPartNames have higher priority than AllowedModuleNames.
                // If the part names are defined, the AllowedModuleNames need to be computed from them.
                if (slot.AllowedPartNames.Count > 0) {
                    foreach (var modulePartName in slot.AllowedPartNames) {
                        if (modulePartNameToModuleName.ContainsKey(modulePartName)) {
                            slotModuleNames.Add(modulePartNameToModuleName[modulePartName]);
                        } else {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slot refers to unavailable Module Part Name.");
                            DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                            return;
                        }
                    }
                } else {
                    foreach (var moduleName in slot.AllowedModuleNames) {
                        slotModuleNames.Add(moduleName);
                    }
                }
            }

            // Collect Module Names that are referenced by Rules
            var ruleModuleNames = new HashSet<string>();
            foreach (var rule in rules) {
                if (rule.IsExplicit) {
                    if (!ruleModuleNames.Contains(rule.Explicit.SourceModuleName)) {
                        ruleModuleNames.Add(rule.Explicit.SourceModuleName);
                    }
                    if (!ruleModuleNames.Contains(rule.Explicit.TargetModuleName)) {
                        ruleModuleNames.Add(rule.Explicit.TargetModuleName);
                    }
                }
                if (rule.IsTyped) {
                    if (!ruleModuleNames.Contains(rule.Typed.ModuleName)) {
                        ruleModuleNames.Add(rule.Typed.ModuleName);
                    }
                }
            }

            // Remove Rules that refer to unavailable Modules.
            for (var ruleIndex = rules.Count - 1; ruleIndex >= 0; ruleIndex--) {
                var rule = rules[ruleIndex];
                if (!(rule.IsExplicit
                    && moduleNames.Contains(rule.Explicit.SourceModuleName)
                    && moduleNames.Contains(rule.Explicit.TargetModuleName))
                    && !(rule.IsTyped
                    && moduleNames.Contains(rule.Typed.ModuleName))) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        "Rule " + rule + " will be excluded from the solution because it does not refer to any " +
                        "existing Module.");
                    rules.RemoveAt(ruleIndex);
                }
            }

            if (rules.Count == 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "All provided Rules refer to unavailable Modules.");
                DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                return;
            }

            // Monoceros currently does not stretch or squeeze Modules to fit into Slots.
            var slotDiagonal = slots[0].Diagonal;
            if (!moduleDiagonal.Equals(slotDiagonal)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Modules and Slots are not defined with the same diagonal.");
                DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                return;
            }

            var slotBasePlane = slots[0].BasePlane;
            var uniqueSlotCenters = new List<Point3i>(slots.Count);
            foreach (var slot in slots) {
                // Monoceros currently does not support heterogeneous Slots
                if (!slot.Diagonal.Equals(slotDiagonal)) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                      "Slots are not defined with the same diagonal.");
                    DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                    return;
                }
                // TODO: Support varying base plane as long as the Slots generate a homogeneous grid
                // with no overlapping Slots. Such collection may be generated by
                // i.e. by Rule Assemble component.
                if (!slot.BasePlane.Equals(slotBasePlane)) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                      "Slots are not defined with the same base plane.");
                    DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                    return;
                }
                if (uniqueSlotCenters.Contains(slot.RelativeCenter)) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot centers are not unique.");
                    DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                    return;
                } else {
                    uniqueSlotCenters.Add(slot.RelativeCenter);
                }

            }

            // If a Module Connector is not referenced by any Rule, it can not be placed next
            // to anything. Therefore such Module can be removed from the list of usable Modules. 
            var modulesConnectorUsePattern = new Dictionary<string, bool[]>();
            foreach (var module in modules) {
                var usePattern = new bool[module.Connectors.Count];
                for (var i = 0; i < usePattern.Length; i++) {
                    usePattern[i] = false;
                }
                modulesConnectorUsePattern.Add(module.Name, usePattern);
            }

            foreach (var rule in rules) {
                if (rule.IsExplicit) {
                    if (modulesConnectorUsePattern.ContainsKey(rule.Explicit.SourceModuleName)
                        && rule.Explicit.SourceConnectorIndex < modulesConnectorUsePattern[rule.Explicit.SourceModuleName].Length) {
                        modulesConnectorUsePattern[rule.Explicit.SourceModuleName][rule.Explicit.SourceConnectorIndex] = true;
                    } else {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Rule " + rule + " refers to unavailable source Module or Connector.");
                        DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                        return;

                    }
                    if (modulesConnectorUsePattern.ContainsKey(rule.Explicit.TargetModuleName)
                        && rule.Explicit.TargetConnectorIndex < modulesConnectorUsePattern[rule.Explicit.TargetModuleName].Length) {
                        modulesConnectorUsePattern[rule.Explicit.TargetModuleName][rule.Explicit.TargetConnectorIndex] = true;
                    } else {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Rule " + rule + " refers to unavailable target Module or Connector.");
                        DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                        return;
                    }
                }
                if (rule.IsTyped) {
                    if (modulesConnectorUsePattern.ContainsKey(rule.Typed.ModuleName)
                        && rule.Typed.ConnectorIndex < modulesConnectorUsePattern[rule.Typed.ModuleName].Length) {
                        modulesConnectorUsePattern[rule.Typed.ModuleName][rule.Typed.ConnectorIndex] = true;
                    } else {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Rule " + rule + " refers to unavailable target Module or Connector.");
                        DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                        return;
                    }
                }
            }


            for (var moduleIndex = modules.Count - 1; moduleIndex >= 0; moduleIndex--) {
                var module = modules[moduleIndex];
                if (!anySlotAllowsAll && !slotModuleNames.Contains(module.Name)
                   || !ruleModuleNames.Contains(module.Name)) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Module \"" + module.Name + "\" will be excluded from the " +
                    "solution because it is not allowed in any Slot or not described by any Rule.");
                    modules.RemoveAt(moduleIndex);
                    moduleNames.Remove(module.Name);
                    foreach (var partName in module.PartNames) {
                        modulePartNames.Remove(partName);
                    }
                    continue;
                }
                if (!module.PartDiagonal.Equals(moduleDiagonal)) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Modules are not defined with the same diagonal.");
                    DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                    return;
                }

                var moduleConnectorsUse = modulesConnectorUsePattern[module.Name];
                var hasUnusedConnector = false;
                foreach (var isUsed in moduleConnectorsUse) {
                    if (!isUsed) {
                        hasUnusedConnector = true;
                        break;
                    }
                }
                if (hasUnusedConnector) {
                    var warningString = "Module \"" + module.Name + "\" will be excluded from the " +
                        "solution. Connectors not described by any Rule: ";
                    for (var i = 0; i < moduleConnectorsUse.Length; i++) {
                        if (!moduleConnectorsUse[i]) {
                            warningString += i + ", ";
                        }
                    }
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, warningString);
                    modules.RemoveAt(moduleIndex);
                    moduleNames.Remove(module.Name);
                    foreach (var partName in module.PartNames) {
                        modulePartNames.Remove(partName);
                    }
                }
            }

            if (modules.Count == 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "There are no Modules with all connectors described by the " +
                                  "given Rules.");
                DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                return;
            }

            // TODO: Consider another Rule cleanup here.

            var allPartsCount = 0;
            foreach (var module in modules) {
                allPartsCount += module.PartCenters.Count;
            }
            if (allPartsCount > Config.MAX_PARTS) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "Too many Module Parts: " + allPartsCount + ". Maximum allowed :" +
                    Config.MAX_PARTS + ".");
                DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                return;
            }


            // Convert AllowEverything Slots into an explicit list of allowed Modules and Module Parts.
            //    Exclude the Out Module because then it could appear also inside the Slot Envelope.
            // Otherwise compute AllowedModuleNames from AllowedPartNames if not empty.
            // Otherwise Compute AllowedPartNames from AllowedModuleNames.

            // TODO: Find a less awkward way of doing this
            moduleNames.Remove(outModule.Name);
            foreach (var partName in outModule.PartNames) {
                modulePartNames.Remove(partName);
            }

            var moduleNamesList = new List<string>(moduleNames);

            for (var slotIndex = 0; slotIndex < slots.Count; slotIndex++) {
                var slot = slots[slotIndex];
                if (slot.AllowsAnyModule) {
                    var slotLoweredFromAllowingAll = new Slot(slot.BasePlane,
                                    slot.RelativeCenter,
                                    slot.Diagonal,
                                    false,
                                    moduleNamesList,
                                    modulePartNames,
                                    allPartsCount);
                    slots[slotIndex] = slotLoweredFromAllowingAll;
                    continue;
                }
                if (slot.AllowedPartNames.Count != 0) {
                    var allowedModuleNamesForSlot = new HashSet<string>();
                    foreach (var partName in slot.AllowedPartNames) {
                        allowedModuleNamesForSlot.Add(modulePartNameToModuleName[partName]);
                    }
                    var slotLoweredFromAllowingParts = new Slot(slot.BasePlane,
                                    slot.RelativeCenter,
                                    slot.Diagonal,
                                    false,
                                    new List<string>(allowedModuleNamesForSlot),
                                    slot.AllowedPartNames,
                                    allPartsCount);
                    slots[slotIndex] = slotLoweredFromAllowingParts;
                    continue;
                }

                var allowedModuleNames = new List<string>();
                var allowedModulePartNames = new List<string>();
                foreach (var moduleName in slot.AllowedModuleNames) {
                    if (moduleNames.Contains(moduleName)) {
                        allowedModuleNames.Add(moduleName);
                    }
                    if (moduleNameToModulePartNames.ContainsKey(moduleName)) {
                        allowedModulePartNames.AddRange(moduleNameToModulePartNames[moduleName]);
                    }
                }
                var slotLoweredFromAllowingModules = new Slot(slot.BasePlane,
                                slot.RelativeCenter,
                                slot.Diagonal,
                                false,
                                allowedModuleNames,
                                allowedModulePartNames,
                                allPartsCount);
                slots[slotIndex] = slotLoweredFromAllowingModules;
            }

            foreach (var slot in slots) {
                if (!slot.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot is invalid: " + slot.IsValidWhyNot);
                    DA.SetDataList(OUT_PARAM_SLOTS, slots);
                    DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                    return;
                }
            }

            // Unwrap typed rules
            var rulesTyped = new List<RuleTyped>();
            var rulesForSolver = new HashSet<RuleForSolver>();
            foreach (var rule in rules) {
                if (rule.IsTyped) {
                    rulesTyped.Add(rule.Typed);
                }
                if (rule.IsExplicit) {
                    var conversionOK = rule.Explicit.ToRuleForSolver(modules, out var ruleForSolver);
                    if (conversionOK) {
                        rulesForSolver.Add(ruleForSolver);
                    } else {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to convert Rule " + rule + " to WFC Solver format.");
                        DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                        return;
                    }
                }
            }

            foreach (var ruleTyped in rulesTyped) {
                var rulesUnwrapeedToExplicit = ruleTyped.ToRulesExplicit(rulesTyped, modules);
                foreach (var ruleUnwrappedToExplicit in rulesUnwrapeedToExplicit) {
                    var conversionOK = ruleUnwrappedToExplicit.ToRuleForSolver(modules, out var ruleForSolver);
                    if (conversionOK) {
                        rulesForSolver.Add(ruleForSolver);
                    } else {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Rule " + rulesTyped +
                            ", which was unwrapped to " + ruleUnwrappedToExplicit + ", failed to " +
                            "convert to WFC Solver format.");
                        DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                        return;
                    }
                }
            }

            // Add internal rules to the main rule set
            foreach (var module in modules) {
                foreach (var internalRule in module.InternalRules) {
                    rulesForSolver.Add(internalRule);
                }
            }

            var slotOrder = new int[slots.Count];
            // Define world space (slots bounding box + 1 layer padding)
            Point3i.ComputeBlockBoundsWithOffset(slots, new Point3i(1, 1, 1), out var worldMin, out var worldMax);
            var worldLength = Point3i.ComputeBlockLength(worldMin, worldMax);
            var worldSlots = new Slot[worldLength];
            for (var originalIndex = 0; originalIndex < slots.Count; originalIndex++) {
                var slot = slots[originalIndex];
                var worldIndex = slot.RelativeCenter.To1D(worldMin, worldMax);
                worldSlots[worldIndex] = slot;
                slotOrder[originalIndex] = worldIndex;
            }

            // Fill unused world slots with Out modules
            for (var i = 0; i < worldSlots.Length; i++) {
                var slot = worldSlots[i];
                var relativeCenter = Point3i.From1D(i, worldMin, worldMax);
                if (slot == null) {
                    worldSlots[i] = new Slot(slotBasePlane,
                                             relativeCenter,
                                             slotDiagonal,
                                             false,
                                             new List<string>() { outModule.Name },
                                             outModule.PartNames,
                                             allPartsCount);
                }
            }

            var worldSize = (worldMax - worldMin) + new Point3i(1, 1, 1);

            if (!worldSize.FitsUshort()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "The world size exceeds minimum or maximum dimensions: " +
                                  ushort.MinValue + " to " + ushort.MaxValue + "in any direction.");
                DA.SetData(OUT_PARAM_DETERMINISTIC, false);
                return;
            }

            uint maxObservationsUint = maxObservations == int.MaxValue
                ? uint.MaxValue
                : (uint)maxObservations;

            // SOLVER
            var stats = Solve(new List<RuleForSolver>(rulesForSolver),
                              worldSize,
                              worldSlots,
                              randomSeed,
                              maxAttempts,
                              maxTimeMillis,
                              maxObservationsUint,
                              entropy,
                              slotOrder,
                              out var solvedSlotPartsTree);

            if (stats.contradictory) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, stats.report);
            }

            for (var originalIndex = 0; originalIndex < solvedSlotPartsTree.Count; originalIndex++) {
                var allowedParts = solvedSlotPartsTree[originalIndex];
                var allowedModules = new HashSet<string>();
                foreach (var partName in allowedParts) {
                    allowedModules.Add(modulePartNameToModuleName[partName]);
                }
                var solvedSlot = new Slot(slotBasePlane,
                                slots[originalIndex].RelativeCenter,
                                slotDiagonal,
                                false,
                                new List<string>(allowedModules),
                                allowedParts,
                                allPartsCount);
                slots[originalIndex] = solvedSlot;
            }

            if (!stats.deterministic) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Current solution is partial. " +
                    "A complete valid solution is not guaranteed!");
            }

            DA.SetData(OUT_PARAM_REPORT, stats.ToString());
            DA.SetDataList(OUT_PARAM_SLOTS, slots);
            DA.SetData(OUT_PARAM_DETERMINISTIC, stats.deterministic);
            DA.SetData(OUT_PARAM_CONTRADICTORY, stats.contradictory);
            DA.SetData(OUT_PARAM_SEED, stats.seed);
            DA.SetData(OUT_PARAM_ATTEMPTS, stats.solveAttempts);
            DA.SetData(OUT_PARAM_OBSERVATIONS, stats.observations);
        }

        private Stats Solve(List<RuleForSolver> rules,
                           Point3i worldSize,
                           Slot[] slots,
                           int randomSeed,
                           int maxAttemptsInt,
                           int maxTime,
                           uint maxObservations,
                           Entropy entropy,
                           int[] slotOrder,
                           out List<List<string>> orderedSlotPartsTree
                           ) {

            var stats = new Stats();
            stats.worldNotCanonical = true;
            stats.contradictory = true;
            stats.deterministic = false;
            stats.observationLimit = maxObservations;
            stats.averageObservations = 0;
            stats.observations = 0;
            stats.partCount = 0;
            stats.slotCount = (uint)slots.Length;
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
                    orderedSlotPartsTree = new List<List<string>>();
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

            for (var slotIndex = 0; slotIndex < slots.Length; ++slotIndex) {
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
                                stats.report = "Monoceros Solver failed: Rules refer to too many Module Parts";
                                orderedSlotPartsTree = new List<List<string>>();
                                return stats;
                            case WfcWorldStateInitResult.ErrWorldDimensionsZero:
                                stats.report = "Monoceros Solver failed: World dimensions are zero.";
                                orderedSlotPartsTree = new List<List<string>>();
                                return stats;
                            default:
                                stats.report = "WFC solver failed to find solution for unknown reason. Please report this error, " +
                                    "including screenshots, Rhino file and Grasshopper file at monoceros@sub.digital. Thank you!";
                                orderedSlotPartsTree = new List<List<string>>();
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
                                orderedSlotPartsTree = new List<List<string>>();
                                foreach (var worldIndex in slotOrder) {
                                    var slotState = worldStateSlots[worldIndex];
                                    var conversionTopartNameSuccessful = outputSlotState(slotState, maxModuleCount, partToName, out var slotParts);
                                    stats.report = "Monoceros Solver failed: World state is contradictory. " +
                                        "Try changing Slots, Modules, Rules or add boundary Rules. Changing " +
                                        "random seed or max attempts will not help.";
                                    if (conversionTopartNameSuccessful) {
                                        orderedSlotPartsTree.Add(slotParts);
                                    } else {
                                        stats.report += " Monoceros WFC Solver returned a unavailable Module Part. The output Slots may be shuffled.";
                                    }
                                }
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
            orderedSlotPartsTree = new List<List<string>>();
            foreach (var worldIndex in slotOrder) {
                var slotState = worldStateSlots[worldIndex];
                var conversionTopartNameSuccessful = outputSlotState(slotState, maxModuleCount, partToName, out var slotParts);
                if (conversionTopartNameSuccessful) {
                    orderedSlotPartsTree.Add(slotParts);
                } else {
                    stats.report += " Monoceros WFC Solver returned a unavailable Module Part. The output Slots may be shuffled.";
                }
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

        private bool outputSlotState(SlotState slotState,
                                      uint maxModuleCount,
                                      Dictionary<byte, string> partToName,
                                      out List<string> slotParts) {
            slotParts = new List<string>();
            for (int blkIndex = 0; blkIndex < 4; ++blkIndex) {
                for (int bitIndex = 0; bitIndex < 64; ++bitIndex) {
                    unsafe {
                        if ((slotState.slot_state[blkIndex] & (1ul << bitIndex)) != 0) {
                            var part = (short)(64 * blkIndex + bitIndex);
                            Debug.Assert(part >= 0);
                            Debug.Assert(part <= byte.MaxValue);
                            Debug.Assert(part < maxModuleCount);
                            var valid = partToName.TryGetValue((byte)part, out var partStr);
                            if (valid) {
                                slotParts.Add(partStr);
                            } else {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
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
