﻿using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

[assembly: GH_Loading(GH_LoadingDemand.ForceDirect)]

namespace Monoceros {
    public class MonocerosInfo : GH_AssemblyInfo {
        public override string Name => "Monoceros";
        public override Bitmap Icon =>
                //Return a 24x24 pixel bitmap to represent this GHA library.
                Properties.Resources.monoceros24;
        public override string Description =>
                //Return a short string describing the purpose of this GHA library.
                "Monoceros: A Wave Function Collapse plug-in by Subdigital";
        public override Guid Id => new Guid("e02a564d-2a18-4990-bfc2-852fb04f9268");

        public override string AuthorName =>
                //Return a string identifying you or your company.
                "Subdigital";
        public override string AuthorContact =>
                //Return a string representing your preferred contact details.
                "www.sub.digital";
    }

    public class MonocerosCategoryIcon : GH_AssemblyPriority {
        public override GH_LoadingInstruction PriorityLoad( ) {
            Instances.ComponentServer.AddCategoryIcon("Monoceros", Properties.Resources.monoceros24);
            Instances.ComponentServer.AddCategorySymbolName("Monoceros", 'M');
            return GH_LoadingInstruction.Proceed;
        }
    }
}
