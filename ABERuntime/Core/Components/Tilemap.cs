using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ABEngine.ABERuntime.ECS;
using Halak;
using Veldrid;

namespace ABEngine.ABERuntime.Components
{
    class ChunkTile
    {
        public Transform spriteTrans { get; set; }
        public List<Vector2> square { get; set; }
    }

    internal class CollisionChunk
	{
        public Dictionary<Vector2, ChunkTile> tiles;

        private Tilemap tilemap;
        private Vector2 tileSize;
        public float layer;

        public bool collisionActive;
        public Transform chunkTrans;

        // Tilesize in world coordinates
        internal CollisionChunk(Tilemap tilemap, Vector2 tileSize, float layer, Transform exTrans = null)
        {
            tiles = new Dictionary<Vector2, ChunkTile>();
            this.tilemap = tilemap;
            this.tileSize = tileSize;
            this.layer = layer;

            _squares = new List<List<Vector2>>();
            edgeDistance = tileSize.X;
            points = new List<Vector2>();
            _edges = new HashSet<KeyValuePair<Vector2, Vector2>>();

            if (exTrans == null)
            {
                chunkTrans = EntityManager.CreateEntity("Chunk", "").transform;
                chunkTrans.parent = tilemap.transform;
            }
            else
            {
                chunkTrans = exTrans;
            }

        }

        #region Algorithm

        private List<List<Vector2>> _squares;

        private HashSet<KeyValuePair<Vector2, Vector2>> _edges;

        private List<Vector2> points;

        private float edgeDistance = 1;

        // https://stackoverflow.com/questions/72998996/drawing-the-outermost-boundaries-of-set-of-squares
        // Outer edge algorithm
        private void ComputeEdges()
        {
            // The edges collection is a set of pair of vertex
            _edges = new HashSet<KeyValuePair<Vector2, Vector2>>();
            foreach (var square in _squares)
            {
                // Iterate over the coordinates to compute the edges
                // Using for loop to skip already processed edges
                var squareCount = square.Count;
                for (var i = 0; i < squareCount; i++)
                {
                    // The source vertex
                    var src = square[i];

                    for (var j = 0; j < squareCount; j++)
                    {
                        if (i == j) continue;
                        // The vertex with whom we want to determine if they form and edge
                        var dest = square[j];

                        // Check the distance between them to filter out the diagonal edges
                        if (!(Math.Abs(Vector2.Distance(src, dest) - edgeDistance) < 0.001)) continue;

                        src = src.RoundTo2Dec();
                        dest = dest.RoundTo2Dec();

                        var edge = new KeyValuePair<Vector2, Vector2>(src, dest);

                        // _edges is a set, making it viable to use Contains
                        // even when the collections contains a lot of elements
                        if (_edges.Contains(edge))
                        {
                            // If the edge already exists in the set,
                            // it means its not part of the border
                            _edges.Remove(edge);
                        }
                        else
                        {
                            _edges.Add(edge);
                        }
                    }
                }
            }

            //var ex = new HashSet<KeyValuePair<Vector2, Vector2>>();
            //for (int i = 0; i < _edges.Count; i++)
            //{
            //    var edge = _edges.ElementAt(i);
            //    var edgRev = new KeyValuePair<Vector2, Vector2>(edge.Value, edge.Key);

            //    if (!ex.Contains(edgRev))
            //        ex.Add(edge);
            //}
            //_edges = ex;
        }

