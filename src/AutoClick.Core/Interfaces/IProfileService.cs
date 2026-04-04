using System.Collections.Generic;
using AutoClick.Core.Models;

namespace AutoClick.Core.Interfaces;

public interface IProfileService
{
    List<GameProfile> GetAll();
    GameProfile? GetByName(string name);
    void Save(GameProfile profile);
    void Delete(string id);
    void Export(string filePath, GameProfile profile);
    GameProfile Import(string filePath);
}
