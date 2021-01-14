// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace Monoceros {
    /// <summary>
    /// Wraps <see cref="Rule"/> type so that it can be used in Grasshopper as
    /// an input, output or a floating parameter.
    /// </summary>
    public class RuleParameter : GH_PersistentParam<Rule> {
        public RuleParameter( ) : base("Rule",
                                       "R",
                                       "Contains a collection of Monoceros Rules.",
                                       "Monoceros",
                                       "Parameters") { }
        public override Guid ComponentGuid => new Guid("9804D786-20FA-4DEF-A68E-7A5D47D8A61D");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.RP;

        public bool Hidden { get => true; set { } }

        protected override GH_GetterResult Prompt_Plural(ref List<Rule> values) {
            values = new List<Rule>();
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Singular(ref Rule value) {
            value = new Rule();
            return GH_GetterResult.success;
        }
    }
}
