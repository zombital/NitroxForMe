using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.GameLogic.Entities;
using NitroxModel.DataStructures.Util;
using NitroxModel.Helper;
using UnityEngine;

namespace NitroxClient.GameLogic.Spawning.WorldEntities
{
    public class ReefbackWorldEntitySpawner : IWorldEntitySpawner
    {
        private readonly DefaultWorldEntitySpawner defaultSpawner;

        public ReefbackWorldEntitySpawner(DefaultWorldEntitySpawner defaultSpawner)
        {
            this.defaultSpawner = defaultSpawner;
        }

        public Optional<GameObject> Spawn(WorldEntity entity, Optional<GameObject> parent, EntityCell cellRoot)
        {
            Optional<GameObject> reefback = defaultSpawner.Spawn(entity, parent, cellRoot);
            if (!reefback.HasValue)
            {
                return Optional.Empty;
            }
            ReefbackLife life = reefback.Value.GetComponent<ReefbackLife>();
            if (life == null)
            {
                return Optional.Empty;
            }
            
            life.initialized = true;
            life.ReflectionCall("SpawnPlants");
            foreach (Entity childEntity in entity.Children)
            {
                if (childEntity is WorldEntity we)
                {
                    Optional<GameObject> child = defaultSpawner.Spawn(we, reefback, cellRoot);
                    if (child.HasValue)
                    {
                        child.Value.AddComponent<ReefbackCreature>();
                    }
                }
            }

            return Optional.Empty;
        }

        public bool SpawnsOwnChildren()
        {
            return true;
        }
    }
}
