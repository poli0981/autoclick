using System.Media;
using AutoClick.Core.Models;

namespace AutoClick.Services;

/// <summary>
/// Plays Windows system sounds for app events.
/// All sounds respect the SoundNotifications setting.
/// </summary>
public class SoundService
{
    private readonly AppSettings _settings;

    public SoundService(AppSettings settings)
    {
        _settings = settings;
    }

    /// <summary>Start All / Start game — positive confirmation beep.</summary>
    public void PlayStart()
    {
        if (!_settings.SoundNotifications) return;
        SystemSounds.Asterisk.Play();
    }

    /// <summary>Stop All / Stop game — neutral notification.</summary>
    public void PlayStop()
    {
        if (!_settings.SoundNotifications) return;
        SystemSounds.Hand.Play();
    }

    /// <summary>Error / warning — something went wrong.</summary>
    public void PlayError()
    {
        if (!_settings.SoundNotifications) return;
        SystemSounds.Exclamation.Play();
    }

    /// <summary>Coordinate picked / action completed.</summary>
    public void PlaySuccess()
    {
        if (!_settings.SoundNotifications) return;
        SystemSounds.Beep.Play();
    }

    /// <summary>Pause / Resume toggle.</summary>
    public void PlayToggle()
    {
        if (!_settings.SoundNotifications) return;
        SystemSounds.Question.Play();
    }
}
