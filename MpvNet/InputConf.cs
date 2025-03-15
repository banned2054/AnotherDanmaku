using MpvNet.Help;

namespace MpvNet;

public class InputConf
{
    private string? _path;

    public InputConf(string path)
    {
        Path = path;
    }

    public string Content { get; set; } = "";

    public string Path
    {
        get => _path ?? "";
        set
        {
            if (_path == value) return;
            _path   = value;
            Content = File.Exists(_path) ? FileHelp.ReadTextFile(_path) : "";
        }
    }

    public bool HasMenu => Content.Contains(App.MenuSyntax + " ");

    public (List<Binding> menuBindings, List<Binding>? confBindings) GetBindings()
    {
        var confBindings = InputHelp.Parse(Content);

        if (HasMenu)
            return (confBindings, confBindings);

        var defaultBindings = InputHelp.GetDefaults();

        foreach (var defaultBinding in from defaultBinding in defaultBindings
                                       from confBinding in confBindings
                                       where defaultBinding.Input   == confBinding.Input &&
                                             defaultBinding.Command != confBinding.Command
                                       select defaultBinding)
            defaultBinding.Input = "";

        foreach (var defaultBinding in defaultBindings)
        foreach (var confBinding in confBindings.Where(confBinding => defaultBinding.Command == confBinding.Command))
            defaultBinding.Input = confBinding.Input;

        return (defaultBindings, confBindings);
    }

    public string GetContent()
    {
        if (HasMenu)
        {
            try
            {
                if (App.Settings.MenuUpdateVersion != 1)
                {
                    var updatedContent = UpdateContent(Content);

                    if (updatedContent != Content)
                    {
                        File.Copy(Path, Path + ".backup", true);
                        File.WriteAllText(Path, Content = updatedContent);
                    }

                    App.Settings.MenuUpdateVersion = 1;
                }
            }
            catch (Exception ex)
            {
                Terminal.WriteError("Failed to update menu." + Br + ex.Message);
            }

            return Content;
        }

        var defaults = InputHelp.GetDefaults();
        var removed  = new List<Binding>();
        var conf     = InputHelp.Parse(Content);

        foreach (var defaultBinding in defaults)
        foreach (var confBinding in conf.Where(confBinding => defaultBinding.Command == confBinding.Command &&
                                                              defaultBinding.Comment == confBinding.Comment))
        {
            defaultBinding.Input = confBinding.Input;
            removed.Add(confBinding);
        }

        foreach (var binding in removed)
            conf.Remove(binding);

        defaults.AddRange(conf);
        return InputHelp.ConvertToString(defaults);
    }

    private static string UpdateContent(string content) => content
                                                          .Replace("script-message mpv.net", "script-message-to mpvnet")
                                                          .Replace("/docs/Manual.md", "/docs/manual.md")
                                                          .Replace("https://github.com/stax76/mpv.net",
                                                                   "https://github.com/mpvnet-player/mpv.net");
}
