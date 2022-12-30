using ream.Interpreting.Objects;

namespace ReamTests;

public class ObjectTests
{
    [SetUp]
    public void Setup() { }

    [Test]
    public void FromNative_GenericCollection()
    {
        List<string> list = new();
        list.Add("Hello");
        list.Add("World");
        list.Add("!");

        ReamObject reamObject = ReamObjectFactory.Create(list);
        ReamString str = reamObject.String();
        string result = str.RepresentAs<string>();
        Assert.AreEqual("['Hello', 'World', '!']", result);

        List<object> genericList = new();
        genericList.Add("Hello");
        genericList.Add(1);
        genericList.Add(false);

        reamObject = ReamObjectFactory.Create(genericList);
        str = reamObject.String();
        result = str.RepresentAs<string>();
        Assert.AreEqual("['Hello', 1, false]", result);
    }

    [Test]
    public void Equal_Overload()
    {
        // Write a test to create 2 ReamObjects and compare them using the .Equal() method
        // The objects should be the same type and have the same value

        ReamObject reamObject1 = ReamObjectFactory.Create(1);
        ReamObject reamObject2 = ReamObjectFactory.Create(1);
        Assert.IsTrue(reamObject1.Equal(reamObject2).RepresentAs<bool>());

        reamObject1 = ReamObjectFactory.Create("Hello");
        reamObject2 = ReamObjectFactory.Create("World");
        Assert.IsFalse(reamObject1.Equal(reamObject2).RepresentAs<bool>());
    }

    [Test]
    public void GreaterEqual_Overload()
    {
        // Make sure the behavior of the .GreaterEqual() method is identical to the .Greater() method

        ReamObject reamObject1 = ReamObjectFactory.Create(5);
        ReamObject reamObject2 = ReamObjectFactory.Create(3);

        Assert.IsTrue(reamObject1.GreaterEqual(reamObject2).RepresentAs<bool>());
        Assert.IsFalse(reamObject2.GreaterEqual(reamObject1).RepresentAs<bool>());

        Assert.AreEqual(reamObject1.GreaterEqual(reamObject2).RepresentAs<bool>(),
            reamObject1.Greater(reamObject2).RepresentAs<bool>()
            || reamObject1.Equal(reamObject2).RepresentAs<bool>());
    }

    [Test]
    public void StringSplit()
    {
        // Write a test to split a string using the .Split() method
        
        ReamString reamString = ReamString.From("Hello World!");
        ReamSequence reamObject = reamString.Split(ReamString.From(" "));
        
        Assert.AreEqual(reamObject.Length().RepresentAs<int>(), 2);
        Assert.AreEqual(reamObject.Index(ReamNumber.From(0)).RepresentAs<string>(), "Hello");
        Assert.AreEqual(reamObject.Index(ReamNumber.From(1)).RepresentAs<string>(), "World!");
    }
}
