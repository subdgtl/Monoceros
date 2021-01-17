using System;
using System.Collections.Generic;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;

namespace Monoceros {
    /// <summary>
    /// <para>
    /// The wrapper class for <see cref="Explicit"/> and <see cref="Typed"/>. 
    /// <see cref="Rule"/> complies with the interface for a custom Grasshopper
    /// data type.
    /// </para>
    /// <para>
    /// May only contain one of the <see cref="Explicit"/> or
    /// <see cref="Typed"/> properties. The other one is <c>null</c>.
    /// </para>
    /// </summary>
    public class Rule : IGH_Goo, IComparable<Rule> {
        private RuleExplicit _ruleExplicit;
        private RuleTyped _ruleTyped;

        /// <summary>
        /// Initializes a new instance of the <see cref="Rule"/> class. The
        /// object will be invalid. Required by Grasshopper.
        /// </summary>
        public Rule( ) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rule"/> class. It will
        /// contain only <see cref="Explicit"/>.
        /// </summary>
        /// <param name="sourceModuleName">The source module name.</param>
        /// <param name="sourceConnectorIndex">The source connector index.
        ///     </param>
        /// <param name="targetModuleName">The target module name.</param>
        /// <param name="targetConnectorIndex">The target connector index.
        ///     </param>
        public Rule(
            string sourceModuleName,
            int sourceConnectorIndex,
            string targetModuleName,
            int targetConnectorIndex
        ) {
            Explicit = new RuleExplicit(sourceModuleName,
                                        sourceConnectorIndex,
                                        targetModuleName,
                                        targetConnectorIndex);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rule"/> class. It will
        /// contain only <see cref="Explicit"/>.
        /// </summary>
        /// <param name="ruleExplicit">The <see cref="RuleExplicit"/> to be
        ///     wrapped into the <see cref="Rule"/>.</param>
        public Rule(
            RuleExplicit ruleExplicit
        ) {
            Explicit = ruleExplicit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rule"/> class. It will
        /// contain only <see cref="Typed"/>.
        /// </summary>
        /// <param name="moduleName">The module name.</param>
        /// <param name="connectorIndex">The connector index.</param>
        /// <param name="connectorType">The connector type.</param>
        public Rule(
            string moduleName,
            int connectorIndex,
            string connectorType
        ) {
            Typed = new RuleTyped(moduleName, connectorIndex, connectorType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rule"/> class.It will
        /// contain only <see cref="Typed"/>.
        /// </summary>
        /// <param name="ruleTyped">The <see cref="RuleTyped"/> to be wrapped
        ///     into the <see cref="Rule"/>.</param>
        public Rule(
            RuleTyped ruleTyped
        ) {
            Typed = ruleTyped;
        }

        /// <summary>
        /// Gets or sets the <see cref="Explicit"/> property.  The
        /// <see cref="Typed"/> property will be set to <c>null</c>.
        /// </summary>
        public RuleExplicit Explicit {
            get => _ruleExplicit;
            set {
                _ruleExplicit = value;
                _ruleTyped = null;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Typed"/> property.  The
        /// <see cref="Explicit"/> property will be set to <c>null</c>.
        /// </summary>
        public RuleTyped Typed {
            get => _ruleTyped;
            set {
                _ruleTyped = value;
                _ruleExplicit = null;
            }
        }

        /// <summary>
        /// Checks if the <see cref="Rule"/> wraps a <see cref="RuleExplicit"/>.
        /// </summary>
        /// <returns>True if contains <see cref="RuleExplicit"/> and does not
        ///     contain <see cref="RuleTyped"/>.</returns>
        public bool IsExplicit => Explicit != null && Typed == null;

        /// <summary>
        /// Checks if the <see cref="Rule"/> wraps a <see cref="RuleTyped"/>.
        /// </summary>
        /// <returns>True if contains <see cref="RuleTyped"/> and does not
        ///     contain <see cref="RuleExplicit"/>.</returns>
        public bool IsTyped => Explicit == null && Typed != null;

        /// <summary>
        /// Checks whether the other object is identical with the current
        /// <see cref="Rule"/>.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj) {
            if (IsExplicit) {
                return Explicit.Equals(obj);
            }
            if (IsTyped) {
                return Typed.Equals(obj);
            }
            return false;
        }

        /// <summary>
        /// Type name. Required by Grasshopper.
        /// </summary>
        public string TypeName => "Monoceros Rule";

        /// <summary>
        /// Type description. Required by Grasshopper.
        /// </summary>
        public string TypeDescription => "Monoceros Connection rule.";


        /// <summary>
        /// Checks if the <see cref="Rule"/> is valid, including the validity of
        /// the wrapped <see cref="RuleExplicit"/> or <see cref="RuleTyped"/>.
        /// </summary>
        public bool IsValid {
            get {
                if (IsExplicit) {
                    return Explicit.IsValid;
                }
                if (IsTyped) {
                    return Typed.IsValid;
                }
                return false;
            }
        }

        /// <summary>
        /// Provides an explanation why is the <see cref="Rule"/> invalid.
        /// </summary>
        public string IsValidWhyNot {
            get {
                if (IsExplicit) {
                    return Explicit.IsValidWhyNot;
                }
                if (IsTyped) {
                    return Typed.IsValidWhyNot;
                }
                return "The rule is neither explicit, nor typed.";
            }
        }

        /// <summary>
        /// The <see cref="Rule"/> cannot be automatically cast from another
        /// type exposed in Grasshopper. Required by Grasshopper. 
        /// </summary>
        /// <param name="rule"></param>
        /// <returns>False</returns>
        public bool CastFrom(object inputData) {
            if (inputData.GetType() == typeof(GH_String)) {
                var ghString = (GH_String)inputData;
                var text = ghString.ToString();
                if (text.Contains(":") && text.Contains("=")) {
                    var subTexts1 = text.Split(':');
                    if (subTexts1.Length != 2) {
                        return false;
                    }
                    var moduleName = subTexts1[0].Trim();
                    var subTexts2 = subTexts1[1].Split('=');
                    if (subTexts2.Length != 2) {
                        return false;
                    }
                    var connectorIndexStr = subTexts2[0].Trim();
                    var connectorType = subTexts2[1].Trim();
                    try {
                        var connectorIndex = int.Parse(connectorIndexStr);
                        Typed = new RuleTyped(moduleName, connectorIndex, connectorType);
                        return true;
                    } catch {
                        return false;
                    }
                }
                if (text.Contains(":") && text.Contains("->")) {
                    var subTexts = text.Split(new string[] { "->" }, StringSplitOptions.None);
                    if (subTexts.Length != 2) {
                        return false;
                    }
                    var sourceSubTexts = subTexts[0].Split(':');
                    if (sourceSubTexts.Length != 2) {
                        return false;
                    }
                    var sourceModuleName = sourceSubTexts[0].Trim();
                    var sourceConnectorIndexStr = sourceSubTexts[1].Trim();

                    var targetSubTexts = subTexts[1].Split(':');
                    if (targetSubTexts.Length != 2) {
                        return false;
                    }
                    var targetModuleName = targetSubTexts[0].Trim();
                    var targetConnectorIndexStr = targetSubTexts[1].Trim();

                    try {
                        var sourceConnectorIndex = int.Parse(sourceConnectorIndexStr);
                        var targetConnectorIndex = int.Parse(targetConnectorIndexStr);
                        Explicit = new RuleExplicit(sourceModuleName,
                                                    sourceConnectorIndex,
                                                    targetModuleName,
                                                    targetConnectorIndex);
                        return true;
                    } catch {
                        return false;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// The <see cref="Rule"/> cannot be automatically cast to another type
        /// exposed in Grasshopper. Required by Grasshopper. 
        /// </summary>
        /// <param name="rule"></param>
        /// <returns>False</returns>
        public bool CastTo<T>(out T target) {
            target = default;
            return false;
        }


        /// <summary>
        /// Duplicates the <see cref="Rule"/>. Required by Grasshopper.
        /// </summary>
        /// <returns>An IGH_Goo.</returns>
        public IGH_Goo Duplicate( ) {
            return (IGH_Goo)MemberwiseClone();
        }

        /// <summary>
        /// Required by Grasshopper.
        /// </summary>
        /// <returns>An IGH_Goo.</returns>
        public IGH_GooProxy EmitProxy( ) {
            return null;
        }

        /// <summary>
        /// De-serialization. Required by Grasshopper for data internalization.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>A bool when successful.</returns>
        public bool Read(GH_IReader reader) {
            if (reader.ItemExists("IsExplicit") && reader.ItemExists("IsTyped")) {
                var isExplicit = reader.GetBoolean("IsExplicit");
                var isTyped = reader.GetBoolean("IsTyped");
                if (isExplicit && !isTyped) {
                    var sourceModuleName = reader.GetString("SourceModuleName");
                    var sourceConnectorIndex = reader.GetInt32("SourceConnectorIndex");
                    var targetModuleName = reader.GetString("TargetModuleName");
                    var targetConnectorIndex = reader.GetInt32("TargetConnectorIndex");
                    var ruleExplicit = new RuleExplicit(sourceModuleName,
                                                        sourceConnectorIndex,
                                                        targetModuleName,
                                                        targetConnectorIndex);
                    Explicit = ruleExplicit;
                    return true;
                }
                if (!isExplicit && isTyped) {
                    var moduleName = reader.GetString("ModuleName");
                    var connectorIndex = reader.GetInt32("ConnectorIndex");
                    var connectorType = reader.GetString("ConnectorType");
                    var ruleTyped = new RuleTyped(moduleName,
                                                  connectorIndex,
                                                  connectorType);
                    Typed = ruleTyped;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Serialization. Required by Grasshopper for data internalization.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <returns>A bool when successful.</returns>
        public bool Write(GH_IWriter writer) {
            writer.SetBoolean("IsExplicit", IsExplicit);
            writer.SetBoolean("IsTyped", IsTyped);
            if (IsExplicit) {
                writer.SetString("SourceModuleName", Explicit.SourceModuleName);
                writer.SetInt32("SourceConnectorIndex", Explicit.SourceConnectorIndex);
                writer.SetString("TargetModuleName", Explicit.TargetModuleName);
                writer.SetInt32("TargetConnectorIndex", Explicit.TargetConnectorIndex);
                return true;
            }
            if (IsTyped) {
                writer.SetString("ModuleName", Typed.ModuleName);
                writer.SetInt32("ConnectorIndex", Typed.ConnectorIndex);
                writer.SetString("ConnectorType", Typed.ConnectorType);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the script variable. Required by Grasshopper.
        /// </summary>
        /// <returns>An object.</returns>
        public object ScriptVariable( ) {
            return this;
        }

        /// <summary>
        /// Returns a user-friendly description of the <see cref="Rule"/>.
        /// </summary>
        /// <returns>A string.</returns>
        public override string ToString( ) {
            if (!IsValid) {
                return IsValidWhyNot;
            }
            if (IsExplicit) {
                return Explicit.ToString();
            }
            if (IsTyped) {
                return Typed.ToString();
            }
            return "The rule is invalid.";
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>An int.</returns>
        public override int GetHashCode( ) {
            if (IsExplicit) {
                return Explicit.GetHashCode();
            }
            if (IsTyped) {
                return Typed.GetHashCode();
            }
            // The rule is invalid, the hash code is not unique
            var hashCode = -1934280001;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeDescription);
            return hashCode;
        }

        /// <summary>
        /// Checks whether the wrapped <see cref="RuleExplicit"/> or
        /// <see cref="RuleTyped"/> is valid with the given
        /// <see cref="Module"/>s.
        /// </summary>
        /// <param name="modules">The modules.</param>
        /// <returns>True if valid.</returns>
        public bool IsValidWithModules(List<Module> modules) {
            // TODO: Check for collisions
            if (IsExplicit) {
                return Explicit.IsValidWithGivenModules(modules);
            }
            if (IsTyped) {
                return Typed.IsValidWithModules(modules);
            }
            return false;
        }

        public int CompareTo(Rule other) {
            if (IsExplicit && other.IsExplicit) {
                return Explicit.CompareTo(other.Explicit);
            }
            if (IsTyped && other.IsTyped) {
                return Typed.CompareTo(other.Typed);
            }
            if (IsTyped && other.IsExplicit) {
                var result = Typed.ModuleName.CompareTo(other.Explicit.SourceModuleName);
                if (result == 0) {
                    result = Typed.ConnectorIndex.CompareTo(other.Explicit.SourceConnectorIndex);
                    if (result == 0) {
                        result = -1;
                    }
                }
                return result;
            }
            if (IsExplicit && other.IsTyped) {
                var result = Explicit.SourceModuleName.CompareTo(other.Typed.ModuleName);
                if (result == 0) {
                    result = Explicit.SourceConnectorIndex.CompareTo(other.Typed.ConnectorIndex);
                    if (result == 0) {
                        result = 1;
                    }
                }
                return result;
            }
            return 0;
        }
    }

    /// <summary>
    /// <para>
    /// Explicit rule for Monoceros <see cref="ComponentSolver"/>.
    /// </para>
    /// <para>
    /// <see cref="RuleExplicit"/> describes an allowed neighborhood of two
    /// instances of <see cref="Module"/> (same or different), touching with one
    /// <see cref="ModuleConnector"/> each.  The first module is identified by
    /// <see cref="SourceModuleName"/> and its connector is identified by
    /// <see cref="SourceConnectorIndex"/>. The second module is identified by
    /// <see cref="TargetModuleName"/> and its connector is identified by
    /// <see cref="TargetConnectorIndex"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The module names are converted to lowercase.
    /// </para>
    /// <para>
    /// Neither the constructor nor <see cref="IsValid"/> check if the two
    /// connectors are opposite.  It is only possible when all modules are
    /// provided and can be done with
    /// <see cref="IsValidWithGivenModules(List{Module})"/>
    /// </para>
    /// <para>
    /// The <see cref="Direction"/> of the connection does not have to be
    /// lower-to-higher order (positive direction).
    /// </para>
    /// <para>
    /// Providing a list of <see cref="Module"/>s, the
    /// <see cref="RuleExplicit"/> can be converted to
    /// <see cref="RuleForSolver"/> with
    /// <see cref="ToRuleForSolver(List{Module}, out RuleForSolver)"/>, which is
    /// checked for validity and converted to lower-to-higher order (positive
    /// direction). 
    /// </para>
    /// <para>
    /// The <see cref="RuleExplicit"/> describes also
    /// <see cref="Module.InternalRules"/>, in which case the rule allows
    /// connection of two submodules of the same <see cref="Module"/>. This is,
    /// however, hidden from the Grasshopper API and is used only internally by
    /// the <see cref="ComponentSolver"/>.
    /// </para>
    /// </remarks>
    public class RuleExplicit : IComparable<RuleExplicit> {
        /// <summary>
        /// Name (unique identifier) of the first module, which is allowed to
        /// touch the second module.
        /// </summary>
        public readonly string SourceModuleName;
        /// <summary>
        /// The index (unique identifier) of the first module's connector, which
        /// is allowed to touch the second module's connector.
        /// </summary>
        public readonly int SourceConnectorIndex;

        /// <summary>
        /// Name (unique identifier) of the second module, which is allowed to
        /// touch the first module.
        /// </summary>
        public readonly string TargetModuleName;
        /// <summary>
        /// The index (unique identifier) of the second module's connector,
        /// which is allowed to touch the first module's connector.
        /// </summary>
        public readonly int TargetConnectorIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="RuleExplicit"/> class.
        /// </summary>
        /// <param name="sourceModuleName">The source (first)
        ///     <see cref="Module"/> name - will be converted to lowercase.
        ///     </param>
        /// <param name="sourceConnectorIndex">The source (first)
        ///     <see cref="Module"/>'s <see cref="ModuleConnector"/> index.
        ///     </param>
        /// <param name="targetModuleName">The target (second)
        ///     <see cref="Module"/> name - will be converted to lowercase.
        ///     </param>
        /// <param name="targetConnectorIndex">The target (second)
        ///     <see cref="Module"/>'s <see cref="ModuleConnector"/> index.
        ///     </param>
        public RuleExplicit(string sourceModuleName,
                            int sourceConnectorIndex,
                            string targetModuleName,
                            int targetConnectorIndex) {
            if (sourceModuleName.Length == 0) {
                throw new Exception("Source module name is empty");
            }
            if (targetModuleName.Length == 0) {
                throw new Exception("Target module name is empty");
            }
            if (sourceConnectorIndex < 0) {
                throw new Exception("Source connector index is negative");
            }
            if (targetConnectorIndex < 0) {
                throw new Exception("Target connector index is negative");
            }
            SourceModuleName = sourceModuleName.ToLower();
            SourceConnectorIndex = sourceConnectorIndex;
            TargetModuleName = targetModuleName.ToLower();
            TargetConnectorIndex = targetConnectorIndex;
        }

        /// <summary>
        /// Checks if the provided object is equal to the current
        /// <see cref="RuleExplicit"/>.  The check is bi-directional, so an
        /// opposite rule is considered to be equal.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj) {
            return obj is RuleExplicit other &&
                   (
                       (
                            SourceModuleName == other.SourceModuleName &&
                            SourceConnectorIndex == other.SourceConnectorIndex &&
                            TargetModuleName == other.TargetModuleName &&
                            TargetConnectorIndex == other.TargetConnectorIndex
                        ) ||
                        (
                            SourceModuleName == other.TargetModuleName &&
                            SourceConnectorIndex == other.TargetConnectorIndex &&
                            TargetModuleName == other.SourceModuleName &&
                            TargetConnectorIndex == other.SourceConnectorIndex
                        )
                   );
        }

        /// <summary>
        /// Checks if the rule does not describe a connector connecting to
        /// itself. Does not check if the two connectors are opposite.  It is
        /// only possible when all modules are provided and can be done with
        /// <see cref="IsValidWithGivenModules(List{Module})"/>
        /// </summary>
        public bool IsValid => !(SourceModuleName == TargetModuleName &&
                                SourceConnectorIndex == TargetConnectorIndex);

        /// <summary>
        /// Returns a message why is the rule invalid.
        /// </summary>
        public string IsValidWhyNot => ToString() + " The connector connects to itself!";


        /// <summary>
        /// Provides a user-friendly description of the rule. Required by
        /// Grasshopper for data peeking.
        /// </summary>
        /// <returns>A string.</returns>
        public override string ToString( ) {
            return SourceModuleName + ":" + SourceConnectorIndex +
                " -> " +
                TargetModuleName + ":" + TargetConnectorIndex;
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>An int.</returns>
        public override int GetHashCode( ) {
            var hashCode = -1103775584;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SourceModuleName);
            hashCode = hashCode * -1521134295 + SourceConnectorIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TargetModuleName);
            hashCode = hashCode * -1521134295 + TargetConnectorIndex.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Converts the <see cref="RuleExplicit"/> to
        /// <see cref="RuleForSolver"/>, which is a format ready to be processed
        /// by the <see cref="ComponentSolver"/>. The conversion checks the
        /// validity of the rule and converts it to lower-to-higher order
        /// (positive direction).
        /// </summary>
        /// <param name="modules">All (related) modules.</param>
        /// <param name="ruleForSolver">Output rule for
        ///     <see cref="ComponentSolver"/>.</param>
        /// <returns>True if the conversion was successful.</returns>
        public bool ToRuleForSolver(List<Module> modules, out RuleForSolver ruleForSolver) {
            if (!IsValidWithGivenModules(modules)) {
                ruleForSolver = default;
                return false;
            }

            var sourceModule = modules.First(module => module.Name == SourceModuleName);
            var targetModule = modules.First(module => module.Name == TargetModuleName);
            var sourceConnector = sourceModule.Connectors[SourceConnectorIndex];
            var targetConnector = targetModule.Connectors[TargetConnectorIndex];

            // Ensure positive direction (lower-to-higher order)
            ruleForSolver = sourceConnector.Direction.Orientation == Orientation.Positive ?
                            new RuleForSolver(sourceConnector.Direction.Axis,
                                              sourceConnector.SubmoduleName,
                                              targetConnector.SubmoduleName) :
                            new RuleForSolver(targetConnector.Direction.Axis,
                                              targetConnector.SubmoduleName,
                                              sourceConnector.SubmoduleName);
            return true;
        }

        /// <summary>
        /// Checks whether the <see cref="RuleExplicit"/> is valid with the
        /// given <see cref="Module"/>s. The rule is invalid if such modules do
        /// not exist or if the direction of the connectors is not opposite.
        /// </summary>
        /// <param name="modules">The modules.</param>
        /// <returns>True if valid.</returns>
        public bool IsValidWithGivenModules(IEnumerable<Module> modules) {
            var sourceModule = modules.FirstOrDefault(module => module.Name == SourceModuleName);
            var targetModule = modules.FirstOrDefault(module => module.Name == TargetModuleName);
            if (sourceModule != null && targetModule != null) {
                var sourceConnector = sourceModule.Connectors.ElementAtOrDefault(SourceConnectorIndex);
                var targetConnector = targetModule.Connectors.ElementAtOrDefault(TargetConnectorIndex);
                // Invalid if modules do not exist or if the direction of the connectors is not opposite
                if (!sourceConnector.Equals(default(ModuleConnector)) &&
                    !targetConnector.Equals(default(ModuleConnector)) &&
                    sourceConnector.Direction.IsOpposite(targetConnector.Direction)) {
                    return true;
                }
            }
            return false;
        }

        public int CompareTo(RuleExplicit other) {
            var result = SourceModuleName.CompareTo(other.SourceModuleName);
            if (result == 0) {
                result = TargetModuleName.CompareTo(other.TargetModuleName);
                if (result == 0) {
                    result = SourceConnectorIndex.CompareTo(other.SourceConnectorIndex);
                    if (result == 0) {
                        result = TargetConnectorIndex.CompareTo(other.TargetConnectorIndex);
                    }
                }
            }
            return result;
        }
    }

    /// <summary>
    /// <para>
    /// Typed rule for Monoceros Solver.
    /// </para>
    /// <para>
    /// <see cref="RuleTyped"/> describes an allowed neighborhood of the current
    /// <see cref="Module"/>'s <see cref="ModuleConnector"/> with another
    /// connector of the same or different <see cref="Module"/>, which has the
    /// same <see cref="ConnectorType"/>.  The module is identified by
    /// <see cref="ModuleName"/> and its connector is identified by
    /// <see cref="ConnectorIndex"/>. The type of the connector is identified by
    /// <see cref="ConnectorType"/>, which is an arbitrary string.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="ModuleName"/> and <see cref="ConnectorType"/> are
    /// converted to lowercase.
    /// </para>
    /// <para>
    /// The same <see cref="ConnectorType"/> can be assigned to connectors in
    /// different directions. 
    /// </para>
    /// <para>
    /// Neither the constructor nor <see cref="IsValid"/> check if the connector
    /// refers to existing <see cref="Module"/> and
    /// <see cref="ModuleConnector"/>.  It is only possible when all modules are
    /// provided and can be done with
    /// <see cref="IsValidWithGivenModules(List{Module})"/>
    /// </para>
    /// <para>
    /// The <see cref="RuleTyped"/> needs to be unwrapped to
    /// <see cref="RuleExplicit"/> before it can be converted to
    /// <see cref="RuleForSolver"/>.
    /// </para>
    /// </remarks>
    public class RuleTyped : IComparable<RuleTyped> {
        /// <summary>
        /// Name (unique identifier) of a <see cref="Module"/>, which is allowed
        /// to touch another <see cref="Module"/> with a
        /// <see cref="ModuleConnector"/> of the same
        /// <see cref="ConnectorType"/>.
        /// </summary>
        public readonly string ModuleName;
        /// <summary>
        /// Index of a <see cref="ModuleConnector"/>, which is allowed to touch
        /// another <see cref="ModuleConnector"/> of the same
        /// <see cref="ConnectorType"/>.
        /// </summary>
        public readonly int ConnectorIndex;
        /// <summary>
        /// Any two opposite <see cref="ModuleConnector"/>s of the same
        /// <see cref="ConnectorType"/> can touch. <see cref="ConnectorType"/>
        /// is an arbitrary string.
        /// </summary>
        public readonly string ConnectorType;

        /// <summary>
        /// Initializes a new instance of the <see cref="RuleTyped"/> class.
        /// </summary>
        /// <param name="moduleName">The <see cref="Module"/> name - will be
        ///     converted to lowercase.</param>
        /// <param name="connectorIndex">The <see cref="Module"/>'s
        ///     <see cref="ModuleConnector"/> index.</param>
        /// <param name="connectorType">The connector type - will be converted
        ///     to lowercase.</param>
        public RuleTyped(string moduleName, int connectorIndex, string connectorType) {
            if (moduleName.Length == 0) {
                throw new Exception("Module name is empty");
            }

            if (connectorIndex < 0) {
                throw new Exception("Connector index is negative");
            }

            if (connectorType.Length == 0) {
                throw new Exception("Connector type name is empty");
            }

            ModuleName = moduleName.ToLower();
            ConnectorIndex = connectorIndex;
            ConnectorType = connectorType.ToLower();
        }

        /// <summary>
        /// Check if the provided object is an identical
        /// <see cref="RuleTyped"/>.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj) {
            return obj is RuleTyped other &&
                   (
                    ModuleName == other.ModuleName &&
                    ConnectorIndex == other.ConnectorIndex &&
                    ConnectorType == other.ConnectorType
                   );
        }

        /// <summary>
        /// Checks if the rule is initiated properly. Always valid due to the
        /// checks in the constructor.
        /// </summary>
        public bool IsValid => true;

        /// <summary>
        /// Should never be called.
        /// </summary>
        public string IsValidWhyNot => "Unknown reason";

        /// <summary>
        /// Provides a user-friendly description of the rule. Required by
        /// Grasshopper for data peeking.
        /// </summary>
        /// <returns>A string.</returns>
        public override string ToString( ) {
            return ModuleName + ":" + ConnectorIndex +
                " = " +
                ConnectorType;
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>An int.</returns>
        public override int GetHashCode( ) {
            var hashCode = 145665365;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ModuleName);
            hashCode = hashCode * -1521134295 + ConnectorIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ConnectorType);
            return hashCode;
        }


        /// <summary>
        /// <para>
        /// Convert to <see cref="RuleExplicit"/>.
        /// </para>
        /// <para>
        /// Checks all other <see cref="RuleTyped"/> for the same
        /// <see cref="ConnectorType"/> and if this and the other rules are
        /// valid (refer to existing <see cref="Module"/> and its
        /// <see cref="ModuleConnector"/>) and the two
        /// <see cref="ModuleConnector"/>s have opposite
        /// <see cref="Direction"/>s, then construct a
        /// <see cref="RuleExplicit"/> describing a connection of the two
        /// <see cref="Module"/>s through the respective
        /// <see cref="ModuleConnector"/>s.
        /// </para>
        /// </summary>
        /// <param name="otherRules">The other rules.</param>
        /// <param name="modules">The modules.</param>
        /// <returns>A list of <see cref="RuleExplicit"/>. The list may be
        ///     empty.</returns>
        public List<RuleExplicit> ToRulesExplicit(IEnumerable<RuleTyped> otherRules,
                                                 List<Module> modules) {
            var rulesExplicit = new List<RuleExplicit>();

            // If the source module and connector exist
            var sourceModule = modules.FirstOrDefault(module => module.Name == ModuleName);
            if (sourceModule == null) {
                return rulesExplicit;
            }

            var sourceConnector = sourceModule.Connectors.ElementAtOrDefault(ConnectorIndex);
            if (sourceConnector.Equals(default(ModuleConnector))) {
                return rulesExplicit;
            }

            // Find all other rules assigning the same connector type
            foreach (var other in otherRules) {
                if (other.ConnectorType != ConnectorType) {
                    continue;
                }

                // Checked if the other rule refers to an existing module and connector
                var targetModule = modules
                    .FirstOrDefault(module => module.Name == other.ModuleName);
                if (targetModule == null) {
                    continue;
                }

                var targetConnector = targetModule.Connectors.ElementAtOrDefault(other.ConnectorIndex);
                if (targetConnector.Equals(default(ModuleConnector))) {
                    continue;
                }

                // Only convert to an explicit rule if the other connector is opposite
                if (targetConnector.Direction.IsOpposite(sourceConnector.Direction)) {
                    rulesExplicit.Add(
                        new RuleExplicit(sourceModule.Name,
                                         ConnectorIndex,
                                         targetModule.Name,
                                         other.ConnectorIndex)
                        );
                }
            }
            return rulesExplicit;
        }

        /// <summary>
        /// Checks whether the <see cref="RuleTyped"/> refers to an existing
        /// <see cref="Module"/> and its <see cref="ModuleConnector"/>.
        /// </summary>
        /// <param name="modules">All <see cref="Module"/>s.</param>
        /// <returns>True if valid.</returns>
        public bool IsValidWithModules(List<Module> modules) {
            var sourceModule = modules.FirstOrDefault(module => module.Name == ModuleName);
            if (sourceModule == null || ConnectorIndex >= sourceModule.Connectors.Count) {
                return false;
            }
            return true;
        }

        public int CompareTo(RuleTyped other) {
            var result = ModuleName.CompareTo(other.ModuleName);
            if (result == 0) {
                result = ConnectorIndex.CompareTo(other.ConnectorIndex);
                if (result == 0) {
                    result = ConnectorType.CompareTo(other.ConnectorType);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// An <see cref="RuleExplicit"/> formated to be ready to be processed by
    /// the Solver.
    /// <list type="bullet">
    ///     <item>
    ///         <term><see cref="Axis"/></term>
    ///         <description>String: <c>x</c> or <c>y</c> or <c>z</c>.
    ///             </description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="LowerSubmoduleName"/></term>
    ///         <description>Unique string identifier of a source (lower X or Y
    ///             or Z coordinate) submodule (size of a single
    ///             <see cref="Slot"/>).</description>
    ///     </item>
    ///     ///
    ///     <item>
    ///         <term><see cref="HigherSubmoduleName"/></term>
    ///         <description>Unique string identifier of a target (higher X or Y
    ///             or Z coordinate) submodule (size of a single
    ///             <see cref="Slot"/>).</description>
    ///     </item>
    /// </list>
    /// </summary>
    [Serializable]
    public struct RuleForSolver {
        /// <summary>
        /// String: <c>x</c> or <c>y</c> or <c>z</c>.
        /// </summary>
        public Axis Axis;
        /// <summary>
        /// Unique string identifier of a source (lower X or Y or Z coordinate)
        /// submodule (size of a single <see cref="Slot"/>).
        /// </summary>
        public string LowerSubmoduleName;
        /// <summary>
        /// Unique string identifier of a target (higher X or Y or Z coordinate)
        /// submodule (size of a single <see cref="Slot"/>).
        /// </summary>
        public string HigherSubmoduleName;

        /// <summary>
        /// Initializes a new instance of the <see cref="RuleForSolver"/> class.
        /// </summary>
        /// <param name="axialDirection">The axial direction: "<c>x</c>" or
        ///     "<c>y</c>" or "<c>z</c>".</param>
        /// <param name="lowerSubmoduleName">The lower submodule name.</param>
        /// <param name="higherSubmoduleName">The higher submodule name.</param>
        public RuleForSolver(Axis axialDirection,
                             string lowerSubmoduleName,
                             string higherSubmoduleName) {
            Axis = axialDirection;
            LowerSubmoduleName = lowerSubmoduleName;
            HigherSubmoduleName = higherSubmoduleName;
        }

        /// <summary>
        /// Check whether the other object is an identical
        /// <see cref="RuleForSolver"/>.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj) {
            return obj is RuleForSolver rule &&
                   Axis == rule.Axis &&
                   LowerSubmoduleName == rule.LowerSubmoduleName &&
                   HigherSubmoduleName == rule.HigherSubmoduleName;
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>An int.</returns>
        public override int GetHashCode( ) {
            var hashCode = 747929822;
            hashCode = hashCode * -1521134295 + Axis.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(LowerSubmoduleName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(HigherSubmoduleName);
            return hashCode;
        }
    }

}
