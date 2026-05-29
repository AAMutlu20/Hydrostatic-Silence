namespace DialogueSystem.Scripts.Data.Supporting.Enums
{
    public enum ComparisonOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual
    }

    public enum VariableOperation
    {
        Set,
        Add,
        Subtract,
        Multiply,
        Divide
    }

    public enum ConditionMode
    {
        All,  // AND
        Any   // OR
    }

    public enum EndBehavior
    {
        CloseDialogue,
        RestartDialogue,
        LoadNextGraph
    }
}
