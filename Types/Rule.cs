using System;
using System.Collections.Generic;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;

namespace WFCPlugin {
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
    public class Rule : IGH_Goo {
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
        public bool IsExplicit( ) {
            return Explicit != null && Typed == null;
        }

        /// <summary>
        /// Checks if the <see cref="Rule"/> wraps a <see cref="RuleTyped"/>.
        /// </summary>
        /// <returns>True if contains <see cref="RuleTyped"/> and does not
        ///     contain <see cref="RuleExplicit"/>.</returns>
        public bool IsTyped( ) {
            return Explicit == null && Typed != null;
        }

        /// <summary>
        /// Checks whether the other object is identical with the current
        /// <see cref="Rule"/>.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj) {
            if (IsExplicit()) {
                return Explicit.Equals(obj);
            }
            if (IsTyped()) {
                return Typed.Equals(obj);
            }
            return false;
        }

        /// <summary>
        /// Type name. Required by Grasshopper.
        /// </summary>
        public string TypeName => "WFC Rule";

        /// <summary>
        /// Type description. Required by Grasshopper.
        /// </summary>
        public string TypeDescription => "WFC Connection rule.";


        /// <summary>
        /// Checks if the <see cref="Rule"/> is valid, including the validity of
        /// the wrapped <see cref="RuleExplicit"/> or <see cref="RuleTyped"/>.
        /// </summary>
        bool IGH_Goo.IsValid {
            get {
                if (IsExplicit()) {
                    return Explicit.IsValid;
                }
                if (IsTyped()) {
                    return Typed.IsValid;
                }
                return false;
            }
        }

        /// <summary>
        /// Provides an explanation why is the <see cref="Rule"/> invalid.
        /// </summary>
        string IGH_Goo.IsValidWhyNot {
            get {
                if (IsExplicit()) {
                    return Explicit.IsValidWhyNot;
                }
                if (IsTyped()) {
                    return Typed.IsValidWhyNot;
                }
                return "The rule is neither explicit, nor typed.";
            }
        }

