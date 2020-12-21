using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WFCTools {

    // TODO: Obsolete
    public class WFCCreateMegamodule : GH_Component {
        public WFCCreateMegamodule() : base("WFC Create megamodule", "WFCMegamodule",
            "Encapsulate all necessary data of a megamodule for consequent slicing.",
            "WaveFunctionCollapse", "Tools") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddGeometryParameter("Simple Geometry", "SG", "Simple Megamodule Geometry", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Production Geometry", "PG", "Production Megamodule Geometry", GH_ParamAccess.list);
            pManager.AddTextParameter("Name", "N", "Megamodule name (except '" + WFCUtilities.EMPTY_MODULE_NAME + "' and '" + WFCUtilities.OUTER_MODULE_NAME + "')", GH_ParamAccess.item);
            pManager.AddColourParameter("Geometry Colour", "C", "Megamodule Geometry Colour", GH_ParamAccess.item, Color.Black);
            pManager.AddPlaneParameter("Base plane", "B", "Grid space base plane", GH_ParamAccess.item, Plane.WorldXY);
            pManager[1].Optional = true;
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
        public override Guid ComponentGuid => new Guid("39307E6E-8AF5-4ED1-BFA5-9F1B2857386C");
    }
}