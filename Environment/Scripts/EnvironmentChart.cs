﻿using UnityEngine;

namespace KRG
{
    [CreateAssetMenu(
        fileName = "SomeWhere_EnvironmentChart.asset",
        menuName = "KRG Scriptable Object/EnvironmentChart",
        order = 503
    )]
    public class EnvironmentChart : ScriptableObject
    {
        public const string ENVIRONMENT_CHART_SUFFIX = "_EnvironmentChart";

        [Header("Game Object Data")]

        [ReadOnly]
        public string FileName;

        [Enum(typeof(EnvironmentID))]
        public int EnvironmentID;

        [Header("Environment Data")]

        public EnvironmentData Data;

        public string AssetPackBundleName => FileName.ToLower();

        public string BundleName => GetBundleName(EnvironmentID);

        private void OnValidate()
        {
            FileName = name.Replace(ENVIRONMENT_CHART_SUFFIX, "");
        }

        public static string GetBundleName(int environmentID)
        {
            return "_e" + environmentID.ToString("D5");
        }
    }
}
