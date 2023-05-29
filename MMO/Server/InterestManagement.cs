using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporNetcode;

namespace VaporMMO
{
    public class InterestManagement
    {
        public int ViewRange { get; set; } = 90;
        public float RebuildInterval { get; set; } = 1.2f;
        public int Resolution => ViewRange / 3;

        private readonly ServerWorldModule _module;
        private readonly int navigationLayers;
        private double lastRebuildTime;
        private readonly HashSet<IServerIdentity> newObservers = new();
        // begin with a large capacity to avoid resizing & allocations.
        private readonly Dictionary<int, SpatialMap<IServerIdentity>> _spatialGrids = new(3);
        private Vector2Int ProjectToGrid(Vector3 position) => Vector2Int.RoundToInt(new Vector2(position.x, position.z) / Resolution);

        public InterestManagement(ServerWorldModule module, int navLayers, int viewRange, float rebuildInterval)
        {
            _module = module;
            ViewRange = viewRange;
            RebuildInterval = rebuildInterval;
            navigationLayers = navLayers;

            for (int i = 0; i < navLayers; i++)
            {
                int layer = i;
                _spatialGrids.Add(layer, new SpatialMap<IServerIdentity>(1024));
            }

            lastRebuildTime = Time.timeAsDouble - RebuildInterval; // Force a build immediatly.
        }

        public void Tick()
        {
            for (int i = 0; i < navigationLayers; i++)
            {
                _spatialGrids[i].ClearNonAlloc();
            }

            // put every connection into the grid at it's main player's position
            // NOTE: player sees in a radius around him. NOT around his pet too.
            foreach (var player in _module.Players.Values)
            {
                // authenticated and joined world with a player?
                if (player.Peer.IsAuthenticated)
                {
                    // calculate current grid position
                    Vector2Int position = ProjectToGrid(player.Position);

                    // put into grid
                    _spatialGrids[player.NavigationLayer].Add(position, player);
                }
            }

            // rebuild all spawned entities' observers every 'interval'
            // this will call OnRebuildObservers which then returns the
            // observers at grid[position] for each entity.
            if (Time.timeAsDouble >= lastRebuildTime + RebuildInterval)
            {
                RebuildAll();
                lastRebuildTime = Time.timeAsDouble;
            }
        }

        public void RebuildAll()
        {
            //foreach (var identity in _module.Players.Values)
            //{
            //    Rebuild(identity, false);
            //}

            foreach (var identity in _module.Entities.Values)
            {
                Rebuild(identity, false);
            }
        }

        public void Rebuild(IServerIdentity entity, bool initialize)
        {
            // clear newObservers hashset before using it
            newObservers.Clear();

            // not force hidden?
            OnRebuildObservers(entity, newObservers);

            // IMPORTANT: AFTER rebuilding add own player connection in any case
            // to ensure player always sees himself no matter what.
            // -> OnRebuildObservers might clear observers, so we need to add
            //    the player's own connection AFTER. 100% fail safe.
            // -> fixes https://github.com/vis2k/Mirror/issues/692 where a
            //    player might teleport out of the ProximityChecker's cast,
            //    losing the own connection as observer.
            if (entity.IsPeer)
            {
                newObservers.Add(entity);
            }

            bool changed = false;

            // add all newObservers that aren't in .observers yet
            foreach (var player in newObservers)
            {
                // only add ready connections.
                // otherwise the player might not be in the world yet or anymore
                if (player != null && player.IsReady)
                {
                    if (initialize || !entity.Observers.ContainsKey(player.Peer.ConnectionID))
                    {
                        // new observer
                        player.AddToObserving(entity);
                        changed = true;
                    }
                }
            }

            // remove all old observers that aren't in newObservers anymore
            foreach (var player in entity.Observers.Values)
            {
                if (!newObservers.Contains(player))
                {
                    // removed observer
                    player.RemoveFromObserving(entity);
                    changed = true;
                }
            }

            // copy new observers to observers
            if (changed)
            {
                entity.Observers.Clear();
                foreach (var player in newObservers)
                {
                    if (player != null && player.IsReady)
                    {
                        entity.Observers.Add(player.Peer.ConnectionID, player);
                    }
                }
            }
        }

        private void OnRebuildObservers(IServerIdentity entity, HashSet<IServerIdentity> newObservers)
        {
            // add everyone in 9 neighbour grid
            // -> pass observers to GetWithNeighbours directly to avoid allocations
            //    and expensive .UnionWith computations.
            Vector2Int current = ProjectToGrid(entity.Position);
            _spatialGrids[entity.NavigationLayer].GetWithNeighbours(current, newObservers);
        }
    }
}
