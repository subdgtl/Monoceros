// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;

namespace WFCToolset
{
    public class RuleExplicit : IGH_Goo
    {
        private string _sourceModuleName;
        private int _sourceConnectorIndex;

        private string _targetModuleName;
        private int _targetConnectorIndex;

        public RuleExplicit(string sourceModuleName,
                            int sourceConnectorIndex,
                            string targetModuleName,
                            int targetConnectorIndex)
        {
            SourceModuleName = sourceModuleName.ToLower();
            SourceConnectorIndex = sourceConnectorIndex;
            TargetModuleName = targetModuleName.ToLower();
            TargetConnectorIndex = targetConnectorIndex;
        }

        public string SourceModuleName { get => _sourceModuleName; set => _sourceModuleName = value; }
        public int SourceConnectorIndex { get => _sourceConnectorIndex; set => _sourceConnectorIndex = value; }
        public string TargetModuleName { get => _targetModuleName; set => _targetModuleName = value; }
        public int TargetConnectorIndex { get => _targetConnectorIndex; set => _targetConnectorIndex = value; }

        public override bool Equals(object obj)
        {
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

        public bool IsValid => SourceModuleName.Length > 0 &&
                TargetModuleName.Length > 0 &&
                SourceModuleName == TargetModuleName ^ SourceConnectorIndex == TargetConnectorIndex;

        public string IsValidWhyNot
        {
            get
            {
                if (SourceModuleName.Length == 0)
                {
                    return "Source module name is empty";
                }
                if (TargetModuleName.Length == 0)
                {
                    return "Target module name is empty";
                }
                if (SourceModuleName == TargetModuleName && SourceConnectorIndex == TargetConnectorIndex)
                {
                    return "The connector connects to itself";
                }
                return "Unknown reason.";
            }
        }

        public string TypeName => "WFC Rule Explicit";
        public string TypeDescription => "WFC Connection rule explicit.";

        public bool CastFrom(object source)
        {
            if (source.GetType() == typeof(Rule))
            {
                var rule = (Rule)source;
                if (rule.IsExplicit())
                {
                    SourceModuleName = rule.RuleExplicit.SourceModuleName;
                    SourceConnectorIndex = rule.RuleExplicit.SourceConnectorIndex;
                    TargetModuleName = rule.RuleExplicit.TargetModuleName;
                    TargetConnectorIndex = rule.RuleExplicit.TargetConnectorIndex;
                    return true;
                }
            }
            return false;
        }

        public bool CastTo<T>(out T target)
        {
            if (IsValid && typeof(T) == typeof(Rule))
            {
                object obj = new Rule((RuleExplicit)Duplicate());
                target = (T)obj;
                return true;
            }

            target = default;
            return false;
        }

        public IGH_Goo Duplicate() => (IGH_Goo)MemberwiseClone();
        public IGH_GooProxy EmitProxy() => null;
        public bool Read(GH_IReader reader) => true;
        public object ScriptVariable() => this;
        public bool Write(GH_IWriter writer) => true;

        public override string ToString()
        {
            return "Explicit connection: " +
                SourceModuleName + ":" + SourceConnectorIndex +
                " -> " +
                TargetModuleName + ":" + TargetConnectorIndex;
        }

        public override int GetHashCode()
        {
            var hashCode = -1103775584;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SourceModuleName);
            hashCode = hashCode * -1521134295 + SourceConnectorIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TargetModuleName);
            hashCode = hashCode * -1521134295 + TargetConnectorIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            return hashCode;
        }

