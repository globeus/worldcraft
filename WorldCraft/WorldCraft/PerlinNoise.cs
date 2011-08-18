﻿#region Using Statements

using System;
using Microsoft.Xna.Framework;

#endregion

namespace WorldCraft
{
    /// <summary>
    /// This is a class able to generate pseudo coherent random noise based on Ken Perlin's algorithm
    /// <see cref="http://mrl.nyu.edu/~perlin/noise/"/>
    /// <seealso cref="http://www.siafoo.net/snippet/144"/>
    /// </summary>
    public sealed class PerlinNoise
    {
        #region Constants

        /// <summary>
        /// Stores the size of the random value array
        /// </summary>
        private int RANDOM_SIZE = 256;

        #endregion

        #region Fields

        /// <summary>
        /// Stores the random values used to generate the noise
        /// </summary>
        private readonly int[] values;

        #endregion

        /// <summary>
        /// Initializes a new instance of the PerlinNoise class.
        /// </summary>
        /// <param name="seed">The seed used to generate the random values</param>
        public PerlinNoise(int seed)
        {
            // Initialize the random values array
            this.values = new int[RANDOM_SIZE*2];

            // Initialize the random generator
            Random generator = new Random(seed);

            // Initialize an array for storing 256 random values
            byte[] source = new byte[RANDOM_SIZE];

            // Generate 256 random byte values
            generator.NextBytes(source);

            // Copy the source data twice to the generator array
            for (int i = 0; i < RANDOM_SIZE; i++)
                this.values[i+RANDOM_SIZE] = this.values[i] = source[i];
        }

        /// <summary>
        /// Generates a one-dimensional noise
        /// </summary>
        /// <param name="x">The entry value for the noise</param>
        /// <returns>A value between [-1;1] representing the noise amount</returns>
        public float Noise(float x)
        {
            // Compute the cell coordinates
            int X = (int)Math.Floor(x) & 255;

            // Retrieve the decimal part of the cell
            x -= (float)Math.Floor(x);

            float u = Fade(x);

            return Lerp(u, Grad(this.values[X], x), Grad(this.values[X+1], x - 1));
        }

        /// <summary>
        /// Generates a bi-dimensional noise
        /// </summary>
        /// <param name="x">The X entry value for the noise</param>
        /// <param name="y">The Y entry value for the noise</param>
        /// <returns>A value between [-1;1] representing the noise amount</returns>
        public float Noise(float x, float y)
        {
            // Compute the cell coordinates
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;

            // Retrieve the decimal part of the cell
            x -= (float)Math.Floor(x);
            y -= (float)Math.Floor(y);

            // Smooth the curve
            float u = Fade(x);
            float v = Fade(y);

            // Fetch some randoms values from the table
            int A = this.values[X] + Y;
            int B = this.values[X + 1] + Y;

            // Interpolate between directions 
            return Lerp(v, Lerp(u, Grad(this.values[A], x, y),
                                   Grad(this.values[B], x - 1, y)),
                           Lerp(u, Grad(this.values[A + 1], x, y - 1),
                                   Grad(this.values[B + 1], x - 1, y - 1)));
        }

        /// <summary>
        /// Generates a tri-dimensional noise
        /// </summary>
        /// <param name="x">The X entry value for the noise</param>
        /// <param name="y">The Y entry value for the noise</param>
        /// <param name="z">The Z entry value for the noise</param>
        /// <returns>A value between [-1;1] representing the noise amount</returns>
        public float Noise(float x, float y, float z)
        {
            // Compute the cell coordinates
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;
            int Z = (int)Math.Floor(z) & 255;

            // Retrieve the decimal part of the cell
            x -= (float)Math.Floor(x);
            y -= (float)Math.Floor(y);
            z -= (float)Math.Floor(z);

            // Smooth the curve
            float u = Fade(x);
            float v = Fade(y);
            float w = Fade(z);

            // Fetch some randoms values from the table
            int A = this.values[X] + Y;
            int AA = this.values[A] + Z;
            int AB = this.values[A + 1] + Z;
            int B = this.values[X + 1] + Y;
            int BA = this.values[B] + Z;
            int BB = this.values[B + 1] + Z;

            // Interpolate between directions
            return Lerp(w, Lerp(v, Lerp(u, Grad(this.values[AA], x, y, z),
                                           Grad(this.values[BA], x - 1, y, z)),
                                   Lerp(u, Grad(this.values[AB], x, y - 1, z),
                                           Grad(this.values[BB], x - 1, y - 1, z))),
                           Lerp(v, Lerp(u, Grad(this.values[AA + 1], x, y, z - 1),
                                           Grad(this.values[BA + 1], x - 1, y, z - 1)),
                                   Lerp(u, Grad(this.values[AB + 1], x, y - 1, z - 1),
                                           Grad(this.values[BB + 1], x - 1, y - 1, z - 1))));
        }

#if XNA
        #region XNA specific methods
        
        /// <summary>
        /// Generates a bi-dimensional noise
        /// </summary>
        /// <param name="value">The vector coordinates of the entry value for the noise</param>
        /// <returns>A value between [-1;1] representing the noise amount</returns>
        public float Noise(Vector2 value)
        {
            return Noise(value.X, value.Y);
        }

        /// <summary>
        /// Generates a tri-dimensional noise
        /// </summary>
        /// <param name="value">The vector coordinates of the entry value for the noise</param>
        /// <returns>A value between [-1;1] representing the noise amount</returns>
        public float Noise(Vector3 value)
        {
            return Noise(value.X, value.Y, value.Z);
        }

