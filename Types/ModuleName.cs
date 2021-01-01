// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;

namespace WFCToolset
{
    public class ModuleName : IGH_Goo
    {
        private string _name;
        public ModuleName()
        {
        }

        public ModuleName(string name)
        {
            if (name.Length == 0)
            {
                throw new Exception("The name is be empty.");
            }
            Name = name;
        }

        public IGH_Goo Duplicate() => (IGH_Goo)MemberwiseClone();

        public IGH_GooProxy EmitProxy() => null;

        public bool CastFrom(object inputData)
        {
            if (inputData.GetType() == typeof(string))
            {
                Name = (string)inputData;
                return true;
            }
            if (inputData.GetType() == typeof(GH_String))
            {
                var ghName = (GH_String)inputData;
                Name = ghName.ToString();
                return true;
            }
            if (inputData.GetType() == typeof(Module))
            {
                var module = (Module)inputData;
                Name = module.Name;
                return true;
            }
            return false;
        }

        public bool CastTo<T>(out T target)
        {
            if (typeof(T) == typeof(string))
            {
                target = (T)Name.Clone();
                return true;
            }
            if (typeof(T) == typeof(GH_String))
            {
                var ghString = new GH_String(Name);
                target = (T)ghString.Duplicate();
                return true;
            }
            target = default;
            return false;
        }

        public object ScriptVariable() => this;

        public bool Write(GH_IWriter writer) => true;

        public bool Read(GH_IReader reader) => true;

        public bool IsValid => Name != null && Name.Length != 0;

        public string IsValidWhyNot => "Module name is empty";

        public string TypeName => "WFC Module Name";

        public string TypeDescription => "Name of a WFC module.";

        public string Name { get => _name; set => _name = value; }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return obj is ModuleName name &&
                   Name == name.Name;
        }

        public override int GetHashCode()
        {
            return 539060726 + EqualityComparer<string>.Default.GetHashCode(Name);
        }
    }
}
