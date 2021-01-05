// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace WFCPlugin {
    public class WFCPluginInfo : GH_AssemblyInfo {
        public override string Name => "WFC Plugin";
        public override Bitmap Icon =>
                //Return a 24x24 pixel bitmap to represent this GHA library.
                WFCPlugin.Properties.Resources.WFC;
        public override string Description =>
                //Return a short string describing the purpose of this GHA library.
                "Subdigital: Wave Function Collapse plug-in for Grasshopper.";
        public override Guid Id => new Guid("e02a564d-2a18-4990-bfc2-852fb04f9268");

        public override string AuthorName =>
                //Return a string identifying you or your company.
                "Subdigital";
        public override string AuthorContact =>
                //Return a string representing your preferred contact details.
                "www.sub.digital | info@sub.digital";
    }
}