        internal bool UpdatePosition(Vector3 clickPos, Vector3 tileWorldPos, ref Vector2 newPivot)
        {
            Vector2 pivotPos = clickPos.RoundTo2Dec().ToVector2();
            if (!tiles.ContainsKey(pivotPos)) // Not clicked on this chunk
                return false;

            Vector2 targPos = tileWorldPos.RoundTo2Dec().ToVector2();

            Vector2 dif = (targPos - pivotPos).RoundTo2Dec();
            if (dif == Vector2.Zero) // No movement
                return false;

            List<Vector2> updatedPoses = new List<Vector2>();
            foreach (var tileKV in tiles)
                updatedPoses.Add((tileKV.Key + dif).RoundTo2Dec());

            bool canMove = tilemap.ValidateChunkMovement(this, updatedPoses, layer);
            if(canMove)
            {
                newPivot = (pivotPos + dif).RoundTo2Dec();
                _squares.Clear();

                Dictionary<Vector2, ChunkTile> newTiles = new Dictionary<Vector2, ChunkTile>();
                int tileInd = 0;
                foreach (var tileKV in tiles)
                {
                    Vector2 newPos = updatedPoses[tileInd++];
                    ChunkTile chunkTile = tileKV.Value;
                    Transform spriteTrans = chunkTile.spriteTrans;

                    ChunkTile newChunkTile = new ChunkTile();
                    if (spriteTrans != null)
                    {
                        spriteTrans.parent = null;

                        Vector3 newPos3 = newPos.ToVector3();
                        newPos3.Z = spriteTrans.localPosition.Z;
                        spriteTrans.localPosition = newPos3;

                        newChunkTile.spriteTrans = spriteTrans;
                    }

                    newChunkTile.square = new List<Vector2>();
                    _squares.Add(newChunkTile.square);

                    for (int i = 0; i < 4; i++)
                        newChunkTile.square.Add(chunkTile.square[i] + dif);


                    newTiles.Add(newPos, newChunkTile);

                }
                tiles.Clear();
                tiles = newTiles;

                ComputeEdges();
                SimplifyShape();

                ResetPivot();

                foreach (var tileKV in tiles)
                {
                    if (tileKV.Value.spriteTrans != null)
                    {
                        tileKV.Value.spriteTrans.parent = chunkTrans;
                    }
                }
            }

            return canMove;
        }

        private void SimplifyShape()
        {
            HashSet<KeyValuePair<Vector2, Vector2>> visited = new HashSet<KeyValuePair<Vector2, Vector2>>();
            points = new List<Vector2>();
            if (_edges.Count > 0)
            {
                // Start from bottom left
                float minX = _edges.OrderBy(e => e.Key.X).First().Key.X;
                var curEdge = _edges.Where(e => e.Key.X == minX).OrderBy(e => e.Key.Y).First();

                var curPoint = curEdge.Key;
                points.Add(curPoint);

                bool pathXChange = false;
                bool pathYChange = false;

                while (true)
                {
                    var paths = _edges.Where(e => e.Key == curPoint);
                    if (paths.Count() == 0)
                        break;

                    bool pathFound = false;
                    KeyValuePair<Vector2, Vector2> curPath = new KeyValuePair<Vector2, Vector2>();
                    foreach (var path in paths)
                    {
                        curPath = path;
                        var pathRev = new KeyValuePair<Vector2, Vector2>(curPath.Value, curPath.Key);
                        if (!visited.Contains(curPath))
                        {
                            pathFound = true;
                            visited.Add(curPath);
                            visited.Add(pathRev);
                            break;
                        }
                    }

                    if (!pathFound)
                        break;

                    Vector2 nextPoint = curPath.Value;

                    bool localXChange = false;
                    bool localYChange = false;
                    if (nextPoint.X != curPoint.X)
                    {
                        localXChange = true;
                        pathXChange = true;
                    }
                    else
                    {
                        localYChange = true;
                        pathYChange = true;
                    }

                    if (pathXChange && pathYChange)
                    {
                        points.Add(curPoint);

                        pathXChange = localXChange;
                        pathYChange = localYChange;
                    }

                    curPoint = nextPoint;

                    if (points.Contains(curPoint))
                        break;
                }
            }

        }

        #endregion

        private void ResetPivot()
        {
            Vector2 mid = Vector2.Zero;
            foreach (var tile in tiles)
                mid += tile.Key;
            mid /= (float)tiles.Count;

            chunkTrans.localPosition = new Vector3(mid.X, mid.Y, chunkTrans.localPosition.Z);
        }

