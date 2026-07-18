using ProtoBuf;

namespace Dumbwaiter
{
    [ProtoContract]
    public class TransitionPacket
    {
        [ProtoMember(1)] public float DurationSeconds;
        [ProtoMember(2)] public int SourceX;
        [ProtoMember(3)] public int SourceY;
        [ProtoMember(4)] public int SourceZ;
        [ProtoMember(5)] public float Volume;
        [ProtoMember(6)] public bool SuppressFade; // cargo send: sounds only, no blackout
    }
}
