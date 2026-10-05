using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
namespace Hope.Launcher;
public sealed class GraphicsOption : INotifyPropertyChanged
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public bool IsToggle { get; init; }
    public double Minimum { get; init; }
    public double Maximum { get; init; }
    public double Step { get; init; } = .01;
    public double Default { get; init; }
    private double number;
    private bool enabled;
    public double Number { get => number; set { number=Math.Clamp(value,Minimum,Maximum); Changed(); Changed(nameof(DisplayValue)); } }
    public bool Enabled { get=>enabled; set {enabled=value;Changed();} }
    public string DisplayValue => Number.ToString("0.###",CultureInfo.InvariantCulture);
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name=null) => PropertyChanged?.Invoke(this,new(name));
    public void Load(Func<string,string,string> read)
    {
        var raw=read(Key,Default.ToString(CultureInfo.InvariantCulture));
        if(IsToggle) Enabled=bool.TryParse(raw,out var flag)?flag:Default!=0;
        else Number=double.TryParse(raw,NumberStyles.Float,CultureInfo.InvariantCulture,out var n)&&double.IsFinite(n)?n:Default;
    }
    public string Literal => IsToggle ? (Enabled?"true":"false") : DisplayValue;
}
public static class AdvancedGraphics
{
    private static GraphicsOption Flag(string key,string title,string description,bool value) => new(){Key=key,Title=title,Description=description,IsToggle=true,Default=value?1:0};
    private static GraphicsOption Range(string key,string title,string description,double value,double min,double max,double step) => new(){Key=key,Title=title,Description=description,Default=value,Minimum=min,Maximum=max,Step=step};
    public static List<GraphicsOption> World() => [
        Range("skate3_field_of_view","Camera field of view","Wider views show more of the street. Degrees.",60,40,120,1),
        Range("skate3_draw_distance_scale","World draw distance","1× is original distance; higher values cost more CPU and GPU time.",2,.25,16,.25),
        Range("skate3_lod_distance_scale","Character and vehicle detail distance","Keeps detailed models visible farther away. 1× is original distance.",2,.25,16,.25)
    ];
    public static List<GraphicsOption> Native() => [
        Flag("skate3_native_render_scene_hdr","HDR lighting pipeline","Internal high precision lighting for bloom, reflections and atmosphere. This does not enable HDR monitor output.",true),
        Flag("skate3_native_render_scene_ssr","Screen-space reflections (experimental)","Reflects visible scenery on glass and water. Requires HDR lighting. May show noise or smearing.",false),
        Range("skate3_native_render_scene_ssr_steps","Reflection quality","Ray steps per pixel; more steps cost more GPU time.",48,8,64,1),
        Range("skate3_native_render_scene_ssr_intensity","Reflection strength","0 removes the screen-space contribution; requires reflections and HDR lighting.",1,0,2,.05),
        Flag("skate3_native_render_scene_shadows","Dynamic shadows","Live shadows from characters and movable objects.",true),
        Flag("skate3_native_render_scene_shadow_static_casters","Live world shadows","Adds live building and street-object shade. Baked lighting is still present.",true),
        Range("skate3_native_render_scene_shadow_static_size","World shadow resolution","Resolution per cascade. Larger maps use more video memory and GPU time.",4096,1024,8192,1024),
        Flag("skate3_native_render_scene_shadow_pcss","Contact-hardening soft shadows","Crisp at contact points, softer farther from the caster.",true),
        Range("skate3_native_render_scene_shadow_pcss_sun_deg","Shadow softness","Sun angular diameter; larger values soften shadow edges.",2.5,.1,8,.1),
        Range("skate3_native_render_scene_ssao_intensity","Ambient occlusion strength","Requires ambient occlusion; 0 removes contact darkening.",1.4,0,4,.1),
        Range("skate3_native_render_scene_ssao_radius","Ambient occlusion radius","World-space size of contact shading. Larger values shade wider areas.",.8,.1,8,.1),
        Range("skate3_native_render_scene_bloom_intensity","Bloom strength","Glow around bright lamps and highlights. Requires bloom and HDR lighting.",.025,0,2,.005),
        Range("skate3_native_render_scene_shafts_steps","Sun-shaft quality","Volumetric ray steps; more steps cost more GPU time. Requires shadows, shafts and HDR lighting.",64,8,64,1),
        Flag("skate3_native_render_scene_tex_mips","Texture mipmaps","Filters distant textures to reduce shimmering.",true),
        Flag("skate3_native_render_scene_decals","Graffiti and painted decals","Displays authored surface artwork.",true),
        Flag("skate3_native_render_scene_quadlists","Particle draws (experimental)","Incomplete: sprite textures are missing, so particles can appear as floating white squares. Off by default.",false)
    ];
}
