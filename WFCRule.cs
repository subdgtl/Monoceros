using System;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;

namespace WFCTools
{
    public class WFCRuleExplicit : IGH_Goo
    {
        public string SourceModuleName;
        public int SourceConnectorIndex;

        public string TargetModuleName;
        public int TargetConnectorIndex;

        public WFCRuleExplicit(string sourceModuleName, int sourceConnectorIndex, string targetModuleName, int targetConnectorIndex)
        {
            SourceModuleName = sourceModuleName.ToLower();
            SourceConnectorIndex = sourceConnectorIndex;
            TargetModuleName = targetModuleName.ToLower();
            TargetConnectorIndex = targetConnectorIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is WFCRuleExplicit other &&
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

        public string TypeName => "WFCRuleExplicit";

        public string TypeDescription => "WFC Connection rule explicit.";

        public bool CastFrom(object source)
        {
            if (source.GetType() == typeof(WFCRule))
            {
                var rule = (WFCRule)source;
                if (rule.RuleExplicit != null)
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
            if (IsValid && typeof(T) == typeof(WFCRule))
            {
                object obj = new WFCRule((WFCRuleExplicit)Duplicate());
                target = (T)obj;
                return true;
            }

            target = default;
            return false;
        }

        public IGH_Goo Duplicate()
        {
            return (IGH_Goo)this.MemberwiseClone();

        }

        public IGH_GooProxy EmitProxy()
        {
            return (IGH_GooProxy)null;
        }

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
            int hashCode = -1103775584;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SourceModuleName);
            hashCode = hashCode * -1521134295 + SourceConnectorIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TargetModuleName);
            hashCode = hashCode * -1521134295 + TargetConnectorIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            return hashCode;
        }

