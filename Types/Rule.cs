// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;

namespace WFCToolset
{
    public class RuleExplicit : IGH_Goo
    {
        public string _sourceModuleName;
        public int _sourceConnectorIndex;

        public string _targetModuleName;
        public int _targetConnectorIndex;

        public RuleExplicit(string sourceModuleName,
                            int sourceConnectorIndex,
                            string targetModuleName,
                            int targetConnectorIndex)
        {
            _sourceModuleName = sourceModuleName.ToLower();
            _sourceConnectorIndex = sourceConnectorIndex;
            _targetModuleName = targetModuleName.ToLower();
            _targetConnectorIndex = targetConnectorIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is RuleExplicit other &&
                   (
                       (
                            _sourceModuleName == other._sourceModuleName &&
                            _sourceConnectorIndex == other._sourceConnectorIndex &&
                            _targetModuleName == other._targetModuleName &&
                            _targetConnectorIndex == other._targetConnectorIndex
                        ) ||
                        (
                            _sourceModuleName == other._targetModuleName &&
                            _sourceConnectorIndex == other._targetConnectorIndex &&
                            _targetModuleName == other._sourceModuleName &&
                            _targetConnectorIndex == other._sourceConnectorIndex
                        )
                   );
        }

        public bool IsValid => _sourceModuleName.Length > 0 &&
                _targetModuleName.Length > 0 &&
                _sourceModuleName == _targetModuleName ^ _sourceConnectorIndex == _targetConnectorIndex;

        public string IsValidWhyNot
        {
            get
            {
                if (_sourceModuleName.Length == 0)
                {
                    return "Source module name is empty";
                }
                if (_targetModuleName.Length == 0)
                {
                    return "Target module name is empty";
                }
                if (_sourceModuleName == _targetModuleName && _sourceConnectorIndex == _targetConnectorIndex)
                {
                    return "The connector connects to itself";
                }
                return "Unknown reason.";
            }
        }

        public string TypeName => "WFCRuleExplicit";

        public string TypeDescription => "WFC Connection rule explicit.";

        public bool CastFrom(object source)
        {
            if (source.GetType() == typeof(Rule))
            {
                var rule = (Rule)source;
                if (rule.IsExplicit())
                {
                    _sourceModuleName = rule._ruleExplicit._sourceModuleName;
                    _sourceConnectorIndex = rule._ruleExplicit._sourceConnectorIndex;
                    _targetModuleName = rule._ruleExplicit._targetModuleName;
                    _targetConnectorIndex = rule._ruleExplicit._targetConnectorIndex;
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

        public IGH_Goo Duplicate()
        {
            return (IGH_Goo)MemberwiseClone();

        }

        public IGH_GooProxy EmitProxy()
        {
            return null;
        }

        public bool Read(GH_IReader reader) => true;

        public object ScriptVariable() => this;

        public bool Write(GH_IWriter writer) => true;
        public override string ToString()
        {
            return "Explicit connection: " +
                _sourceModuleName + ":" + _sourceConnectorIndex +
                " -> " +
                _targetModuleName + ":" + _targetConnectorIndex;
        }

        public override int GetHashCode()
        {
            var hashCode = -1103775584;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_sourceModuleName);
            hashCode = hashCode * -1521134295 + _sourceConnectorIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_targetModuleName);
            hashCode = hashCode * -1521134295 + _targetConnectorIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            return hashCode;
        }

        public RuleForSolver ToWFCRuleSolver(List<Module> modules)
        {
            var sourceModule = modules.Find(module => module.Name == _sourceModuleName)
                ?? throw new Exception("Rule" + this + " expects a non-existing module " + _sourceModuleName);
            if (_sourceConnectorIndex >= sourceModule.Connectors.Count)
            {
                throw new Exception("Rule" + this + " expects a non-existing connector" + _sourceConnectorIndex);
            }
            var sourceConnector = sourceModule.Connectors[_sourceConnectorIndex];
            var targetModule = modules.Find(module => module.Name == _targetModuleName)
                ?? throw new Exception("Rule" + this + " requires a non-existing module " + _targetModuleName);
            if (_targetConnectorIndex >= targetModule.Connectors.Count)
            {
                throw new Exception("Rule" + this + " expects a non-existing connector" + _targetConnectorIndex);
            }
            var targetConnector = targetModule.Connectors[_targetConnectorIndex];

            if (!sourceConnector._direction.IsOpposite(targetConnector._direction))
            {
                throw new Exception("Connectors " +
                    _sourceModuleName + ":" +
                    _sourceConnectorIndex + " and " +
                    _targetModuleName + ":" +
                    _targetConnectorIndex + " are not opposite");
            }
            if (sourceConnector._direction._orientation == Orientation.Positive)
            {
                return new RuleForSolver(sourceConnector._direction._axis.ToString("g"),
                                         sourceConnector._submoduleName,
                                         targetConnector._submoduleName);
            }
            else
            {
                return new RuleForSolver(targetConnector._direction._axis.ToString("g"),
                                         targetConnector._submoduleName,
                                         sourceConnector._submoduleName);
            }
        }
    }

