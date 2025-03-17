using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SerializableDict : MonoBehaviour
{
    public Dictionary<string, GameObject> dictionary;
    [SerializeField] NewDictItem[] dictionaryHolder;

    private void Awake()
    {
        dictionary = ToDictionary();
    }

    public Dictionary<string, GameObject> ToDictionary()
    {
        Dictionary<string, GameObject> newDict = new();
        foreach (NewDictItem item in dictionaryHolder)
        {
            newDict.Add(item.name, item.obj);
        }
        return newDict;
    }

    [Serializable]
    struct NewDictItem
    {
        [SerializeField] public string name;
        [SerializeField] public GameObject obj;
    }
}




