using AutoClick.Core.Models;

namespace AutoClick.Core.Interfaces;

public interface IGameDetector
{
    List<GameWindowInfo> GetRunningWindows();
    bool IsProcessAlive(int processId);
    bool IsWindowValid(IntPtr handle);
}
