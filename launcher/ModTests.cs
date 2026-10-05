using System.IO;
using System.Text;
using System.Text.Json;
namespace Hope.Launcher;
public static class ModTests
{
    public static void Run(string root, Action<bool,string> check)
    {
        Directory.CreateDirectory(root);
        var settings = Path.Combine(root,"settings.toml");
        var original = "# untouched\r\nskate3_field_of_view = 60 # camera\r\nskate3_native_render_scene_bloom = true\r\nother = 99\r\n[section]\r\nother = 5\r\n";
        File.WriteAllText(settings, original, new UTF8Encoding(true));
        var originalBytes = File.ReadAllBytes(settings);
        var manager = new ModManager(root,root,()=>false);
        check(manager.List().Count == 5 && manager.List().All(m=>!m.Enabled),"Five built-in mods start disabled");
        manager.Apply(["wide-streets","clean-lens","no-intro-videos"]);
        check(File.ReadAllText(settings).Contains("skate3_field_of_view = 75") && File.ReadAllText(settings).Contains("skate3_frontend_movies_auto_skip = true"),"Multiple mods apply supported values");
        check(manager.List().Count(m=>m.Enabled)==3,"Enabled selection persists");
        manager.Apply([]);
        check(File.ReadAllBytes(settings).SequenceEqual(originalBytes),"Disable restores original bytes including BOM, comments and tables");
        manager.Apply(["wide-streets"]);
        File.WriteAllText(settings,File.ReadAllText(settings).Replace("skate3_field_of_view = 75","skate3_field_of_view = 90"));
        manager.Apply([]);
        check(File.ReadAllText(settings).Contains("skate3_field_of_view = 90"),"Later manual camera edits survive disabling");
        var manifest = """{"formatVersion":1,"id":"custom-view","name":"Custom View","author":"Fixture","description":"Custom camera","settings":{"skate3_field_of_view":80}}""";
        var imported = Path.Combine(root,"custom.hope-mod.json"); File.WriteAllText(imported,manifest);
        manager.Import(imported);
        check(manager.List().Count==6 && !manager.List().Last().Enabled,"Imported mod starts disabled");
        var before=File.ReadAllBytes(settings);
        bool rejected=false; try{manager.Apply(["wide-streets","custom-view"]);}catch(Exception e) when(e is IOException or UnauthorizedAccessException){rejected=true;}
        check(rejected && File.ReadAllBytes(settings).SequenceEqual(before),"Conflicting mods do not change settings");
        rejected=false;try{manager.Import(imported);}catch(Exception e) when(e is IOException or UnauthorizedAccessException){rejected=true;}
        check(rejected,"Duplicate import preserves existing mod");
        foreach(var bad in new[]{manifest.Replace("80}","500}"),manifest.Replace("skate3_field_of_view","unknown_executable"),manifest.Replace("80}","true}"),manifest.Replace("\"formatVersion\":1","\"formatVersion\":2"),manifest.Replace("\"id\":\"custom-view\"","\"id\":\"../escape\""),manifest.Replace("\"formatVersion\":1","\"formatVersion\":1,\"formatVersion\":1")})
        {
            rejected=false;try{ModManager.Parse(bad);}catch(Exception e) when(e is IOException or InvalidDataException or JsonException){rejected=true;}
            check(rejected,"Invalid or unsupported manifest refused");
        }
        var career=Path.Combine(root,"another-career");Directory.CreateDirectory(career);
        check(new ModManager(root,career,()=>false).List().Count==6 && new ModManager(root,career,()=>false).List().All(m=>!m.Enabled),"Shared library keeps career enablement separate");
        rejected=false;try{new ModManager(root,root,()=>true).Apply(["custom-view"]);}catch(Exception e) when(e is IOException or UnauthorizedAccessException){rejected=true;}
        check(rejected && File.ReadAllBytes(settings).SequenceEqual(before),"Running game blocks apply without changes");
        var ledger=Path.Combine(root,"mods","state.json");var priorState=File.ReadAllBytes(ledger);
        using(var locked=new FileStream(ledger,FileMode.Open,FileAccess.Read,FileShare.Read))
        {
            rejected=false;try{manager.Apply(["custom-view"]);}catch(Exception e) when(e is IOException or UnauthorizedAccessException){rejected=true;}
            check(rejected && File.ReadAllBytes(settings).SequenceEqual(before),"Locked ledger rolls settings back");
        }
        check(File.ReadAllBytes(ledger).SequenceEqual(priorState) && !File.Exists(Path.Combine(root,"mods","transaction.json")),"Failed apply preserves ledger and clears recovered journal");
        manager.Apply(["custom-view"]); manager.Apply([]);
        check(File.ReadAllBytes(settings).SequenceEqual(before),"Imported mod applies and reverses");
        File.AppendAllText(settings,"\nskate3_field_of_view = 100\n"); // append is in table, not root
        var malformed="skate3_field_of_view = 60\nskate3_field_of_view = 70\n";File.WriteAllText(settings,malformed);
        rejected=false;try{manager.Apply(["wide-streets"]);}catch(Exception e) when(e is IOException or UnauthorizedAccessException){rejected=true;}
        check(rejected && File.ReadAllText(settings)==malformed,"Duplicate root settings refuse mutation");
    }
}
