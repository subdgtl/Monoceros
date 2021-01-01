// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;

namespace WFCToolset
{
    public class ComponentFauxSolver : GH_Component
    {
        public ComponentFauxSolver() : base("WFC Faux Solver", "WFCFauxSolver",
            "Faux WFC Solver.",
            "WaveFunctionCollapse", "Solver")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new SlotParameter(), "Slots", "S", "All WFC Slots", GH_ParamAccess.list);
            pManager.AddParameter(new ModuleParameter(), "Modules", "M", "All WFC Modules", GH_ParamAccess.list);
            pManager.AddParameter(new RuleParameter(), "Rules", "R", "All WFC rules", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Random Seed", "S", "Random Seed", GH_ParamAccess.item, 42);
            pManager.AddIntegerParameter("Max Attempts", "A", "Maximum Number of Solver Attempts", GH_ParamAccess.item, 10);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new SlotParameter(), "Slots", "S", "Solved WFC Slots", GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var slotsRaw = new List<Slot>();
            var modules = new List<Module>();
            var rulesRaw = new List<Rule>();
            var seed = 42;
            var attempts = 10;

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

            var diagonal = slotsRaw.First().Diagonal;

            // Check if all slots have the same slot diagonal
            if (slotsRaw.Any(slot => slot.Diagonal != diagonal))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slots are not defined with the same diagonal.");
                return;
            }

