using System.Text.Json;
using AutoClick.Core.Abstractions;
using AutoClick.Core.Models;
using AutoClick.Core.Sessions;
using AutoClick.Core.Settings;
using AutoClick.Services.Persistence;
using Microsoft.Extensions.Time.Testing;

namespace AutoClick.Services.Tests;

public sealed class PersistenceTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 15, 8, 0, 0, TimeSpan.Zero));
    private readonly AppPaths _paths;

    public PersistenceTests() => _paths = new AppPaths(_dir.Path);

    public void Dispose() => _dir.Dispose();

    private SettingsStore Settings() => new(_paths, _time, TestLog.For<SettingsStore>());

    private ProfileStore Profiles() => new(_paths, _time, TestLog.For<ProfileStore>());

    [Fact]
    public void Settings_FreshInstall_CreatesDefaults()
    {
        var store = Settings();

        Assert.Equal(SettingsLoadResult.Created, store.Load());
        Assert.True(File.Exists(_paths.SettingsFile));
        Assert.False(store.Current.AlwaysRunAsAdmin);
        Assert.Equal(AppTheme.System, store.Current.Theme);
    }

    [Fact]
    public void Settings_V1Upgrade_MigratesAndKeepsTheV1File()
    {
        File.Copy(Fixtures.PathOf("v1", "settings.json"), _paths.LegacySettingsFile);
        var before = File.ReadAllText(_paths.LegacySettingsFile);
        var store = Settings();

        Assert.Equal(SettingsLoadResult.Migrated, store.Load());

        Assert.True(store.Current.AlwaysRunAsAdmin);
        Assert.True(store.Current.ShowAdminModeNotice);
        Assert.Equal("vi", store.Current.Language);
        Assert.Equal(before, File.ReadAllText(_paths.LegacySettingsFile));
        Assert.True(File.Exists(Path.Combine(_paths.BackupDirectory, "settings.v1.json")));
        Assert.Equal(SettingsLoadResult.Loaded, Settings().Load());
    }

    [Fact]
    public void Settings_V1ProfilesOnly_CountsAsUpgrade()
    {
        Directory.CreateDirectory(_paths.ProfilesDirectory);
        File.Copy(Fixtures.PathOf("v1", "profiles", "a1b2c3d4.json"), Path.Combine(_paths.ProfilesDirectory, "a1b2c3d4.json"));
        var store = Settings();

        Assert.Equal(SettingsLoadResult.Migrated, store.Load());
        Assert.True(store.Current.AlwaysRunAsAdmin);
    }

    [Fact]
    public void Settings_EmptyProfilesFolder_IsNotAnUpgrade()
    {
        Directory.CreateDirectory(_paths.ProfilesDirectory);

        Assert.Equal(SettingsLoadResult.Created, Settings().Load());
    }

    [Fact]
    public void Settings_CorruptFile_IsMovedAsideAndDefaultsUsed()
    {
        File.WriteAllText(_paths.SettingsFile, "{ this is not json");
        var store = Settings();

        Assert.Equal(SettingsLoadResult.RecoveredFromCorruption, store.Load());

        Assert.NotNull(store.CorruptBackupPath);
        Assert.True(File.Exists(store.CorruptBackupPath));
        Assert.Equal(new AppSettings().Normalize(), store.Current);
    }

    [Fact]
    public void Settings_Update_SavesAtomicallyAndRaisesChanged()
    {
        var store = Settings();
        store.Load();
        AppSettings? changed = null;
        store.Changed += (_, s) => changed = s;

        store.Update(s => s with { Language = "ko", MaxGamesInQueue = 999 });

        Assert.Equal("ko", changed?.Language);
        Assert.Equal(50, store.Current.MaxGamesInQueue);
        Assert.Contains("\"language\": \"ko\"", File.ReadAllText(_paths.SettingsFile), StringComparison.Ordinal);
        Assert.False(File.Exists(_paths.SettingsFile + ".tmp"));
    }

    [Fact]
    public void Settings_ImportV1File_KeepsMachineSpecificValues()
    {
        var store = Settings();
        store.Load();
        store.Update(s => s with { AlwaysRunAsAdmin = true, MainWindow = new WindowPlacement { Width = 900, Height = 700 } });

        var imported = store.Import(Fixtures.PathOf("v1", "settings.json"));

        Assert.Equal("vi", imported.Language);
        Assert.True(imported.AlwaysRunAsAdmin);
        Assert.Equal(900, imported.MainWindow?.Width);
    }

    [Fact]
    public void Settings_ImportGarbage_Throws()
    {
        var path = _dir.Combine("bad.json");
        File.WriteAllText(path, "[1,2,3]");
        var store = Settings();
        store.Load();

        Assert.ThrowsAny<JsonException>(() => store.Import(path));
    }

    [Fact]
    public void Profiles_SaveAndLoad_RoundTripsEveryField()
    {
        var store = Profiles();
        var saved = store.Save(new GameProfile
        {
            Name = "Farm",
            ClickPoints = [new ClickPoint { X = 1, Y = 2, ReferenceColor = 0x112233 }, new ClickPoint { ClickType = ClickType.Keystroke, VirtualKeyCode = 0x41 }],
            SequenceDelayMs = 300,
        });

        var loaded = Assert.Single(store.GetAll());

        Assert.Equal(saved.Id, loaded.Id);
        Assert.Equal(saved.ClickPoints, loaded.ClickPoints);
        Assert.Equal(_time.GetLocalNow(), loaded.CreatedAt);
    }

    [Fact]
    public void Profiles_ReadsV1Files()
    {
        Directory.CreateDirectory(_paths.ProfilesDirectory);
        File.Copy(Fixtures.PathOf("v1", "profiles", "a1b2c3d4.json"), Path.Combine(_paths.ProfilesDirectory, "a1b2c3d4.json"));

        var profile = Assert.Single(Profiles().GetAll());

        Assert.Equal(32, profile.ClickPoints[2].VirtualKeyCode);
    }

    [Fact]
    public void Profiles_FileNameIsTheIdThatCounts()
    {
        Directory.CreateDirectory(_paths.ProfilesDirectory);
        File.WriteAllText(Path.Combine(_paths.ProfilesDirectory, "deadbeef.json"), """{"id": "..\\..\\evil", "name": "Tampered"}""");
        File.WriteAllText(Path.Combine(_paths.ProfilesDirectory, "not a valid id.json"), """{"id": "abcdefgh", "name": "Ignored"}""");

        var profile = Assert.Single(Profiles().GetAll());

        Assert.Equal("deadbeef", profile.Id);
    }

    [Fact]
    public void Profiles_ImportNeverWritesOutsideTheProfilesFolder()
    {
        var store = Profiles();

        var imported = store.Import(new GameProfile { Id = "..\\..\\Startup\\evil", Name = "Imported" });

        Assert.True(GameProfile.IsValidId(imported.Id));
        Assert.Equal([Path.Combine(_paths.ProfilesDirectory, imported.Id + ".json")], Directory.EnumerateFiles(_dir.Path, "*.json", SearchOption.AllDirectories).Where(f => !f.EndsWith("settings.v2.json", StringComparison.Ordinal)));
    }

    [Fact]
    public void Profiles_ImportWithAnExistingName_ReplacesThatProfile()
    {
        var store = Profiles();
        var original = store.Save(new GameProfile { Name = "Farm", SequenceDelayMs = 1 });

        var imported = store.Import(new GameProfile { Name = "farm", SequenceDelayMs = 2 });

        Assert.Equal(original.Id, imported.Id);
        Assert.Equal(original.CreatedAt, imported.CreatedAt);
        Assert.Equal(2, Assert.Single(store.GetAll()).SequenceDelayMs);
    }

    [Fact]
    public void Profiles_DeleteRejectsUnsafeIds()
    {
        var store = Profiles();

        Assert.False(store.Delete("..\\settings.v2"));
        Assert.False(store.Delete("abcdef12"));
    }

    [Fact]
    public void Session_ExportThenRead_RoundTrips()
    {
        var path = _dir.Combine("s.autoclick-session");
        var session = SessionImport.Read(Fixtures.PathOf("v1", "session.autoclick-session"));

        SessionImport.Export(path, session);
        var read = SessionImport.Read(path);

        Assert.Equal(session.Settings, read.Settings);
        Assert.Equal(session.Games[0].ClickPoints, read.Games[0].ClickPoints);
    }

    [Fact]
    public void Session_Plan_MatchesRunningWindowsAndKeepsMachineSettings()
    {
        var session = new SessionExport
        {
            Settings = new AppSettings { Language = "ja" },
            Games =
            [
                new SavedGame { ProcessName = "Game", WindowTitle = "Main" },
                new SavedGame { ProcessName = "Solo", WindowTitle = "Old title" },
                new SavedGame { ProcessName = "Missing", WindowTitle = "x" },
            ],
        };
        var windows = new List<WindowInfo>
        {
            new(1, 10, "game", "Launcher", "", ElevationState.SameOrLower),
            new(2, 10, "game", "Main", "", ElevationState.SameOrLower),
            new(3, 11, "Solo", "New title", "", ElevationState.SameOrLower),
        };
        var current = new AppSettings { AlwaysRunAsAdmin = true };

        var plan = SessionImport.Plan(session, current, windows);

        Assert.Equal([(nint)2, (nint)3], plan.Matched.Select(m => m.Window.Handle));
        Assert.Equal("Missing", Assert.Single(plan.Unmatched).ProcessName);
        Assert.Equal("ja", plan.Settings.Language);
        Assert.True(plan.Settings.AlwaysRunAsAdmin);
    }
}
