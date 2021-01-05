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

namespace WFCPlugin
{
    /// <summary>
    /// <para>
    /// The <see cref="Slot"/> is a basic unit of the WFC World. It has s shape of 
    /// a cuboid and occupies a single cell of a homogeneous discrete 3D grid. 
    /// It specifies which <see cref="Module"/>s can be placed into the respective
    /// World grid cell.
    /// </para>
    /// <para>
    /// The <see cref="Slot"/>s of the world do not have to form any specific continuous blob,
    /// neither must they fill an entire orthogonal block of the World. 
    /// The <see cref="ComponentFauxSolver"/> encapsulates the <see cref="Slot"/>s into a 
    /// cuboid block, adds missing slots with only an "Out" module allowed and wraps the
    /// entire World into another layer of "Out"-enabled slots from each of the 6 sides of 
    /// the World block.
    /// </para>
    /// <para>
    /// The <see cref="Slot"/> dimension is specified by its <see cref="Diagonal"/>
    /// and has to match the dimensions of the World grid. The <see cref="BasePlane"/>
    /// specifies the origin and orientation of the World grid, in which is the 
    /// <see cref="Slot"/> defined. All <see cref="Slot"/>s of the world have to be
    /// anchored by the same <see cref="BasePlane"/>.
    /// The <see cref="Slot"/> is located at <see cref="RelativeCenter"/>, which 
    /// specifies its integer coordinate in the World grid. Its Cartesian
    /// <see cref="AbsoluteCenter"/> has to be calculated from the <see cref="BasePlane"/>, 
    /// <see cref="Diagonal"/> and <see cref="RelativeCenter"/>.
    /// </para>
    /// <para>
    /// The main purpose of the <see cref="Slot"/> is to specify, which <see cref="Module"/>s
    /// can be placed into the corresponding World grid cell by the <see cref="ComponentFauxSolver"/>
    /// and the <see cref="ComponentPostprocessor"/>. The Grasshopper API only reveals the list of
    /// <see cref="AllowedModuleNames"/> (because the concept of submodules is repressed). Before the 
    /// <see cref="Slot"/> can be processed by the <see cref="ComponentFauxSolver"/> 
    /// or <see cref="ComponentPostprocessor"/>, it has to unwrap <see cref="AllowedSubmoduleNames"/>
    /// from a given list of <see cref="Module"/>s.
    /// The <see cref="ComponentFauxSolver"/> then should reduce the number of allowed submodules 
    /// to one. If the remaining submodule is <see cref="Module.PivotSubmoduleName"/> 
    /// (the one that triggers the entire module placement), the submodule's parent <see cref="Module"/> 
    /// will be oriented from its own <see cref="Module.Pivot"/> onto <see cref="Slot.Pivot"/> 
    /// by the <see cref="ComponentPostprocessor"/>.
    /// </para>
    /// <para>
    /// In Grasshopper the <see cref="Slot"/> can be instantiated with a list of 
    /// <see cref="AllowedModuleNames"/> using the <see cref="ComponentConstructSlotWithModules"/>.
    /// It is also possible to instantiate a <see cref="Slot"/> with any module allowed
    /// (except "Out" module, which is a special case) using <see cref="ComponentConstructSlotWithAll"/>.
    /// In such case the <see cref="AllowAnyModule"/> flag is set to True.
    /// That is especially useful because the list of existing <see cref="Module"/> names does not have to 
    /// be known.
    /// </para>
    /// <para>
    /// In a special case, the <see cref="Slot"/> can be defined as containing the "Out" module, which is
    /// a <see cref="Module"/> with a single submodule, no geometry and predefined external <see cref="Rule"/>s
    /// typed as Indifferent. The "Out" <see cref="Slot"/> can be created only internally ad it is not possible
    /// to instantiate it from Grasshopper.
    /// </para>
    /// </summary>
    public class Slot : IGH_Goo, IGH_PreviewData, IGH_BakeAwareObject
    {
        /// <summary>
        /// The <see cref="BasePlane"/>
        /// specifies the origin and orientation of the World grid, in which is the 
        /// <see cref="Slot"/> defined. All <see cref="Slot"/>s of the world have to be
        /// anchored by the same <see cref="BasePlane"/>.
        /// </summary>
        public readonly Plane BasePlane;

        /// <summary>
        /// The <see cref="Slot"/> is located at <see cref="RelativeCenter"/>, which 
        /// specifies its integer coordinate in the World grid. 
        /// </summary>
        public readonly Point3i RelativeCenter;

        /// <summary>
        /// The <see cref="Slot"/> dimension is specified by its <see cref="Diagonal"/>
        /// and has to match the dimensions of the World grid. 
        /// </summary>
        public readonly Vector3d Diagonal;