        public WFCRuleSolver ToWFCRuleSolver(List<WFCModule> modules)
        {
            var sourceModule = modules.Find(module => module.Name == SourceModuleName) ?? throw new Exception("Rule" + this + " expects a non-existing module " + SourceModuleName);
            if (SourceConnectorIndex >= sourceModule.Connectors.Count)
            {
                throw new Exception("Rule" + this + " expects a non-existing connector" + SourceConnectorIndex);
            }
            var sourceConnector = sourceModule.Connectors[SourceConnectorIndex];
            var targetModule = modules.Find(module => module.Name == TargetModuleName) ?? throw new Exception("Rule" + this + " requires a non-existing module " + TargetModuleName);
            if (TargetConnectorIndex >= targetModule.Connectors.Count)
            {
                throw new Exception("Rule" + this + " expects a non-existing connector" + TargetConnectorIndex);
            }
            var targetConnector = targetModule.Connectors[TargetConnectorIndex];

            if (!sourceConnector.Direction.IsOpposite(targetConnector.Direction))
            {
                throw new Exception("Connectors " + SourceModuleName + ":" + SourceConnectorIndex + " and " + TargetModuleName + ":" + TargetConnectorIndex + " are not opposite");
            }
            if (sourceConnector.Direction.Orientation == Orientation.Positive)
            {
                return new WFCRuleSolver(sourceConnector.Direction.Axis.ToString("g"), sourceConnector.SubmoduleName, targetConnector.SubmoduleName);
            }
            else
            {
                return new WFCRuleSolver(targetConnector.Direction.Axis.ToString("g"), targetConnector.SubmoduleName, sourceConnector.SubmoduleName);
            }
        }
    }

    public class WFCRuleTyped : IGH_Goo
    {
        public string ModuleName;
        public int ConnectorIndex;

        public string ConnectorType;

        // Not case sensitive
        public WFCRuleTyped(string moduleName, int connectorIndex, string connectorType)
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

        public override bool Equals(object obj)
        {
            return obj is WFCRuleTyped other &&
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

        public string TypeName => "WFCRuleTyped";

        public string TypeDescription => "WFC Connection rule typed.";

        public bool CastFrom(object source)
        {
            if (source.GetType() == typeof(WFCRule))
            {
                var rule = (WFCRule)source;
                if (rule.RuleTyped != null)
                {
                    this.ModuleName = rule.RuleTyped.ModuleName;
                    this.ConnectorIndex = rule.RuleTyped.ConnectorIndex;
                    this.ConnectorType = rule.RuleTyped.ConnectorType;
                    return true;
                }
            }
            return false;
        }

        public bool CastTo<T>(out T target)
        {
            if (IsValid && typeof(T) == typeof(WFCRule))
            {
                object obj = new WFCRule((WFCRuleTyped)Duplicate());
                target = (T)obj;
                return true;
            }

            target = default;
            return false;
        }

        public IGH_Goo Duplicate()
        {
            return (IGH_Goo)this.MemberwiseClone();

        }

        public IGH_GooProxy EmitProxy()
        {
            return (IGH_GooProxy)null;
        }

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
            int hashCode = 145665365;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ModuleName);
            hashCode = hashCode * -1521134295 + ConnectorIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ConnectorType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            return hashCode;
        }

        // TODO: If the rule cannot be converted, consider returning null or some sort of an invalid rule, 
        // rather than throwing an exception. The exception happens when the user tries to apply the rules 
        // on an unrelated or incomplete list of modules. This shouldn't happen (or should we try to cope with it?).
        public List<WFCRuleExplicit> ToWFCRuleExplicit(List<WFCRuleTyped> otherRules, List<WFCModule> modules)
        {
            var rulesExplicit = new List<WFCRuleExplicit>();
            var sourceModule = modules.Find(module => module.Name == ModuleName) ?? throw new Exception("Rule" + this + " expects a non-existing module " + ModuleName);
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
                var targetModule = modules.Find(module => module.Name == other.ModuleName) ?? throw new Exception("Rule" + this + " expects a non-existing module " + other.ModuleName);
                if (other.ConnectorIndex >= targetModule.Connectors.Count)
                {
                    throw new Exception("Rule" + this + " expects a non-existing connector" + other.ConnectorIndex);
                }
                var targetConnector = targetModule.Connectors[other.ConnectorIndex];
                if (targetConnector.Direction.IsOpposite(sourceConnector.Direction))
                {
                    rulesExplicit.Add(
                        new WFCRuleExplicit(sourceModule.Name, sourceConnector.ConnectorIndex, targetModule.Name, targetConnector.ConnectorIndex)
                        );
                }
            }
            return rulesExplicit;
        }
    }

    public class WFCRule : IGH_Goo
    {
        public WFCRuleExplicit RuleExplicit;
        public WFCRuleTyped RuleTyped;

        public WFCRule()
        {
        }

        public WFCRule(
            string sourceModuleName,
            int sourceConnectorIndex,
            string targetModuleName,
            int targetConnectorIndex
        )
        {
            RuleExplicit = new WFCRuleExplicit(sourceModuleName, sourceConnectorIndex, targetModuleName, targetConnectorIndex);
            RuleTyped = null;
        }
        public WFCRule(
            WFCRuleExplicit ruleExplicit
        )
        {
            RuleExplicit = ruleExplicit;
            RuleTyped = null;
        }

        public WFCRule(
            string moduleName,
            int connectorIndex,
            string connectorType
        )
        {
            RuleTyped = new WFCRuleTyped(moduleName, connectorIndex, connectorType);
            RuleExplicit = null;
        }

        public WFCRule(
            WFCRuleTyped ruleTyped
        )
        {
            RuleTyped = ruleTyped;
            RuleExplicit = null;
        }

        public override bool Equals(object obj)
        {
            if (RuleExplicit != null)
            {
                return RuleExplicit == obj;
            }
            if (RuleTyped != null)
            {
                return RuleTyped == obj;
            }
            return false;
        }

        public string TypeName => "WFCRule";

        public string TypeDescription => "WFC Connection rule.";

        bool IGH_Goo.IsValid
        {
            get
            {
                if (RuleExplicit != null && RuleTyped != null)
                {
                    return false;
                }
                if (RuleExplicit != null)
                {
                    return ((IGH_Goo)RuleExplicit).IsValid;
                }
                if (RuleTyped != null)
                {
                    return ((IGH_Goo)RuleExplicit).IsValid;
                }
                return false;

            }
        }

        string IGH_Goo.IsValidWhyNot
        {
            get
            {
                if (RuleExplicit != null && RuleTyped != null)
                {
                    return "The rule the rule appears to be both explicit and typed.";
                }
                if (RuleExplicit != null)
                {
                    return ((IGH_Goo)RuleExplicit).IsValidWhyNot;
                }
                if (RuleTyped != null)
                {
                    return ((IGH_Goo)RuleTyped).IsValidWhyNot;
                }
                return "The rule is neither explicit, nor typed.";
            }
        }

        public bool CastFrom(object rule)
        {
            if (rule.GetType() == typeof(WFCRuleExplicit))
            {
                RuleExplicit = (WFCRuleExplicit)rule;
                RuleTyped = null;
                return true;
            }
            if (rule.GetType() == typeof(WFCRuleTyped))
            {
                RuleTyped = (WFCRuleTyped)rule;
                RuleExplicit = null;
                return true;
            }
            return false;
        }

        public bool CastTo<T>(out T target)
        {
            if (RuleExplicit != null && typeof(T) == typeof(WFCRuleExplicit))
            {
                target = (T)RuleExplicit.Duplicate();
                return true;
            }
            if (RuleTyped != null && typeof(T) == typeof(WFCRuleTyped))
            {
                target = (T)RuleTyped.Duplicate();
                return true;
            }

            target = default;
            return false;

        }


        public IGH_Goo Duplicate()
        {
            return (IGH_Goo)this.MemberwiseClone();

        }

        public IGH_GooProxy EmitProxy()
        {
            return (IGH_GooProxy)null;
        }

        public bool Read(GH_IReader reader) => true;

        public object ScriptVariable() => this;

        public bool Write(GH_IWriter writer) => true;

        public override string ToString()
        {
            if (RuleExplicit != null && RuleTyped == null)
            {
                return RuleExplicit.ToString();
            }
            if (RuleTyped != null && RuleExplicit == null)
            {
                return RuleTyped.ToString();
            }
            return "The rule is invalid.";
        }

        public override int GetHashCode()
        {
            if (RuleExplicit != null && RuleTyped == null)
            {
                return RuleExplicit.GetHashCode();
            }
            if (RuleTyped != null && RuleExplicit == null)
            {
                return RuleTyped.GetHashCode();
            }
            // The rule is invalid, the hash code is not unique
            int hashCode = -1934280001;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeDescription);
            return hashCode;
        }

    }


    public struct WFCRuleSolver
    {
        public string AxialDirection;
        public string SourceSubmoduleName;
        public string TargetSubmoduleName;

        public WFCRuleSolver(string axialDirection, string sourceSubmoduleName, string targetSubmoduleName)
        {
            AxialDirection = axialDirection;
            SourceSubmoduleName = sourceSubmoduleName;
            TargetSubmoduleName = targetSubmoduleName;
        }

        public override bool Equals(object obj)
        {
            return obj is WFCRuleSolver rule &&
                   AxialDirection == rule.AxialDirection &&
                   SourceSubmoduleName == rule.SourceSubmoduleName &&
                   TargetSubmoduleName == rule.TargetSubmoduleName;
        }

        public override int GetHashCode()
        {
            int hashCode = -733970503;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AxialDirection);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SourceSubmoduleName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TargetSubmoduleName);
            return hashCode;
        }
    }
}
