using System.Media;
using AutoClick.Services.Persistence;

namespace AutoClick.UI.Services;

/// <summary>Short system sounds for app events; respects the "Sound notifications" setting.</summary>
public sealed class SoundService(SettingsStore settings)
{
    public void Start() => Play(SystemSounds.Asterisk);

    public void Stop() => Play(SystemSounds.Hand);

    public void Toggle() => Play(SystemSounds.Question);

    public void Success() => Play(SystemSounds.Beep);

    public void Error() => Play(SystemSounds.Exclamation);

    private void Play(SystemSound sound)
    {
        if (settings.Current.SoundNotifications)
        {
            sound.Play();
        }
    }
}
