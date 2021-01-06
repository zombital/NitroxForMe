using System;
using System.Runtime.Serialization;
using ProtoBufNet;

namespace NitroxModel.DataStructures.GameLogic.Entities
{
    /*
     * A world entity is an object physically in the world with a transform.  It is either a global root entity
     * or something that phases out with the clip map manager.
     */
    [Serializable]
    [ProtoContract]
    public class WorldEntity : Entity
    {
        public AbsoluteEntityCell AbsoluteEntityCell => new AbsoluteEntityCell(Transform.Position, Level);

        [ProtoMember(1)]
        public NitroxTransform Transform { get; set; }

        [ProtoMember(2)]
        public int Level { get; set; }

        [ProtoMember(3)]
        public string ClassId { get; set; }

        /// <summary>
        ///     Keeps track if an entity was spawned by the server or a player
        ///     Server-spawned entities need to be techType white-listed to be simulated
        /// </summary>
        [ProtoMember(4)]
        public bool SpawnedByServer;

        [ProtoMember(5)]
        public NitroxId WaterParkId { get; set; }

        [ProtoMember(6)]
        public bool ExistsInGlobalRoot { get; set; }

        protected WorldEntity()
        {
            // Constructor for serialization. Has to be "protected" for json serialization.
        }

        public WorldEntity(NitroxVector3 localPosition, NitroxQuaternion localRotation, NitroxVector3 scale, NitroxTechType techType, int level, string classId, bool spawnedByServer, NitroxId id, WorldEntity parentEntity, bool existsInGlobalRoot, NitroxId waterParkId)
        {
            Transform = new NitroxTransform(localPosition, localRotation, scale, this);
            TechType = techType;
            Id = id;
            Level = level;
            ClassId = classId;
            SpawnedByServer = spawnedByServer;
            WaterParkId = waterParkId;
            ExistsInGlobalRoot = existsInGlobalRoot;

            if (parentEntity != null)
            {
                ParentId = parentEntity.Id;
                Transform.SetParent(parentEntity.Transform);
            }
        }

        [ProtoAfterDeserialization]
        private void ProtoAfterDeserialization()
        {
            Transform.Entity = this;
        }

        [OnDeserialized]
        private void JsonAfterDeserialization(StreamingContext context)
        {
            ProtoAfterDeserialization();
        }

        public override string ToString()
        {
            return $"[WorldEntity Transform: {Transform} Level: {Level} ClassId: {ClassId} SpawnedByServer: {SpawnedByServer} WaterParkId: {WaterParkId} ExistsInGlobalRoot: {ExistsInGlobalRoot} {base.ToString()}]";
        }
    }
}
