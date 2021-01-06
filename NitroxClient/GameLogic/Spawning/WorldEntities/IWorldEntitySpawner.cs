using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.GameLogic.Entities;
using NitroxModel.DataStructures.Util;
using UnityEngine;

namespace NitroxClient.GameLogic.Spawning.WorldEntities
{
    /**
     * Allows us to create custom entity spawners for different world entity types.
     */
    public interface IWorldEntitySpawner
    {
        Optional<GameObject> Spawn(WorldEntity entity, Optional<GameObject> parent, EntityCell cellRoot);
        bool SpawnsOwnChildren();
    }
}
