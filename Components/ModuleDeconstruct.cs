using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace WFCPlugin {
    /// <summary>
    /// <para>
    /// Grasshopper component: WFC Deconstruct Module To Components
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
                                                   "Deconstruct WFC Module into name, base " +
                                                   "plane, connector planes, connector numbers " +
                                                   "and properties.",
                                                   "WaveFunctionCollapse",
                                                   "Module") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new ModuleParameter(),
                                  "Module",
                                  "M",
                                  "WFC Module",
                                  GH_ParamAccess.item);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "All WFC Rules. (Optional)",
                                  GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new ModuleNameParameter(),
                                  "Name",
                                  "N",
                                  "Module name (converted to lowercase).",
                                  GH_ParamAccess.list);
            pManager.AddPointParameter("Slot Centers",
                                       "SC",
                                       "Used slots centers to be used for module reconstruction.",
                                       GH_ParamAccess.list);
            pManager.AddGeometryParameter("Geometry",
                                          "G",
                                          "Geometry contained in the module.",
                                          GH_ParamAccess.list);
            pManager.AddPlaneParameter("Base Plane",
                                       "B",
                                       "Grid space base plane. Defines orientation of the grid.",
                                       GH_ParamAccess.list);
            pManager.AddVectorParameter("Grid Slot Diagonal",
                                        "D",
                                        "Grid slot diagonal vector specifying slot dimension in " +
                                        "base-plane-aligned axes.",
                                        GH_ParamAccess.list);
            pManager.AddPlaneParameter("Connectors",
                                       "C",
                                       "Connector planes",
                                       GH_ParamAccess.list);
            pManager.AddVectorParameter("Connector Direction",
                                        "CD",
                                        "Connector direction base-plane-aligned axis vector - " +
                                        "a list parallel to C.",
                                        GH_ParamAccess.list);
            pManager.AddBooleanParameter("Connector Use Pattern",
                                        "CP",
                                        "Connector use pattern - a list parallel to C. " +
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

            var baseAlignmentTransform = Transform.PlaneToPlane(Plane.WorldXY, module.BasePlane);
            var scalingTransform = Transform.Scale(module.BasePlane,
                                                   module.SlotDiagonal.X,
                                                   module.SlotDiagonal.Y,
                                                   module.SlotDiagonal.Z);

            var submoduleCentersNormalized = module
                .SubmoduleCenters
                .Select(center => center.ToPoint3d());
            var submoduleCenters = submoduleCentersNormalized
                .Select(center => {
                    center.Transform(baseAlignmentTransform);
                    center.Transform(scalingTransform);
                    return center;
                });

            var connectorUsePattern = Enumerable.Repeat(false, module.Connectors.Count).ToList();
            foreach (var existingRule in existingRules) {
                if (existingRule.IsExplicit() &&
                    existingRule.Explicit.SourceModuleName == module.Name &&
                    existingRule.Explicit.SourceConnectorIndex < module.Connectors.Count) {
                    connectorUsePattern[existingRule.Explicit.SourceConnectorIndex] = true;
                }

                if (existingRule.IsExplicit() &&
                    existingRule.Explicit.TargetModuleName == module.Name &&
                    existingRule.Explicit.TargetConnectorIndex < module.Connectors.Count) {
                    connectorUsePattern[existingRule.Explicit.TargetConnectorIndex] = true;
                }

                if (existingRule.IsTyped() &&
                    existingRule.Typed.ModuleName == module.Name &&
                    existingRule.Typed.ConnectorIndex < module.Connectors.Count) {
                    connectorUsePattern[existingRule.Typed.ConnectorIndex] = true;
                }
            }

            DA.SetDataList(0, new List<ModuleName> { new ModuleName(module.Name) });
            DA.SetDataList(1, submoduleCenters);
            DA.SetDataList(2, module.Geometry);
            DA.SetDataList(3, new List<Plane> { module.BasePlane });
            DA.SetDataList(4, new List<Vector3d> { module.SlotDiagonal });

            var connectors = module.Connectors;
            DA.SetDataList(5, connectors.Select(connector => connector.AnchorPlane));
            DA.SetDataList(6, connectors.Select(connector => connector.Direction.ToVector()));
            DA.SetDataList(7, existingRules.Count > 0 ? connectorUsePattern : null);
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.M;

        /// <summary>
        /// Each component must have a unique Guid to identify it.  It is vital
        /// this Guid doesn't change otherwise old ghx files that use the old ID
        /// will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("DD254DF4-9369-4FF3-B7BD-BA8F7FB2327E");
    }
}
