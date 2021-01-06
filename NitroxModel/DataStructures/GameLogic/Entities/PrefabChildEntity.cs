using System;
using ProtoBufNet;

namespace NitroxModel.DataStructures.GameLogic.Entities
{
    /*
     * A PrefabChildEntity is a gameobject that resides inside of a spawned prefab.  Although the server knows about these,
     * it is too cost prohibitive for it to send spawn data for all of these.  Instead, we let the game spawn them and tag
     * the entity after the fact.  An example of this is a keypad in the aurora; there is an overarching Door prefab with 
     * the keypad baked in - we simply update the id of the keypad on spawn.  Each PrefabChildEntity will always bubble up
     * to a root WorldEntity.
     */
    [Serializable]
    [ProtoContract]
    public class PrefabChildEntity : Entity
    {       
        [ProtoMember(1)]
        public int ExistingGameObjectChildIndex { get; set; }

        protected PrefabChildEntity()
        {
            // Constructor for serialization. Has to be "protected" for json serialization.
        }

        public PrefabChildEntity(NitroxId id, NitroxTechType techType, int existingGameObjectChildIndex, Entity parent)
        {
            Id = id;
            TechType = techType;
            ExistingGameObjectChildIndex = existingGameObjectChildIndex;
            ParentId = parent.Id;
        }
    }
}
