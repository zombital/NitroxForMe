using NitroxClient.MonoBehaviours;
using NitroxModel.DataStructures;
using NitroxModel.DataStructures.GameLogic.Entities;
using NitroxModel.DataStructures.Util;
using UnityEngine;

namespace NitroxClient.GameLogic.Spawning.WorldEntities
{
    public class CellRootSpawner : IWorldEntitySpawner
    {
        public Optional<GameObject> Spawn(WorldEntity entity, Optional<GameObject> parent, EntityCell cellRoot)
        {
            NitroxInt3 cellId = entity.AbsoluteEntityCell.CellId;
            NitroxInt3 batchId = entity.AbsoluteEntityCell.BatchId;

            cellRoot.liveRoot.name = $"CellRoot {cellId.X}, {cellId.Y}, {cellId.Z}; Batch {batchId.X}, {batchId.Y}, {batchId.Z}";

            NitroxEntity.SetNewId(cellRoot.liveRoot, entity.Id);

            LargeWorldStreamer.main.cellManager.QueueForAwake(cellRoot);

            return Optional.OfNullable(cellRoot.liveRoot);
        }

        public bool SpawnsOwnChildren()
        {
            return false;
        }
    }
}