        internal bool AddCollision(Vector2 tilePos, Transform spriteTrans, bool updatePivot = true)
        {
            Vector2 extents = tileSize / 2f;

            var tileCorners = new List<Vector2>
            {
                new Vector2(tilePos.X - extents.X, tilePos.Y - extents.Y),
                new Vector2(tilePos.X - extents.X, tilePos.Y + extents.Y),
                new Vector2(tilePos.X + extents.X, tilePos.Y + extents.Y),
                new Vector2(tilePos.X + extents.X, tilePos.Y - extents.Y),
            };

            ChunkTile chunkTile = new ChunkTile()
            {
                square = tileCorners,
                spriteTrans = spriteTrans
            };

            tiles.Add(tilePos, chunkTile);
            _squares.Add(tileCorners);

            ComputeEdges();
            SimplifyShape();

            if (updatePivot)
            {
                foreach (var tileKV in tiles)
                    tileKV.Value.spriteTrans?.SetParent(null, true);

                ResetPivot();

                foreach (var tileKV in tiles)
                    tileKV.Value.spriteTrans?.SetParent(chunkTrans, true);
            }

            return true;

            //if (points.Count <= 8)
            //    return true;
            //else
            //{
            //    _squares.Remove(tileCorners);
            //    tiles.Remove(tilePos);

            //    _edges = edgesBack;
            //    points = pointsBack;

            //    return false;
            //}
        }

        internal void TilePathSearch(Vector2 tile, Vector2 remTile, List<Vector2> visited)
        {
            if (visited.Contains(tile))
                return;

            visited.Add(tile);

            var adjacents = tile.GetAdjacent(tileSize);
            foreach (var adjacent in adjacents)
            {
                if (adjacent == remTile)
                    continue;

                if (tiles.ContainsKey(adjacent))
                    TilePathSearch(adjacent, remTile, visited);
            }
        }

        internal bool RemoveCollision(Vector2 tilePos)
        {
            // Get neigbors
            List<Vector2> adjacents = tilePos.GetAdjacent(tileSize);
            List<Vector2> neighbors = new List<Vector2>();
            foreach (var adjacent in adjacents)
                if (tiles.ContainsKey(adjacent))
                    neighbors.Add(adjacent);


            if (neighbors.Count > 1)
            {
                List<Vector2> visited = new List<Vector2>();
                visited.Add(tilePos);
                Vector2 start = neighbors[0];
                TilePathSearch(start, tilePos, visited); ;
                if (visited.Count != tiles.Count)
                    return false;
            }

            ChunkTile chunkTile = tiles[tilePos];
            if (chunkTile.spriteTrans != null)
                chunkTile.spriteTrans.parent = tilemap.transform;

            tiles.Remove(tilePos);
            _squares.Remove(chunkTile.square);

            foreach (var tileKV in tiles)
                tileKV.Value.spriteTrans?.SetParent(null, true);

            ResetPivot();

            foreach (var tileKV in tiles)
                tileKV.Value.spriteTrans?.SetParent(chunkTrans, true);

            ComputeEdges();
            SimplifyShape();

           

            return true;
        }

        internal void RemoveSprite(Vector3 worldPos)
        {
            Vector2 tilePos = worldPos.RoundTo2Dec().ToVector2();
            tiles[tilePos].spriteTrans = null;
        }

        internal void AddSprite(Vector3 worldPos, Transform spriteTrans)
        {
            Vector2 tilePos = worldPos.RoundTo2Dec().ToVector2();
            if (tiles[tilePos].spriteTrans != null)
                tiles[tilePos].spriteTrans.parent = tilemap.transform;
            tiles[tilePos].spriteTrans = spriteTrans;
        }


        internal List<Vector2> GetCollisionShape()
		{
            if (tiles.Count == 0)
                return null;

            //edgeDistance = tileSize.X;
            //ComputeEdges();

            List<Vector2> points = new List<Vector2>();
            foreach (var edge in _edges)
            {
                points.Add(edge.Key);
                points.Add(edge.Value);
            }

            return points;
		}

