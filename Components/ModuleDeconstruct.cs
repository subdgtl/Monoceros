using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Monoceros {
    /// <summary>
    /// <para>
    /// Grasshopper component: Monoceros Deconstruct Module To Components
    /// </para>
    /// <para>
    /// Outputs the internal fields and properties of the <see cref="Module"/>
    /// in the same fashion and order of the inputs of <see cref="Module"/>
    /// constructors. This way a <see cref="Module"/> can be deconstructed and
    /// directly reconstructed.
    /// </para>
    /// </summary>
    public class ComponentModuleDeconstruct : GH_Component {
        public ComponentModuleDeconstruct( ) : base("Deconstruct Module",
                                                   "DeconModule",
                                                   "Deconstruct Monoceros Module into name, base " +
                                                   "plane, Connector planes, Connector numbers " +
                                                   "and properties.",
                                                   "Monoceros",
                                                   "Module") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleParameter(),
                                  "Module",
                                  "M",
                                  "Monoceros Module",
                                  GH_ParamAccess.item);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "All Monoceros Rules. (Optional)",
                                  GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Module Name",
                                  "MN",
                                  "Module name (converted to lowercase).",
                                  GH_ParamAccess.list);
            pManager.AddPointParameter("Module Part Center Points",
                                       "Pt",
                                       "Points in the center of Module Parts. Module parts will be fit" +
                                       " into the Slots of the Envelope.",
                                       GH_ParamAccess.list);
            pManager.AddGeometryParameter("Geometry",
                                          "G",
                                          "Geometry contained in the module.",
                                          GH_ParamAccess.list);
            pManager.AddPlaneParameter("Base Plane",
                                       "B",
                                       "Grid space base plane. Defines orientation of the grid.",
                                       GH_ParamAccess.list);
            pManager.AddVectorParameter("Module Part Diagonal",
                                        "D",
                                        "Vector specifying single Module Part dimensions" +
                                        "in base-plane-aligned XYZ axes. The Module Part Diagonal " +
                                        "must match Envelope's Slot diagonals.",
                                        GH_ParamAccess.list);
            pManager.AddBooleanParameter("Is Compact",
                                        "C",
                                        "Does the Module hold together?",
                                        GH_ParamAccess.list);
            pManager.AddBooleanParameter("Is Valid",
                                        "V",
                                        "Is the Module valid for the Monoceros WFC Solver?",
                                        GH_ParamAccess.list);
            pManager.AddPlaneParameter("Connectors",
                                       "CP",
                                       "Connector planes",
                                       GH_ParamAccess.list);
            pManager.AddIntegerParameter("Connector Indices",
                                         "CI",
                                         "Connector indices",
                                         GH_ParamAccess.list);
            pManager.AddVectorParameter("Connector Directions",
                                        "CD",
                                        "Directions of connectors as unit vectors aligned to the " +
                                        "base plane - a list parallel to CP.",
                                        GH_ParamAccess.list);
            pManager.AddBooleanParameter("Connector Use Pattern",
                                        "CU",
                                        "Connector use pattern - a list parallel to CP. " +
                                        "(only if R are provided)",
                                        GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            var module = new Module();
            var existingRules = new List<Rule>();

            if (!DA.GetData(0, ref module)) {
                return;
            }

            DA.GetDataList(1, existingRules);

            if (module == null) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The is null or invalid.");
                return;
            }

            if (!module.IsValid) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The module is invalid.");
            }

            var partCenters = module
                .PartCenters
                .Select(center => center.ToCartesian(module.BasePlane, module.PartDiagonal));

            var connectorUsePattern = Enumerable.Repeat(false, module.Connectors.Count).ToList();
            foreach (var existingRule in existingRules) {
                if (existingRule == null || !existingRule.IsValid) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The rule is null or invalid.");
                    continue;
                }

                if (existingRule.IsExplicit &&
                    existingRule.Explicit.SourceModuleName == module.Name &&
                    existingRule.Explicit.SourceConnectorIndex < module.Connectors.Count) {
                    connectorUsePattern[existingRule.Explicit.SourceConnectorIndex] = true;
                }

                if (existingRule.IsExplicit &&
                    existingRule.Explicit.TargetModuleName == module.Name &&
                    existingRule.Explicit.TargetConnectorIndex < module.Connectors.Count) {
                    connectorUsePattern[existingRule.Explicit.TargetConnectorIndex] = true;
                }

                if (existingRule.IsTyped &&
                    existingRule.Typed.ModuleName == module.Name &&
                    existingRule.Typed.ConnectorIndex < module.Connectors.Count) {
                    connectorUsePattern[existingRule.Typed.ConnectorIndex] = true;
                }
            }

            DA.SetDataList(0, new [] { new ModuleName(module.Name) });
            DA.SetDataList(1, partCenters);
            DA.SetDataList(2, module.Geometry.Concat(module.ReferencedGeometry));
            DA.SetDataList(3, new [] { module.BasePlane });
            DA.SetDataList(4, new [] { module.PartDiagonal });

            DA.SetDataList(5, new [] { module.Compact });
            DA.SetDataList(6, new [] { module.IsValid });

            var connectors = module.Connectors;
            DA.SetDataList(7, connectors.Select(connector => connector.AnchorPlane));
            DA.SetDataList(8, connectors.Select((_, i) => i));
            DA.SetDataList(9, connectors.Select(connector => connector.Direction.ToVector()));
            DA.SetDataList(10, existingRules.Count > 0 ? connectorUsePattern : null);
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.module_deconstruct;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("DD254DF4-9369-4FF3-B7BD-BA8F7FB2327E");
    }
}
