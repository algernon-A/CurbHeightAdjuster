using System;
using System.Collections.Generic;
using UnityEngine;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Class to manage changes to buildings (parking lots).
    /// </summary>
    public static class ParkingLots
    {
        // Dictionary of altered parking buildings.
        private readonly static Dictionary<BuildingInfo, ParkingRecord> parkingRecords = new Dictionary<BuildingInfo, ParkingRecord>();

        // Hashset of currently processed network meshes.
        private readonly static HashSet<Mesh> processedMeshes = new HashSet<Mesh>();


        /// <summary>
        /// Curb height adjustment change to make.
        /// </summary>
        private static float HeightAdjustment => RoadHandler.OriginalCurbHeight + RoadHandler.NewCurbHeight;


        /// <summary>
        /// Raises 'parking lot roads' parking lot 'buildings' in line with current curb height settings.
        /// Called via transpiler insert.
        /// </summary>
        public static void RaiseParkingLots()
        {
            Logging.KeyMessage("raising parking lots");

            // Iterate through all networks in list.
            for (uint i = 0; i < PrefabCollection<BuildingInfo>.LoadedCount(); ++i)
            {
                BuildingInfo building = PrefabCollection<BuildingInfo>.GetLoaded(i);

                // Skip any null prefabs.
                if (building?.name == null)
                {
                    continue;
                }

                try
                {
                    // Check for parking lot road prefab prefixes.
                    int periodIndex = building.name.IndexOf(".");
                    if (periodIndex > 0)
                    {
                        string steamID = building.name.Substring(0, periodIndex);

                        // Parking lot roads (including PLR 2).
                        if (steamID.Equals("1285201733") || steamID.Equals("1293870311") || steamID.Equals("1293869603") || steamID.Equals("1969111282") /*building.name.Equals("1969111282.PLR II - 10 Spots with Planters_Data"))*/)
                        {
                            // Found a match - raise the mesh.
                            Logging.KeyMessage("raising Parking Lot Road ", building.name);

                            // Local references.
                            Mesh mesh = building.m_mesh;
                            Vector3[] vertices = mesh.vertices;

                            // Record original vertices.
                            ParkingRecord parkingRecord = new ParkingRecord
                            {
                                vertices = vertices
                            };

                            // Check to see if we've already adjusted this mesh.
                            if (processedMeshes.Contains(mesh))
                            {
                                Logging.KeyMessage("skipping processed mesh");
                                parkingRecord.vertices = null;
                            }
                            else
                            {
                                // Raise mesh.
                                RaiseMesh(mesh);
                                if (RoadHandler.DoLODs)
                                {
                                    RaiseMesh(building.m_lodMesh);
                                }
                            }

                            // Raise props in building.
                            foreach (BuildingInfo.Prop prop in building.m_props)
                            {
                                parkingRecord.propHeights.Add(prop, prop.m_position.y);
                                if (prop.m_position.y < 0)
                                {
                                    prop.m_position.y -= HeightAdjustment;
                                }
                            }

                            // Add original data record to dictionary.
                            parkingRecords.Add(building, parkingRecord);
                        }
                        // Big Parking Lots.
                        else if ((steamID.Equals("2115188517") || steamID.Equals("2121900156") || steamID.Equals("2116510188")) && building.m_props != null)
                        {
                            // Create new parkingRecord with original vertices (which will remain unaltered).
                            ParkingRecord parkingRecord = new ParkingRecord
                            {
                                vertices = building.m_mesh?.vertices
                            };

                            // Raise invisible parking space markers in building.
                            foreach (BuildingInfo.Prop prop in building.m_props)
                            {
                                if (prop.m_prop.name.Equals("Invisible Parking Space"))
                                {
                                    parkingRecord.propHeights.Add(prop, prop.m_position.y);
                                    prop.m_position.y -= HeightAdjustment;
                                }
                            }

                            // If we raised any invisible parking lot markers, add the parkingRecord to our list.
                            if (parkingRecord.propHeights.Count > 0)
                            {
                                // Found a match - raise the mesh.
                                Logging.Message("raised Big Parking Lot ", building.name);
                                parkingRecords.Add(building, parkingRecord);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // Don't let a single failure stop us.
                    Logging.LogException(e, "exception checking building ", building.name);
                }
            }

            Logging.Message("finished raising parking lots");
        }


        /// <summary>
        /// Reverts curb height changes (back to original).
        /// </summary>
        internal static void Revert()
        {
            // Iterate through all parking records in dictionary.
            foreach (KeyValuePair<BuildingInfo, ParkingRecord> buildingEntry in parkingRecords)
            {
                // Skip any null entries (due to skipped shared meshes).
                if (buildingEntry.Value.vertices != null)
                {
                    // Restore building vertices.
                    buildingEntry.Key.m_mesh.vertices = buildingEntry.Value.vertices;

                }

                // Restore prop heights.
                foreach (KeyValuePair<BuildingInfo.Prop, float> propEntry in buildingEntry.Value.propHeights)
                {
                    propEntry.Key.m_position.y = propEntry.Value;
                }
            }
        }


        /// <summary>
        /// Applies updated curb height and mesh changes.
        /// </summary>
        internal static void Apply()
        {
            // Ensure processed mesh list is clear, just in case.
            processedMeshes.Clear();


            // Iterate through all parking records in dictionary.
            {
                foreach (KeyValuePair<BuildingInfo, ParkingRecord> buildingEntry in parkingRecords)
                {
                    // Restore building vertices and then re-adjust mesh.
                    if (buildingEntry.Key.m_mesh != null && buildingEntry.Value.vertices != null)
                    {
                        buildingEntry.Key.m_mesh.vertices = buildingEntry.Value.vertices;
                        RaiseMesh(buildingEntry.Key.m_mesh);
                    }

                    // Adjust prop heights.
                    foreach (KeyValuePair<BuildingInfo.Prop, float> propEntry in buildingEntry.Value.propHeights)
                    {
                        propEntry.Key.m_position.y = propEntry.Value - HeightAdjustment;
                    }
                }
            }

            // Clear processed mesh list once done.
            processedMeshes.Clear();
        }


        /// <summary>
        /// Raises the vertices of the given mesh by the current curb height adjustment.
        /// </summary>
        /// <param name="mesh">Mesh to modify</param>
        private static void RaiseMesh(Mesh mesh)
        {
            // Check if we've already done this one.
            if (processedMeshes.Contains(mesh))
            {
                Logging.KeyMessage("skipping duplicate parking mesh ", mesh.name ?? "null");
                return;
            }

            // Amount to raise up from original height.
            float adjustment = HeightAdjustment;

            // Create new vertices array (changing individual elements within the existing array won't work with locked meshes).
            Vector3[] newVertices = new Vector3[mesh.vertices.Length];
            mesh.vertices.CopyTo(newVertices, 0);

            // Minimum height check, to avoid double-processing vertices.
            float minHeight = 0f;

            // Iterate through all vertices to raise them.
            for (int i = 0; i < newVertices.Length; ++i)
            {
                // If this vertex is lower than our current stored minimum, update the stored minimum height.
                if (newVertices[i].y < minHeight)
                {
                    minHeight = newVertices[i].y;
                }

                // Raise vertex.
                newVertices[i].y -= adjustment;
            }

            // Assign new vertices to mesh if minimum height check was passed (parking lot road parking lot minHeight will be -0.2794001).
            if (minHeight < -0.279)
            {
                mesh.vertices = newVertices;

                // Record mesh as being altered.
                processedMeshes.Add(mesh);
            }
        }
    }
}