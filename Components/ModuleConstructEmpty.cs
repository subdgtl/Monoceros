﻿using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WFCPlugin
{
    /// <summary>
    /// <para>
    /// Grasshopper component: WFC Construct Empty Module
    /// </para>
    /// <para>
    /// Construct the "Empty" module. "Empty" is a system reserved module, which
    /// consists of a single submodule, has no geometry and all its connectors
    /// are assigned a <see cref="RuleTyped"/> with "Indifferent" connector
    /// type.
    /// </para>
    /// <para>
    /// Grasshopper inputs:
    /// <list type="bullet">
    ///     <item>
    ///         <term><see cref="Plane"/> Base Plane</term>
    ///         <description>Grid space base plane. Defines orientation of the
    ///             grid. Item access. Default: <see cref="Plane.WorldXY"/>
    ///             </description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Vector3d"/> Grid Slot Diagonal</term>
    ///         <description>World grid slot diagonal vector specifying single
    ///             grid slot dimension in base-plane-aligned XYZ axes. Item
    ///             access. Default: <c>Vector3d(1.0, 1.0, 1.0).</c>
    ///             </description>
    ///     </item>
    /// </list>
    /// </para>
    /// <para>
    /// Grasshopper outputs:
    /// <list type="bullet">
    ///     <item>
    ///         <term><see cref="Module"/> Module</term>
    ///         <description>WFC Module. Item access.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Rule"/> Rules</term>
    ///         <description>WFC Rules making the Empty rule Indifferent. List
    ///             access.</description>
    ///     </item>
    /// </list>
    /// </para>
    /// </summary>
    public class ComponentModuleEmpty : GH_Component
    {
        public ComponentModuleEmpty() : base("WFC Construct Empty Module",
                                             "WFCModuleEmpty",
                                             "Construct an empty WFC Module.",
                                             "WaveFunctionCollapse",
                                             "Module")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Base Plane",
                                       "B",
                                       "Grid space base plane. Defines orientation of the grid.",
                                       GH_ParamAccess.item,
                                       Plane.WorldXY);
            pManager.AddVectorParameter(
               "Grid Slot Diagonal",
               "D",
               "World grid slot diagonal vector specifying single grid slot dimension in " +
               "base-plane-aligned XYZ axes",
               GH_ParamAccess.item,
               new Vector3d(1.0, 1.0, 1.0)
               );
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ModuleParameter(),
                                  "Module",
                                  "M",
                                  "Empty WFC Module",
                                  GH_ParamAccess.item);
            pManager.AddParameter(new RuleParameter(),
                                  "Rules",
                                  "R",
                                  "WFC Rules making the Empty rule Indifferent",
                                  GH_ParamAccess.list);
        }

        /// <summary>
        /// Wrap input geometry into module cages.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from
        ///     input parameters and to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Plane basePlane = new Plane();
            Vector3d slotDiagonal = new Vector3d();

            if (!DA.GetData(0, ref basePlane))
            {
                return;
            }

            if (!DA.GetData(1, ref slotDiagonal))
            {
                return;
            }

            if (slotDiagonal.X <= 0 || slotDiagonal.Y <= 0 || slotDiagonal.Z <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                                  "One or more slot dimensions are not larger than 0.");
                return;
            }

            Module.GenerateEmptySingleModuleWithBasePlane(Config.EMPTY_MODULE_NAME,
                                                          Config.INDIFFERENT_TAG,
                                                          basePlane,
                                                          slotDiagonal,
                                                          out Module moduleEmpty,
                                                          out List<RuleTyped> rulesExternal);

            DA.SetData(0, moduleEmpty);
            DA.SetDataList(1, rulesExternal.Select(ruleTyped => new Rule(ruleTyped)));
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
        public override Guid ComponentGuid => new Guid("3473FD2C-D314-44C7-A5B6-EE30B008B04C");
    }
}
