using System.Text.Json;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

public sealed class EzSettings
{
    public bool EasyPathEnabled { get; set; } = true;

    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EZ",
            "settings.json");

    public static EzSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new EzSettings();
            }

            return JsonSerializer.Deserialize<EzSettings>(File.ReadAllText(FilePath))
                   ?? new EzSettings();
        }
        catch (JsonException)
        {
            return new EzSettings();
        }
        catch (IOException)
        {
            return new EzSettings();
        }
    }

    public void Save()
    {
        string? directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(FilePath, json);
    }

    public void ShowInteractive()
    {
        using IApplication app = Application.Create();
        app.Init();

        using Window window = new()
        {
            Title = "EZ Settings",
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = 60,
            Height = 10
        };

        CheckBox pathCheck = new()
        {
            X = 2,
            Y = 1,
            Text = "Enable easy path for git commands",
            Value = EasyPathEnabled ? CheckState.Checked : CheckState.UnChecked
        };

        Button saveButton = new()
        {
            X = Pos.Center() - 10,
            Y = 5,
            Text = "Save",
            IsDefault = true
        };

        Button cancelButton = new()
        {
            X = Pos.Center() + 2,
            Y = 5,
            Text = "Cancel"
        };

        saveButton.Accepted += (_, _) =>
        {
            EasyPathEnabled = pathCheck.Value == CheckState.Checked;
            Save();
            app.RequestStop();
        };

        cancelButton.Accepted += (_, _) => app.RequestStop();

        window.Add(pathCheck, saveButton, cancelButton);
        app.Run(window);
    }
}
