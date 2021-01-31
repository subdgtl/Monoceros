using System;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;

namespace Monoceros {
    public class ConnectorIndex : IGH_Goo {
        public uint Index;
        private readonly bool _isValid;

        public ConnectorIndex( ) {
            _isValid = false;
        }

        public ConnectorIndex(uint index) {
            Index = index;
            _isValid = true;
        }

        /// <summary>
        /// Duplicates the module. Required by Grasshopper.
        /// </summary>
        /// <returns>An IGH_Goo.</returns>
        public IGH_Goo Duplicate( ) {
            return (IGH_Goo)MemberwiseClone();
        }

        public bool CastFrom(object inputData) {
            if (inputData.GetType() == typeof(GH_Integer)) {
                var gHIndex = (GH_Integer)inputData;
                var index = gHIndex.Value;
                if (index < uint.MinValue) {
                    return false;
                }
                Index = (uint)index;
                return true;
            }
            if (inputData.GetType() == typeof(GH_Number)) {
                var gHIndex = (GH_Number)inputData;
                var index = Math.Floor(gHIndex.Value);
                if (index < uint.MinValue) {
                    return false;
                }
                Index = (uint)index;
                return true;
            }
            if (inputData.GetType() == typeof(GH_String)) {
                var gHIndex = (GH_String)inputData;
                var indexString = gHIndex.Value;
                var success = uint.TryParse(indexString, out var index);
                if (!success || index < 0) {
                    return false;
                }
                Index = index;
                return true;
            }
            return false;
        }

        public bool CastTo<T>(out T target) {
            if (typeof(T) == typeof(int)) {
                target = (T)Convert.ChangeType(Index, typeof(int));
                return true;
            }
            if (typeof(T) == typeof(double)) {
                target = (T)Convert.ChangeType(Index, typeof(double));
                return true;
            }
            if (typeof(T) == typeof(string)) {
                var s = Index.ToString();
                target = (T)s.Clone();
                return true;
            }
            if (typeof(T) == typeof(GH_Integer)) {
                var index = (int)Index;
                var gHIndex = new GH_Integer(index);
                target = (T)gHIndex.Duplicate();
                return true;
            }
            if (typeof(T) == typeof(GH_Number)) {
                var index = (double)Index;
                var gHIndex = new GH_Number(index);
                target = (T)gHIndex.Duplicate();
                return true;
            }
            if (typeof(T) == typeof(GH_String)) {
                var s = Index.ToString();
                var ghString = new GH_String(s);
                target = (T)ghString.Duplicate();
                return true;
            }
            target = default;
            return false;
        }

        /// <summary>
        /// Scripts the variable. Required by Grasshopper.
        /// </summary>
        /// <returns>An object.</returns>
        public object ScriptVariable( ) {
            return this;
        }

        /// <summary>
        /// Serialization. Required by Grasshopper for data internalization.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <returns>A bool when successful.</returns>
        public bool Write(GH_IWriter writer) {
            writer.SetInt32("Index", Convert.ToInt32(Index));
            return true;
        }

        /// <summary>
        /// De-serialization. Required by Grasshopper for data internalization.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>A bool when successful.</returns>
        public bool Read(GH_IReader reader) {
            if (reader.ItemExists("Index")) {
                var index = reader.GetInt32("Index");
                if (index < uint.MinValue) {
                    return false;
                }
                Index = (uint)index;
                return true;
            } else {
                return false;
            }
        }


        /// <summary>
        /// Indicates why is the module name not valid. Required by Grasshopper.
        /// </summary>
        public string IsValidWhyNot => "Connector Index was not defined";

        /// <summary>
        /// Gets the type name. Required by Grasshopper.
        /// </summary>
        public string TypeName => "Monoceros Connector Index";

        /// <summary>
        /// Gets the type description. Required by Grasshopper.
        /// </summary>
        public string TypeDescription => "Index of a Monoceros Module Connector.";

        bool IGH_Goo.IsValid => _isValid;

        /// <summary>
        /// Converts the module name into a string. Required by Grasshopper for
        /// data peeking.
        /// </summary>
        public override string ToString( ) {
            return _isValid ? Index.ToString() : IsValidWhyNot;
        }

        /// <summary>
        /// Determines whether the module name equals the other.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj) {
            return obj is ConnectorIndex other &&
                   Index == other.Index;
        }

        public IGH_GooProxy EmitProxy( ) {
            return null;
        }

        public override int GetHashCode( ) {
            return -2134847229 + Index.GetHashCode();
        }
    }
}