        public bool ToWFCRuleSolver(List<Module> modules, out RuleForSolver ruleForSolver)
        {

            var sourceModule = modules.FirstOrDefault(module => module.Name == SourceModuleName);
            var targetModule = modules.FirstOrDefault(module => module.Name == TargetModuleName);
            if (sourceModule == null || targetModule == null)
            {
                ruleForSolver = default;
                return false;
            }
            var sourceConnector = sourceModule.Connectors.FirstOrDefault(connector => connector.ConnectorIndex == SourceConnectorIndex);
            var targetConnector = sourceModule.Connectors.FirstOrDefault(connector => connector.ConnectorIndex == TargetConnectorIndex);

            if (sourceConnector.Equals(default(ModuleConnector)) || targetConnector.Equals(default(ModuleConnector)))
            {
                ruleForSolver = default;
                return false;
            }

            ruleForSolver = sourceConnector.Direction._orientation == Orientation.Positive ?
                            new RuleForSolver(sourceConnector.Direction._axis.ToString("g"),
                                              sourceConnector.SubmoduleName,
                                              targetConnector.SubmoduleName) :
                            new RuleForSolver(targetConnector.Direction._axis.ToString("g"),
                                              targetConnector.SubmoduleName,
                                              sourceConnector.SubmoduleName);
            return true;
        }