        // TODO: Consider allowing to cast from a string in the format defined by ToString 
        // or even in the original X,mod1,mod2
        /// <summary>
        /// The <see cref="Rule"/> cannot be automatically cast from another
        /// type exposed in Grasshopper. Required by Grasshopper. 
        /// </summary>
        /// <param name="rule"></param>
        /// <returns>False</returns>
        public bool CastFrom(object rule) {
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

        // TODO: Find out what this is and what should be done here
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
            if (IsExplicit()) {
                return Explicit.ToString();
            }
            if (IsTyped()) {
                return Typed.ToString();
            }
            return "The rule is invalid.";
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>An int.</returns>
        public override int GetHashCode( ) {
            if (IsExplicit()) {
                return Explicit.GetHashCode();
            }
            if (IsTyped()) {
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
            if (IsExplicit()) {
                return Explicit.IsValidWithGivenModules(modules);
            }
            if (IsTyped()) {
                return Typed.IsValidWithModules(modules);
            }
            return false;
        }
    }

    /// <summary>
    /// <para>
    /// Explicit rule for WFC <see cref="ComponentFauxSolver"/>.
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
    /// <see cref="ToWFCRuleSolver(List{Module}, out RuleForSolver)"/>, which is
    /// checked for validity and converted to lower-to-higher order (positive
    /// direction). 
    /// </para>
    /// <para>
    /// The <see cref="RuleExplicit"/> describes also
    /// <see cref="Module.InternalRules"/>, in which case the rule allows
    /// connection of two submodules of the same <see cref="Module"/>. This is,
    /// however, hidden from the Grasshopper API and is used only internally by
    /// the <see cref="ComponentFauxSolver"/>.
    /// </para>
    /// </remarks>
    public class RuleExplicit {
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
        public bool IsValid => SourceModuleName == TargetModuleName ^
                               SourceConnectorIndex == TargetConnectorIndex;

        /// <summary>
        /// Returns a message why is the rule invalid.
        /// </summary>
        public string IsValidWhyNot => "The connector connects to itself";


        /// <summary>
        /// Provides a user-friendly description of the rule. Required by
        /// Grasshopper for data peeking.
        /// </summary>
        /// <returns>A string.</returns>
        public override string ToString( ) {
            return "Explicit connection: " +
                SourceModuleName + ":" + SourceConnectorIndex +
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
        /// by the <see cref="ComponentFauxSolver"/>. The conversion checks the
        /// validity of the rule and converts it to lower-to-higher order
        /// (positive direction).
        /// </summary>
        /// <param name="modules">All (related) modules.</param>
        /// <param name="ruleForSolver">Output rule for
        ///     <see cref="ComponentFauxSolver"/>.</param>
        /// <returns>True if the conversion was successful.</returns>
        public bool ToWFCRuleSolver(List<Module> modules, out RuleForSolver ruleForSolver) {
            // Check if such modules and their connectors exist
            var sourceModule = modules.FirstOrDefault(module => module.Name == SourceModuleName);
            var targetModule = modules.FirstOrDefault(module => module.Name == TargetModuleName);
            if (sourceModule == null || targetModule == null) {
                ruleForSolver = default;
                return false;
            }
            var sourceConnector = sourceModule.Connectors.ElementAtOrDefault(SourceConnectorIndex);
            var targetConnector = sourceModule.Connectors.ElementAtOrDefault(TargetConnectorIndex);

            if (sourceConnector.Equals(default(ModuleConnector)) ||
                targetConnector.Equals(default(ModuleConnector))) {
                ruleForSolver = default;
                return false;
            }

            // Ensure positive direction (lower-to-higher order)
            ruleForSolver = sourceConnector.Direction.Orientation == Orientation.Positive ?
                            new RuleForSolver(sourceConnector.Direction.Axis.ToString("g"),
                                              sourceConnector.SubmoduleName,
                                              targetConnector.SubmoduleName) :
                            new RuleForSolver(targetConnector.Direction.Axis.ToString("g"),
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
            var sourceConnector = sourceModule.Connectors.ElementAtOrDefault(SourceConnectorIndex);
            var targetConnector = targetModule.Connectors.ElementAtOrDefault(TargetConnectorIndex);

            // Invalid if modules do not exist or if the direction of the connectors is not opposite
            if (sourceModule == null ||
                targetModule == null ||
                sourceConnector.Equals(default(ModuleConnector)) ||
                targetConnector.Equals(default(ModuleConnector)) ||
                !sourceConnector.Direction.IsOpposite(targetConnector.Direction)) {
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// <para>
    /// Typed rule for WFC Solver.
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
    public class RuleTyped {
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
            return "Typed connector: " +
                ModuleName + ":" + ConnectorIndex +
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
        public List<RuleExplicit> ToRuleExplicit(IEnumerable<RuleTyped> otherRules,
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
    }

    /// <summary>
    /// An <see cref="RuleExplicit"/> formated to be ready to be processed by
    /// the Solver.
    /// <list type="bullet">
    ///     <item>
    ///         <term><see cref="AxialDirection"/></term>
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
    public struct RuleForSolver {
        /// <summary>
        /// String: <c>x</c> or <c>y</c> or <c>z</c>.
        /// </summary>
        public readonly string AxialDirection;
        /// <summary>
        /// Unique string identifier of a source (lower X or Y or Z coordinate)
        /// submodule (size of a single <see cref="Slot"/>).
        /// </summary>
        public readonly string LowerSubmoduleName;
        /// <summary>
        /// Unique string identifier of a target (higher X or Y or Z coordinate)
        /// submodule (size of a single <see cref="Slot"/>).
        /// </summary>
        public readonly string HigherSubmoduleName;

        /// <summary>
        /// Initializes a new instance of the <see cref="RuleForSolver"/> class.
        /// </summary>
        /// <param name="axialDirection">The axial direction: "<c>x</c>" or
        ///     "<c>y</c>" or "<c>z</c>".</param>
        /// <param name="lowerSubmoduleName">The lower submodule name.</param>
        /// <param name="higherSubmoduleName">The higher submodule name.</param>
        public RuleForSolver(string axialDirection,
                             string lowerSubmoduleName,
                             string higherSubmoduleName) {
            AxialDirection = axialDirection.ToLower();
            if (AxialDirection != "x" && AxialDirection != "y" && AxialDirection != "z") {
                throw new Exception("The axial direction is invalid: " + axialDirection);
            }
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
                   AxialDirection == rule.AxialDirection &&
                   LowerSubmoduleName == rule.LowerSubmoduleName &&
                   HigherSubmoduleName == rule.HigherSubmoduleName;
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>An int.</returns>
        public override int GetHashCode( ) {
            var hashCode = -733970503;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AxialDirection);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(LowerSubmoduleName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(HigherSubmoduleName);
            return hashCode;
        }
    }

}
