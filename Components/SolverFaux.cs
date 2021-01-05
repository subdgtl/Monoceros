﻿using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WFCPlugin
{
    public class ComponentFauxSolver : GH_Component
    {
        public ComponentFauxSolver() : base("WFC Faux Solver",
                                            "WFCFauxSolver",
                                            "Faux WFC Solver.",
                                            "WaveFunctionCollapse",
                                            "Solver")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
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
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
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
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Slot> slotsRaw = new List<Slot>();
            List<Module> modules = new List<Module>();
            List<Rule> rulesRaw = new List<Rule>();
            int seed = 42;
            int attempts = 10;

            if (!DA.GetDataList(0, slotsRaw))
            {
                return;
            }

            if (!DA.GetDataList(1, modules))
            {
                return;
            }

            if (!DA.GetDataList(2, rulesRaw))
            {
                return;
            }

            if (!DA.GetData(3, ref seed))
            {
                return;
            }

            if (!DA.GetData(4, ref attempts))
            {
                return;
            }

            // Check if there are any slots to define the world
            if (slotsRaw.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No slots to define world.");
                return;
            }

            Rhino.Geometry.Vector3d diagonal = slotsRaw.First().Diagonal;

            // Check if all slots have the same slot diagonal
            if (slotsRaw.Any(slot => slot.Diagonal != diagonal))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slots are not defined with the same diagonal.");
                return;
            }

            // Check if all slots have the same base plane 
            // TODO: Consider smarter compatibility check or base plane conversion
            Rhino.Geometry.Plane slotBasePlane = slotsRaw.First().BasePlane;
            if (slotsRaw.Any(slot => slot.BasePlane != slotBasePlane))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Slots are not defined with the same base plane.");
                return;
            }

            if (!AreSlotLocationsUnique(slotsRaw))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot centers are not unique.");
                return;
            }

            if (slotsRaw.Any(slot => slot.AllowedNothing))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Some slots allow no modules to be placed.");
                return;
            }

            if (modules.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No modules to populate world.");
                return;
            }

            Rhino.Geometry.Vector3d moduleDiagonal = modules.First().SlotDiagonal;

            if (modules.Any(module => module.SlotDiagonal != moduleDiagonal))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Modules are not defined with the same diagonal.");
                return;
            }

            if (!AreModuleNamesUnique(modules))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module names are not unique.");
                return;
            }

            int allSubmodulesCount = modules
                .Aggregate(0, (sum, module) => sum + module.SubmoduleCenters.Count);

            if (moduleDiagonal != diagonal)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "Modules and slots are not defined with the same diagonal.");
                return;
            }

            // Generate Out module
            Module.GenerateEmptySingleModule(Config.OUTER_MODULE_NAME,
                                             Config.INDIFFERENT_TAG,
                                             new Rhino.Geometry.Vector3d(1, 1, 1),
                                             out Module moduleOut,
                                             out List<RuleTyped> rulesOutTyped);
            IEnumerable<Rule> rulesOut = rulesOutTyped.Select(ruleTyped => new Rule(ruleTyped));
            rulesRaw.AddRange(rulesOut);

            // Convert AllowEverything slots into an explicit list of allowed modules (except Out)
            List<string> allModuleNames = modules.Select(module => module.Name).ToList();
            IEnumerable<Slot> slotsUnwrapped = slotsRaw.Select(slotRaw =>
                slotRaw.AllowAnyModule ?
                    slotRaw.DuplicateWithModuleNames(allModuleNames) :
                    slotRaw
            );

            modules.Add(moduleOut);

            // TODO: This may be redundant
            IEnumerable<Rule> rulesDistinct = rulesRaw.Distinct();

            // Unwrap typed rules
            IEnumerable<RuleTyped> rulesTyped = rulesDistinct.Where(rule => rule.IsTyped()).Select(rule => rule.Typed);
            IEnumerable<RuleExplicit> rulesTypedUnwrappedToExplicit = rulesTyped
                .SelectMany(ruleTyped => ruleTyped.ToRuleExplicit(rulesTyped, modules));

            IEnumerable<RuleExplicit> rulesExplicit = rulesDistinct
                .Where(rule => rule.IsExplicit())
                .Select(rule => rule.Explicit);

            // Deduplicate rules again
            IEnumerable<RuleExplicit> rulesUnwrappedExplicit = rulesExplicit.Concat(rulesTypedUnwrappedToExplicit).Distinct();

            // Filter out invalid rules (not connecting the same connectors && connecting opposing connectors)
            IEnumerable<RuleExplicit> rulesValid = rulesUnwrappedExplicit.Where(rule => rule.IsValidWithGivenModules(modules));

            // Convert rules to solver format
            List<RuleForSolver> rulesForSolver = new List<RuleForSolver>();
            foreach (RuleExplicit rule in rulesValid)
            {
                if (rule.ToWFCRuleSolver(modules, out RuleForSolver ruleForSolver))
                {
                    rulesForSolver.Add(ruleForSolver);
                }
            }

            // Add internal rules to the main rule set
            foreach (Module module in modules)
            {
                rulesForSolver.AddRange(module.InternalRules);
            }

            List<int> slotOrder = new List<int>(slotsRaw.Count);
            // Define world space (slots bounding box + 1 layer padding)
            ComputeWorldRelativeBounds(slotsUnwrapped, out Point3i worldMin, out Point3i worldMax);
            int worldLength = ComputeWorldLength(worldMin, worldMax);
            // TODO: Investigate more elegant solutions
            List<Slot> worldSlots = Enumerable.Repeat<Slot>(null, worldLength).ToList();
            foreach (Slot slot in slotsUnwrapped)
            {
                int index = From3DTo1D(slot.RelativeCenter - worldMin, worldMin, worldMax);
                worldSlots[index] = slot;
                slotOrder.Add(index);
            }

            // Fill unused world slots with Out modules
            for (int i = 0; i < worldSlots.Count; i++)
            {
                Slot slot = worldSlots[i];
                Point3i relativeCenter = From1DTo3D(i, worldMin, worldMax) + worldMin;
                if (slot == null)
                {
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
            IEnumerable<Slot> worldPreprocessed = worldSlots.Select(slotRaw =>
            {
                if (slotRaw.AllowedSubmoduleNames.Count != 0 && !slotRaw.AllowAnyModule)
                {
                    return slotRaw.DuplicateWithSubmodulesCount(allSubmodulesCount);
                }

                List<string> submoduleNames = new List<string>();
                foreach (string moduleName in slotRaw.AllowedModuleNames)
                {
                    Module module = modules.Find(m => m.Name == moduleName);
                    submoduleNames.AddRange(module.SubmoduleNames);
                }
                if (submoduleNames.Count == 0)
                {
                    throw new Exception("Slot is empty in spite of previous checks.");
                }
                return slotRaw.DuplicateWithSubmodulesCountAndNames(allSubmodulesCount,
                                                                    submoduleNames);
            });

            // Convert slots into the solver world format
            IEnumerable<List<string>> worldForSolver = worldPreprocessed.Select(slot => slot.AllowedSubmoduleNames);

            // FAUX SOLVER
            // Scan all slots, pick one submodule for each
            // The solution may contain more than one value as an output: 
            // useful for Step Solver and for post-processor tuning
            Random random = new Random();
            List<List<string>> fauxSolution = worldForSolver
                .Select(names => new List<string>() { names[random.Next(names.Count)] })
                .ToList();


            // Remember module name for each submodule name
            Dictionary<string, string> submoduleToModuleName = new Dictionary<string, string>();
            foreach (Module module in modules)
            {
                foreach (string submoduleName in module.SubmoduleNames)
                {
                    submoduleToModuleName.Add(submoduleName, module.Name);
                }
            }

            // Sort slots into the same order as they were input
            IEnumerable<Slot> slotsSolved = slotOrder.Select(index =>
            {
                List<string> allowedSubmodules = fauxSolution[index];
                List<string> allowedModules = allowedSubmodules
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

            // Return processed slots
            DA.SetDataList(0, slotsSolved);
        }

        private static void ComputeWorldRelativeBounds(IEnumerable<Slot> slots,
                                                       out Point3i min,
                                                       out Point3i max)
        {
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int minZ = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;
            int maxZ = int.MinValue;

            foreach (Slot slot in slots)
            {
                Point3i center = slot.RelativeCenter;
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

        private static int ComputeWorldLength(Point3i min, Point3i max)
        {
            int lengthX = max.X - min.X;
            int lengthY = max.Y - min.Y;
            int lengthZ = max.Z - min.Z;

            return (lengthX * lengthY * lengthZ);
        }

        private static int From3DTo1D(Point3i p, Point3i min, Point3i max)
        {
            int lengthX = max.X - min.X;
            int lengthY = max.Y - min.Y;

            return (lengthX * lengthY * p.Z) + lengthX * p.Y + p.X;
        }

        private static Point3i From1DTo3D(int index, Point3i min, Point3i max)
        {
            int lengthX = max.X - min.X;
            int lengthY = max.Y - min.Y;

            int lengthXY = lengthX * lengthY;
            int z = index / lengthXY;
            int y = (index % lengthXY) / lengthX;
            int x = index % lengthX;

            return new Point3i(x, y, z);
        }

        private bool AreModuleNamesUnique(List<Module> modules)
        {
            for (int i = 0; i < modules.Count; i++)
            {
                for (int j = i + 1; j < modules.Count; j++)
                {
                    if (modules[i].Name == modules[j].Name)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool AreSlotLocationsUnique(List<Slot> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                for (int j = i + 1; j < slots.Count; j++)
                {
                    if (slots[i].RelativeCenter.Equals(slots[j].RelativeCenter))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to
        /// septenary), each of which can be combined with the
        /// GH_Exposure.obscure flag, which ensures the component will only be
        /// visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the
        /// User Interface. Icons need to be 24x24 pixels.
        /// </summary>
        protected override Bitmap Icon => Properties.Resources.WFC;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("7F0AF403-5401-4C93-88B4-812CB460D0FD");
    }
}
