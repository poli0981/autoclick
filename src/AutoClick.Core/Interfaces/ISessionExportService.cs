using AutoClick.Core.Models;

namespace AutoClick.Core.Interfaces;

public interface ISessionExportService
{
    void Export(string filePath, SessionExport payload);
    SessionExport Import(string filePath);
}