            // Check if all slots have the same base plane 
            // TODO: Consider smarter compatibility check or base plane conversion
            var slotBasePlane = slotsRaw.First().BasePlane;
            if (slotsRaw.Any(slot => slot.BasePlane != slotBasePlane))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slots are not defined with the same base plane.");
                return;
            }

            // Check if all slots have unique location
            if (!AreSlotLocationsUnique(slotsRaw))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Slot centers are not unique.");
                return;
            }

            // Check if all slots are non-empty
            if (slotsRaw.Any(slot => slot.AllowedNothing))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Some slots allow no modules to be placed.");
                return;
            }

            // Check if there are any modules to populate the world
            if (modules.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No modules to populate world.");
                return;
            }

            var moduleDiagonal = modules.First().SlotDiagonal;

            // Check if all modules have the same slot diagonal
            if (modules.Any(module => module.SlotDiagonal != moduleDiagonal))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Modules are not defined with the same diagonal.");
                return;
            }

            // Check if all module names are unique
            if (!AreModuleNamesUnique(modules))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Module names are not unique.");
                return;
            }

            var allSubmodulesCount = modules.Aggregate(0, (sum, module) => sum + module.SubmoduleCenters.Count);

            // Check if slots and modules have the same slot diagonal
            if (moduleDiagonal != diagonal)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Modules and slots are not defined with the same diagonal.");
                return;
            }

            // Add internal rules to the main rule set
            var allInternalRules = modules
                .SelectMany(module => module.InternalRules)
                .Select(ruleExplicit => new Rule(ruleExplicit));
            rulesRaw.AddRange(allInternalRules);

            // Generate Out module
            Module.GenerateNamedEmptySingleModule(Configuration.OUTER_MODULE_NAME,
                                                  Configuration.INDIFFERENT_TAG,
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

            // Add Out modules to collection
            modules.Add(moduleOut);

            // Deduplicate rules
            // TODO: This may be redundant
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
            foreach (var rule in rulesValid)
            {
                if (rule.ToWFCRuleSolver(modules, out var ruleForSolver))
                {
                    rulesForSolver.Add(ruleForSolver);
                }

            }

            var slotOrder = new List<int>(slotsRaw.Count);
            // Define world space (slots bounding box + 1 layer padding)
            ComputeWorldRelativeBounds(slotsUnwrapped, out var worldMin, out var worldMax);
            var worldLength = ComputeWorldLength(worldMin, worldMax);
            // TODO: Investigate more elegant solutions
            var worldSlots = Enumerable.Repeat<Slot>(null, worldLength).ToList();
            foreach (var slot in slotsUnwrapped)
            {
                var index = From3DTo1D(slot.RelativeCenter - worldMin, worldMin, worldMax);
                worldSlots[index] = slot;
                slotOrder.Add(index);
            }

            // Fill unused world slots with Out modules
            for (var i = 0; i < worldSlots.Count; i++)
            {
                var slot = worldSlots[i];
                var relativeCenter = From1DTo3D(i, worldMin, worldMax) + worldMin;
                if (slot == null)
                {
                    worldSlots[i] = new Slot(
                        slotBasePlane,
                        relativeCenter,
                        diagonal,
                        false,
                        new List<string>() { moduleOut.Name },
                        new List<string>(),
                        allSubmodulesCount
                        );
                }
            }

            // Unwrap module names to submodule names for all slots
            var worldPreprocessed = worldSlots.Select(slotRaw =>
            {
                if (slotRaw.AllowedSubmoduleNames.Count != 0 && !slotRaw.AllowAnyModule)
                {
                    return slotRaw.DuplicateWithSubmodulesCount(allSubmodulesCount);
                }

                var submoduleNames = new List<string>();
                foreach (var moduleName in slotRaw.AllowedModuleNames)
                {
                    var module = modules.Find(m => m.Name == moduleName);
                    submoduleNames.AddRange(module.SubmoduleNames);
                }
                if (submoduleNames.Count == 0)
                {
                    throw new Exception("Slot is empty in spite of previous checks.");
                }
                return slotRaw.DuplicateWithSubmodulesCountAndSubmoduleNames(allSubmodulesCount, submoduleNames);
            });

            // Convert slots into the solver world format
            var worldForSolver = worldPreprocessed.Select(slot => slot.AllowedSubmoduleNames);

            // FAUX SOLVER
            // Scan all slots, pick one submodule for each
            // The solution may contain more than one value as an output: useful for Step Solver and for post-processor tuning
            var random = new Random();
            var fauxSolution = worldForSolver.Select(names => new List<string>() { names[random.Next(names.Count)] }).ToList();


            // Remember module name for each submodule name
            var submoduleToModuleName = new Dictionary<string, string>();
            foreach (var module in modules)
            {
                foreach (var submoduleName in module.SubmoduleNames)
                {
                    submoduleToModuleName.Add(submoduleName, module.Name);
                }
            }

            // Sort slots into the same order as they were input
            var slotsSolved = slotOrder.Select(index =>
            {
                var allowedSubmodules = fauxSolution[index];
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
            }).ToList();

            // Return processed slots
            DA.SetDataList(0, slotsSolved);
        }

        private static void ComputeWorldRelativeBounds(IEnumerable<Slot> slots, out Point3i min, out Point3i max)
        {
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var minZ = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;
            var maxZ = int.MinValue;

            foreach (var slot in slots)
            {
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

        private static int ComputeWorldLength(Point3i min, Point3i max)
        {
            var lengthX = max.X - min.X;
            var lengthY = max.Y - min.Y;
            var lengthZ = max.Z - min.Z;

            return (lengthX * lengthY * lengthZ);
        }

        private static int From3DTo1D(Point3i p, Point3i min, Point3i max)
        {
            var lengthX = max.X - min.X;
            var lengthY = max.Y - min.Y;

            return (lengthX * lengthY * p.Z) + lengthX * p.Y + p.X;
        }

        private static Point3i From1DTo3D(int index, Point3i min, Point3i max)
        {
            var lengthX = max.X - min.X;
            var lengthY = max.Y - min.Y;

            var lengthXY = lengthX * lengthY;
            var z = index / lengthXY;
            var y = (index % lengthXY) / lengthX;
            var x = index % lengthX;

            return new Point3i(x, y, z);
        }

        private bool AreModuleNamesUnique(List<Module> modules)
        {
            for (var i = 0; i < modules.Count; i++)
            {
                for (var j = i + 1; j < modules.Count; j++)
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
            for (var i = 0; i < slots.Count; i++)
            {
                for (var j = i + 1; j < slots.Count; j++)
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
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                Properties.Resources.WFC;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("7F0AF403-5401-4C93-88B4-812CB460D0FD");
    }
}
