using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;

namespace Monoceros {

    public class ComponentRuleExplicitBetweenTwoSets : GH_Component {
        public ComponentRuleExplicitBetweenTwoSets( )
            : base("Construct Explicit Rule between 2 Lists",
                   "RuleExp2Lists",
                   "Construct a Monoceros Explicit Rule (connector-to-connector) between " +
                   "all listed Connectors of all listed Modules of two lists. The existence " +
                   "of the Module and the Connector as well as whether the Connectors are " +
                   "opposite is checked only if the optional list of all Modules is provided. " +
                   "Otherwise use Collect Rules component to remove the invalid Rules.",
                   "Monoceros",
                   "Rule") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Source Module Names",
                                  "SMN",
                                  "Source Module names",
                                  GH_ParamAccess.tree);
            pManager.AddParameter(new ConnectorIndexParameter(),
                                  "Source Connector Indices",
                                  "SC",
                                  "Source Connector numbers",
                                  GH_ParamAccess.tree);
            pManager.AddParameter(new ModuleParameter(),
                                  "All Modules",
                                  "M",
                                  "All Monoceros Modules (Optional)",
                                  GH_ParamAccess.list);
            pManager[2].Optional = true;
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Target Module Names",
                                  "TMN",
                                  "Target Module names",
                                  GH_ParamAccess.tree);
            pManager.AddParameter(new ConnectorIndexParameter(),
                                  "Target Connector Indices",
                                  "TC",
                                  "Target Connector numbers",
                                  GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new RuleParameter(),
                                  "Rules Explicit",
                                  "R",
                                  "Explicit Monoceros Rules",
                                  // TODO: consider returning a tree
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            if (!DA.GetDataTree(0, out GH_Structure<ModuleName> sourceNamesRaw)) {
                return;
            }
            if (!DA.GetDataTree(1, out GH_Structure<ConnectorIndex> sourceConnectorIndicesRaw)) {
                return;
            }
            if (!DA.GetDataTree(3, out GH_Structure<ModuleName> targetNamesRaw)) {
                return;
            }
            if (!DA.GetDataTree(4, out GH_Structure<ConnectorIndex> targetConnectorIndicesRaw)) {
                return;
            }

            var modules = new List<Module>();
            var modulesProvided = false;
            if (DA.GetDataList(2, modules)) {
                modulesProvided = true;
                var invalidModuleCount = modules.RemoveAll(module => module == null || !module.IsValid);

                if (invalidModuleCount > 0) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                      invalidModuleCount + " Modules are null or invalid and were removed.");
                }
            } else {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No Modules provided." +
                    " Rule validity could not be determined. Some Rules may be removed by the Solver.");
            }

            var sourceLongestTreeCount = Math.Max(sourceNamesRaw.PathCount, sourceConnectorIndicesRaw.PathCount);
            var targteLongestTreeCount = Math.Max(targetNamesRaw.PathCount, targetConnectorIndicesRaw.PathCount);

            var rules = new List<Rule>();

            for (var sourceTreeIteration = 0; sourceTreeIteration < sourceLongestTreeCount; sourceTreeIteration++) {
                var sourceNamePath = sourceNamesRaw.Paths[Math.Min(sourceNamesRaw.PathCount - 1, sourceTreeIteration)];
                var sourceConnectorPath = sourceConnectorIndicesRaw.Paths[Math.Min(sourceConnectorIndicesRaw.PathCount - 1, sourceTreeIteration)];

                var sourceNames = sourceNamesRaw.get_Branch(sourceNamePath);
                var sourceConnectorIndices = sourceConnectorIndicesRaw.get_Branch(sourceConnectorPath);

                var sourceLongestListCount = Math.Max(sourceNames.Count, sourceConnectorIndices.Count);
                for (var sourceListIteration = 0; sourceListIteration < sourceLongestListCount; sourceListIteration++) {
                    var sourceName = ((ModuleName)sourceNames[Math.Min(sourceNames.Count - 1, sourceListIteration)]).Name;
                    var sourceConnectorIndex = ((ConnectorIndex)sourceConnectorIndices[Math.Min(sourceConnectorIndices.Count - 1, sourceListIteration)]).Index;

                    for (var targetTreeIteration = 0; targetTreeIteration < targteLongestTreeCount; targetTreeIteration++) {

                        var targetNamePath = targetNamesRaw.Paths[Math.Min(targetNamesRaw.PathCount - 1, targetTreeIteration)];
                        var targetConnectorPath = targetConnectorIndicesRaw.Paths[Math.Min(targetConnectorIndicesRaw.PathCount - 1, targetTreeIteration)];

                        var targetNames = targetNamesRaw.get_Branch(targetNamePath);
                        var targetConnectorIndices = targetConnectorIndicesRaw.get_Branch(targetConnectorPath);

                        var targetLongestListCount = Math.Max(targetNames.Count, targetConnectorIndices.Count);
                        for (var targetListIteration = 0; targetListIteration < targetLongestListCount; targetListIteration++) {
                            var targetName = ((ModuleName)targetNames[Math.Min(targetNames.Count - 1, targetListIteration)]).Name;
                            var targetConnectorIndex = ((ConnectorIndex)targetConnectorIndices[Math.Min(targetConnectorIndices.Count - 1, targetListIteration)]).Index;

                            var rule = new Rule(sourceName, sourceConnectorIndex, targetName, targetConnectorIndex);

                            if (!rule.IsValid) {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, rule.IsValidWhyNot);
                                continue;
                            }

                            if (!modulesProvided || rule.IsValidWithModules(modules)) {
                                rules.Add(rule);
                            }
                        }

                    }
                }
            }

            var rulesDeduplicated = rules.Distinct().ToList();
            rulesDeduplicated.Sort();

            if (!rulesDeduplicated.Any()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to construct any Rule.");
                return;
            }


            DA.SetDataList(0, rulesDeduplicated);
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.rule_explicit_construct_2_lists;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("1FD4E379-3A9D-40FD-8A5E-113DC8AF9431");
    }
}
