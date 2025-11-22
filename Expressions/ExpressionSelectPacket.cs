using ProtoBuf;

namespace Expressions;

[ProtoContract]
public class ExpressionSelectionPacket
{
    [ProtoMember(1)] public string EyebrowsVariant;

    [ProtoMember(2)] public string EyesVariant;

    [ProtoMember(3)] public string MouthVariant;
}