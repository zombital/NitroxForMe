using System;
using System.Collections.Generic;
using NitroxClient.Communication.Abstract;
using NitroxClient.GameLogic.Spawning;
using NitroxClient.GameLogic.Spawning.Metadata;
using NitroxClient.MonoBehaviours;
using NitroxModel.DataStructures;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.GameLogic.Entities;
using NitroxModel.DataStructures.GameLogic.Entities.Metadata;
using NitroxModel.DataStructures.Util;
using NitroxModel.Logger;
using NitroxModel.Packets;
using NitroxModel_Subnautica.DataStructures;
using UnityEngine;

namespace NitroxClient.GameLogic
{
    public class Entities
    {
        private readonly IPacketSender packetSender;

        private readonly HashSet<NitroxId> alreadySpawnedIds = new HashSet<NitroxId>();
        private readonly Dictionary<NitroxId, List<Entity>> pendingParentEntitiesByParentId = new Dictionary<NitroxId, List<Entity>>();

        private readonly Dictionary<Type, IEntitySpawner> entitySpawnersByType = new Dictionary<Type, IEntitySpawner>();

        public Entities(IPacketSender packetSender)
        {
            this.packetSender = packetSender;

            entitySpawnersByType[typeof(WorldEntity)] = new WorldEntitySpawner();
            entitySpawnersByType[typeof(PrefabChildEntity)] = new PrefabChildEntitySpawner();
        }

        public void BroadcastTransforms(Dictionary<NitroxId, GameObject> gameObjectsById)
        {
            EntityTransformUpdates update = new EntityTransformUpdates();

            foreach (KeyValuePair<NitroxId, GameObject> gameObjectWithId in gameObjectsById)
            {
                if (gameObjectWithId.Value)
                {
                    update.AddUpdate(gameObjectWithId.Key, gameObjectWithId.Value.transform.position.ToDto(), gameObjectWithId.Value.transform.rotation.ToDto());
                }
            }

            packetSender.Send(update);
        }

        public void BroadcastMetadataUpdate(NitroxId id, EntityMetadata metadata)
        {
            packetSender.Send(new EntityMetadataUpdate(id, metadata));
        }

        public void BroadcastEntitySpawnedByClient(WorldEntity entity)
        {
            packetSender.Send(new EntitySpawnedByClient(entity));
        }

        public void Spawn(List<WorldEntity> entities)
        {
            foreach (WorldEntity entity in entities)
            {
                try
                {
                    if (WasAlreadySpawned(entity.Id))
                    {
                        UpdatePosition(entity);
                    }
                    else if (entity.ParentId != null)
                    {
                        AddPendingParentEntity(entity);
                    }
                    else
                    {
                        Spawn(entity);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to process Entity {entity.Id}, a {entity.TechType}");
                }
            }
        }

        private void Spawn(Entity entity)
        {
            alreadySpawnedIds.Add(entity.Id);

            bool spawnerHandledChildren;

            IEntitySpawner entitySpawner = entitySpawnersByType[entity.GetType()];
            Optional<GameObject> gameObject = entitySpawner.Spawn(entity, out spawnerHandledChildren);

            if (gameObject.HasValue)
            {
                Optional<EntityMetadataProcessor> metadataProcessor = EntityMetadataProcessor.FromMetaData(entity.Metadata);

                if (metadataProcessor.HasValue)
                {
                    metadataProcessor.Value.ProcessMetadata(gameObject.Value, entity.Metadata);
                }
            }

            if (!spawnerHandledChildren)
            {
                foreach (Entity childEntity in entity.Children)
                {
                    if (!WasAlreadySpawned(childEntity.Id))
                    {
                        Spawn(childEntity);
                    }
                }

                SpawnAnyPendingChildren(entity);
            }
        }

        private void SpawnAnyPendingChildren(Entity entity)
        {
            if (pendingParentEntitiesByParentId.TryGetValue(entity.Id, out List<Entity> pendingEntities))
            {
                foreach (Entity child in pendingEntities)
                {
                    if (!WasAlreadySpawned(child.Id))
                    {
                        Spawn(child);
                    }
                }

                pendingParentEntitiesByParentId.Remove(entity.Id);
            }
        }

        private void UpdatePosition(WorldEntity entity)
        {
            LargeWorldStreamer.main.cellManager.UnloadBatchCells(entity.AbsoluteEntityCell.CellId.ToUnity()); // Just in case

            Optional<GameObject> opGameObject = NitroxEntity.GetObjectFrom(entity.Id);

            if (!opGameObject.HasValue)
            {
#if DEBUG
                Log.Error($"Entity was already spawned but not found(is it in another chunk?) NitroxId: {entity.Id} TechType: {entity.TechType} ClassId: {entity.ClassId} Transform: {entity.Transform}");
#endif
                return;
            }

            opGameObject.Value.transform.position = entity.Transform.Position.ToUnity();
            opGameObject.Value.transform.rotation = entity.Transform.Rotation.ToUnity();
            opGameObject.Value.transform.localScale = entity.Transform.LocalScale.ToUnity();            
        }

        private void AddPendingParentEntity(Entity entity)
        {
            if (!pendingParentEntitiesByParentId.TryGetValue(entity.ParentId, out List<Entity> pendingEntities))
            {
                pendingEntities = new List<Entity>();
                pendingParentEntitiesByParentId[entity.ParentId] = pendingEntities;
            }

            pendingEntities.Add(entity);
        }

        public bool WasAlreadySpawned(NitroxId id) => alreadySpawnedIds.Contains(id);

        public bool RemoveEntity(NitroxId id) => alreadySpawnedIds.Remove(id);
    }
}