        internal JValue SerializeChunk()
        {
            JsonObjectBuilder jChunk = new JsonObjectBuilder(3000);
            jChunk.Put("Layer",     layer);
            jChunk.Put("TileSize",  tileSize);
            jChunk.Put("ChunkGUID", chunkTrans.entity.Get<Guid>().ToString());

            JsonArrayBuilder tilesArr = new JsonArrayBuilder(2000);
            foreach (var tilePos in tiles.Keys)
                tilesArr.Push(tilePos);

            jChunk.Put("Tiles", tilesArr.Build());

            JsonArrayBuilder pointsArr = new JsonArrayBuilder(2000);

            foreach (var point in points)
            {
                pointsArr.Push(point.X);
                pointsArr.Push(point.Y);
            }

            jChunk.Put("Collision", pointsArr.Build());

            return jChunk.Build();
        }

        internal void Delete()
        {
            points.Clear();
            _edges.Clear();
            _squares.Clear();
            tiles.Clear();
        }
    }

	public class Tilemap : JSerializable
	{
		public Texture2D tileImage { get; set; }
		public Dictionary<Vector3, Tile> tiles { get; set; }

		public Transform transform;
        private string transformGuidStr { get;  set; }

        // Editor scene mappings
        JValue jChunks;
        //Dictionary<Vector3, Transform> placements;
        //Dictionary<Vector3, CollisionChunk> collisionedTiles;

        List<CollisionChunk> collisionChunks;
        Dictionary<float, List<CollisionChunk>> layerChunksDict;

		public Tilemap()
		{
			tileImage = AssetCache.GetDefaultTexture();
			tiles = new Dictionary<Vector3, Tile>();
            collisionChunks = new List<CollisionChunk>();
            layerChunksDict = new Dictionary<float, List<CollisionChunk>>();
		}

        List<int> layerIds;
        internal void ResetRenderLayers()
        {
            layerIds = new List<int>();
            foreach (Tile tile in tiles.Values)
            {
                if (tile.spriteTrans != null)
                {
                    var sprite = tile.spriteTrans.entity.Get<Sprite>();
                    layerIds.Add(sprite.renderLayerIndex);
                    sprite.renderLayerIndex = 0;
                }
            }
        }

        internal void RecoverRenderLayers()
        {
            int listId = 0;
            foreach (Tile tile in tiles.Values)
            {
                if (tile.spriteTrans != null)
                {
                    var sprite = tile.spriteTrans.entity.Get<Sprite>();
                    sprite.renderLayerIndex = layerIds[listId++];
                }
            }
            layerIds.Clear();
        }

        internal void DeleteChunk(CollisionChunk chunk)
        {
            var layerChunks = layerChunksDict[chunk.layer];
            layerChunks.Remove(chunk);
            collisionChunks.Remove(chunk);

            foreach (Vector2 tilePos in chunk.tiles.Keys)
            {
                Vector3 worldPos = new Vector3(tilePos.X, tilePos.Y, chunk.layer);

                if(tiles.TryGetValue(worldPos, out Tile tile))
                {
                    if (tile.spriteTrans != null)
                        tile.spriteTrans.entity.DestroyEntity();
                }
                tiles.Remove(worldPos);
            }

            chunk.Delete();
        }

        internal CollisionChunk GetSelectionChunk(Vector3 tileWorldPos)
        {
            tileWorldPos = tileWorldPos.RoundTo2Dec();

            if(tiles.TryGetValue(tileWorldPos, out Tile tile))
                return tile.chunk;

            return null;
        }

