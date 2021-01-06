using NitroxModel.DataStructures.GameLogic;
using NitroxServer.GameLogic.Entities.Spawning;
using System.Collections.Generic;
using System.Linq;
using NitroxModel.DataStructures.Util;
using NitroxModel.Logger;
using NitroxModel.DataStructures;
using NitroxModel.DataStructures.GameLogic.Entities;

namespace NitroxServer.GameLogic.Entities
{
    public class WorldEntityManager
    {
        private readonly EntityRegistry entityRegistry;

        // Phasing entities can disappear if you go out of range. 
        private readonly Dictionary<AbsoluteEntityCell, List<WorldEntity>> phasingEntitiesByAbsoluteCell;

        // Global root entities that are always visible.
        private readonly Dictionary<NitroxId, WorldEntity> globalRootEntitiesById;

        private readonly BatchEntitySpawner batchEntitySpawner;

        public WorldEntityManager(EntityRegistry entityRegistry, BatchEntitySpawner batchEntitySpawner)
        {
            List<Entity> entities = entityRegistry.GetAllEntities();
            List<WorldEntity> worldEntities = entities.OfType<WorldEntity>().ToList();

            globalRootEntitiesById = worldEntities.Where(entity => entity.ExistsInGlobalRoot)
                                                  .ToDictionary(entity => entity.Id);

            phasingEntitiesByAbsoluteCell = worldEntities.Where(entity => !entity.ExistsInGlobalRoot)
                                                         .GroupBy(entity => entity.AbsoluteEntityCell)
                                                         .ToDictionary(group => group.Key, group => group.ToList());
            this.entityRegistry = entityRegistry;
            this.batchEntitySpawner = batchEntitySpawner;
        }

        public List<WorldEntity> GetVisibleEntities(AbsoluteEntityCell[] cells)
        {
            LoadUnspawnedEntities(cells);

            List<WorldEntity> entities = new List<WorldEntity>();

            foreach (AbsoluteEntityCell cell in cells)
            {
                List<WorldEntity> cellEntities = GetEntities(cell);
                entities.AddRange(cellEntities.Where(entity => cell.Level <= entity.Level));
            }

            return entities;
        }

        public List<WorldEntity> GetGlobalRootEntities()
        {
            lock (globalRootEntitiesById)
            {
                return new List<WorldEntity>(globalRootEntitiesById.Values);
            }
        }

        public List<WorldEntity> GetEntities(AbsoluteEntityCell absoluteEntityCell)
        {
            List<WorldEntity> result;

            lock (phasingEntitiesByAbsoluteCell)
            {
                if (!phasingEntitiesByAbsoluteCell.TryGetValue(absoluteEntityCell, out result))
                {
                    result = phasingEntitiesByAbsoluteCell[absoluteEntityCell] = new List<WorldEntity>();
                }
            }

            return result;
        }

        public Optional<AbsoluteEntityCell> UpdateEntityPosition(NitroxId id, NitroxVector3 position, NitroxQuaternion rotation)
        {
            Optional<WorldEntity> opEntity = entityRegistry.GetEntityById<WorldEntity>(id);

            if (!opEntity.HasValue)
            {
                Log.Debug("Could not update entity position because it was not found (maybe it was recently picked up)");
                return Optional.Empty;
            }

            WorldEntity entity = opEntity.Value;
            AbsoluteEntityCell oldCell = entity.AbsoluteEntityCell;

            entity.Transform.Position = position;
            entity.Transform.Rotation = rotation;

            AbsoluteEntityCell newCell = entity.AbsoluteEntityCell;
            if (oldCell != newCell)
            {
                EntitySwitchedCells(entity, oldCell, newCell);
            }

            return Optional.Of(newCell);
        }

        public void RegisterNewEntity(WorldEntity entity)
        {
            entityRegistry.AddEntity(entity);

            if (entity.ExistsInGlobalRoot)
            {
                lock (globalRootEntitiesById)
                {
                    if (!globalRootEntitiesById.ContainsKey(entity.Id))
                    {
                        globalRootEntitiesById.Add(entity.Id, entity);
                    }
                    else
                    {
                        Log.Info("Entity Already Exists for Id: " + entity.Id + " Item: " + entity.TechType);
                    }
                }
            }
            else
            {
                lock (phasingEntitiesByAbsoluteCell)
                {
                    List<WorldEntity> phasingEntitiesInCell = null;

                    if (!phasingEntitiesByAbsoluteCell.TryGetValue(entity.AbsoluteEntityCell, out phasingEntitiesInCell))
                    {
                        phasingEntitiesInCell = phasingEntitiesByAbsoluteCell[entity.AbsoluteEntityCell] = new List<WorldEntity>();
                    }

                    phasingEntitiesInCell.Add(entity);
                }
            }
        }

        public void PickUpEntity(NitroxId id)
        {
            Optional<Entity> entity = entityRegistry.RemoveEntity(id);
            
            if (entity.HasValue && entity.Value is WorldEntity worldEntity)
            {
                if (worldEntity.ExistsInGlobalRoot)
                {
                    lock (globalRootEntitiesById)
                    {
                        globalRootEntitiesById.Remove(id);
                    }
                }
                else
                {
                    lock (phasingEntitiesByAbsoluteCell)
                    {
                        List<WorldEntity> entities;

                        if (phasingEntitiesByAbsoluteCell.TryGetValue(worldEntity.AbsoluteEntityCell, out entities))
                        {
                            entities.Remove(worldEntity);
                        }
                    }
                }

                if (worldEntity.ParentId != null)
                {
                    Optional<Entity> parent = entityRegistry.GetEntityById(worldEntity.ParentId);

                    if (parent.HasValue)
                    {
                        parent.Value.Children.Remove(worldEntity);
                    }
                }
            }
        }

        private void LoadUnspawnedEntities(AbsoluteEntityCell[] cells)
        {
            IEnumerable<NitroxInt3> distinctBatchIds = cells.Select(cell => cell.BatchId).Distinct();

            foreach (NitroxInt3 batchId in distinctBatchIds)
            {
                List<Entity> spawnedEntities = batchEntitySpawner.LoadUnspawnedEntities(batchId);
                entityRegistry.AddEntities(spawnedEntities);

                List<WorldEntity> worldEntities = spawnedEntities.OfType<WorldEntity>().ToList();

                lock (phasingEntitiesByAbsoluteCell)
                {
                    foreach (WorldEntity entity in worldEntities)
                    {
                        if (entity.ParentId != null)
                        {
                            Optional<WorldEntity> opEnt = entityRegistry.GetEntityById<WorldEntity>(entity.ParentId);

                            if (opEnt.HasValue)
                            {
                                entity.Transform.SetParent(opEnt.Value.Transform);
                            }
                            else
                            {
                                Log.Error("Parent not Found! Are you sure it exists? " + entity.ParentId);
                            }
                        }

                        List<WorldEntity> entitiesInCell = GetEntities(entity.AbsoluteEntityCell);
                        entitiesInCell.Add(entity);                        
                    }                    
                }
            }
        }

        private void EntitySwitchedCells(WorldEntity entity, AbsoluteEntityCell oldCell, AbsoluteEntityCell newCell)
        {
            if (entity.ExistsInGlobalRoot)
            {
                return; // We don't care what cell a global root entity resides in.  Only phasing entities.
            }

            lock (phasingEntitiesByAbsoluteCell)
            {
                List<WorldEntity> oldList = GetEntities(oldCell);
                oldList.Remove(entity);

                List<WorldEntity> newList = GetEntities(newCell);
                newList.Add(entity);
            }
        }
    }
}
