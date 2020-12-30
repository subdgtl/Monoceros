// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace WFCToolset
{
    /// <summary>
    /// WFC Module type containing name, geometry and submodule and connector info, 
    /// including internal rules holding the module together.
    /// </summary>
    public class Slot : IGH_Goo, IGH_PreviewData, IGH_BakeAwareObject
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

        public readonly bool AllowedEverything;

        public readonly List<string> AllowedModules;
        public readonly List<string> AllowedSubmodules;

        private readonly int _allSubmodulesCount;

        public Slot()
        {
        }

        public Slot(Plane basePlane,
                    Point3i relativeCenter,
                    Vector3d diagonal,
                    bool allowedEverthing,
                    List<string> allowedModules,
                    List<string> allowedSubModules,
                    int allSubmodulesCount)
        {
            if (diagonal.X <= 0 || diagonal.Y <= 0 || diagonal.Z <= 0)
            {
                throw new Exception("One or more slot dimensions are not larger than 0.");
            }

            if (allSubmodulesCount < 0)
            {
                throw new Exception("All submodules count is lower than 0.");
            }

            Diagonal = diagonal;
            BasePlane = basePlane;
            RelativeCenter = relativeCenter;
            AllowedEverything = allowedEverthing;
            AllowedModules = allowedModules;
            AllowedSubmodules = allowedSubModules;
            _allSubmodulesCount = allSubmodulesCount;
        }

        public Slot DuplicateWithModuleNames(List<string> moduleNames)
        {
            return new Slot(
                        BasePlane,
                        RelativeCenter,
                        Diagonal,
                        AllowedEverything,
                        moduleNames,
                        AllowedSubmodules,
                        AllSubmodulesCount
                        );
        }

        public Slot DuplicateWithSubmodulesCount(int allSubmodulesCount)
        {
            return new Slot(
                        BasePlane,
                        RelativeCenter,
                        Diagonal,
                        AllowedEverything,
                        AllowedModules,
                        AllowedSubmodules,
                        allSubmodulesCount
                        );
        }

        public Slot DuplicateWithSubmodulesCountAndSubmoduleNames(int allSubmodulesCount, List<string> submoduleNames)
        {
            return new Slot(
                        BasePlane,
                        RelativeCenter,
                        Diagonal,
                        false,
                        AllowedModules,
                        submoduleNames,
                        allSubmodulesCount
                        );
        }

        public Point3d AbsoluteCenter
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
            if (typeof(T) == typeof(GH_Point))
            {
                var absoluteCenter = new GH_Point(AbsoluteCenter);
                target = (T)absoluteCenter.Duplicate();
                return true;
            }

            if (typeof(T) == typeof(GH_Box))
            {
                var box = new GH_Box(Cage);
                target = (T)box.Duplicate();
                return true;
            }

            if (typeof(T) == typeof(GH_Brep))
            {
                var boxBrep = new GH_Brep(Cage.ToBrep());
                target = (T)boxBrep.Duplicate();
                return true;
            }

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

            if (AllowedEverything)
            {
                color = Configuration.CAGE_EVERYTHING_COLOR;
            }

            if (AllowedNothing)
            {
                color = Configuration.CAGE_NONE_COLOR;
            }

            var submodulesCount = AllowedSubmodules.Count;

            if (submodulesCount == 1 && AllSubmodulesCount != 0)
            {
                color = Configuration.CAGE_ONE_COLOR;
            }

            if (submodulesCount > 1 && AllSubmodulesCount != 0)
            {
                var t = (double)submodulesCount / AllSubmodulesCount;
                color = System.Drawing.Color.FromArgb(
                    Convert.ToByte(Configuration.CAGE_TWO_COLOR.A + (Configuration.CAGE_EVERYTHING_COLOR.A - Configuration.CAGE_TWO_COLOR.A) * t),
                    Convert.ToByte(Configuration.CAGE_TWO_COLOR.R + (Configuration.CAGE_EVERYTHING_COLOR.R - Configuration.CAGE_TWO_COLOR.R) * t),
                    Convert.ToByte(Configuration.CAGE_TWO_COLOR.G + (Configuration.CAGE_EVERYTHING_COLOR.G - Configuration.CAGE_TWO_COLOR.G) * t),
                    Convert.ToByte(Configuration.CAGE_TWO_COLOR.B + (Configuration.CAGE_EVERYTHING_COLOR.B - Configuration.CAGE_TWO_COLOR.B) * t)
                    );
            }

            var material = args.Material;
            material.Diffuse = color;

            args.Pipeline.DrawBrepShaded(Cage.ToBrep(), material);
        }

        public bool AllowedNothing => !AllowedEverything && AllowedModules.Count == 0;

        public bool IsValid => Diagonal != null;

        public string IsValidWhyNot => "Slot not initiated";

        public string TypeName => "WFC Slot";

        public string TypeDescription => "WFC World Slot that may contain module parts.";

        public override string ToString()
        {
            var pt = new GH_Point(AbsoluteCenter);
            var diagonal = new GH_Vector(Diagonal);
            var plane = new GH_Plane(BasePlane);
            var containment = "";
            if (AllowedEverything)
            {
                containment = "all modules";
            }
            if (AllowedNothing)
            {
                containment = "no modules";
            }
            if (!AllowedNothing && !AllowedEverything)
            {
                var count = AllowedModules.Count;
                if (count == 1)
                {
                    containment = "module '" + AllowedModules[0] + "'";
                }
                else
                {
                    containment = count + " modules";
                }
            }
            return "Slot contains " + containment + ". Center at " + pt + ", base plane at " + plane + " with dimensions " + diagonal + ".";
        }

        public BoundingBox ClippingBox => Cage.BoundingBox;

        public bool IsBakeCapable => IsValid;

        public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            BakeGeometry(doc, new ObjectAttributes(), obj_ids);
        }

        public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            if (att == null)
            {
                att = doc.CreateDefaultAttributes();
            }

            var color = Configuration.CAGE_UNKNOWN_COLOR;

            if (AllowedEverything)
            {
                color = Configuration.CAGE_EVERYTHING_COLOR;
            }

            if (AllowedNothing)
            {
                color = Configuration.CAGE_NONE_COLOR;
            }

            var submodulesCount = AllowedSubmodules.Count;

            if (submodulesCount == 1 && AllSubmodulesCount != 0)
            {
                color = Configuration.CAGE_ONE_COLOR;
            }

            if (submodulesCount != 1 && AllSubmodulesCount != 0)
            {
                var t = (double)submodulesCount / AllSubmodulesCount;
                color = System.Drawing.Color.FromArgb(
                    Convert.ToByte(Configuration.CAGE_TWO_COLOR.A + (Configuration.CAGE_EVERYTHING_COLOR.A - Configuration.CAGE_TWO_COLOR.A) * t),
                    Convert.ToByte(Configuration.CAGE_TWO_COLOR.R + (Configuration.CAGE_EVERYTHING_COLOR.R - Configuration.CAGE_TWO_COLOR.R) * t),
                    Convert.ToByte(Configuration.CAGE_TWO_COLOR.G + (Configuration.CAGE_EVERYTHING_COLOR.G - Configuration.CAGE_TWO_COLOR.G) * t),
                    Convert.ToByte(Configuration.CAGE_TWO_COLOR.B + (Configuration.CAGE_EVERYTHING_COLOR.B - Configuration.CAGE_TWO_COLOR.B) * t)
                    );
            }

            var cageAttributes = att.Duplicate();
            cageAttributes.ObjectColor = color;
            cageAttributes.ColorSource = ObjectColorSource.ColorFromObject;

            obj_ids.Add(doc.Objects.AddBox(Cage, cageAttributes));
        }

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

        public int AllSubmodulesCount => _allSubmodulesCount;
    }
}
