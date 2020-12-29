// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace WFCToolset
{
    /// <summary>
    /// WFC Module type containing name, geometry and submodule and connector info, 
    /// including internal rules holding the module together.
    /// </summary>
    public class Slot : IGH_Goo, IGH_PreviewData
    {
        /// <summary>
        /// Base plane of the module's coordinate system.
        /// </summary>
        public readonly Plane BasePlane;

        /// <summary>
        /// Centers of submodules for module reconstruction.
        /// </summary>
        public readonly Point3i RelativeCenter;

        /// <summary>
        /// Dimensions of a single slot. Slots are boxes encapsulating the module geometry.
        /// The filed's purpose is to check compatibility of various modules with the world they should populate.
        /// In the future the modules could be squished to fit the world's slot dimensions.
        /// </summary>
        public readonly Vector3d Diagonal;

        public readonly bool AllowedEverthing;

        public readonly bool CameFromSolver;

        public List<string> _allowedModules;
        public List<string> _allowedSubModules;

        private readonly int _allSubmodulesCount;

        public Slot()
        {
        }

        public Slot(Plane basePlane,
                    Point3i relativeCenter,
                    Vector3d diagonal,
                    bool allowedEverthing,
                    bool cameFromSolver,
                    List<string> allowedModules,
                    List<string> allowedSubModules,
                    int allSubmodulesCount)
        {
            BasePlane = basePlane;
            RelativeCenter = relativeCenter;
            Diagonal = diagonal;
            AllowedEverthing = allowedEverthing;
            CameFromSolver = cameFromSolver;
            _allowedModules = allowedModules;
            _allowedSubModules = allowedSubModules;
            _allSubmodulesCount = allSubmodulesCount;
        }

        private Point3d AbsoluteCenter
        {
            get
            {
                var baseAlignmentTransform = Transform.PlaneToPlane(Plane.WorldXY, BasePlane);
                var scalingTransform = Transform.Scale(BasePlane, Diagonal.X, Diagonal.Y, Diagonal.Z);

                var submoduleCenter = RelativeCenter.ToPoint3d();
                submoduleCenter.Transform(baseAlignmentTransform);
                submoduleCenter.Transform(scalingTransform);
                return submoduleCenter;
            }
        }

        public IGH_Goo Duplicate() => (IGH_Goo)MemberwiseClone();

        public IGH_GooProxy EmitProxy() => null;

        public bool CastFrom(object source) => false;

        public bool CastTo<T>(out T target)
        {
            target = default;
            return false;
        }

        public object ScriptVariable() => this;

        public bool Write(GH_IWriter writer) => true;

        public bool Read(GH_IReader reader) => true;

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            var color = Configuration.CAGE_UNKNOWN_COLOR;

            if (AllowedEverthing)
            {
                color = Configuration.CAGE_EVERYTHING_COLOR;
            }

            if (AllowedNothing)
            {
                color = Configuration.CAGE_NONE_COLOR;
            }

            var submodulesCount = _allowedSubModules.Count;

            if (CameFromSolver && submodulesCount == 1)
            {
                color = Configuration.CAGE_ONE_COLOR;
            }

            if (CameFromSolver && submodulesCount != 1)
            {
                var t = (double)submodulesCount / _allSubmodulesCount;
                color = System.Drawing.Color.FromArgb(
                    Convert.ToByte(Configuration.CAGE_TWO_COLOR.A + (Configuration.CAGE_EVERYTHING_COLOR.A - Configuration.CAGE_TWO_COLOR.A) * t),
                    Convert.ToByte(Configuration.CAGE_TWO_COLOR.R + (Configuration.CAGE_EVERYTHING_COLOR.R - Configuration.CAGE_TWO_COLOR.R) * t),
                    Convert.ToByte(Configuration.CAGE_TWO_COLOR.G + (Configuration.CAGE_EVERYTHING_COLOR.G - Configuration.CAGE_TWO_COLOR.G) * t),
                    Convert.ToByte(Configuration.CAGE_TWO_COLOR.B + (Configuration.CAGE_EVERYTHING_COLOR.B - Configuration.CAGE_TWO_COLOR.B) * t)
                    );
            }

            args.Pipeline.DrawBox(Cage, color);
        }

        public bool AllowedNothing => _allowedModules.Count == 0;

        public bool IsValid => true;

        public string IsValidWhyNot => "Never";

        public string TypeName => "WFCSlot";

        public string TypeDescription => "WFC World Slot that may contain module parts.";

        public BoundingBox ClippingBox => Cage.BoundingBox;

        private Box Cage
        {
            get
            {
                var boxPlane = BasePlane.Clone();
                boxPlane.Origin = AbsoluteCenter;
                var xInterval = new Interval(-Diagonal.X / 2, Diagonal.X / 2);
                var yInterval = new Interval(-Diagonal.Y / 2, Diagonal.Y / 2);
                var zInterval = new Interval(-Diagonal.Z / 2, Diagonal.Z / 2);
                var box = new Box(boxPlane, xInterval, yInterval, zInterval);
                return box;
            }
        }
    }
}
