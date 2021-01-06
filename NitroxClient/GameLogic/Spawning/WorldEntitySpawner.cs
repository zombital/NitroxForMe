using System.Collections.Generic;
using NitroxClient.GameLogic.Spawning.WorldEntities;
using NitroxClient.MonoBehaviours;
using NitroxModel.DataStructures.GameLogic.Entities;
using NitroxModel.DataStructures.Util;
using NitroxModel.Helper;
using NitroxModel_Subnautica.DataStructures;
using UnityEngine;

namespace NitroxClient.GameLogic.Spawning
{
    public class WorldEntitySpawner : EntitySpawner<WorldEntity>
    {
        private readonly WorldEntitySpawnerResolver worldEntitySpawnResolver = new WorldEntitySpawnerResolver();
        private readonly Dictionary<Int3, BatchCells> batchCellsById;

        public WorldEntitySpawner()
        {
            batchCellsById = (Dictionary<Int3, BatchCells>)LargeWorldStreamer.main.cellManager.ReflectionGet("batch2cells");
        }

        public override Optional<GameObject> OnSpawn(WorldEntity entity, out bool spawnedChildren)
        {
            LargeWorldStreamer.main.cellManager.UnloadBatchCells(entity.AbsoluteEntityCell.CellId.ToUnity()); // Just in case

            EntityCell cellRoot = EnsureCell(entity);

            Optional<GameObject> parent = (entity.ParentId != null) ? NitroxEntity.GetObjectFrom(entity.ParentId) : Optional.Empty;

            IWorldEntitySpawner entitySpawner = worldEntitySpawnResolver.ResolveEntitySpawner(entity);

            spawnedChildren = entitySpawner.SpawnsOwnChildren();

            return entitySpawner.Spawn(entity, parent, cellRoot);
        }

        private EntityCell EnsureCell(WorldEntity entity)
        {
            EntityCell entityCell;

            Int3 batchId = entity.AbsoluteEntityCell.BatchId.ToUnity();
            Int3 cellId = entity.AbsoluteEntityCell.CellId.ToUnity();

            if (!batchCellsById.TryGetValue(batchId, out BatchCells batchCells))
            {
                batchCells = LargeWorldStreamer.main.cellManager.InitializeBatchCells(batchId);
            }

            entityCell = batchCells.Get(cellId, entity.AbsoluteEntityCell.Level);

            if (entityCell == null)
            {
                entityCell = batchCells.Add(cellId, entity.AbsoluteEntityCell.Level);
                entityCell.Initialize();
            }

            entityCell.EnsureRoot();

            return entityCell;
        }
    }
}
