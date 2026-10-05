using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Hope.Launcher;

public partial class MainWindow : Window, IDisposable
{
    private readonly LauncherState state;
    private readonly DispatcherTimer monitor;
    private readonly ControllerNavigation controller;
    private readonly List<string> photos = [];
    private int photoIndex;
    private string? photoWarning;
    private string? settingsWarning;
    private SaveEntry? pendingRemoval;
    private string page = "Home";
    private bool loading;
    private bool lastRunning;
    private bool loadingCareers;
    private readonly List<GraphicsOption> worldGraphics = AdvancedGraphics.World();
    private readonly List<GraphicsOption> nativeGraphics = AdvancedGraphics.Native();

    public MainWindow(string root)
    {
        state = new(root);
        InitializeComponent();
        WorldGraphicsOptions.ItemsSource = worldGraphics;
        NativeGraphicsOptions.ItemsSource = nativeGraphics;
        var directory = Path.Combine(root, "assets", "gameplay");
        if (Directory.Exists(directory)) photos.AddRange(Directory.EnumerateFiles(directory, "*.jpg").Order());
        if (state.Preferences.PhotoPath is { } photo && File.Exists(photo)) photos.Insert(0, photo);
        photoIndex = Math.Clamp(state.Preferences.PhotoIndex, 0, Math.Max(0, photos.Count - 1));
        LoadPhoto();
        LoadGraphics();
        ReloadCareers();
        Navigate("Home");
        RefreshStatus();
        if ((state.PreferencesWarning ?? settingsWarning ?? photoWarning) is { } warning) Feedback.Text = warning;
        monitor = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        monitor.Tick += (_, _) => RefreshStatus();
        monitor.Start();
        controller = new ControllerNavigation(ControllerInput, connected =>
        {
            ControllerStatus.Text = connected ? "Controller connected · XInput" : "Controller disconnected · keyboard and mouse remain available";
            if (!connected) InputHint.Text = "Tab to move • Enter to select • Esc to go back";
        });
        Loaded += (_, _) => HomeNav.Focus();
        Closed += (_, _) => Dispose();
    }

    public void Dispose() { monitor.Stop(); controller.Dispose(); }

    private void Run(Action action)
    {
        try { action(); }
        catch (Exception error) { Feedback.Text = error.Message; Feedback.Foreground = (Brush)FindResource("Pink"); }
    }

    public void Navigate(string destination)
    {
        PageScroll.ScrollToTop();
        pendingRemoval = null;
        DeleteConfirm.Visibility = Visibility.Collapsed;
        page = destination;
        foreach (var nav in new[] { HomeNav, GraphicsNav, ModsNav, SavesNav, SetupNav, HelpNav, CreditsNav })
        {
            bool active = (string)nav.Tag == destination;
            nav.Foreground = (Brush)FindResource(active ? "Lime" : "Mint");
            nav.Background = active ? new SolidColorBrush(Color.FromArgb(150, 54, 40, 70)) : Brushes.Transparent;
        }
        HomePage.Visibility = destination == "Home" ? Visibility.Visible : Visibility.Collapsed;
        DetailsPage.Visibility = destination == "Home" ? Visibility.Collapsed : Visibility.Visible;
        GraphicsPanel.Visibility = destination == "Graphics" ? Visibility.Visible : Visibility.Collapsed;
        ModsPanel.Visibility = destination == "Mods" ? Visibility.Visible : Visibility.Collapsed;
        SavesPanel.Visibility = destination == "Saves" ? Visibility.Visible : Visibility.Collapsed;
        SetupPanel.Visibility = destination == "Setup" ? Visibility.Visible : Visibility.Collapsed;
        HelpPanel.Visibility = destination == "Help" ? Visibility.Visible : Visibility.Collapsed;
        CreditsPanel.Visibility = destination == "Credits" ? Visibility.Visible : Visibility.Collapsed;
        (PageTitle.Text, PageIntro.Text) = destination switch
        {
            "Graphics" => ("MAKE IT YOURS.", "Set up your next session. Close the game before saving changes here; use the in-game Graphics page for live adjustments."),
            "Mods" => ("REMIX YOUR RIDE.", "Choose mods, then apply. Changes take effect next launch, for the selected career only. All five built-ins start disabled."),
            "Saves" => ("YOUR LINES LIVE HERE.", "Manage career saves across the profiles in this installation. Every removal keeps a recovery copy."),
            "Setup" => ("BRING YOUR BOARD.", "You provide the game. HOPE brings the PC interface. No ISO downloads or retail game files are included."),
            "Help" => ("BACK ON YOUR FEET.", "Controls, settings recovery and your selected career folder, all in one place."),
            "Credits" => ("RESPECT THE ROOTS.", "HOPE stands on the work of the original Skate3Recomp creator and the wider recompilation community."),
            _ => ("HOPE", "Hills, Ollies, Pavement, Expression")
        };
        if (destination == "Mods") RefreshMods();
        if (destination == "Saves") RefreshSaves();
        if (destination == "Graphics") LoadGraphics();
        if (destination == "Setup") IsoLabel.Text = state.HasIso ? Path.GetFileName(state.Preferences.IsoPath) : "No ISO selected";
        if (destination == "Help") RefreshRecovery();
        Feedback.Text = settingsWarning ?? (destination == "Home" ? "Built for your next session." : "Changes stay with this installation.");
        Feedback.Foreground = (Brush)FindResource("Mint");
    }

