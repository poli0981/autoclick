using AutoClick.Core.Models;

namespace AutoClick.Core.Interfaces;

public interface ISettingsService
{
    string SettingsFilePath { get; }
    AppSettings Load();
    void Save(AppSettings settings);
    void Export(string filePath, AppSettings settings);
    AppSettings Import(string filePath);
}
