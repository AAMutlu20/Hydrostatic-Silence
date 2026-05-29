using System;
using System.Collections.Generic;
using DialogueSystem.Scripts.Data.Supporting.Enums;
using UnityEngine;

namespace DialogueSystem.Scripts.Runtime.Variables
{
    /// <summary>
    /// Runtime store for all dialogue variables (bool, int, float, string).
    /// Not a MonoBehaviour — owned by DialogueManager.
    /// </summary>
    public class VariableStore
    {
        private Dictionary<string, bool>   boolVariables   = new Dictionary<string, bool>();
        private Dictionary<string, int>    intVariables    = new Dictionary<string, int>();
        private Dictionary<string, float>  floatVariables  = new Dictionary<string, float>();
        private Dictionary<string, string> stringVariables = new Dictionary<string, string>();

        // ===== SETTERS =====

        public void SetBool(string key, bool value)   => boolVariables[key]   = value;
        public void SetInt(string key, int value)     => intVariables[key]    = value;
        public void SetFloat(string key, float value) => floatVariables[key]  = value;
        public void SetString(string key, string value) => stringVariables[key] = value;

        // ===== GETTERS =====

        public bool   GetBool(string key, bool defaultValue = false)     => boolVariables.TryGetValue(key, out var v)   ? v : defaultValue;
        public int    GetInt(string key, int defaultValue = 0)           => intVariables.TryGetValue(key, out var v)    ? v : defaultValue;
        public float  GetFloat(string key, float defaultValue = 0f)      => floatVariables.TryGetValue(key, out var v)  ? v : defaultValue;
        public string GetString(string key, string defaultValue = "")    => stringVariables.TryGetValue(key, out var v) ? v : defaultValue;

        // ===== ARITHMETIC HELPERS =====

        public void AddInt(string key, int amount)      => intVariables[key]   = GetInt(key) + amount;
        public void SubtractInt(string key, int amount) => intVariables[key]   = GetInt(key) - amount;
        public void MultiplyInt(string key, int amount) => intVariables[key]   = GetInt(key) * amount;
        public void DivideInt(string key, int amount)
        {
            if (amount == 0) { Debug.LogWarning($"[VariableStore] Divide by zero on '{key}'"); return; }
            intVariables[key] = GetInt(key) / amount;
        }

        // ===== GENERIC APPLY (used by SetVariableNode runner) =====

        public void Apply(string varName, VariableType type, Data.Supporting.Enums.VariableOperation op,
                          Data.Supporting.Classes.VariableValue val)
        {
            switch (type)
            {
                case VariableType.Bool:
                    SetBool(varName, val.boolValue);
                    break;
                case VariableType.Int:
                    int cur = GetInt(varName);
                    int result = op switch
                    {
                        Data.Supporting.Enums.VariableOperation.Set      => val.intValue,
                        Data.Supporting.Enums.VariableOperation.Add      => cur + val.intValue,
                        Data.Supporting.Enums.VariableOperation.Subtract => cur - val.intValue,
                        Data.Supporting.Enums.VariableOperation.Multiply => cur * val.intValue,
                        Data.Supporting.Enums.VariableOperation.Divide   => val.intValue != 0 ? cur / val.intValue : cur,
                        _ => val.intValue
                    };
                    SetInt(varName, result);
                    break;
                case VariableType.Float:
                    float curf = GetFloat(varName);
                    float resultf = op switch
                    {
                        Data.Supporting.Enums.VariableOperation.Set      => val.floatValue,
                        Data.Supporting.Enums.VariableOperation.Add      => curf + val.floatValue,
                        Data.Supporting.Enums.VariableOperation.Subtract => curf - val.floatValue,
                        Data.Supporting.Enums.VariableOperation.Multiply => curf * val.floatValue,
                        Data.Supporting.Enums.VariableOperation.Divide   => val.floatValue != 0 ? curf / val.floatValue : curf,
                        _ => val.floatValue
                    };
                    SetFloat(varName, resultf);
                    break;
                case VariableType.String:
                    SetString(varName, val.stringValue);
                    break;
            }
        }

        // ===== CONDITION EVALUATION =====

        public bool Evaluate(Data.Supporting.Classes.Condition c)
        {
            switch (c.variableType)
            {
                case VariableType.Bool:
                    return Compare(GetBool(c.variableName), c.compareValue.boolValue, c.comparison);
                case VariableType.Int:
                    return Compare(GetInt(c.variableName), c.compareValue.intValue, c.comparison);
                case VariableType.Float:
                    return Compare(GetFloat(c.variableName), c.compareValue.floatValue, c.comparison);
                case VariableType.String:
                    return GetString(c.variableName) == c.compareValue.stringValue;
                default:
                    return false;
            }
        }

        private bool Compare<T>(T a, T b, Data.Supporting.Enums.ComparisonOperator op) where T : IComparable<T>
        {
            int cmp = a.CompareTo(b);
            return op switch
            {
                Data.Supporting.Enums.ComparisonOperator.Equals             => cmp == 0,
                Data.Supporting.Enums.ComparisonOperator.NotEquals          => cmp != 0,
                Data.Supporting.Enums.ComparisonOperator.GreaterThan        => cmp > 0,
                Data.Supporting.Enums.ComparisonOperator.LessThan           => cmp < 0,
                Data.Supporting.Enums.ComparisonOperator.GreaterThanOrEqual => cmp >= 0,
                Data.Supporting.Enums.ComparisonOperator.LessThanOrEqual    => cmp <= 0,
                _ => false
            };
        }

        // ===== SAVE / LOAD =====

        public VariableStoreSaveData GetSaveData() => new VariableStoreSaveData
        {
            boolVars   = new Dictionary<string, bool>(boolVariables),
            intVars    = new Dictionary<string, int>(intVariables),
            floatVars  = new Dictionary<string, float>(floatVariables),
            stringVars = new Dictionary<string, string>(stringVariables)
        };

        public void LoadSaveData(VariableStoreSaveData data)
        {
            boolVariables   = data.boolVars   != null ? new Dictionary<string, bool>(data.boolVars)     : new Dictionary<string, bool>();
            intVariables    = data.intVars    != null ? new Dictionary<string, int>(data.intVars)       : new Dictionary<string, int>();
            floatVariables  = data.floatVars  != null ? new Dictionary<string, float>(data.floatVars)   : new Dictionary<string, float>();
            stringVariables = data.stringVars != null ? new Dictionary<string, string>(data.stringVars) : new Dictionary<string, string>();
        }

        public void Clear()
        {
            boolVariables.Clear();
            intVariables.Clear();
            floatVariables.Clear();
            stringVariables.Clear();
        }

        public void LogAll()
        {
            Debug.Log("[VariableStore] === BOOLS ===");
            foreach (var kv in boolVariables) Debug.Log($"  {kv.Key} = {kv.Value}");
            Debug.Log("[VariableStore] === INTS ===");
            foreach (var kv in intVariables)  Debug.Log($"  {kv.Key} = {kv.Value}");
            Debug.Log("[VariableStore] === FLOATS ===");
            foreach (var kv in floatVariables) Debug.Log($"  {kv.Key} = {kv.Value}");
            Debug.Log("[VariableStore] === STRINGS ===");
            foreach (var kv in stringVariables) Debug.Log($"  {kv.Key} = {kv.Value}");
        }
    }

    [Serializable]
    public class VariableStoreSaveData
    {
        public Dictionary<string, bool>   boolVars;
        public Dictionary<string, int>    intVars;
        public Dictionary<string, float>  floatVars;
        public Dictionary<string, string> stringVars;
    }
}
