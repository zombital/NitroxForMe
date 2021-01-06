using System;
using System.Collections.Generic;
using NitroxModel.DataStructures.GameLogic.Entities;
using NitroxModel.DataStructures.GameLogic.Entities.Metadata;
using ProtoBufNet;

namespace NitroxModel.DataStructures.GameLogic
{
    [Serializable]
    [ProtoContract]
    [ProtoInclude(50, typeof(WorldEntity))]
    [ProtoInclude(60, typeof(PrefabChildEntity))]
    public abstract class Entity
    {
        [ProtoMember(1)]
        public NitroxId Id { get; set; }

        [ProtoMember(2)]
        public NitroxTechType TechType { get; set; }

        [ProtoMember(3)]
        public EntityMetadata Metadata { get; set; }

        [ProtoMember(4)]
        public NitroxId ParentId { get; set; }

        public List<Entity> Children { get; set; } = new List<Entity>();

        protected Entity()
        {
            // Constructor for serialization. Has to be "protected" for json serialization.
        }

        public override string ToString()
        {
            return $"[Entity id: {Id} techType: {TechType} Metadata: {Metadata} ParentId: {ParentId} ChildEntities: {string.Join(",\n ", Children)}]";
        }
    }
}
