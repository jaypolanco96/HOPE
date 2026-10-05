using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
namespace Hope.Launcher;
public static class AdvancedGraphicsTests
{
    public static void Run(string root, Action<bool,string> check)
    {
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root,"portable.txt"),"");
        check(new LauncherState(root).SettingsPath == Path.Combine(root,"settings.toml"), "Graphics fixture is confined to temporary portable root");
        var path=Path.Combine(root,"settings.toml");
        File.WriteAllText(path,"unrelated = 73\nskate3_native_render_scene = true\nskate3_native_render_scene_ssr = false\nskate3_native_render_scene_bloom_intensity = 0.025\n");
        using(var window=new MainWindow(root))
        {
            window.Navigate("Graphics");
            var world=window.WorldGraphicsOptions.Items.Cast<GraphicsOption>().ToList();
            var native=window.NativeGraphicsOptions.Items.Cast<GraphicsOption>().ToList();
            check(world.Count==3 && native.Count==16,"Advanced controls expose actual renderer settings");
            check(native.Single(o=>o.Key.EndsWith("bloom_intensity")).Number==.025 && !native.Single(o=>o.Key.EndsWith("quadlists")).Enabled,"Precise bloom loads and experimental particles start off: " + native.Single(o=>o.Key.EndsWith("bloom_intensity")).Number.ToString("R"));
            world.Single(o=>o.Key=="skate3_draw_distance_scale").Number=3.25;
            native.Single(o=>o.Key.EndsWith("ssr")).Enabled=true;
            native.Single(o=>o.Key.EndsWith("ssr_steps")).Number=56;
            native.Single(o=>o.Key.EndsWith("bloom_intensity")).Number=.035;
            var culture=CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("fr-FR");
                window.SaveGraphicsButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            } finally {CultureInfo.CurrentCulture=culture;}
            check(SettingsFile.Read(path,"skate3_draw_distance_scale","")=="3.25" && SettingsFile.Read(path,"skate3_native_render_scene_bloom_intensity","")=="0.035","Advanced graphics save valid TOML independently of locale: " + window.Feedback.Text + " / " + File.ReadAllText(path));
            check(SettingsFile.Read(path,"skate3_native_render_scene_ssr","")=="true" && SettingsFile.Read(path,"skate3_native_render_scene_ssr_steps","")=="56","Reflections and quality save together");
            check(SettingsFile.Read(path,"unrelated","")=="73","Graphics save preserves unrelated values");
            window.RendererCombo.SelectedIndex=1;
            check(!window.AdvancedNativePanel.IsEnabled && window.WorldGraphicsOptions.IsEnabled,"Emulated renderer disables native-only controls");
        }
        using(var window=new MainWindow(root))
        {
            var native=window.NativeGraphicsOptions.Items.Cast<GraphicsOption>().ToList();
            check(native.Single(o=>o.Key.EndsWith("ssr")).Enabled && native.Single(o=>o.Key.EndsWith("ssr_steps")).Number==56,"Advanced graphics round trip through launcher");
        }
        var option=AdvancedGraphics.World()[0];
        option.Load((key,fallback)=>"NaN");check(option.Number==60,"Non-finite settings recover to safe defaults");
        option.Load((key,fallback)=>"500");check(option.Number==120,"Out-of-range values clamp to renderer limits");
    }
}
