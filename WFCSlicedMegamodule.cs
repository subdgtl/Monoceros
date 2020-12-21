using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WFCTools {

    // TODO: Obsolete

    /// <summary>
    /// ESubmodule face relation to other submodules. 
    /// </summary>
    public enum Relation {
        InternalFace,
        ExternalFace,
        unknown
    }

    /// <summary>
    /// A face and its direction within an orthogonal 3D grid. 
    /// </summary>
    public struct SubmoduleFace {
        /// <summery>
        /// One of 6 grid module faces.
        /// </summery>
        public BrepFace Face;
        /// <summery>
        /// Direction of the face within an orthogonal grid.
        /// </summery>
        public Direction GridDirection;
        /// <summery>
        /// Relation of the face with other submodules.
        /// </summery>
        public Relation GridRelation;
        /// <summery>
        /// Submodule face center.
        /// </summery>
        public Point3d Center;
        /// <summery>
        /// Parent submodule of the current face.
        /// </summery>
        public string ParentSubmoduleName;

        public override bool Equals(object obj) {
            return obj is SubmoduleFace face &&
                   EqualityComparer<BrepFace>.Default.Equals(Face, face.Face) &&
                   EqualityComparer<Direction>.Default.Equals(GridDirection, face.GridDirection) &&
                   GridRelation == face.GridRelation &&
                   Center.Equals(face.Center) &&
                   ParentSubmoduleName == face.ParentSubmoduleName;
        }

        public override int GetHashCode() {
            int hashCode = -1542689279;
            hashCode = hashCode * -1521134295 + EqualityComparer<BrepFace>.Default.GetHashCode(Face);
            hashCode = hashCode * -1521134295 + GridDirection.GetHashCode();
            hashCode = hashCode * -1521134295 + GridRelation.GetHashCode();
            hashCode = hashCode * -1521134295 + Center.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ParentSubmoduleName);
            return hashCode;
        }

        public static bool operator ==(SubmoduleFace left, SubmoduleFace right) {
            return left.Equals(right);
        }

        public static bool operator !=(SubmoduleFace left, SubmoduleFace right) {
            return !(left == right);
        }
    }

    /// <summary>
    /// Submodule of a sliced megamodule.
    /// Contains Name, WorldAlignedFacesWithDirection, WorldAlignedPivot, WorldAlignedBox, PlaneAlignedBoundingBox.
    /// </summary>
    public struct Submodule {
        /// <summary>
        /// Submodule name.
        /// </summary>
        public string Name;
        /// <summary>
        /// Submodule faces within an orthogonal 3D grid.
        /// </summary>
        public List<SubmoduleFace> WorldAlignedSubmoduleFaces;
        /// <summary>
        /// A plane oriented according to megamodule's base plane pivot with an origin in the center of the submodule.
        /// </summary>
        public Plane WorldAlignedPivot;
        /// <summary>
        /// A box representing the submodule oriented according to the megamodule's base plane pivot.
        /// </summary>
        public Box WorldAlignedBox;
        /// <summary>
        /// A bounding box representing the submodule in coordinates of the megamodule's base plane pivot. 
        /// It can be used for simple detections and manipulations because it is certainly orthogonal.
        /// </summary>
        public BoundingBox PlaneAlignedBoundingBox;
        /// <summary>
        /// A reference to the parent sliced megamodule for purposed of world solution building 
        /// (placing geometry into proper slots).
        /// </summary>
        public WFCSlicedMegamodule SlicedMegamodule;
    }

    /// <summary>
    /// Sliced megamodule.
    /// Contains Name, WorldAlignedPivot, WorldAlignedGeometry, Submodules.
    /// </summary>
    public class WFCSlicedMegamodule : IGH_Goo {
        /// <summary>
        /// Megamodule name.
        /// </summary>
        public string Name;
        /// <summary>
        /// Bbase plane pivot that defines the orientation and origin of the sliced submodules.
        /// </summary>
        public Plane WorldAlignedPivot;
        /// <summary>
        /// Geometry belonging to the megamodule in the original world-aligned orientation.
        /// The simple geometry will be used for slicing the megamodule into submodules.
        /// </summary>
        public List<GeometryBase> WorldAlignedSimpleGeometry;
        /// <summary>
        /// Geometry belonging to the megamodule in the original world-aligned orientation.
        /// The production geometry is not used for slicing the megamodule into submodules.
        /// </summary>
        public List<GeometryBase> WorldAlignedProductionGeometry;
        /// <summary>
        /// Color to be used for displaying and baking the megamodule geometry.
        /// </summary>
        public Color Colour;
        /// <summary>
        /// Sliced submodules.
        /// </summary>
        public List<Submodule> Submodules;


        public WFCSlicedMegamodule() {
            WorldAlignedSimpleGeometry = new List<GeometryBase>();
            WorldAlignedProductionGeometry = new List<GeometryBase>();
            Name = "";
            WorldAlignedPivot = Plane.WorldXY;
            Colour = Color.Black;
            Submodules = new List<Submodule>();
        }

        public bool IsValid => true;

        public string IsValidWhyNot => "Dunno.";

        public string TypeName => "WFCSlicedMegamodule";

        public string TypeDescription => "Megamodule geometry sliced and processed for WFC ruleset calculation.";

        public bool CastFrom(object source) => false;

        public bool CastTo<T>(out T target) {
            target = default;
            return false;
        }

        public IGH_Goo Duplicate() {
            return (IGH_Goo)this.MemberwiseClone();
        }

        public IGH_GooProxy EmitProxy() {
            return (IGH_GooProxy)null;
        }

        public bool Read(GH_IReader reader) => true;

        public object ScriptVariable() => this;

        public bool Write(GH_IWriter writer) => true;

        public override string ToString() {
            return WorldAlignedSimpleGeometry
                .Aggregate(
                "Megamodule name: " + Name + System.Environment.NewLine + "Submodules count:" + Submodules.Count + System.Environment.NewLine,
                (str, geo) => str + "- " + geo.ToString() + System.Environment.NewLine
                );
        }
    }
}
