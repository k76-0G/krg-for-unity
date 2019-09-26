﻿using UnityEngine;

namespace KRG
{
    [CreateAssetMenu(
        fileName = "SomeOne_CharacterDossier.asset",
        menuName = "KRG Scriptable Object/CharacterDossier",
        order = 304
    )]
    public sealed class CharacterDossier : ScriptableObject
    {
        private const string CHARACTER_DOSSIER_SUFFIX = "_CharacterDossier";
        private const string IDLE_ANIMATION_SUFFIX = "_Idle_RasterAnimation";

        [Header("Game Object Data")]

        [Enum(typeof(CharacterID))]
        public int CharacterID;

        public string FileName;

        public string FullName;

        public CharacterType CharacterType;

        [Header("Character Data")]

        public CharacterData Data;

        [Header("Graphic Data")]

        public GraphicData GraphicData;

        private void OnValidate()
        {
            FileName = name.Replace(CHARACTER_DOSSIER_SUFFIX, "");

            if (string.IsNullOrEmpty(GraphicData.IdleRasterAnimationName))
            {
                GraphicData.IdleRasterAnimationName = FileName + IDLE_ANIMATION_SUFFIX;
            }
        }
    }
}