    private void Navigate_Click(object sender, RoutedEventArgs e) => Run(() => Navigate((string)((Button)sender).Tag));
    private void Quit_Click(object sender, RoutedEventArgs e) => Close();
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        if (pendingRemoval != null) CancelDelete_Click(sender, e);
        else Navigate("Home");
        e.Handled = true;
    }

    private void RefreshStatus()
    {
        Run(() =>
        {
            var running = state.IsGameRunning();
            StatusBadge.Text = running ? "●  SESSION IN PROGRESS" : state.HasGame ? "●  READY TO ROLL" : "●  YOUR ISO NEEDED";
            ReadyTitle.Text = running ? "Your session is running." : state.HasGame ? "Your next line is waiting." : "Start with your own Skate 3 ISO.";
            ReadyDetail.Text = state.HasGame ? "Game files installed • Your career stays with this copy" : "Choose your Xbox 360 ISO in Game setup.";
            PlayButton.Content = running ? "GAME IS RUNNING" : state.HasGame ? "LET’S SKATE  →" : "SET UP HOPE  →";
            PlayButton.IsEnabled = !running;
            CareerCombo.IsEnabled = !running;
            NewCareerButton.IsEnabled = !running && state.HasGame;
            if (lastRunning && !running) Navigate("Home");
            GraphicsForm.IsEnabled = !running;
            WorldGraphicsOptions.IsEnabled = !running;
            AdvancedNativePanel.IsEnabled = !running && RendererCombo.SelectedIndex == 0;
            SaveGraphicsButton.IsEnabled = SaveGraphicsTopButton.IsEnabled = !running;
            ApplyModsButton.IsEnabled = DisableModsButton.IsEnabled = !running;
            FullscreenCheck.IsEnabled = VsyncCheck.IsEnabled = FpsCheck.IsEnabled = !running;
            ResetSettingsButton.IsEnabled = !running;
            RestoreSettingsButton.IsEnabled = !running && BackupCombo.Items.Count > 0;
            BackupCombo.IsEnabled = !running && BackupCombo.Items.Count > 0;
            if (running != lastRunning && page == "Saves") RefreshSaves();
            lastRunning = running;
        });
    }

    private void Play_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (state.IsGameRunning()) { Feedback.Text = "HOPE is already running. Return to your game window."; return; }
        if (!state.HasGame && !state.HasIso) { Navigate("Setup"); return; }
        var process = Process.Start(state.BuildStartInfo()) ?? throw new IOException("The game could not be started.");
        process.Dispose();
        Feedback.Text = state.HasGame ? "HOPE started. Start opens Skate 3's menu. Escape or RB + Start opens PC settings." : "Opening the ISO installer. Keep its window open until setup finishes.";
        RefreshStatus();
    });
    private void ReloadCareers()
    {
        loadingCareers = true;
        CareerCombo.Items.Clear();
        foreach (var choice in Careers.List(state.BundleRoot))
        {
            var item = new ComboBoxItem { Content = choice.Label, Tag = choice };
            CareerCombo.Items.Add(item);
            if (choice.Id == state.Preferences.CareerId) CareerCombo.SelectedItem = item;
        }
        if (CareerCombo.SelectedIndex < 0) CareerCombo.SelectedIndex = 0;
        loadingCareers = false;
    }
    private void Career_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (loadingCareers || CareerCombo.SelectedItem is not ComboBoxItem { Tag: CareerChoice choice }) return;
        Run(() =>
        {
            state.SelectCareer(choice.Id);
            if (page == "Mods") RefreshMods();
            LoadGraphics();
            RefreshStatus();
            Feedback.Text = "Career selected. Play continues this career; other careers stay separate.";
        });
        ReloadCareers();
    }
    private async void NewCareer_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        RequireGameClosed();
        if (MessageBox.Show(this, "Create a separate career and return to first-time setup? Your current saves stay in their own folder. This copies the game program and settings, so it needs additional disk space.",
            "HOPE — New career", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) != MessageBoxResult.OK) return;
        Feedback.Text = "Preparing a separate career. Your existing progress stays in its own folder…";
        IsEnabled = false;
        var id = await Task.Run(() => Careers.Create(state));
        state.SelectCareer(id);
        ReloadCareers();
        LoadGraphics();
        RefreshStatus();
        Feedback.Text = "New career ready. Choose Play to start setup. Switch back using the career list.";
      }
      catch (Exception error) { Feedback.Text = error.Message; Feedback.Foreground = (Brush)FindResource("Pink"); }
      finally { IsEnabled = true; }
    }

    private void LoadGraphics()
    {
        loading = true;
        settingsWarning = null;
        string Read(string key, string fallback)
        {
            try { return SettingsFile.Read(state.SettingsPath, key, fallback); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                settingsWarning = "Game settings could not be read. Open Help & recovery. Your career saves are intact.";
                return fallback;
            }
        }
        bool Flag(string key, bool fallback) => bool.TryParse(Read(key, fallback.ToString()), out var v) ? v : fallback;
        int Number(string key, int fallback) => int.TryParse(Read(key, fallback.ToString()), out var v) ? v : fallback;
        RendererCombo.SelectedIndex = Flag("skate3_native_render_scene", true) ? 0 : 1;
        ResolutionCombo.SelectedIndex = Math.Clamp(Number("draw_resolution_scale_x", 2), 1, 3) - 1;
        AaCombo.SelectedIndex = Array.IndexOf(new[] { 1, 2, 4, 8 }, Number("skate3_native_render_scene_msaa", 4));
        if (AaCombo.SelectedIndex < 0) AaCombo.SelectedIndex = 2;
        AoCheck.IsChecked = Flag("skate3_native_render_scene_ssao", true);
        AoQualityCheck.IsChecked = Flag("skate3_native_render_scene_ssao_full_res", false);
        FogCheck.IsChecked = Flag("skate3_native_render_scene_fog", true);
        HazeCheck.IsChecked = Flag("skate3_native_render_scene_haze", true);
        ShaftsCheck.IsChecked = Flag("skate3_native_render_scene_shafts", true);
        BloomCheck.IsChecked = Flag("skate3_native_render_scene_bloom", true);
        FullscreenCheck.IsChecked = Flag("fullscreen", true);
        VsyncCheck.IsChecked = Flag("vsync", false);
        FpsCheck.IsChecked = Flag("show_fps_counter", false);
        foreach (var option in worldGraphics.Concat(nativeGraphics)) option.Load(Read);
        loading = false;
        UpdateNativeControls();
    }

    private void Renderer_Changed(object sender, SelectionChangedEventArgs e) { if (!loading && NativeEffects != null) UpdateNativeControls(); }
    private void UpdateNativeControls() { NativeEffects.IsEnabled = AaCombo.IsEnabled = RendererCombo.SelectedIndex == 0; NativeEffects.Opacity = NativeEffects.IsEnabled ? 1 : 0.45; AdvancedNativePanel.IsEnabled = NativeEffects.IsEnabled && !state.IsGameRunning(); AdvancedNativePanel.Opacity = NativeEffects.IsEnabled ? 1 : .45; }
    private void RequireGameClosed()
    {
        if (state.IsGameRunning()) throw new IOException("Close the game before changing settings or removing saves here.");
    }
    private void Input_KeyDown(object sender, KeyEventArgs e) => InputHint.Text = "Tab to move • Enter to select • Esc to go back";
    private void Input_MouseDown(object sender, MouseButtonEventArgs e) => InputHint.Text = "Click to choose • Scroll for more • Esc to go back";
    private void RefreshRecovery()
    {
        BackupCombo.Items.Clear();
        foreach (var backup in SettingsRecovery.List(state.SettingsPath))
            BackupCombo.Items.Add(new ComboBoxItem { Content = backup.Label, Tag = backup });
        BackupCombo.SelectedIndex = BackupCombo.Items.Count > 0 ? 0 : -1;
        BackupCombo.Visibility = BackupCombo.Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        BackupStatus.Visibility = BackupCombo.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RestoreSettingsButton.IsEnabled = !state.IsGameRunning() && BackupCombo.Items.Count > 0;
    }
    private void ResetSettings_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        RequireGameClosed();
        if (MessageBox.Show(this, "Reset game settings for this career? Current settings will be backed up. Career saves, profiles and other careers are preserved.",
            "HOPE — Reset settings", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) != MessageBoxResult.OK) return;
        var backup = SettingsRecovery.Reset(state.SettingsPath);
        LoadGraphics();
        RefreshRecovery();
        Feedback.Text = backup == null ? "Settings are already at defaults." : "Game settings reset. Your career is intact. Choose a backup below to undo this change.";
    });
    private void RestoreSettings_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        RequireGameClosed();
        if (BackupCombo.SelectedItem is not ComboBoxItem { Tag: SettingsBackup backup }) throw new IOException("Choose a settings backup first.");
        if (MessageBox.Show(this, "Restore this settings backup? Current settings will also be backed up. Saves and profiles stay intact.",
            "HOPE — Restore settings", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) != MessageBoxResult.OK) return;
        SettingsRecovery.Restore(state.SettingsPath, backup.Path);
        LoadGraphics();
        RefreshRecovery();
        Feedback.Text = "Settings restored. They will apply next launch; your career is intact.";
    });
    private void OpenCareer_Click(object sender, RoutedEventArgs e) => Run(() =>
        Process.Start(new ProcessStartInfo(state.ActiveRoot) { UseShellExecute = true })?.Dispose());
    private void OpenCaptures_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        var folder = Path.Combine(state.ActiveRoot, "cache", "performance");
        if (!Directory.Exists(folder)) { Feedback.Text = "No captures yet. Press F8 in game to start, then F8 again to save. If your cache is elsewhere, the game log lists its capture location."; return; }
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true })?.Dispose();
    });
    private void SaveGraphics_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        RequireGameClosed();
        string Flag(CheckBox box) => box.IsChecked == true ? "true" : "false";
        var scale = (ResolutionCombo.SelectedIndex + 1).ToString();
        var updates = new Dictionary<string, string>
        {
            ["skate3_native_render_scene"] = RendererCombo.SelectedIndex == 0 ? "true" : "false",
            ["resolution_scale"] = scale, ["draw_resolution_scale_x"] = scale, ["draw_resolution_scale_y"] = scale,
            ["skate3_native_render_scene_msaa"] = new[] { 1, 2, 4, 8 }[AaCombo.SelectedIndex].ToString(),
            ["skate3_native_render_scene_ssao"] = Flag(AoCheck), ["skate3_native_render_scene_ssao_full_res"] = Flag(AoQualityCheck),
            ["skate3_native_render_scene_fog"] = Flag(FogCheck), ["skate3_native_render_scene_haze"] = Flag(HazeCheck),
            ["skate3_native_render_scene_shafts"] = Flag(ShaftsCheck), ["skate3_native_render_scene_bloom"] = Flag(BloomCheck),
            ["fullscreen"] = Flag(FullscreenCheck), ["vsync"] = Flag(VsyncCheck), ["show_fps_counter"] = Flag(FpsCheck)
        };
        foreach (var option in worldGraphics.Concat(nativeGraphics)) updates[option.Key] = option.Literal;
        SettingsFile.Update(state.SettingsPath, updates);
        Feedback.Text = "Graphics saved. Your next session will use these settings.";
    });

    private ModManager Mods() => new(state.BundleRoot, state.ActiveRoot, () => state.IsGameRunning());
    private void RefreshMods()
    {
        var manager = Mods(); ModList.ItemsSource = manager.List();
        ModsStatus.Text = manager.Warning.Length > 0 ? manager.Warning : "Tick your choices, then Apply. Imports join the library and start disabled.";
    }
    private void ImportMod_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        var picker = new OpenFileDialog { Title = "Import a HOPE settings mod", Filter = "HOPE settings mod (*.hope-mod.json)|*.hope-mod.json", CheckFileExists = true };
        if (picker.ShowDialog(this) != true) return;
        var name = Mods().Import(picker.FileName); RefreshMods(); Feedback.Text = name + " imported. Tick it and Apply to enable it.";
    });
    private void ApplyMods_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        RequireGameClosed(); Feedback.Text = Mods().Apply(ModList.Items.Cast<ModChoice>().Where(m => m.Enabled).Select(m => m.Definition.Id)); RefreshMods(); LoadGraphics();
    });
    private void DisableMods_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        RequireGameClosed(); Feedback.Text = Mods().Apply([]); RefreshMods(); LoadGraphics();
    });

    private void RefreshSaves()
    {
        pendingRemoval = null;
        DeleteConfirm.Visibility = Visibility.Collapsed;
        SaveList.ItemsSource = SaveStorage.List(state.UserRoot, state.ActiveRoot);
        SaveList.SelectedIndex = 0;
        if (SaveList.Items.Count == 0) Feedback.Text = "No saves found in this installation yet. Start a career to create one.";
    }
    private void RefreshSaves_Click(object sender, RoutedEventArgs e) => Run(RefreshSaves);
    private void OpenSaves_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        var folder = SaveList.SelectedItem is SaveEntry save ? save.PackageRoot : state.UserRoot;
        if (!Directory.Exists(folder)) throw new IOException("The save folder hasn't been created yet.");
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true })?.Dispose();
    });
    private void DeleteSave_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        RequireGameClosed();
        pendingRemoval = SaveList.SelectedItem as SaveEntry ?? throw new IOException("Choose a save first.");
        DeleteLabel.Text = $"Remove {pendingRemoval.Label}? A recovery copy will be kept.";
        DeleteConfirm.Visibility = Visibility.Visible;
        KeepSaveButton.Focus();
    });
    private void CancelDelete_Click(object sender, RoutedEventArgs e) { pendingRemoval = null; DeleteConfirm.Visibility = Visibility.Collapsed; Feedback.Text = "Save kept."; }
    private void ConfirmDelete_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        RequireGameClosed();
        if (pendingRemoval is not { } selected) throw new IOException("Choose the save again.");
        if (!SaveStorage.List(state.UserRoot, state.ActiveRoot).Contains(selected)) throw new IOException("The save list changed. Refresh and choose the save again.");
        var recovery = SaveStorage.Archive(selected);
        RefreshSaves();
        Feedback.Text = "Save removed from active saves. Recovery copy: " + recovery;
    });

    private void ChooseIso_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        RequireGameClosed();
        var picker = new OpenFileDialog { Title = "Choose your own Skate 3 Xbox 360 ISO", Filter = "Xbox 360 ISO (*.iso)|*.iso", CheckFileExists = true };
        if (picker.ShowDialog(this) != true) return;
        state.Preferences.IsoPath = picker.FileName;
        state.SavePreferences();
        IsoLabel.Text = Path.GetFileName(picker.FileName);
        Feedback.Text = "Your ISO is selected. Install / start HOPE will open the game installer.";
    });
    private void ChooseGameFolder_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        RequireGameClosed();
        var picker = new OpenFolderDialog { Title = "Choose your installed Skate 3 game folder" };
        if (picker.ShowDialog(this) != true) return;
        if (!File.Exists(Path.Combine(picker.FolderName, "default.xex"))) throw new IOException("This folder does not contain default.xex. Choose the extracted game folder.");
        SettingsFile.Update(state.ConfigPath, new Dictionary<string, string> { ["game_data_root"] = JsonSerializer.Serialize(picker.FolderName) });
        RefreshStatus();
        Feedback.Text = "Installed game folder selected. You're ready to play.";
    });
    private void ChoosePhoto_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        var picker = new OpenFileDialog { Title = "Choose a gameplay screenshot", Filter = "Gameplay images|*.jpg;*.jpeg;*.png", CheckFileExists = true };
        if (picker.ShowDialog(this) != true) return;
        var image = ReadPhoto(picker.FileName);
        if (image.PixelWidth < 1280 || image.PixelHeight < 720) throw new IOException("Choose an HD screenshot at least 1280 × 720.");
        state.Preferences.PhotoPath = picker.FileName;
        state.Preferences.PhotoIndex = 0;
        state.SavePreferences();
        photos.Insert(0, picker.FileName);
        photoIndex = 0;
        LoadPhoto();
        Feedback.Text = "Gameplay background updated.";
    });
    private void LoadPhoto()
    {
        while (photos.Count > 0)
        {
            photoIndex = Math.Clamp(photoIndex, 0, photos.Count - 1);
            try
            {
                var image = ReadPhoto(photos[photoIndex]);
                Backdrop.Source = image;
                PhotoCaption.Text = $"{photoIndex + 1:00} / {photos.Count:00}   •   SKATE 3 STREET FLIGHT";
                return;
            }
            catch (Exception error) when (error is IOException or FileFormatException or NotSupportedException or ArgumentException)
            {
                // A missing or damaged optional photo must not block Play or saves.
                photos.RemoveAt(photoIndex);
                photoWarning = "A background image could not be read. Choose another HD screenshot in Game setup.";
                Feedback.Text = photoWarning;
            }
        }
        Backdrop.Source = null;
        PhotoCaption.Text = "CHOOSE YOUR HD GAMEPLAY PHOTO IN GAME SETUP";
    }
    private static BitmapImage ReadPhoto(string path)
    {
        // Own the stream explicitly: failed URI-based decoding can retain a
        // file handle, preventing the player from replacing a damaged image.
        using var stream = File.OpenRead(path);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
    private void StepPhoto(int step)
    {
        if (photos.Count == 0) return;
        photoIndex = (photoIndex + step + photos.Count) % photos.Count;
        LoadPhoto();
        state.Preferences.PhotoIndex = photoIndex;
        state.SavePreferences();
    }
    private void PreviousPhoto_Click(object sender, RoutedEventArgs e) => Run(() => StepPhoto(-1));
    private void NextPhoto_Click(object sender, RoutedEventArgs e) => Run(() => StepPhoto(1));

    private void ControllerInput(ushort buttons)
    {
        // Leave system file dialogs and other applications in control.
        if (!IsActive || !IsEnabled) return;
        InputHint.Text = "D-pad / stick to move • A to select • B to go back";
        Run(() =>
        {
            if ((buttons & 0x2000) != 0)
            {
                if (pendingRemoval != null) CancelDelete_Click(this, new RoutedEventArgs());
                else Navigate("Home");
                return;
            }
            var focused = Keyboard.FocusedElement as FrameworkElement;
            if ((buttons & 3) != 0)
                (focused ?? HomeNav).MoveFocus(new TraversalRequest((buttons & 1) != 0 ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next));
            if ((buttons & 12) != 0)
            {
                int delta = (buttons & 4) != 0 ? -1 : 1;
                if (focused is Slider slider && slider.IsEnabled) slider.Value = Math.Clamp(slider.Value + delta * slider.SmallChange, slider.Minimum, slider.Maximum);
                else if (focused is ComboBox combo && combo.IsEnabled)
                    combo.SelectedIndex = Math.Clamp(combo.SelectedIndex + delta, 0, combo.Items.Count - 1);
                else if (focused is ListBox list && list.Items.Count > 0)
                    list.SelectedIndex = Math.Clamp(list.SelectedIndex + delta, 0, list.Items.Count - 1);
                else (focused ?? HomeNav).MoveFocus(new TraversalRequest(delta < 0 ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next));
            }
            focused = Keyboard.FocusedElement as FrameworkElement;
            if ((buttons & 0x1000) != 0)
            {
                if (focused is Button button && button.IsEnabled) button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                else if (focused is CheckBox checkbox && checkbox.IsEnabled) checkbox.IsChecked = checkbox.IsChecked != true;
                else if (focused is ComboBox combo && combo.IsEnabled && combo.Items.Count > 0) combo.SelectedIndex = (combo.SelectedIndex + 1) % combo.Items.Count;
            }
        });
    }
}
