using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WFCTools {

    // TODO: Refactor into the new architecture
    internal struct MegamoduleGeometryForPlacing {
        public IEnumerable<GeometryBase> Geometry;
        public string MegamoduleName;
        public Plane SourcePlane;
        public Plane TargetPlane;
        public Color MegamoduleColour;
        public override bool Equals(object obj) {
            return obj is MegamoduleGeometryForPlacing placing &&
                   MegamoduleName == placing.MegamoduleName &&
                   SourcePlane.Equals(placing.SourcePlane) &&
                   TargetPlane.Equals(placing.TargetPlane) &&
                   EqualityComparer<Color>.Default.Equals(MegamoduleColour, placing.MegamoduleColour);
        }

        public override int GetHashCode() {
            int hashCode = 12323277;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(MegamoduleName);
            hashCode = hashCode * -1521134295 + SourcePlane.GetHashCode();
            hashCode = hashCode * -1521134295 + TargetPlane.GetHashCode();
            hashCode = hashCode * -1521134295 + MegamoduleColour.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(MegamoduleGeometryForPlacing left, MegamoduleGeometryForPlacing right) {
            return left.Equals(right);
        }

        public static bool operator !=(MegamoduleGeometryForPlacing left, MegamoduleGeometryForPlacing right) {
            return !(left == right);
        }
    }

    public class WFCSolutionBuilder : GH_Component {
        public WFCSolutionBuilder() : base("WFC Solution Builder", "WFCBuilder",
            "Build the WFC solution out of the actual geometry.",
            "WaveFunctionCollapse", "Tools") {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddParameter(new WFCSlicedMegamoduleParameter(), "Sliced Megamodules", "SMM", "Sliced Megamodules", GH_ParamAccess.list);
            pManager.AddVectorParameter(
                "Submodule Diagonal",
                "D",
                "Module diagonal vector specifying module dimension in base-plane-aligned XYZ axes",
                GH_ParamAccess.item,
                new Vector3d(10.0, 10.0, 10.0)
                );
            pManager.AddPointParameter("World Slot Coordinates", "S", "Coordinates of world slots in integer grid units", GH_ParamAccess.list);
            pManager.AddTextParameter("Allowed Modules", "M", "Allowed modules in respective world slots. This list is parallel to S.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("World Base Plane", "B", "Grid base plane", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddBooleanParameter("Use Production Geometry", "P", "True = Use production geometry, False = Use simplified geometry", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddGeometryParameter("Megamodule Geometry", "G", "Megamodules placed into grid slots", GH_ParamAccess.list);
            pManager.AddTextParameter("Megamodule Names", "N", "Megamodule names. This list is parallel to G.", GH_ParamAccess.list);
            pManager.AddColourParameter("Megamodule Colours", "C", "Megamodule colours. This list is parallel to G.", GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            List<WFCSlicedMegamodule> megamodules = new List<WFCSlicedMegamodule>();
            Vector3d slotDiagonal = new Vector3d();
            List<Point3d> slotPoints = new List<Point3d>();
            List<string> slotModuleNames = new List<string>();
            Plane worldBasePlane = Plane.WorldXY;
            bool useProductionGeometry = false;

            if (!DA.GetDataList(0, megamodules)) return;
            if (!DA.GetData(1, ref slotDiagonal)) return;
            if (!DA.GetDataList(2, slotPoints)) return;
            if (!DA.GetDataList(3, slotModuleNames)) return;
            if (!DA.GetData(4, ref worldBasePlane)) return;
            if (!DA.GetData(5, ref useProductionGeometry)) return;

            List<string> justNames = megamodules.Select(mG => mG.Name).ToList(); ;
            if (justNames.Count != justNames.Distinct().Count()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Megamodule names are not unique.");
                return;
            }

            if (slotPoints.Count != slotModuleNames.Count()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "World Slot Coordinates list and Allowed Modules list bust me equally long.");
                return;
            }

            if (slotPoints.Count != slotPoints.Distinct().Count()) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Some slots seem to contain more modules. The wolrd solution is non-deterministic.");
                return;
            }

            Dictionary<string, Submodule> submoduleMap = new Dictionary<string, Submodule>();
            foreach (WFCSlicedMegamodule megamodule in megamodules) {
                foreach (Submodule submodule in megamodule.Submodules) {
                    submoduleMap.Add(submodule.Name, submodule);
                }
            }

            List<MegamoduleGeometryForPlacing> megamodulesToBePlaced = new List<MegamoduleGeometryForPlacing>();

            for (int i = 0; i < slotPoints.Count; i++) {
                string slotModuleName = slotModuleNames[i];
                if (slotModuleName != WFCUtilities.EMPTY_MODULE_NAME && slotModuleName != WFCUtilities.OUTER_MODULE_NAME) {
                    Submodule submodule = submoduleMap[slotModuleName];
                    Point3d centerCartesian = new Point3d(
                        (slotPoints[i].X * slotDiagonal.X),
                        (slotPoints[i].Y * slotDiagonal.Y),
                        (slotPoints[i].Z * slotDiagonal.Z)
                        );
                    centerCartesian.Transform(Transform.PlaneToPlane(Plane.WorldXY, worldBasePlane));
                    Plane slotBasePlane = worldBasePlane.Clone();
                    slotBasePlane.Origin = centerCartesian;
                    Plane submoduleBasePlane = submodule.WorldAlignedPivot;
                    Plane megamoduleWorldAlignedPivot = submodule.SlicedMegamodule.WorldAlignedPivot;
                    Plane megamoduleTargetPlane = megamoduleWorldAlignedPivot.Clone();
                    megamoduleTargetPlane.Transform(Transform.PlaneToPlane(submoduleBasePlane, slotBasePlane));
                    megamodulesToBePlaced.Add(new MegamoduleGeometryForPlacing() {
                        Geometry = submodule.SlicedMegamodule.WorldAlignedSimpleGeometry.Select(geo => geo.Duplicate()),
                        MegamoduleName = submodule.SlicedMegamodule.Name,
                        SourcePlane = megamoduleWorldAlignedPivot.Clone(),
                        TargetPlane = megamoduleTargetPlane,
                        MegamoduleColour = submodule.SlicedMegamodule.Colour
                    });
                }
            }

            megamodulesToBePlaced.Distinct();

            // TODO turn into two-depth data tree
            List<GeometryBase> placedMegamoduleGeometry = new List<GeometryBase>();
            List<string> placedMegamoduleNames = new List<string>();
            List<Color> placedMegamoduleColours = new List<Color>();

            foreach (MegamoduleGeometryForPlacing megamoduleToBePlaced in megamodulesToBePlaced) {
                List<GeometryBase> placedGeometry = megamoduleToBePlaced.Geometry.Select(geo => {
                    geo.Transform(Transform.PlaneToPlane(megamoduleToBePlaced.SourcePlane, megamoduleToBePlaced.TargetPlane));
                    return geo;
                }).ToList();
                placedMegamoduleGeometry.AddRange(placedGeometry);
                placedMegamoduleNames.AddRange(Enumerable.Repeat(megamoduleToBePlaced.MegamoduleName, placedGeometry.Count));
                placedMegamoduleColours.AddRange(Enumerable.Repeat(megamoduleToBePlaced.MegamoduleColour, placedGeometry.Count));
            }

            DA.SetDataList(0, placedMegamoduleGeometry);
            DA.SetDataList(1, placedMegamoduleNames);
            DA.SetDataList(2, placedMegamoduleColours);
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
                Properties.Resources.B;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("DEF6F50C-3F3D-4A02-B70D-416321D4B617");
    }
}