    public class RuleTyped : IGH_Goo
    {
        public string _moduleName;
        public int _connectorIndex;

        public string _connectorType;

        public RuleTyped(string connectorType)
        {
            _connectorType = connectorType;
        }

        // Not case sensitive
        public RuleTyped(string moduleName, int connectorIndex, string connectorType)
        {
            if (moduleName.Length > 0)
            {
                _moduleName = moduleName.ToLower();
            }
            else
            {
                throw new Exception("Module name is empty");
            }

            _connectorIndex = connectorIndex;

            if (connectorType.Length > 0)
            {
                _connectorType = connectorType.ToLower();
            }
            else
            {
                throw new Exception("Connector type name is empty");
            }
        }

        public override bool Equals(object obj)
        {
            return obj is RuleTyped other &&
                   (
                    _moduleName == other._moduleName &&
                    _connectorIndex == other._connectorIndex &&
                    _connectorType == other._connectorType
                   );
        }

        public bool IsValid => _moduleName.Length > 0 && _connectorType.Length > 0;

        public string IsValidWhyNot
        {
            get
            {
                if (_moduleName.Length == 0)
                {
                    return "Module name is empty";
                }
                if (_connectorType.Length == 0)
                {
                    return "Connector type name is empty";
                }
                return "Unknown reason.";
            }
        }

        public string TypeName => "WFCRuleTyped";

        public string TypeDescription => "WFC Connection rule typed.";