        /// <summary>
        /// Flag marking whether the <see cref="Slot"/> may contain any module without 
        /// the necessity explicitly of specifying a list of <see cref="AllowedModuleNames"/>
        /// </summary>
        public readonly bool AllowAnyModule;

        /// <summary>
        /// An explicit list of <see cref="Module"/> names that can be placed into the <see cref="Slot"/>.
        /// The Grasshopper API only reveals the list of <see cref="AllowedModuleNames"/> 
        /// (because the concept of submodules is repressed). Before the <see cref="Slot"/> can be 
        /// processed by the <see cref="ComponentFauxSolver"/> or <see cref="ComponentPostprocessor"/>, 
        /// it has to unwrap <see cref="AllowedSubmoduleNames"/> from a given list of <see cref="Module"/>s.
        /// </summary>
        public readonly List<string> AllowedModuleNames;

        /// <summary>
        /// An explicit list of submodule names that can be placed into the <see cref="Slot"/>.
        /// The Grasshopper API only reveals the list of <see cref="AllowedModuleNames"/> 
        /// (because the concept of submodules is repressed). Before the <see cref="Slot"/> can be 
        /// processed by the <see cref="ComponentFauxSolver"/> or <see cref="ComponentPostprocessor"/>, 
        /// it has to unwrap <see cref="AllowedSubmoduleNames"/> from a given list of <see cref="Module"/>s.
        /// This is always done internally.
        /// </summary>
        public readonly List<string> AllowedSubmoduleNames;

        /// <summary>
        /// Holds the number of existing submodules in the solution for viewport display purposes. 
        /// The color of the slot represents its entropy (number of allowed submodules).
        /// </summary>
        public readonly int AllSubmodulesCount;


        /// <summary>
        /// Initializes a new instance of the <see cref="Slot"/> class. Creates an invalid instance.
        /// Required by Grasshopper.
        /// </summary>
        public Slot()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Slot"/> class.
        /// </summary>
        /// <param name="basePlane">The base plane - has to match with the other <see cref="Slot"/>s of the World.</param>
        /// <param name="relativeCenter">The relative center.</param>
        /// <param name="diagonal">The <see cref="Slot"/> diagonal - has to match with the other <see cref="Slot"/>s 
        /// of the World and with all <see cref="Module"/>s</param>
        /// <param name="allowAnyModule">If true, any submodule can be placed into the <see cref="Slot"/>.</param>
        /// <param name="allowedModules">The allowed modules.</param>
        /// <param name="allowedSubModules">The allowed submodules.</param>
        /// <param name="allSubmodulesCount">The all submodules count.</param>
        public Slot(Plane basePlane,
                    Point3i relativeCenter,
                    Vector3d diagonal,
                    bool allowAnyModule,
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
            AllowAnyModule = allowAnyModule;
            AllowedModuleNames = allowedModules;
            AllowedSubmoduleNames = allowedSubModules;
            AllSubmodulesCount = allSubmodulesCount;
        }

        /// <summary>
        /// Duplicates the <see cref="Slot"/> with <see cref="Module"/> names explicitly specified.
        /// Useful for unwrapping <see cref="Slot"/>s that <see cref="AllowAnyModule"/>.
        /// </summary>
        /// <param name="moduleNames">The new <see cref="AllowedModuleNames"/>.</param>
        /// <returns>A Slot.</returns>
        public Slot DuplicateWithModuleNames(List<string> moduleNames)
        {
            return new Slot(
                        BasePlane,
                        RelativeCenter,
                        Diagonal,
                        AllowAnyModule,
                        moduleNames,
                        AllowedSubmoduleNames,
                        AllSubmodulesCount
                        );
        }

        /// <summary>
        /// Duplicates the <see cref="Slot"/> with <see cref="AllSubmodulesCount"/> specified.
        /// Useful for already unwrapped <see cref="Slot"/>s that do not yet know how to display themselves.
        /// </summary>
        /// <param name="allSubmodulesCount">The number of all submodules in the solution.</param>
        /// <returns>A Slot.</returns>
        public Slot DuplicateWithSubmodulesCount(int allSubmodulesCount)
        {
            return new Slot(
                        BasePlane,
                        RelativeCenter,
                        Diagonal,
                        AllowAnyModule,
                        AllowedModuleNames,
                        AllowedSubmoduleNames,
                        allSubmodulesCount
                        );
        }