        internal CollisionChunk RemoveCollision(Vector3 tileWorldPos)
        {
            tileWorldPos = tileWorldPos.RoundTo2Dec();

            Tile tile = null;
            if (tiles.TryGetValue(tileWorldPos, out tile))
            {
                if(tile.chunk == null)
                    return null;
            }
            else
                return null;

            List<CollisionChunk> layerChunks = null;
            if (!layerChunksDict.TryGetValue(tileWorldPos.Z, out layerChunks))
            {
                return null;
            }

            CollisionChunk chunk = tile.chunk;
            Vector2 tilePos = tileWorldPos.ToVector2();
            bool removed = tile.chunk.RemoveCollision(tilePos);
            if (!removed)
                return null;

            if (chunk.tiles.Count == 0)
            {
                layerChunks.Remove(chunk);
                collisionChunks.Remove(chunk);
            }
            if (layerChunks.Count == 0)
                layerChunksDict.Remove(tileWorldPos.Z);

            if (tile.spriteTrans == null)
                tiles.Remove(tileWorldPos);
            else
                tile.chunk = null;
            return chunk;
        }

        internal CollisionChunk AddCollision(Vector3 tileWorldPos)
        {
            tileWorldPos = tileWorldPos.RoundTo2Dec();
            Tile tile = null;

            // Do nothing if collision exists
            if (tiles.TryGetValue(tileWorldPos, out tile))
            {
                if (tile.chunk != null)
                    return null;
            }
            else
            {
                tile = new Tile(tileWorldPos);
                tiles.Add(tileWorldPos, tile);
            }

            // No chunk list for this layer. Create one
            List<CollisionChunk> layerChunks = null;
            if (!layerChunksDict.TryGetValue(tileWorldPos.Z, out layerChunks))
            {
                layerChunks = new List<CollisionChunk>();
                layerChunksDict.Add(tileWorldPos.Z, layerChunks);
            }

            Transform spriteTrans = tile.spriteTrans;

            Vector2 tilePos = tileWorldPos.ToVector2();
            Vector2 tileSize = tileImage.spriteSize.PixelToWorld();

            // Find neigbors start from left
            List<Vector2> neighbors = tilePos.GetAdjacent(tileSize);

            // Check if neigbors in a chunk
            CollisionChunk exChunk = null;
            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector2 neighbor = neighbors[i];
                foreach (var chunk in layerChunks)
                {
                    if (chunk.tiles.ContainsKey(neighbor))
                    {
                        if (chunk.AddCollision(tilePos, spriteTrans)) // No problem
                        {
                            exChunk = chunk;
                            tile.chunk = chunk;
                            break;
                        }
                    }
                }

                if (exChunk != null)
                    return exChunk;
            }

            // No suitable chunk found
            exChunk = new CollisionChunk(this, tileImage.spriteSize.PixelToWorld(), tileWorldPos.Z);
            collisionChunks.Add(exChunk);
            layerChunksDict[tileWorldPos.Z].Add(exChunk);

            exChunk.AddCollision(tilePos, spriteTrans);
            tile.chunk = exChunk;
            return exChunk;
        }

		internal bool HasPlacement(Vector3 worldPos)
		{
            if (tiles.TryGetValue(worldPos, out Tile tile))
            {
                if(tile.spriteTrans != null)
                    return true;
            }

			return false;
		}

		internal int GetPlacementSpriteID(Vector3 worldPos)
		{
            if (tiles.TryGetValue(worldPos, out Tile tile))
            {
                if (tile.spriteTrans != null)
                    return tile.spriteTrans.entity.Get<Sprite>().GetSpriteID();
            }

			return -1;
		}

		internal void AddTile(Transform sprite, int spriteID)
		{
			Vector3 pos = sprite.worldPosition.RoundTo2Dec();

            CollisionChunk chunk = null;
            if (tiles.TryGetValue(pos, out Tile oldTile))
			{
                chunk = oldTile.chunk;
				RemoveTile(pos);
			}

            Tile newTile = new Tile(pos)
            {
                spriteId = spriteID,
                spriteTrans = sprite
            };

            if (chunk != null)
            {
                newTile.chunk = chunk;
                chunk.AddSprite(pos, sprite);
                tiles[pos] = newTile;
            }
            else
                tiles.Add(pos, newTile);
        }

