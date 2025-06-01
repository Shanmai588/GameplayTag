using System;
using UnityEngine;

namespace GameplayTag
{
    /// <summary>
    /// Data structure for tag metadata
    /// </summary>
    [Serializable]
    public class GameplayTagData
    {
        public string tagName;
        public string description;
        public string category;
        public bool isNetworked;
        public Color debugColor = Color.white;
    }

}