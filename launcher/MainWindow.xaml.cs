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
    private SaveEntry? pendingRemoval;
    private string page = "Home";
    private bool loading;
    private bool lastRunning;

    public MainWindow(string root)
    {
        state = new(root);
        InitializeComponent();
        var directory = Path.Combine(root, "assets", "gameplay");
        if (Directory.Exists(directory)) photos.AddRange(Directory.EnumerateFiles(directory, "*.jpg").Order());
        if (state.Preferences.PhotoPath is { } photo && File.Exists(photo)) photos.Insert(0, photo);
        photoIndex = Math.Clamp(state.Preferences.PhotoIndex, 0, Math.Max(0, photos.Count - 1));
        LoadPhoto();
        LoadGraphics();
        Navigate("Home");
        RefreshStatus();
        if (state.PreferencesWarning is { } warning) Feedback.Text = warning;
        monitor = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        monitor.Tick += (_, _) => RefreshStatus();
        monitor.Start();
        controller = new ControllerNavigation(ControllerInput);
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
        pendingRemoval = null;
        DeleteConfirm.Visibility = Visibility.Collapsed;
        page = destination;
        foreach (var nav in new[] { HomeNav, GraphicsNav, SavesNav, SetupNav, CreditsNav })
        {
            bool active = (string)nav.Tag == destination;
            nav.Foreground = (Brush)FindResource(active ? "Lime" : "Mint");
            nav.Background = active ? new SolidColorBrush(Color.FromArgb(150, 54, 40, 70)) : Brushes.Transparent;
        }
        HomePage.Visibility = destination == "Home" ? Visibility.Visible : Visibility.Collapsed;
        DetailsPage.Visibility = destination == "Home" ? Visibility.Collapsed : Visibility.Visible;
        GraphicsPanel.Visibility = destination == "Graphics" ? Visibility.Visible : Visibility.Collapsed;
        SavesPanel.Visibility = destination == "Saves" ? Visibility.Visible : Visibility.Collapsed;
        SetupPanel.Visibility = destination == "Setup" ? Visibility.Visible : Visibility.Collapsed;
        CreditsPanel.Visibility = destination == "Credits" ? Visibility.Visible : Visibility.Collapsed;
        (PageTitle.Text, PageIntro.Text) = destination switch
        {
            "Graphics" => ("MAKE IT YOURS.", "Set up your next session. Close the game before saving changes here; use the in-game Graphics page for live adjustments."),
            "Saves" => ("YOUR LINES LIVE HERE.", "Manage career saves across the profiles in this installation. Every removal keeps a recovery copy."),
            "Setup" => ("BRING YOUR BOARD.", "You provide the game. HOPE brings the PC interface. No ISO downloads or retail game files are included."),
            "Credits" => ("RESPECT THE ROOTS.", "HOPE stands on the work of the original Skate3Recomp creator and the wider recompilation community."),
            _ => ("HOPE", "Hills, Ollies, Pavement, Expression")
        };
        if (destination == "Saves") RefreshSaves();
        if (destination == "Graphics") LoadGraphics();
        if (destination == "Setup") IsoLabel.Text = state.HasIso ? Path.GetFileName(state.Preferences.IsoPath) : "No ISO selected";
        Feedback.Text = destination == "Home" ? "Built for your next session." : "Changes stay with this installation.";
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
            GraphicsForm.IsEnabled = !running;
            SaveGraphicsButton.IsEnabled = !running;
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
        Feedback.Text = state.HasGame ? "HOPE started. Escape or RB + Start opens the PC menu." : "Opening the ISO installer. Keep its window open until setup finishes.";
        RefreshStatus();
    });

    private void LoadGraphics()
    {
        loading = true;
        bool Flag(string key, bool fallback) => bool.TryParse(SettingsFile.Read(state.SettingsPath, key, fallback.ToString()), out var v) ? v : fallback;
        int Number(string key, int fallback) => int.TryParse(SettingsFile.Read(state.SettingsPath, key, fallback.ToString()), out var v) ? v : fallback;
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
        loading = false;
        UpdateNativeControls();
    }

    private void Renderer_Changed(object sender, SelectionChangedEventArgs e) { if (!loading && NativeEffects != null) UpdateNativeControls(); }
    private void UpdateNativeControls() { NativeEffects.IsEnabled = AaCombo.IsEnabled = RendererCombo.SelectedIndex == 0; NativeEffects.Opacity = NativeEffects.IsEnabled ? 1 : 0.45; }
    private void RequireGameClosed()
    {
        if (state.IsGameRunning()) throw new IOException("Close the game before changing settings or removing saves here.");
    }
    private void SaveGraphics_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        RequireGameClosed();
        string Flag(CheckBox box) => box.IsChecked == true ? "true" : "false";
        var scale = (ResolutionCombo.SelectedIndex + 1).ToString();
        SettingsFile.Update(state.SettingsPath, new Dictionary<string, string>
        {
            ["skate3_native_render_scene"] = RendererCombo.SelectedIndex == 0 ? "true" : "false",
            ["resolution_scale"] = scale, ["draw_resolution_scale_x"] = scale, ["draw_resolution_scale_y"] = scale,
            ["skate3_native_render_scene_msaa"] = new[] { 1, 2, 4, 8 }[AaCombo.SelectedIndex].ToString(),
            ["skate3_native_render_scene_ssao"] = Flag(AoCheck), ["skate3_native_render_scene_ssao_full_res"] = Flag(AoQualityCheck),
            ["skate3_native_render_scene_fog"] = Flag(FogCheck), ["skate3_native_render_scene_haze"] = Flag(HazeCheck),
            ["skate3_native_render_scene_shafts"] = Flag(ShaftsCheck), ["skate3_native_render_scene_bloom"] = Flag(BloomCheck)
        });
        Feedback.Text = "Graphics saved. Your next session will use these settings.";
    });

    private void RefreshSaves()
    {
        pendingRemoval = null;
        DeleteConfirm.Visibility = Visibility.Collapsed;
        SaveList.ItemsSource = SaveStorage.List(state.UserRoot, state.BundleRoot);
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
        if (!SaveStorage.List(state.UserRoot, state.BundleRoot).Contains(selected)) throw new IOException("The save list changed. Refresh and choose the save again.");
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
        var image = new BitmapImage();
        image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.UriSource = new Uri(picker.FileName); image.EndInit();
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
        if (photos.Count == 0) { PhotoCaption.Text = "YOUR GAMEPLAY PHOTO GOES HERE"; return; }
        var image = new BitmapImage();
        image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.UriSource = new Uri(photos[photoIndex]); image.EndInit(); image.Freeze();
        Backdrop.Source = image;
        PhotoCaption.Text = $"{photoIndex + 1:00} / {photos.Count:00}   •   SKATE 3 STREET FLIGHT";
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
                if (focused is ComboBox combo && combo.IsEnabled)
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