		internal void RemoveTile(Vector3 worldPos)
		{
            if (tiles.TryGetValue(worldPos, out Tile tile))
            {
                if (tile.spriteTrans == null)
                    return;

                EntityManager.DestroyEntity(tile.spriteTrans.entity);
                tile.spriteTrans = null;

                if(tile.chunk == null)
                    tiles.Remove(worldPos);
                else
                    tile.chunk.RemoveSprite(worldPos);
            }
        }

        internal bool ValidateChunkMovement(CollisionChunk chunk, List<Vector2> poses, float layer)
        {
            foreach (var pose in poses)
            {
                if (chunk.tiles.ContainsKey(pose))
                    continue;

                Vector3 worldPos = new Vector3(pose.X, pose.Y, layer);
                if (tiles.ContainsKey(worldPos))
                    return false;
            }

            // Handle data alterations here
            Dictionary<Vector3, Tile> tempMap = new Dictionary<Vector3, Tile>();

            // Remove all
            foreach (var cTile in chunk.tiles)
            {
                Vector3 worldPos = new Vector3(cTile.Key.X, cTile.Key.Y, layer);

                if(tiles.ContainsKey(worldPos))
                {
                    tempMap.Add(worldPos, tiles[worldPos]);
                    tiles.Remove(worldPos);
                }
            }

            // Add new
            int tileInd = 0;
            foreach (var cTile in chunk.tiles)
            {
                Vector3 oldWorldPos = new Vector3(cTile.Key.X, cTile.Key.Y, layer);
                Vector2 newPos = poses[tileInd++];
                Vector3 newWorldPos = new Vector3(newPos.X, newPos.Y, layer);

                Tile tile = tempMap[oldWorldPos];
                tile.worldPos = newWorldPos;
                tiles.Add(newWorldPos, tile);
            }

            return true;
        }

		internal List<Transform> GetAllSprites()
		{
            List<Transform> sprites = new List<Transform>();
            foreach (Tile tile in tiles.Values)
            {
                if (tile.spriteTrans != null)
                    sprites.Add(tile.spriteTrans);
            }

            return sprites;
		}

        internal List<CollisionChunk> GetAllChunks()
        {
            return collisionChunks;
        }

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(20000);
            jObj.Put("type", GetType().ToString());
            //jObj.Put("TransformGuid", transform.entity.Get<Guid>().ToString());
            jObj.Put("TileImage", AssetCache.GetAssetSceneIndex(this.tileImage.fPathHash));

            JsonArrayBuilder tilesArr = new JsonArrayBuilder(10000);
			foreach (var tile in tiles.Values)
			{
                JsonObjectBuilder tileObj = new JsonObjectBuilder(200);
                tileObj.Put("TilePos", tile.worldPos);
                tileObj.Put("SpriteID", tile.spriteId);

                if(tile.spriteTrans != null)
                    tileObj.Put("TransGUID", tile.spriteTrans.entity.Get<Guid>().ToString());
                else
                    tileObj.Put("TransGUID", "");

                if (tile.chunk != null)
                    tileObj.Put("ChunkGUID", tile.chunk.chunkTrans.entity.Get<Guid>().ToString());
                else
                    tileObj.Put("ChunkGUID", "");

                tilesArr.Push(tileObj.Build());
            }

			jObj.Put("Tiles", tilesArr.Build());

            JsonArrayBuilder chunksArr = new JsonArrayBuilder(5000);
            foreach (var chunk in collisionChunks)
            {
                chunksArr.Push(chunk.SerializeChunk());
            }

            jObj.Put("Chunks", chunksArr.Build());

