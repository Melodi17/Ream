namespace ream.Interpreting;

public static class MemberType
{
    public const string Type = "~type";
    public const string Truthy = "~truthy";
    
    public const string Iterate = "~iterate";
    public const string Index = "~index";
    public const string SetIndex = "~setIndex";
    public const string Call = "~call";
    public const string Member = "~member";
    public const string SetMember = "~setMember";
    public const string String = "~string";
    
    public const string Equal = "~equal";
    public const string NotEqual = "~notEqual";
    public const string Less = "~less";
    public const string LessEqual = "~lessEqual";
    public const string Greater = "~greater";
    public const string GreaterEqual = "~greaterEqual";
    
    public const string Add = "~add";
    public const string Subtract = "~subtract";
    public const string Multiply = "~multiply";
    public const string Divide = "~divide";
    public const string Modulo = "~modulo";
    
    public const string Negate = "~negate";
    public const string Not = "~not";
    
    public const string Instance = "new";
    public const string InstanceInternal = "~instance";
}
