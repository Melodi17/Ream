namespace ream.Interpreting.Objects;

public static class ReamGarbageCollector
{
    public static void Collect()
    {
        GC.Collect();
    }
}
