using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;

namespace Monoceros {
    public class Exporter : GH_Component {
        public Exporter( ) : base("Monoceros To WFC Exporter",
                                  "Exporter",
                                  "Monoceros Solver for the Wave Function Collapse",
                                  "Monoceros",
                                  "Main") {
        }

        public override Guid ComponentGuid => new Guid("BB62A03B-3A18-45A9-B48C-3F5B0C43F366");

        protected override System.Drawing.Bitmap Icon => Properties.Resources.monoceros24;

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
            pManager.AddTextParameter("World File",
                                      "WF",
                                      "Output file for world data",
                                      GH_ParamAccess.item);
            pManager.AddTextParameter("Rule File",
                                      "RF",
                                      "Output file for rule set",
                                      GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddBooleanParameter("Success",
                                         "OK",
                                         "Did the Monoceros WFC Solver find a valid solution?",
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
            var worldFileName = "";
            var rulesetFileName = "";
            var success = false;

            // Due to many early return branches it is easier to set and the re-set the output pin
            DA.SetData(0, false);

            if (!DA.GetDataList(0, slotsRaw)) {
                return;
            }

            if (!DA.GetDataList(1, modulesRaw)) {
                return;
            }

            if (!DA.GetDataList(2, rulesRaw)) {
                return;
            }

            if (!DA.GetData(3, ref worldFileName)) {
                return;
            }

            if (!DA.GetData(4, ref rulesetFileName)) {
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

            if (!slotsClean.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Slots collected.");
                return;
            }

            var modulesClean = new List<Module>();
            foreach (var module in modulesRaw) {
                if (module == null || !module.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module is null or invalid.");
                } else {
                    modulesClean.Add(module);
                }
            }

            if (!modulesClean.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Modules collected.");
                return;
            }

            var rulesClean = new List<Rule>();
            foreach (var rule in rulesRaw) {
                if (rule == null || !rule.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Rule is null or invalid.");
                } else {
                    rulesClean.Add(rule);
                }
            }

            if (!rulesClean.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid Modules collected.");
                return;
            }

            // TODO: Consider smarter compatibility check or non-uniform scaling
            if (!Slot.AreSlotDiagonalsCompatible(slotsClean)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slots are not defined with the same diagonal.");
                return;
            }
            var diagonal = slotsClean.First().Diagonal;

            // TODO: Consider smarter compatibility check or base plane conversion
            if (!Slot.AreSlotBasePlanesCompatible(slotsClean)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slots are not defined with the same base plane.");
                return;
            }
            var slotBasePlane = slotsClean.First().BasePlane;

            if (!Slot.AreSlotLocationsUnique(slotsClean)) {
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
                                             allPartsCount);
                }
            }

            // Unwrap module names to part names for all slots
            var worldForSolver = worldSlots.Select(slotRaw => {
                if (slotRaw.AllowedPartNames.Count != 0) {
                    return slotRaw.DuplicateWithPartsCount(allPartsCount);
                }

                var partNames = new List<string>();
                foreach (var moduleName in slotRaw.AllowedModuleNames) {
                    var module = modulesUsable.Find(m => m.Name == moduleName);
                    partNames.AddRange(module.PartNames);
                }
                return slotRaw.DuplicateWithPartsCountAndNames(allPartsCount, partNames);
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
                DA.SetData(0, false);
                return;
            }

            // EXPORTER
            var rules = rulesForSolver.Distinct().ToList();
            writeRules(rules, rulesetFileName);
            writeWorld(worldSize, worldForSolver.ToList(), worldFileName);

            success = true;

            DA.SetData(0, success);
        }

        private void writeWorld(Point3i worldSize, List<Slot> worldForSolver, string worldFileName) {
            var lines = new List<string>() { worldSize.X + " " + worldSize.Y + " " + worldSize.Z, "" };

            for (int z = worldSize.Z - 1; z >= 0; z--) {
                for (int y = worldSize.Y - 1; y >= 0; y--) {
                    var line = "";
                    for (int x = 0; x < worldSize.X; x++) {
                        var slot = worldForSolver[From3DTo1D(new Point3i(x, y, z), worldSize)];
                        var slotParts = slot.AllowedPartNames.Aggregate("", (l, s) => l += "," + s).Remove(0, 1);
                        line += "[" + slotParts + "]";
                    }
                    lines.Add(line);
                }
                lines.Add("");
            }

            File.WriteAllLines(worldFileName, lines);
        }

        public void writeRules(List<RuleForSolver> rules, string rulesetFileName) {

            var lines = rules
                .Select(rule => rule.Axis.ToString() + "," + rule.LowerPartName + "," + rule.HigherPartName)
                .ToArray();

            File.WriteAllLines(rulesetFileName, lines);
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

    }

}
