namespace SEUtilityTools.API.Struct
{
    public struct GPS
    {
        public string Name { get; set; }
        public Vector3 Coordinates { get; set; }

        public GPS(string name, float x, float y, float z)
        {
            Name = name;
            Coordinates = new Vector3(x, y, z);
        }

        public GPS(string name, Vector3 coordinates)
        {
            Name = name;
            Coordinates = coordinates;
        }

        /// <summary>
        /// Returns the GPS coordinates in the Space Engineers format.
        /// </summary>
        public override string ToString()
        {
            return $"GPS:{Name}:{Coordinates.X}:{Coordinates.Y}:{Coordinates.Z}";
        }
    }
}