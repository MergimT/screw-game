using System;
using System.Collections.Generic;

namespace ScrewGame.Core
{
    /// <summary>Immutable authored level. Treat instances as read-only after loading.</summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public string Id = "";
        public int ContentVersion = 1;
        public string Name = "";
        public string ObjectId = "";
        public string Family = "";
        public int TrayPositions = RulesDefaults.TrayPositions;
        public int TrayCapacity = RulesDefaults.TrayCapacity;
        public int BufferSlots = RulesDefaults.BufferSlots;
        public List<int> TrayQueue = new List<int>();
        public List<PartDefinition> Parts = new List<PartDefinition>();
        public List<ScrewDefinition> Screws = new List<ScrewDefinition>();
        public CameraRange Camera = new CameraRange();
        public LevelMetadata Meta = new LevelMetadata();
    }

    [Serializable]
    public sealed class PartDefinition
    {
        public string Id = "";
        public List<string> Blockers = new List<string>();
        public string Shape = "box";
        public float[] Position = { 0f, 0f, 0f };
        public float[] Rotation = { 0f, 0f, 0f };
        public float[] Size = { 1f, 0.2f, 1f };
        public int Material;
    }

    [Serializable]
    public sealed class ScrewDefinition
    {
        public string Id = "";
        public string PartId = "";
        public int Color;
        /// <summary>World position of the screw head in object space.</summary>
        public float[] Position = { 0f, 0f, 0f };
        /// <summary>Outward axis the screw is removed along.</summary>
        public float[] Normal = { 0f, 1f, 0f };
    }

    [Serializable]
    public sealed class CameraRange
    {
        public float MinYaw = -180f;
        public float MaxYaw = 180f;
        public float MinPitch = 10f;
        public float MaxPitch = 80f;
        public float Distance = 9f;
    }

    [Serializable]
    public sealed class LevelMetadata
    {
        public string IntroducedSkill = "";
        public int DifficultyHypothesis = 1;
        public string LayoutId = "";
        public string ReviewStatus = "unreviewed";
    }

    public static class RulesDefaults
    {
        public const int RulesVersion = 1;
        public const int TrayPositions = 2;
        public const int TrayCapacity = 3;
        public const int BufferSlots = 5;
        public const int MinInitialTrayColors = 2;
        public const int PaletteSize = 8;
    }
}
