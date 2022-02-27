using System.Collections.Generic;
using UnityEngine;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Class to hold original data for networks (prior to curb height alteration).
    /// </summary>
    public class NetRecord
    {
        // Network surface level.
        public float surfaceLevel;

        // Network segment vertices.
        public Dictionary<NetInfo.Segment, NetComponentRecord> segmentDict = new Dictionary<NetInfo.Segment, NetComponentRecord>();

        // Network node vertices.
        public Dictionary<NetInfo.Node, NetComponentRecord> nodeDict = new Dictionary<NetInfo.Node, NetComponentRecord>();

        // Network lane vertical offsets.
        public Dictionary<NetInfo.Lane, float> laneDict = new Dictionary<NetInfo.Lane, float>();

        // Bridge pillar vertical offsets.
        public bool adjustPillars = false;
        public float bridgePillarOffset = 0f;
        public float middlePillarOffset = 0f;
    } 


    /// <summary>
    /// Struct to hold references to original vertex arrays (main and LOD) and flags to indicate eligibility.
    /// </summary>
    public struct NetComponentRecord
    {
        public NetInfo netInfo;
        public bool eligibleCurbs;
        public bool eligibleBridge;
        public Vector3[] mainVerts;
        public Vector3[] lodVerts;
    }


    /// <summary>
    /// Class to hold original data for parking assets (prior to curb height alteration).
    /// </summary>
    public class ParkingRecord
    {
        // Building mesh vertices.
        public Vector3[] vertices;

        // Prop heights.
        public Dictionary<BuildingInfo.Prop, float> propHeights = new Dictionary<BuildingInfo.Prop, float>();
    }
}