        public bool IsValidWithModules(List<Module> modules)
        {
            var sourceModule = modules.FirstOrDefault(module => module.Name == SourceModuleName);
            var targetModule = modules.FirstOrDefault(module => module.Name == TargetModuleName);
            var sourceConnector = sourceModule.Connectors.FirstOrDefault(connector => connector.ConnectorIndex == SourceConnectorIndex);
            var targetConnector = targetModule.Connectors.FirstOrDefault(connector => connector.ConnectorIndex == TargetConnectorIndex);

            // If such module does not exist or if the direction is not opposite
            if (sourceModule == null ||
                targetModule == null ||
                sourceConnector.Equals(default(ModuleConnector)) ||
                targetConnector.Equals(default(ModuleConnector)) ||
                !sourceConnector.Direction.IsOpposite(targetConnector.Direction))
            {
                return false;
            }
            return true;
        }
    }

    public class RuleTyped : IGH_Goo
    {
        private string _moduleName;
        private int _connectorIndex;

        private string _connectorType;

        // Not case sensitive
        public RuleTyped(string moduleName, int connectorIndex, string connectorType)
        {
            if (moduleName.Length > 0)
            {
                ModuleName = moduleName.ToLower();
            }
            else
            {
                throw new Exception("Module name is empty");
            }

            ConnectorIndex = connectorIndex;

            if (connectorType.Length > 0)
            {
                ConnectorType = connectorType.ToLower();
            }
            else
            {
                throw new Exception("Connector type name is empty");
            }
        }

        public string ModuleName { get => _moduleName; set => _moduleName = value; }
        public int ConnectorIndex { get => _connectorIndex; set => _connectorIndex = value; }
        public string ConnectorType { get => _connectorType; set => _connectorType = value; }

        public override bool Equals(object obj)
        {
            return obj is RuleTyped other &&
                   (
                    ModuleName == other.ModuleName &&
                    ConnectorIndex == other.ConnectorIndex &&
                    ConnectorType == other.ConnectorType
                   );
        }

        public bool IsValid => ModuleName.Length > 0 && ConnectorType.Length > 0;

        public string IsValidWhyNot
        {
            get
            {
                if (ModuleName.Length == 0)
                {
                    return "Module name is empty";
                }
                if (ConnectorType.Length == 0)
                {
                    return "Connector type name is empty";
                }
                return "Unknown reason.";
            }
        }

        public string TypeName => "WFC Rule Typed";
        public string TypeDescription => "WFC Connection rule typed.";

        public bool CastFrom(object source)
        {
            if (source.GetType() == typeof(Rule))
            {
                var rule = (Rule)source;
                if (rule.RuleTyped != null)
                {
                    ModuleName = rule.RuleTyped.ModuleName;
                    ConnectorIndex = rule.RuleTyped.ConnectorIndex;
                    ConnectorType = rule.RuleTyped.ConnectorType;
                    return true;
                }
            }
            return false;
        }

        public bool CastTo<T>(out T target)
        {
            if (IsValid && typeof(T) == typeof(Rule))
            {
                object obj = new Rule((RuleTyped)Duplicate());
                target = (T)obj;
                return true;
            }

            target = default;
            return false;
        }

        public IGH_Goo Duplicate() => (IGH_Goo)MemberwiseClone();
        public IGH_GooProxy EmitProxy() => null;
        public bool Read(GH_IReader reader) => true;
        public object ScriptVariable() => this;
        public bool Write(GH_IWriter writer) => true;

        public override string ToString()
        {
            return "Typed connector: " +
                ModuleName + ":" + ConnectorIndex +
                " = " +
                ConnectorType;
        }

        public override int GetHashCode()
        {
            var hashCode = 145665365;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ModuleName);
            hashCode = hashCode * -1521134295 + ConnectorIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ConnectorType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            return hashCode;
        }

        // TODO: If the rule cannot be converted, consider returning null or some sort of an invalid rule, 
        // rather than throwing an exception. The exception happens when the user tries to apply the rules 
        // on an unrelated or incomplete list of modules. This shouldn't happen (or should we try to cope with it?).
        public List<RuleExplicit> ToRuleExplicit(IEnumerable<RuleTyped> otherRules, List<Module> modules)
        {
            var rulesExplicit = new List<RuleExplicit>();
            var sourceModule = modules.Find(module => module.Name == ModuleName)
                ?? throw new Exception("Rule" + this + " expects a non-existing module " + ModuleName);
            if (ConnectorIndex >= sourceModule.Connectors.Count)
            {
                throw new Exception("Rule" + this + " expects a non-existing connector" + ConnectorIndex);
            }
            var sourceConnector = sourceModule.Connectors[ConnectorIndex];
            foreach (var other in otherRules)
            {
                if (other.ConnectorType != ConnectorType)
                {
                    continue;
                }
                var targetModule = modules.Find(module => module.Name == other.ModuleName)
                    ?? throw new Exception("Rule" + this + " expects a non-existing module " + other.ModuleName);
                if (other.ConnectorIndex >= targetModule.Connectors.Count)
                {
                    throw new Exception("Rule" + this + " expects a non-existing connector" + other.ConnectorIndex);
                }
                var targetConnector = targetModule.Connectors[other.ConnectorIndex];
                if (targetConnector.Direction.IsOpposite(sourceConnector.Direction))
                {
                    rulesExplicit.Add(
                        new RuleExplicit(sourceModule.Name,
                                         sourceConnector.ConnectorIndex,
                                         targetModule.Name,
                                         targetConnector.ConnectorIndex)
                        );
                }
            }
            return rulesExplicit;
        }

        public bool IsValidWithModules(List<Module> modules)
        {
            var sourceModule = modules.FirstOrDefault(module => module.Name == ModuleName);
            if (sourceModule == null ||
                !sourceModule.Connectors.Any(connector => connector.ConnectorIndex == ConnectorIndex))
            {
                return false;
            }
            return true;
        }
    }

    public class Rule : IGH_Goo
    {
        private RuleExplicit _ruleExplicit;
        private RuleTyped _ruleTyped;

        public Rule()
        {
        }

        public Rule(
            string sourceModuleName,
            int sourceConnectorIndex,
            string targetModuleName,
            int targetConnectorIndex
        )
        {
            RuleExplicit = new RuleExplicit(sourceModuleName, sourceConnectorIndex, targetModuleName, targetConnectorIndex);
            RuleTyped = null;
        }
        public Rule(
            RuleExplicit ruleExplicit
        )
        {
            RuleExplicit = ruleExplicit;
            RuleTyped = null;
        }

        public Rule(
            string moduleName,
            int connectorIndex,
            string connectorType
        )
        {
            RuleTyped = new RuleTyped(moduleName, connectorIndex, connectorType);
            RuleExplicit = null;
        }

        public Rule(
            RuleTyped ruleTyped
        )
        {
            RuleTyped = ruleTyped;
            RuleExplicit = null;
        }

        public RuleExplicit RuleExplicit { get => _ruleExplicit; set => _ruleExplicit = value; }
        public RuleTyped RuleTyped { get => _ruleTyped; set => _ruleTyped = value; }

        public bool IsExplicit() => RuleExplicit != null && RuleTyped == null;
        public bool IsTyped() => RuleExplicit == null && RuleTyped != null;

        public override bool Equals(object obj)
        {
            if (IsExplicit())
            {
                return RuleExplicit == obj;
            }
            if (IsTyped())
            {
                return RuleTyped == obj;
            }
            return false;
        }

        public string TypeName => "WFC Rule";
        public string TypeDescription => "WFC Connection rule.";
        bool IGH_Goo.IsValid => IsExplicit() || IsTyped();

        string IGH_Goo.IsValidWhyNot
        {
            get
            {
                if (IsExplicit())
                {
                    return ((IGH_Goo)RuleExplicit).IsValidWhyNot;
                }
                if (IsTyped())
                {
                    return ((IGH_Goo)RuleTyped).IsValidWhyNot;
                }
                return "The rule is neither explicit, nor typed.";
            }
        }

        public bool CastFrom(object rule)
        {
            if (rule.GetType() == typeof(RuleExplicit))
            {
                RuleExplicit = (RuleExplicit)rule;
                RuleTyped = null;
                return true;
            }
            if (rule.GetType() == typeof(RuleTyped))
            {
                RuleTyped = (RuleTyped)rule;
                RuleExplicit = null;
                return true;
            }
            return false;
        }

        public bool CastTo<T>(out T target)
        {
            if (IsExplicit() && typeof(T) == typeof(RuleExplicit))
            {
                target = (T)RuleExplicit.Duplicate();
                return true;
            }
            if (IsTyped() && typeof(T) == typeof(RuleTyped))
            {
                target = (T)RuleTyped.Duplicate();
                return true;
            }

            target = default;
            return false;
        }


        public IGH_Goo Duplicate() => (IGH_Goo)MemberwiseClone();
        public IGH_GooProxy EmitProxy() => null;
        public bool Read(GH_IReader reader) => true;
        public object ScriptVariable() => this;
        public bool Write(GH_IWriter writer) => true;

        public override string ToString()
        {
            if (IsExplicit())
            {
                return RuleExplicit.ToString();
            }
            if (IsTyped())
            {
                return RuleTyped.ToString();
            }
            return "The rule is invalid.";
        }

        public override int GetHashCode()
        {
            if (IsExplicit())
            {
                return RuleExplicit.GetHashCode();
            }
            if (IsTyped())
            {
                return RuleTyped.GetHashCode();
            }
            // The rule is invalid, the hash code is not unique
            var hashCode = -1934280001;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeDescription);
            return hashCode;
        }

        public bool IsValidWithModules(List<Module> modules)
        {
            if (IsExplicit())
            {
                return RuleExplicit.IsValidWithModules(modules);
            }
            if (IsTyped())
            {
                return RuleTyped.IsValidWithModules(modules);
            }
            return false;
        }
    }

    public struct RuleForSolver
    {
        public readonly string AxialDirection;
        public readonly string SourceSubmoduleName;
        public readonly string TargetSubmoduleName;

        public RuleForSolver(string axialDirection, string sourceSubmoduleName, string targetSubmoduleName)
        {
            AxialDirection = axialDirection;
            SourceSubmoduleName = sourceSubmoduleName;
            TargetSubmoduleName = targetSubmoduleName;
        }

        public override bool Equals(object obj)
        {
            return obj is RuleForSolver rule &&
                   AxialDirection == rule.AxialDirection &&
                   SourceSubmoduleName == rule.SourceSubmoduleName &&
                   TargetSubmoduleName == rule.TargetSubmoduleName;
        }

        public override int GetHashCode()
        {
            var hashCode = -733970503;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AxialDirection);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SourceSubmoduleName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TargetSubmoduleName);
            return hashCode;
        }
    }

}