        /// <summary>
        /// Duplicates the <see cref="Slot"/> with allowed submodule names and <see cref="AllSubmodulesCount"/> specified.
        /// Useful for assigning <see cref="AllowedSubmoduleNames"/> to <see cref="Slot"/>s that only have user defined 
        /// <see cref="AllowedModuleNames"/> when the number of all submodules in the solution is already known.
        /// </summary>
        /// <param name="allSubmodulesCount">The number of all submodules in the solution.</param>
        /// <param name="submoduleNames">The new <see cref="AllowedSubmoduleNames"/>.</param>
        /// <returns>A Slot.</returns>
        public Slot DuplicateWithSubmodulesCountAndSubmoduleNames(int allSubmodulesCount, List<string> submoduleNames)
        {
            return new Slot(
                        BasePlane,
                        RelativeCenter,
                        Diagonal,
                        false,
                        AllowedModuleNames,
                        submoduleNames,
                        allSubmodulesCount
                        );
        }

        /// <summary>
        /// The <see cref="Slot"/>'s Cartesian <see cref="AbsoluteCenter"/> has to be calculated 
        /// from the <see cref="BasePlane"/>, <see cref="Diagonal"/> and <see cref="RelativeCenter"/>.
        /// </summary>
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

        /// <summary>
        /// Required by Grasshopper.
        /// </summary>
        /// <returns>An IGH_Goo.</returns>
        public IGH_Goo Duplicate() => (IGH_Goo)MemberwiseClone();

        /// <summary>
        /// Required by Grasshopper.
        /// </summary>
        /// <returns>An IGH_GooProxy.</returns>
        public IGH_GooProxy EmitProxy() => null;

        /// <summary>
        /// The Slot cannot be automatically cast from any existing Grasshopper type.
        /// Required by Grasshopper.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>False</returns>
        public bool CastFrom(object source) => false;

        /// <summary>
        /// Casts the <see cref="Slot"/> to a <see cref="GH_Point"/> (from <see cref="AbsoluteCenter"/>,
        /// to a <see cref="Box"/> (from <see cref="Cage"/>) or to a <see cref="Brep"/> (from <see cref="Cage"/>).
        /// Required by Grasshopper for automatic type casting.
        /// </summary>
        /// <param name="target">The target Grasshopper geometry.</param>
        /// <returns>True when successful.</returns>
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

        // TODO: Find out what this is and what should be done here
        /// <summary>
        /// Returns the script variable.
        /// Required by Grasshopper.
        /// </summary>
        /// <returns>An object.</returns>
        public object ScriptVariable() => this;

        // TODO: Do this for real
        /// <summary>
        /// De-serialization.
        /// Required by Grasshopper for data internalization.
        /// </summary>
        /// <remarks>Not implemented yet.</remarks>
        /// <param name="reader">The reader.</param>
        /// <returns>A bool when successful.</returns>
        public bool Read(GH_IReader reader) => true;

        // TODO: Do this for real
        /// <summary>
        /// Serialization.
        /// Required by Grasshopper for data internalization.
        /// </summary>
        /// <remarks>Not implemented yet.</remarks>
        /// <param name="writer">The writer.</param>
        /// <returns>A bool when successful.</returns>
        public bool Write(GH_IWriter writer) => true;


        /// <summary>
        /// Does nothing.
        /// Required by Grasshopper.
        /// </summary>
        /// <param name="args">The args.</param>
        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
        }

        /// <summary>
        /// Draws the <see cref="Cage"/> of the <see cref="Slot"/> into the viewport.
        /// The color hints the entropy of the <see cref="Slot"/>.
        /// <list type="bullet">
        /// <item>
        /// <term><see cref="Configuration.CAGE_UNKNOWN_COLOR"/></term>
        /// <description>Impossible to determine the entropy, either because <see cref="AllowedSubmoduleNames"/> 
        /// has not been unwrapped or the <see cref="AllSubmodulesCount"/> is yet unknown.</description>
        /// </item>
        /// <item>
        /// <term><see cref="Configuration.CAGE_EVERYTHING_COLOR"/></term>
        /// <description>The <see cref="Slot"/> allows placement of any <see cref="Module"/>. 
        /// The <see cref="AllowAnyModule"/> flag is on.</description>
        /// </item>
        /// <item>
        /// <term><see cref="Configuration.CAGE_NONE_COLOR"/></term>
        /// <description>The <see cref="Slot"/> does not allow any <see cref="Module"/> to be placed. 
        /// Such <see cref="Slot"/> can only be generated by the <see cref="ComponentFauxSolver"/>.
        /// The solution is contradictory and therefore invalid.</description>
        /// </item>
        /// <item>
        /// <term><see cref="Configuration.CAGE_ONE_COLOR"/></term>
        /// <description>The <see cref="Slot"/> is deterministic and allows only single submodule 
        /// (and therefore also a single <see cref="Module"/>) to be placed onto its 
        /// <see cref="Pivot"/>. This is the desired state.</description>
        /// </item>
        /// <item>
        /// <term>Gradient between <see cref="Configuration.CAGE_ONE_COLOR"/> and 
        /// <see cref="Configuration.CAGE_EVERYTHING_COLOR"/></term>
        /// <description>The <see cref="Slot"/> is not deterministic (allows placement of multiple 
        /// <see cref="Module"/>s)to be placed). 
        /// The color from the gradient hints the level of entropy (number of submodules still allowed).</description>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="args">The preview mesh arguments.</param>
        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            var color = Configuration.CAGE_UNKNOWN_COLOR;