        #endregion
#endif

        #region Private helpers methods

        /// <summary>
        /// Smooth the entry value
        /// </summary>
        /// <param name="t">The entry value</param>
        /// <returns>The smoothed value</returns>
        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        /// <summary>
        /// Interpolate linearly between A and B.
        /// </summary>
        /// <param name="t">The amount of the interpolation</param>
        /// <param name="a">The starting value</param>
        /// <param name="b">The ending value</param>
        /// <returns>The interpolated value between A and B</returns>
        private static float Lerp(float t, float a, float b)
        {
            return a + t * (b - a);
        }

        /// <summary>
        /// Modifies the result by adding a directional bias
        /// </summary>
        /// <param name="hash">The random value telling in which direction the bias will occur</param>
        /// <param name="x">The amount of the bias on the X axis</param>
        /// <returns>The directional bias strength</returns>
        private static float Grad(int hash, float x)
        {
            // Result table
            // ---+------+----
            //  0 | 0000 |  x 
            //  1 | 0001 | -x 

            return (hash & 1) == 0 ? x : -x;
        }

        /// <summary>
        /// Modifies the result by adding a directional bias
        /// </summary>
        /// <param name="hash">The random value telling in which direction the bias will occur</param>
        /// <param name="x">The amount of the bias on the X axis</param>
        /// <param name="y">The amount of the bias on the Y axis</param>
        /// <returns>The directional bias strength</returns>
        private static float Grad(int hash, float x, float y)
        {
            // Fetch the last 3 bits
            int h = hash & 3;

            // Result table for U
            // ---+------+---+------
            //  0 | 0000 | x |  x
            //  1 | 0001 | x |  x
            //  2 | 0010 | x | -x
            //  3 | 0011 | x | -x

            float u = (h & 2) == 0 ? x : -x;

            // Result table for V
            // ---+------+---+------
            //  0 | 0000 | y |  y
            //  1 | 0001 | y | -y
            //  2 | 0010 | y |  y
            //  3 | 0011 | y | -y

            float v = (h & 1) == 0 ? y : -y;

            // Result table for U + V
            // ---+------+----+----+--
            //  0 | 0000 |  x |  y |  x + y
            //  1 | 0001 |  x | -y |  x - y
            //  2 | 0010 | -x |  y | -x + y
            //  3 | 0011 | -x | -y | -x - y

            return u + v;
        }

        /// <summary>
        /// Modifies the result by adding a directional bias
        /// </summary>
        /// <param name="hash">The random value telling in which direction the bias will occur</param>
        /// <param name="x">The amount of the bias on the X axis</param>
        /// <param name="y">The amount of the bias on the Y axis</param>
        /// <param name="z">The amount of the bias on the Z axis</param>
        /// <returns>The directional bias strength</returns>
        private static float Grad(int hash, float x, float y, float z)
        {
            // Fetch the last 4 bits
            int h = hash & 15;

            // Result table for U
            // ---+------+---+------
            //  0 | 0000 | x |  x
            //  1 | 0001 | x | -x
            //  2 | 0010 | x |  x
            //  3 | 0011 | x | -x
            //  4 | 0100 | x |  x
            //  5 | 0101 | x | -x
            //  6 | 0110 | x |  x
            //  7 | 0111 | x | -x
            //  8 | 1000 | y |  y
            //  9 | 1001 | y | -y
            // 10 | 1010 | y |  y
            // 11 | 1011 | y | -y
            // 12 | 1100 | y |  y
            // 13 | 1101 | y | -y
            // 14 | 1110 | y |  y
            // 15 | 1111 | y | -y

            float u = h < 8 ? x : y;

            // Result table for V
            // ---+------+---+------
            //  0 | 0000 | y |  y
            //  1 | 0001 | y |  y
            //  2 | 0010 | y | -y
            //  3 | 0011 | y | -y
            //  4 | 0100 | z |  z
            //  5 | 0101 | z |  z
            //  6 | 0110 | z | -z
            //  7 | 0111 | z | -z
            //  8 | 1000 | z |  z
            //  9 | 1001 | z |  z
            // 10 | 1010 | z | -z
            // 11 | 1011 | z | -z
            // 12 | 1100 | x |  x
            // 13 | 1101 | z |  z
            // 14 | 1110 | x | -x
            // 15 | 1111 | z | -z

            float v = h < 4 ? y : h == 12 || h == 14 ? x : z;

            // Result table for U+V
            // ---+------+----+----+-------
            //  0 | 0000 |  x |  y |  x + y
            //  1 | 0001 | -x |  y | -x + y
            //  2 | 0010 |  x | -y |  x - y
            //  3 | 0011 | -x | -y | -x - y
            //  4 | 0100 |  x |  z |  x + z
            //  5 | 0101 | -x |  z | -x + z
            //  6 | 0110 |  x | -z |  x - z
            //  7 | 0111 | -x | -z | -x - z
            //  8 | 1000 |  y |  z |  y + z
            //  9 | 1001 | -y |  z | -y + z
            // 10 | 1010 |  y | -z |  y - z
            // 11 | 1011 | -y | -z | -y - z

            // The four last results already exists in the table before
            // They are doubled because you must get a result for all
            // 4-bit combinaisons values.

            // 12 | 1100 |  y |  x |  y + x
            // 13 | 1101 | -y |  z | -y + z
            // 14 | 1110 |  y | -x |  y - x
            // 15 | 1111 | -y | -z | -y - z

            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        #endregion
    }
}
