using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WFCPlugin
{
    // TODO: Make bake aware and think about using blocks. Override baking output geometry.
    // TODO: Think about how to bake contradictory and non-deterministic slots.
    public class ComponentMaterialize : GH_Component
    {
        public ComponentMaterialize() : base("WFC Materialize",
                                             "WFCMaterialize",
                                             "WFC Materialize.",
                                             "WaveFunctionCollapse",
                                             "Materialize")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new SlotParameter(),
                                  "Slot",
                                  "S",
                                  "WFC Slot",
                                  GH_ParamAccess.item);
            pManager.AddParameter(new ModuleParameter(),
                                  "Modules",
                                  "M",
                                  "All WFC Modules",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry",
                                          "G",
                                          "Geometry placed into WFC Slot",
                                          GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Slot slot = new Slot();
            List<Module> modules = new List<Module>();

            if (!DA.GetData(0, ref slot))
            {
                return;
            }

            if (!DA.GetDataList(1, modules))
            {
                return;
            }

            IEnumerable<GeometryBase> geometry = Enumerable.Empty<GeometryBase>();

            // TODO: Think about what to do with contradictory and non-deterministic slots.
            if (slot.AllowedSubmoduleNames.Count == 1)
            {
                string slotSubmoduleName = slot.AllowedSubmoduleNames.First();
                Module placedModule = modules
                    .FirstOrDefault(module => module.PivotSubmoduleName == slotSubmoduleName);
                if (placedModule != null)
                {
                    geometry = placedModule.Geometry.Select(geo =>
                    {
                        GeometryBase placedGeometry = geo.Duplicate();
                        placedGeometry
                        .Transform(Transform.PlaneToPlane(placedModule.Pivot, slot.Pivot));
                        return placedGeometry;
                    });
                }
            }

            // Return placed geometry
            DA.SetDataList(0, geometry);
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
        public override Guid ComponentGuid => new Guid("D32A9B20-4138-4C24-A11F-E139383776B2");
    }
}