        public bool CastFrom(object source)
        {
            if (source.GetType() == typeof(Rule))
            {
                var rule = (Rule)source;
                if (rule._ruleTyped != null)
                {
                    _moduleName = rule._ruleTyped._moduleName;
                    _connectorIndex = rule._ruleTyped._connectorIndex;
                    _connectorType = rule._ruleTyped._connectorType;
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

        public IGH_Goo Duplicate()
        {
            return (IGH_Goo)MemberwiseClone();

        }

        public IGH_GooProxy EmitProxy()
        {
            return null;
        }

        public bool Read(GH_IReader reader) => true;

        public object ScriptVariable() => this;

        public bool Write(GH_IWriter writer) => true;
        public override string ToString()
        {
            return "Typed connector: " +
                _moduleName + ":" + _connectorIndex +
                " = " +
                _connectorType;
        }

        public override int GetHashCode()
        {
            var hashCode = 145665365;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_moduleName);
            hashCode = hashCode * -1521134295 + _connectorIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_connectorType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            return hashCode;
        }

        // TODO: If the rule cannot be converted, consider returning null or some sort of an invalid rule, 
        // rather than throwing an exception. The exception happens when the user tries to apply the rules 
        // on an unrelated or incomplete list of modules. This shouldn't happen (or should we try to cope with it?).
        public List<RuleExplicit> ToRuleExplicit(IEnumerable<RuleTyped> otherRules, List<Module> modules)
        {
            var rulesExplicit = new List<RuleExplicit>();
            var sourceModule = modules.Find(module => module.Name == _moduleName)
                ?? throw new Exception("Rule" + this + " expects a non-existing module " + _moduleName);
            if (_connectorIndex >= sourceModule.Connectors.Count)
            {
                throw new Exception("Rule" + this + " expects a non-existing connector" + _connectorIndex);
            }
            var sourceConnector = sourceModule.Connectors[_connectorIndex];
            foreach (var other in otherRules)
            {
                if (other._connectorType != _connectorType)
                {
                    continue;
                }
                var targetModule = modules.Find(module => module.Name == other._moduleName)
                    ?? throw new Exception("Rule" + this + " expects a non-existing module " + other._moduleName);
                if (other._connectorIndex >= targetModule.Connectors.Count)
                {
                    throw new Exception("Rule" + this + " expects a non-existing connector" + other._connectorIndex);
                }
                var targetConnector = targetModule.Connectors[other._connectorIndex];
                if (targetConnector._direction.IsOpposite(sourceConnector._direction))
                {
                    rulesExplicit.Add(
                        new RuleExplicit(sourceModule.Name,
                                         sourceConnector._connectorIndex,
                                         targetModule.Name,
                                         targetConnector._connectorIndex)
                        );
                }
            }
            return rulesExplicit;
        }
    }

    public class Rule : IGH_Goo
    {
        public RuleExplicit _ruleExplicit;
        public RuleTyped _ruleTyped;

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
            _ruleExplicit = new RuleExplicit(sourceModuleName, sourceConnectorIndex, targetModuleName, targetConnectorIndex);
            _ruleTyped = null;
        }
        public Rule(
            RuleExplicit ruleExplicit
        )
        {
            _ruleExplicit = ruleExplicit;
            _ruleTyped = null;
        }

        public Rule(
            string moduleName,
            int connectorIndex,
            string connectorType
        )
        {
            _ruleTyped = new RuleTyped(moduleName, connectorIndex, connectorType);
            _ruleExplicit = null;
        }

        public Rule(
            RuleTyped ruleTyped
        )
        {
            _ruleTyped = ruleTyped;
            _ruleExplicit = null;
        }

        public bool IsExplicit()
        {
            return _ruleExplicit != null && _ruleTyped == null;
        }

        public bool IsTyped()
        {
            return _ruleExplicit == null && _ruleTyped != null;
        }

        public override bool Equals(object obj)
        {
            if (IsExplicit())
            {
                return _ruleExplicit == obj;
            }
            if (IsTyped())
            {
                return _ruleTyped == obj;
            }
            return false;
        }

        public string TypeName => "WFCRule";

        public string TypeDescription => "WFC Connection rule.";

        bool IGH_Goo.IsValid => IsExplicit() || IsTyped();

        string IGH_Goo.IsValidWhyNot
        {
            get
            {
                if (IsExplicit())
                {
                    return ((IGH_Goo)_ruleExplicit).IsValidWhyNot;
                }
                if (IsTyped())
                {
                    return ((IGH_Goo)_ruleTyped).IsValidWhyNot;
                }
                return "The rule is neither explicit, nor typed.";
            }
        }

        public bool CastFrom(object rule)
        {
            if (rule.GetType() == typeof(RuleExplicit))
            {
                _ruleExplicit = (RuleExplicit)rule;
                _ruleTyped = null;
                return true;
            }
            if (rule.GetType() == typeof(RuleTyped))
            {
                _ruleTyped = (RuleTyped)rule;
                _ruleExplicit = null;
                return true;
            }
            return false;
        }

        public bool CastTo<T>(out T target)
        {
            if (IsExplicit() && typeof(T) == typeof(RuleExplicit))
            {
                target = (T)_ruleExplicit.Duplicate();
                return true;
            }
            if (IsTyped() && typeof(T) == typeof(RuleTyped))
            {
                target = (T)_ruleTyped.Duplicate();
                return true;
            }

            target = default;
            return false;

        }


        public IGH_Goo Duplicate()
        {
            return (IGH_Goo)MemberwiseClone();

        }

        public IGH_GooProxy EmitProxy()
        {
            return null;
        }

        public bool Read(GH_IReader reader) => true;

        public object ScriptVariable() => this;

        public bool Write(GH_IWriter writer) => true;

        public override string ToString()
        {
            if (IsExplicit())
            {
                return _ruleExplicit.ToString();
            }
            if (IsTyped())
            {
                return _ruleTyped.ToString();
            }
            return "The rule is invalid.";
        }

        public override int GetHashCode()
        {
            if (IsExplicit())
            {
                return _ruleExplicit.GetHashCode();
            }
            if (IsTyped())
            {
                return _ruleTyped.GetHashCode();
            }
            // The rule is invalid, the hash code is not unique
            var hashCode = -1934280001;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeDescription);
            return hashCode;
        }

    }

    public struct RuleForSolver
    {
        public string _axialDirection;
        public string _sourceSubmoduleName;
        public string _targetSubmoduleName;

        public RuleForSolver(string axialDirection, string sourceSubmoduleName, string targetSubmoduleName)
        {
            _axialDirection = axialDirection;
            _sourceSubmoduleName = sourceSubmoduleName;
            _targetSubmoduleName = targetSubmoduleName;
        }

        public override bool Equals(object obj)
        {
            return obj is RuleForSolver rule &&
                   _axialDirection == rule._axialDirection &&
                   _sourceSubmoduleName == rule._sourceSubmoduleName &&
                   _targetSubmoduleName == rule._targetSubmoduleName;
        }

        public override int GetHashCode()
        {
            var hashCode = -733970503;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_axialDirection);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_sourceSubmoduleName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_targetSubmoduleName);
            return hashCode;
        }
    }
}
