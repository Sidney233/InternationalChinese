using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataStructure
{
    [Serializable]
    public class Dialogue
    {
        public string character;
        public string line;
        public string translation;
    }
    [Serializable]
    public class MultipleChoice
    {
        public string question;
        public string translation;
        public Option option;
        public char answer;
        public string tts_path;
    }
    [Serializable]
    public class Option
    {
        public string A;
        public string B;
        public string C;
        public string D;
    }
    [Serializable]
    public class NewWord
    {
        public string word;
        public string pronunciation;
        public string meaning;
        public string partOfSpeech;
        public string audio;
    }
    [Serializable]
    public class Reading
    {
        public int id;
        public string sentence;
        public string pinyin;
        public string English;
        public string tts_path;
    }
    [Serializable]
    public class Writing
    {
        public int id;
        public string chinese_character;
        public string pinyin;
        public string guide;
        public string definition;
    }
    [Serializable]
    public class FillBlank
    {
        public int id;
        public string question;
        public string pinyin;
        public string English;
        public string answer;
        public string tts_path;
    }
}
