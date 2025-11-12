using System;

[Serializable]
public class LevelData
{
    public int level;
    public string[] words;
}

[Serializable]
public class VocabDataWrapper
{
    public LevelData[] Levels; 
}