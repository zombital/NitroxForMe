
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.Util;
using UnityEngine;

namespace NitroxClient.GameLogic.Spawning
{
    public abstract class EntitySpawner<T> : IEntitySpawner where T : Entity
    {
        public abstract Optional<GameObject> OnSpawn(T entity, out bool spawnedChildren);

        public Optional<GameObject> Spawn(Entity entity, out bool spawnedChildren)
        {
            return OnSpawn((T)entity, out spawnedChildren);
        }
    }
}
