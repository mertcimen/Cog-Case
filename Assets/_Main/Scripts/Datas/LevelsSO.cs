using System.Collections;
using System.Collections.Generic;
using _Main.Scripts.LevelEditor;
using BaseSystems.Scripts.LevelSystem;
using UnityEngine;
using TriInspector;

namespace _Main.Scripts.Data
{
    [CreateAssetMenu(fileName = "Levels", menuName = "Data/Levels")]
    public class LevelsSO : ScriptableObject
    {
        [SerializeField] private List<LevelDataSO> levels = new();

        public List<LevelDataSO> Levels
        {
            get => levels;
            set => levels = value;
        }

        public int Count => levels?.Count ?? 0;

        public LevelDataSO Get(int index)
        {
            if (levels == null || levels.Count == 0) return null;
            if (index < 0 || index >= levels.Count) return null;
            return levels[index];
        }
    }
    [System.Serializable]
    public class LevelData
    {
        [SerializeField] private Level level;
        [SerializeField] private bool isLoopingLevel = true;

        public Level Level
        {
            get => level;
            set => level = value;
        }
        
    }
}