            return jObj.Build();
        }

        public void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);

            //transformGuidStr = data["TransformGuid"];

            int assetSceneIndex = data["TileImage"];
            tileImage = AssetCache.GetAssetFromSceneIndex(assetSceneIndex) as Texture2D;
            if (tileImage == null)
                tileImage = AssetCache.GetDefaultTexture();


            foreach (var jTile in data["Tiles"].Array())
            {
                string transGuid = jTile["TransGUID"];
                string chunkGuid = jTile["ChunkGUID"];
                int spriteID = jTile["SpriteID"];
                Vector3 tileWorldPos = jTile["TilePos"];

                Tile tile = new Tile(tileWorldPos)
                {
                    transformGuid = transGuid,
                    chunkGuid = chunkGuid,
                    spriteId = spriteID
                };

                tiles.Add(tileWorldPos, tile);
            }

            if (Game.gameMode == GameMode.Runtime) // Chunk collision for game
            {
                foreach (var jChunk in data["Chunks"].Array())
                {
                    Vector2 curPoint = Vector2.Zero;
                    List<Vector2> colliderPoints = new List<Vector2>();

                    int index = 0;

                    foreach (var axis in jChunk["Collision"].Array())
                    {
                        if (index % 2 == 0)
                            curPoint.X = axis;
                        else
                        {
                            curPoint.Y = axis;
                            colliderPoints.Add(curPoint);
                        }

                        index++;
                    }

                    Rigidbody rb = new Rigidbody();
                    PolygonCollider collider = new PolygonCollider(colliderPoints);
                    var spriteEnt = EntityManager.CreateEntity("TilemapCollider", "EditorCollider", rb, collider, true);
                }
            }
            else // Save chunk tiles data for later use in Editor
            {
                jChunks = data["Chunks"];
            }
        }

        public void SetReferences()
        {
            // Transform reference
            //var transGuid = Guid.Parse(transformGuidStr);
            //Transform trans = Game.GameWorld.GetEntities().FirstOrDefault(e => e.Get<Guid>().Equals(transGuid)).transform;

            if (Game.gameMode == GameMode.Editor)
            {
                foreach (var tile in tiles.Values)
                {
                    if (!string.IsNullOrEmpty(tile.transformGuid))
                    {
                        var transGuid = Guid.Parse(tile.transformGuid);
                        Transform trans = Game.GameWorld.GetEntities().FirstOrDefault(e => e.Get<Guid>().Equals(transGuid)).transform;
                        if (trans != null)
                            tile.spriteTrans = trans;
                    }
                }

                foreach (var jChunk in jChunks.Array())
                {
                    Vector2 tileSize = jChunk["TileSize"];
                    float layer = jChunk["Layer"];
                    Guid chunkGuid = Guid.Parse(jChunk["ChunkGUID"]);
                    Transform trans = Game.GameWorld.GetEntities().FirstOrDefault(e => e.Get<Guid>().Equals(chunkGuid)).transform;

                    CollisionChunk chunk = new CollisionChunk(this, tileSize, layer, trans);
                    collisionChunks.Add(chunk);

                    if (layerChunksDict.ContainsKey(layer))
                        layerChunksDict[layer].Add(chunk);
                    else
                        layerChunksDict.Add(layer, new List<CollisionChunk>() { chunk });

                    foreach (Vector2 jTile in jChunk["Tiles"].Array())
                    {
                        Vector3 tileWorldPos = new Vector3(jTile.X, jTile.Y, layer);
                        if(tiles.TryGetValue(tileWorldPos, out Tile tile))
                        {
                           
                            if(chunk.AddCollision(jTile, tile.spriteTrans, false))
                                tile.chunk = chunk;
                        }

                    }
                }
            }


			//foreach (var spriteKV in placements)
			//{
			//	spriteKV.Value.parent = transform;
			//}
		}

        public JSerializable GetCopy()
        {
            throw new NotImplementedException();
        }

		public void SetTransform(Transform transform)
		{
			this.transform = transform;
		}
    }

	public class Tile
	{
		public Vector3 worldPos { get; set; }
        public Transform spriteTrans { get; set; }
        public int spriteId { get; set; }
        internal string transformGuid { get; set; }

        internal CollisionChunk chunk { get; set; }
        internal string chunkGuid { get; set; }

        public Tile(Vector3 worldPos)
		{
			this.worldPos = worldPos;
		    spriteId = -1;
            spriteTrans = null;
            transformGuid = null;

            chunk = null;
            chunkGuid = null;
		}
	}
}