            if (AllowAnyModule)
            {
                color = Configuration.CAGE_EVERYTHING_COLOR;
            }

            if (AllowedNothing)
            {
                color = Configuration.CAGE_NONE_COLOR;
            }

            var submodulesCount = AllowedSubmoduleNames.Count;

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

        /// <summary>
        /// True if the <see cref="Slot"/> is contradictory and therefore does not allow placement 
        /// of any submodule and therefore also any <see cref="Module"/>.
        /// </summary>
        public bool AllowedNothing =>
            (!AllowAnyModule && AllowedModuleNames.Count == 0) ||
            (!AllowAnyModule && AllowedSubmoduleNames.Count == 0);

        /// <summary>
        /// Checks the validity of a <see cref="Slot"/>. 
        /// Required by Grasshopper.
        /// </summary>
        // Only Grasshopper can instantiate an invalid slot and in such case 
        // the fields are set to their defaults. It is sufficient to null-check 
        // just one of them.
        public bool IsValid => Diagonal != null;

        /// <summary>
        /// Explains the invalidity of the <see cref="Slot"/>.
        /// Required by Grasshopper.
        /// </summary>
        public string IsValidWhyNot => "Slot not initiated";

        /// <summary>
        /// Gets the type name.
        /// Required by Grasshopper.
        /// </summary>
        public string TypeName => "WFC Slot";

        /// <summary>
        /// Gets the type description.
        /// Required by Grasshopper.
        /// </summary>
        public string TypeDescription => "WFC World Slot that may contain module parts.";

        /// <summary>
        /// Provides an user-friendly description of a <see cref="Slot"/>.
        /// Required by Grasshopper.
        /// </summary>
        /// <returns>A string.</returns>
        public override string ToString()
        {
            var pt = new GH_Point(AbsoluteCenter);
            var diagonal = new GH_Vector(Diagonal);
            var plane = new GH_Plane(BasePlane);
            var containment = "";
            if (AllowAnyModule)
            {
                containment = "all modules";
            }
            if (AllowedNothing)
            {
                containment = "no modules";
            }
            if (!AllowedNothing && !AllowAnyModule)
            {
                var count = AllowedModuleNames.Count;
                if (count == 1)
                {
                    containment = "module '" + AllowedModuleNames[0] + "'";
                }
                else
                {
                    containment = count + " modules";
                }
            }
            return "Slot contains " + containment + ". Center at " + pt + ", base plane at " + plane + " with dimensions " + diagonal + ".";
        }

        /// <summary>
        /// Required by Grasshopper for baking.
        /// </summary>
        public BoundingBox ClippingBox => Cage.BoundingBox;

        /// <summary>
        /// Required by Grasshopper for baking.
        /// </summary>
        public bool IsBakeCapable => IsValid;

        /// <summary>
        /// Bakes the <see cref="Cage"/> with no <see cref="ObjectAttributes"/> provided.
        /// Required by Grasshopper for baking.
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <param name="obj_ids">The obj_ids.</param>
        public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            BakeGeometry(doc, new ObjectAttributes(), obj_ids);
        }

        /// <summary>
        /// Bakes the <see cref="Cage"/> in color hinting its entropy.
        /// Required by Grasshopper.
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <param name="att">The att.</param>
        /// <param name="obj_ids">The obj_ids.</param>
        public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            if (att == null)
            {
                att = doc.CreateDefaultAttributes();
            }

            var color = Configuration.CAGE_UNKNOWN_COLOR;

            if (AllowAnyModule)
            {
                color = Configuration.CAGE_EVERYTHING_COLOR;
            }

            if (AllowedNothing)
            {
                color = Configuration.CAGE_NONE_COLOR;
            }

            var submodulesCount = AllowedSubmoduleNames.Count;

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

        /// <summary>
        /// Gets the pivot of the <see cref="Slot"/>.
        /// The pivot is a plane at the <see cref="AbsoluteCenter"/> of the <see cref="Slot"/> 
        /// aligned to the <see cref="BasePlane"/>.
        /// </summary>
        public Plane Pivot
        {
            get
            {
                var slotPivot = BasePlane.Clone();
                slotPivot.Origin = AbsoluteCenter;
                return slotPivot;
            }
        }


        /// <summary>
        /// Gets the <see cref="Slot"/> cage - a cuboid at the position of the <see cref="Slot"/> 
        /// with dimensions defined by the <see cref="Diagonal"/>. It reveals the respective cell
        /// of the WFC World.
        /// </summary>
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
