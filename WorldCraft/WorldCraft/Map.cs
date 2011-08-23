using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace WorldCraft
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class Map : Microsoft.Xna.Framework.DrawableGameComponent
    {
        #region Properties

        private Game1 _game;
        private Chunk[] _chunks;

        public const short NUM_CHUNKS_WIDTH = 1;
        public const short NUM_CHUNKS_DEPTH = 1;
        public const short NUM_CHUNKS_HEIGHT = 1;
        public const short MAP_WATER_HEIGHT = 25;

        public const float GRAVITY = 2f;

        public VertexBuffer SolidVertexBuffer { get; protected set; }
        public IndexBuffer SolidIndexBuffer { get; protected set; }
        public List<int> SolidIndexList { get; protected set; }
        public List<VertexPositionNormalTexture> SolidVertexList { get; protected set; }

        public int Seed { get; protected set; }
        public Texture2D Texture { get; protected set; }

        public int NumBlocks
        {
            get
            {
                return NUM_CHUNKS_WIDTH * Chunk.WIDTH * NUM_CHUNKS_HEIGHT * Chunk.HEIGHT * NUM_CHUNKS_DEPTH * Chunk.DEPTH;
            }
        }

        public int NumVertices
        {
            get
            {
                int total = 0;
                foreach (var chunk in _chunks)
                {
                    total += chunk.NumVertices;
                }
                return total;
            }
        }

        #endregion

        #region GameComponent

        public Map(Game1 game)
            : base(game)
        {
            _game = game;

            Seed = new Random().Next();

            SolidVertexList = new List<VertexPositionNormalTexture>();
            SolidIndexList = new List<int>();

            _chunks = new Chunk[NUM_CHUNKS_WIDTH * NUM_CHUNKS_DEPTH * NUM_CHUNKS_HEIGHT];
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here

            base.Initialize();
        }

        protected override void LoadContent()
        {
            Texture = _game.Content.Load<Texture2D>("texture");

            GenerateChunks();
            foreach (var chunk in _chunks)
                chunk.Initialize();

            base.LoadContent();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            foreach (Chunk chunk in _chunks)
                chunk.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// Called when the DrawableGameComponent needs to be drawn. Override this method
        //  with component-specific drawing code.
        /// </summary>
        /// <param name="gameTime">Time passed since the last call to Draw.</param>
        public override void Draw(GameTime gameTime)
        {
            foreach (Chunk chunk in _chunks)
                chunk.Draw(gameTime);

            base.Draw(gameTime);
        }

        #endregion

        #region Chunk accessor

        public Chunk GetChunkAt(int mapX, int mapY, int mapZ)
        {
            if (mapX < 0
                || mapY < 0
                || mapZ < 0
                || mapX > NUM_CHUNKS_WIDTH * Chunk.WIDTH - 1
                || mapY > NUM_CHUNKS_HEIGHT * Chunk.HEIGHT - 1
                || mapZ > NUM_CHUNKS_DEPTH * Chunk.DEPTH - 1)
                return null;
            else
                return _chunks[(int)((float)mapX / Chunk.WIDTH) * NUM_CHUNKS_DEPTH * NUM_CHUNKS_HEIGHT
                    + (int)((float)mapZ / Chunk.DEPTH) * NUM_CHUNKS_HEIGHT
                    + (int)((float)mapY / Chunk.HEIGHT)];
        }

        #endregion


        #region Chunks generation

        private void GenerateChunks()
        {
            for (int x = 0; x < NUM_CHUNKS_WIDTH; x++)
                for (int z = 0; z < NUM_CHUNKS_DEPTH; z++)
                {
                    int offset = x * NUM_CHUNKS_DEPTH * NUM_CHUNKS_HEIGHT + z * NUM_CHUNKS_HEIGHT;

                    for (int y = 0; y < NUM_CHUNKS_HEIGHT; y++)
                    {
                        _chunks[offset + y] = Generate2DNoiseChunk(new Vector3(x, y, z));
                    }
                }
        }

        #endregion

        #region Blocks generation

        private Chunk Generate2DNoiseChunk(Vector3 chunkOffset)
        {
            PerlinNoise perlinNoise = new PerlinNoise(_game.Map.Seed);

            int noiseWidth = Map.NUM_CHUNKS_WIDTH * Chunk.WIDTH;
            int noiseDepth = Map.NUM_CHUNKS_DEPTH * Chunk.DEPTH;

            int mapHeight = Map.NUM_CHUNKS_HEIGHT * Chunk.HEIGHT;

            Block[] blocks = new Block[Chunk.WIDTH * Chunk.HEIGHT * Chunk.DEPTH];

            for (int x = 0; x < Chunk.WIDTH; x++)
                for (int z = 0; z < Chunk.DEPTH; z++)
                {
                    int offset = x * Chunk.DEPTH * Chunk.HEIGHT + z * Chunk.HEIGHT;

                    int mapX = (int)chunkOffset.X * Chunk.WIDTH + x;
                    int mapY = (int)chunkOffset.Y * Chunk.HEIGHT;
                    int mapZ = (int)chunkOffset.Z * Chunk.DEPTH + z;

                    float octave1 = (perlinNoise.Noise(2.0f * mapX * 1 / noiseWidth, 2.0f * mapZ * 1 / noiseDepth) + 1) / 2 * 0.7f;
                    float octave2 = (perlinNoise.Noise(4.0f * mapX * 1 / noiseWidth, 4.0f * mapZ * 1 / noiseDepth) + 1) / 2 * 0.2f;
                    float octave3 = (perlinNoise.Noise(8.0f * mapX * 1 / noiseWidth, 8.0f * mapZ * 1 / noiseDepth) + 1) / 2 * 0.1f;

                    float rnd = octave1 + octave2 + octave3;

                    int grassHeight = (int)Math.Floor(rnd * mapHeight) - mapY;

                    int dirtHeight = grassHeight - 4;

                    int curY = 0;

                    for (; curY < (dirtHeight) && curY < Chunk.HEIGHT; curY++)
                        blocks[offset + curY] = new Block(BlockType.Rock);

                    for (; curY < grassHeight && curY < Chunk.HEIGHT; curY++)
                        blocks[offset + curY] = new Block(BlockType.Dirt);

                    if (curY == grassHeight && curY < Chunk.HEIGHT)
                    {
                        blocks[offset + curY] = new Block(BlockType.Grass);
                        curY++;
                    }

                    for (; curY < Chunk.HEIGHT; curY++)
                    {
                        blocks[offset + curY] = new Block(BlockType.None);
                    }

                }

            return new Chunk(_game, chunkOffset, blocks);
        }

        private Chunk Generate3DNoiseChunk(Vector3 chunkOffset)
        {
            int noiseWidth = Map.NUM_CHUNKS_WIDTH * Chunk.WIDTH;
            int noiseDepth = Map.NUM_CHUNKS_DEPTH * Chunk.DEPTH;
            int noiseHeight = Map.NUM_CHUNKS_HEIGHT * Chunk.HEIGHT;

            Block[] blocks = new Block[Chunk.WIDTH * Chunk.HEIGHT * Chunk.DEPTH];

            for (int x = 0; x < Chunk.WIDTH; x++)
                for (int z = 0; z < Chunk.DEPTH; z++)
                {
                    int offset = x * Chunk.DEPTH * Chunk.HEIGHT + z * Chunk.HEIGHT;

                    for (int y = 0; y < Chunk.HEIGHT; y++)
                    {
                        int mapX = (int)chunkOffset.X * Chunk.WIDTH + x;
                        int mapY = (int)chunkOffset.Y * Chunk.HEIGHT + y;
                        int mapZ = (int)chunkOffset.Z * Chunk.DEPTH + z;

                        float octave1 = (SimplexNoise.noise(2.0f * mapX * 1 / noiseWidth, 2.0f * mapY * 1 / noiseHeight, 2.0f * mapZ * 1 / noiseDepth) + 1) / 2 * 0.7f;
                        float octave2 = (SimplexNoise.noise(4.0f * mapX * 1 / noiseWidth, 4.0f * mapY * 1 / noiseHeight, 4.0f * mapZ * 1 / noiseDepth) + 1) / 2 * 0.2f;
                        float octave3 = (SimplexNoise.noise(8.0f * mapX * 1 / noiseWidth, 8.0f * mapY * 1 / noiseHeight, 8.0f * mapZ * 1 / noiseDepth) + 1) / 2 * 0.1f;

                        float rnd = octave1 + octave2 + octave3;

                        if (rnd < 0.5)
                            blocks[offset + y] = new Block(BlockType.Rock);
                        else
                            blocks[offset + y] = new Block(BlockType.None);
                    }

                }

            return new Chunk(_game, chunkOffset, blocks);
        }

        #endregion
    }
}
