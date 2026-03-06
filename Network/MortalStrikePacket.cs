using Fika.Core.Networking.LiteNetLib.Utils;

namespace MortalStrike.Network
{
    /// <summary>
    /// Кастомный пакет для синхронизации звуков/оповещений обстрела между headless и клиентами.
    /// </summary>
    public struct MortalStrikePacket : INetSerializable
    {
        public byte   EventType;  // тип пакета
        public string ProfileId;  // для Damage: ProfileId цели
        public float  Damage;     // для Damage: итоговый урон

        public const byte MinuteBefore = 0;
        public const byte TenSeconds   = 1;
        public const byte Bombing      = 2;
        public const byte End          = 3;
        public const byte Damage_t     = 4; // headless → клиент: применить урон

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(EventType);
            if (EventType == Damage_t)
            {
                writer.Put(ProfileId ?? "");
                writer.Put(Damage);
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            EventType = reader.GetByte();
            if (EventType == Damage_t)
            {
                ProfileId = reader.GetString();
                Damage    = reader.GetFloat();
            }
        }
    }
}
