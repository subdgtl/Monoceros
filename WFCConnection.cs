using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;

namespace WFCTools
{
    // TODO: Obsolete
    public class WFCConnection : IGH_Goo
    {
        public ConnectionType SourceConnectionType;
        public ConnectionType TargetConnectionType;
        public Direction Direction;
        public string SourceModuleName;
        public string TargetModuleName;

        public override bool Equals(object obj)
        {
            return obj is WFCConnection connector &&
                   (
                       (
                           EqualityComparer<Direction>.Default.Equals(Direction, connector.Direction) &&
                           (
                                SourceModuleName == connector.SourceModuleName ||
                                (
                                    SourceConnectionType == ConnectionType.Indifferent &&
                                    connector.SourceConnectionType == ConnectionType.Indifferent
                                )
                           ) &&
                           (
                                TargetModuleName == connector.TargetModuleName ||
                                (
                                    TargetConnectionType == ConnectionType.Indifferent &&
                                    connector.TargetConnectionType == ConnectionType.Indifferent
                                )
                            )
                        ) ||
                        (
                           connector.Direction.IsOpposite(Direction) &&
                           (
                                SourceModuleName == connector.TargetModuleName ||
                                (
                                    SourceConnectionType == ConnectionType.Indifferent &&
                                    connector.TargetConnectionType == ConnectionType.Indifferent
                                )
                           ) &&
                           (
                                TargetModuleName == connector.SourceModuleName ||
                                (
                                    TargetConnectionType == ConnectionType.Indifferent &&
                                    connector.SourceConnectionType == ConnectionType.Indifferent
                                )
                            )
                        )
                   );
        }

        public override int GetHashCode()
        {
            int hashCode = -283642641;
            hashCode = hashCode * -1521134295 + Direction.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SourceModuleName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TargetModuleName);
            return hashCode;
        }

        public bool ToPositive(out WFCConnection positive)
        {
            if (Direction.Orientation == Orientation.Positive)
            {
                positive = this;
                return true;
            }

            var flippedDirection = Direction.ToFlipped();
            positive = new WFCConnection()
            {
                SourceConnectionType = TargetConnectionType,
                TargetConnectionType = SourceConnectionType,
                Direction = flippedDirection,
                SourceModuleName = TargetModuleName,
                TargetModuleName = SourceModuleName
            };
            return true;
        }

        public bool IsValid => !(SourceModuleName.Length == 0 && SourceConnectionType != ConnectionType.Indifferent) &&
                !(TargetModuleName.Length == 0 && TargetConnectionType != ConnectionType.Indifferent);

        public string IsValidWhyNot
        {
            get
            {
                if (SourceModuleName.Length == 0 && SourceConnectionType != ConnectionType.Indifferent)
                {
                    return "Unspecified source module name.";
                }
                if (TargetModuleName.Length == 0 && TargetConnectionType != ConnectionType.Indifferent)
                {
                    return "Unspecified target module name.";
                }
                return "Unknown reason.";
            }
        }

        public string TypeName => "WFCConnection";

        public string TypeDescription => "Megamodule input data.";

        public bool CastFrom(object source) => false;

        public bool CastTo<T>(out T target)
        {
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
            return Direction +
                ": (" +
                WFCUtilities.ToString(SourceConnectionType) +
                ")" +
                SourceModuleName +
                " -> (" +
                WFCUtilities.ToString(TargetConnectionType) +
                ")" +
                TargetModuleName;
        }

    }
}
