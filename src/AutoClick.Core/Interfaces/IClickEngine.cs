using System.Threading;
using System.Threading.Tasks;
using AutoClick.Core.Models;

namespace AutoClick.Core.Interfaces;

public interface IClickEngine
{
    Task StartAsync(GameSession session, CancellationToken cancellationToken);
    void Pause(GameSession session);
    void Resume(GameSession session);
    void Stop(GameSession session);
}
