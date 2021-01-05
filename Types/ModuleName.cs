using System;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;

namespace WFCPlugin {
    // TODO: Consider this to be a wrapper around an UID instead of an arbitrary string name
    /// <summary>
    /// <para>
    /// The class is a wrapper around a string name of a module, which serves as
    /// its unique identifier.
    /// </para>
    /// </summary>
    /// <remarks>
    /// In Grasshopper, any custom type is cast into a string, which is used for
    /// its description in data peeking. For the <see cref="Module"/> type,
    /// however, this interferes with its automatic casting into its name, which
    /// would be a simple way of using the actual Module type as its name, which
    /// is also its unique identifier. The trade-off was either simple use of
    /// the Module type data as its reference or having an informative
    /// description. Therefore there was a new data type introduced: 
    /// <see cref="ModuleName"/>. The module automatically casts to the
    /// ModuleName, which automatically casts to and from string. Therefore it
    /// is possible to use the module as its name while having a detailed
    /// description at the same time.
    /// </remarks>
    public class ModuleName : IGH_Goo {
        private string _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleName"/> class.
        /// </summary>
        /// <remarks>
        /// Required by Grasshopper; generates an invalid instance.
        /// </remarks>
        public ModuleName( ) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleName"/> class.
        /// </summary>
        /// <param name="name">The module name.</param>
        public ModuleName(string name) {
            if (name.Length == 0) {
                throw new Exception("The name is be empty.");
            }
            Name = name;
        }

        /// <summary>
        /// Duplicates the module. Required by Grasshopper.
        /// </summary>
        /// <returns>An IGH_Goo.</returns>
        public IGH_Goo Duplicate( ) {
            return (IGH_Goo)MemberwiseClone();
        }

        // TODO: Find out what this is
        /// <summary>
        /// Emits the proxy. Required by Grasshopper.
        /// </summary>
        /// <returns>An IGH_GooProxy.</returns>
        public IGH_GooProxy EmitProxy( ) {
            return null;
        }

        /// <summary>
        /// Casts from a string or a <see cref="GH_String"/> or a
        /// <see cref="Module"/> to <see cref="ModuleName"/>. Required by
        /// Grasshopper.
        /// </summary>
        /// <param name="inputData">The input data.</param>
        /// <returns>A bool if the cast was successful.</returns>
        public bool CastFrom(object inputData) {
            if (inputData.GetType() == typeof(string)) {
                Name = (string)inputData;
                return true;
            }
            if (inputData.GetType() == typeof(GH_String)) {
                var ghName = (GH_String)inputData;
                Name = ghName.ToString();
                return true;
            }
            if (inputData.GetType() == typeof(Module)) {
                var module = (Module)inputData;
                Name = module.Name;
                return true;
            }
            return false;
        }

        public bool CastTo<T>(out T target) {
            if (typeof(T) == typeof(string)) {
                target = (T)Name.Clone();
                return true;
            }
            if (typeof(T) == typeof(GH_String)) {
                var ghString = new GH_String(Name);
                target = (T)ghString.Duplicate();
                return true;
            }
            target = default;
            return false;
        }

        // TODO: Find out what this is and what should be done here
        /// <summary>
        /// Scripts the variable. Required by Grasshopper.
        /// </summary>
        /// <returns>An object.</returns>
        public object ScriptVariable( ) {
            return this;
        }

        // TODO: Do this for real
        /// <summary>
        /// Serialization. Required by Grasshopper for data internalization.
        /// </summary>
        /// <remarks>
        /// Not implemented yet.
        /// </remarks>
        /// <param name="writer">The writer.</param>
        /// <returns>A bool when successful.</returns>
        public bool Write(GH_IWriter writer) {
            return true;
        }

        // TODO: Do this for real
        /// <summary>
        /// De-serialization. Required by Grasshopper for data internalization.
        /// </summary>
        /// <remarks>
        /// Not implemented yet.
        /// </remarks>
        /// <param name="reader">The reader.</param>
        /// <returns>A bool when successful.</returns>
        public bool Read(GH_IReader reader) {
            return true;
        }

        /// <summary>
        /// Gets a value indicating whether the module name is valid. Required
        /// by Grasshopper.
        /// </summary>
        public bool IsValid => Name != null && Name.Length != 0;

        // TODO: Check whether this works
        /// <summary>
        /// Indicates why is the module name not valid. Required by Grasshopper.
        /// </summary>
        public string IsValidWhyNot => "Module name is empty";

        /// <summary>
        /// Gets the type name. Required by Grasshopper.
        /// </summary>
        public string TypeName => "WFC Module Name";

        /// <summary>
        /// Gets the type description. Required by Grasshopper.
        /// </summary>
        public string TypeDescription => "Name of a WFC module.";

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get => _name; set => _name = value; }

        /// <summary>
        /// Converts the module name into a string. Required by Grasshopper for
        /// data peeking.
        /// </summary>
        public override string ToString( ) {
            return Name;
        }

        /// <summary>
        /// Determines whether the module name equals the other.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj) {
            return obj is ModuleName name &&
                   Name == name.Name;
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>An int.</returns>
        public override int GetHashCode( ) {
            return 539060726 + EqualityComparer<string>.Default.GetHashCode(Name);
        }
    }
}
