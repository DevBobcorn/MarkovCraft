using System;

namespace MarkovBlocks.Mapping
{
    /// <summary>
    /// Represents a location into a Minecraft world
    /// </summary>
    public struct Location
    {
        /// <summary>
        /// The X Coordinate
        /// </summary>
        public double X;
        
        /// <summary>
        /// The Y Coordinate (vertical)
        /// </summary>
        public double Y;

        /// <summary>
        /// The Z coordinate
        /// </summary>
        public double Z;

        /// <summary>
        /// Get location with zeroed coordinates
        /// </summary>
        public static Location Zero
        {
            get
            {
                return new Location(0, 0, 0);
            }
        }

        /// <summary>
        /// Create a new location
        /// </summary>
        public Location(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Compare two locations. Locations are equals if the integer part of their coordinates are equals.
        /// </summary>
        /// <param name="obj">Object to compare to</param>
        /// <returns>TRUE if the locations are equals</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is Location)
            {
                return ((int)this.X) == ((int)((Location)obj).X)
                    && ((int)this.Y) == ((int)((Location)obj).Y)
                    && ((int)this.Z) == ((int)((Location)obj).Z);
            }
            return false;
        }

        /// <summary>
        /// Compare two locations. Locations are equals if the integer part of their coordinates are equals.
        /// </summary>
        /// <param name="loc1">First location to compare</param>
        /// <param name="loc2">Second location to compare</param>
        /// <returns>TRUE if the locations are equals</returns>
        public static bool operator ==(Location loc1, Location loc2)
        {
            if (loc1 == null && loc2 == null)
                return true;
            if (loc1 == null || loc2 == null)
                return false;
            return loc1.Equals(loc2);
        }

        /// <summary>
        /// Compare two locations. Locations are not equals if the integer part of their coordinates are not equals.
        /// </summary>
        /// <param name="loc1">First location to compare</param>
        /// <param name="loc2">Second location to compare</param>
        /// <returns>TRUE if the locations are equals</returns>
        public static bool operator !=(Location loc1, Location loc2)
        {
            if (loc1 == null && loc2 == null)
                return false;
            if (loc1 == null || loc2 == null)
                return true;
            return !loc1.Equals(loc2);
        }

        /// <summary>
        /// Sums two locations and returns the result.
        /// </summary>
        /// <exception cref="NullReferenceException">
        /// Thrown if one of the provided location is null
        /// </exception>
        /// <param name="loc1">First location to sum</param>
        /// <param name="loc2">Second location to sum</param>
        /// <returns>Sum of the two locations</returns>
        public static Location operator +(Location loc1, Location loc2)
        {
            return new Location
            (
                loc1.X + loc2.X,
                loc1.Y + loc2.Y,
                loc1.Z + loc2.Z
            );
        }

        /// <summary>
        /// Substract a location to another
        /// </summary>
        /// <exception cref="NullReferenceException">
        /// Thrown if one of the provided location is null
        /// </exception>
        /// <param name="loc1">First location</param>
        /// <param name="loc2">Location to substract to the first one</param>
        /// <returns>Sum of the two locations</returns>
        public static Location operator -(Location loc1, Location loc2)
        {
            return new Location
            (
                loc1.X - loc2.X,
                loc1.Y - loc2.Y,
                loc1.Z - loc2.Z
            );
        }

        /// <summary>
        /// Multiply a location by a scalar value
        /// </summary>
        /// <param name="loc">Location to multiply</param>
        /// <param name="val">Scalar value</param>
        /// <returns>Product of the location and the scalar value</returns>
        public static Location operator *(Location loc, double val)
        {
            return new Location
            (
                loc.X * val,
                loc.Y * val,
                loc.Z * val
            );
        }

        /// <summary>
        /// Divide a location by a scalar value
        /// </summary>
        /// <param name="loc">Location to divide</param>
        /// <param name="val">Scalar value</param>
        /// <returns>Result of the division</returns>
        public static Location operator /(Location loc, double val)
        {
            return new Location
            (
                loc.X / val,
                loc.Y / val,
                loc.Z / val
            );
        }

        /// <summary>
        /// DO NOT USE. Defined to comply with C# requirements requiring a GetHashCode() when overriding Equals() or ==
        /// </summary>
        /// <remarks>
        /// A modulo will be applied if the location is outside the following ranges:
        /// X: -4096 to +4095
        /// Y: -32 to +31
        /// Z: -4096 to +4095
        /// </remarks>
        /// <returns>A simplified version of the location</returns>
        public override int GetHashCode()
        {
            return (((int)X) & ~((~0) << 13)) << 19
                 | (((int)Y) & ~((~0) << 13)) << 13
                 | (((int)Z) & ~((~0) << 06)) << 00;
        }

        /// <summary>
        /// Convert the location into a string representation
        /// </summary>
        /// <returns>String representation of the location</returns>
        public override string ToString()
        {
            return String.Format("X:{0:0.00} Y:{1:0.00} Z:{2:0.00}", X, Y, Z);
        }

    }
}
