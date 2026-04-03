namespace AutoClick.Core.Interfaces;

public interface IMemoryManager
{
    long GetCurrentMemoryUsageMb();
    void ForceCleanup();
    bool IsOverLimit(long limitMb);
}
