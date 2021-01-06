using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.Util;
using UnityEngine;

namespace NitroxClient.GameLogic.Spawning
{
    public interface IEntitySpawner
    {
        public Optional<GameObject> Spawn(Entity entity, out bool spawnedChildren);
    }
}
