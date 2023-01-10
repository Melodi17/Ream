using Ream.Interpreting;
using ream.Interpreting.Objects;

namespace ReamTests;


public class FunctionTests
{
    [SetUp]
    public void Setup()
    {
    }
    
    [Test]
    public void CSharp_Call_With_External_Type()
    {
        ReamFunctionExternal func = ReamFunctionExternal.From(this.Add);
        ReamObject result = func.Call(ReamSequence.From(new object[] { 1, 2 }));
        Assert.AreEqual(3, result.RepresentAs<int>());
    }
    
    public int Add(int a, int b)
    {
        return a + b;
    }
    
    public ReamNumber AddInternal(ReamNumber a, ReamNumber b)
    {
        return (ReamNumber)a.Add(b);
    }
    
    [Test]
    public void CSharp_Call_With_Internal_Type()
    {
        ReamFunctionExternal func = ReamFunctionExternal.From(this.AddInternal);
        ReamObject result = func.Call(ReamSequence.From(new object[] {1, 2}));
        Assert.AreEqual(3, result.RepresentAs<int>());
    }
    
    
}
