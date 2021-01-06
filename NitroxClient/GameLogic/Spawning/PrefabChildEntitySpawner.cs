using NitroxClient.MonoBehaviours;
using NitroxModel.DataStructures.GameLogic.Entities;
using NitroxModel.DataStructures.Util;
using NitroxModel.Logger;
using UnityEngine;

namespace NitroxClient.GameLogic.Spawning
{
    public class PrefabChildEntitySpawner : EntitySpawner<PrefabChildEntity>
    {
        // When we first encounter a PrefabChildEntity, we simply need to assign it the right id matching the server
        public override Optional<GameObject> OnSpawn(PrefabChildEntity entity, out bool spawnedChildren)
        {
            GameObject parent = NitroxEntity.RequireObjectFrom(entity.ParentId);

            if (parent.transform.childCount - 1 < entity.ExistingGameObjectChildIndex)
            {
                Log.Error($"Parent {parent} did not have a child at index {entity.ExistingGameObjectChildIndex}");

                // prevent any further calls for children as we can't find this object.
                spawnedChildren = true;
                return Optional.Empty;
            }

            GameObject gameObject = parent.transform.GetChild(entity.ExistingGameObjectChildIndex).gameObject;

            NitroxEntity.SetNewId(gameObject, entity.Id);

            spawnedChildren = false;

            return Optional.OfNullable(gameObject);
        }

    }
}
