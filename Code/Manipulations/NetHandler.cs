namespace CurbHeightAdjuster
{
    /// <summary>
    /// Class to manage changes to networks.
    /// </summary>
    public static class NetHandler
    {


        /// <summary>
        /// Called on load to scan through all loaded NetInfos, build the database, and apply network manipulations (meshes and lanes).
        /// </summary>
        public static void OnLoad()
        {
            RoadHandler.OnLoad();
            PathHandler.OnLoad();
        }
    }
}