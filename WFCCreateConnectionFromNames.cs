using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WFCTools {
    // TODO: Obsolete, replace with Explicit and Typed Rule literal
    public class WFCCreateConnectionFromNames : GH_Component {
        public WFCCreateConnectionFromNames() : base("WFC Create Connection From Names", "WFCConnectionNames",
            "Create module connection rule from module names.",
            "WaveFunctionCollapse", "Connections") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddVectorParameter("Direction Vector", "D", "Orthogonal direction vector (+X, +Y, +Z, -X, -Y, -Z).", GH_ParamAccess.list);
            pManager.AddTextParameter("Source Module Name", "SN", "Source module name or " + WFCUtilities.ReservedNames() + ".", GH_ParamAccess.list);
            pManager.AddTextParameter("Target Module Name", "TN", "Target module name or " + WFCUtilities.ReservedNames() + ".", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddParameter(new WFCMegamoduleParameter(), "Megamodules", "MM", "Megamodules", GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            List<IGH_GeometricGoo> simpleGeometryRaw = new List<IGH_GeometricGoo>();
            List<IGH_GeometricGoo> productionGeometryRaw = new List<IGH_GeometricGoo>();
            string name = "";
            Plane basePlane = new Plane();
            Color colour = new Color();

            if (!DA.GetDataList(0, simpleGeometryRaw)) return;
            DA.GetDataList(1, productionGeometryRaw);
            if (!DA.GetData(2, ref name)) return;
            if (!DA.GetData(3, ref colour)) return;
            if (!DA.GetData(4, ref basePlane)) return;

            List<WFCMegamodule> megamoduleGeometries = new List<WFCMegamodule>();

            if (name.Length == 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Megamodule name is empty.");
                return;
            }

            if (name == WFCUtilities.EMPTY_MODULE_NAME || name == WFCUtilities.OUTER_MODULE_NAME) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The megamodule name cannot be '" + name + "' because it is reserved by WFC.");
                return;
            }

            List<GeometryBase> simpleGeometryClean = simpleGeometryRaw
               .Where(goo => goo != null)
               .Select(ghGeo =>
                   GH_Convert.ToGeometryBase(ghGeo)
               ).ToList();

            if (simpleGeometryClean.Count == 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Megamodule contains no valid geometry.");
                return;
            }

            List<GeometryBase> productionGeometryClean = productionGeometryRaw
               .Where(goo => goo != null)
               .Select(ghGeo =>
                   GH_Convert.ToGeometryBase(ghGeo)
               ).ToList();


            WFCMegamodule megamodule = new WFCMegamodule {
                SimpleGeometry = simpleGeometryClean,
                ProductionGeometry = productionGeometryClean,
                Name = name,
                BasePlane = basePlane,
                Colour = colour
            };

            DA.SetData(0, megamodule);
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
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                WFCTools.Properties.Resources.M;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("227E76C0-7A14-4121-82A2-C2124D5AC475");
    }
}