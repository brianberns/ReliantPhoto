namespace Reliant.Photo

open System
open System.Diagnostics
open System.IO
open System.Text.Json

type Settings =
    {
        Left : int
        Top : int
        Width : double
        Height : double
        Maximized : bool
    }

module Settings =

    let private settingsFile =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "ReliantPhoto",
            "Settings.json")
            |> FileInfo

    let private options =
        JsonSerializerOptions(WriteIndented = true)

    let save (settings : Settings) =
        try
            let json = JsonSerializer.Serialize(settings, options)
            settingsFile.Directory.Create()
            File.WriteAllText(settingsFile.FullName, json)
        with exn ->
            Trace.WriteLine($"Could not save settings: {exn.Message}")

    let tryLoad () =
        try
            if settingsFile.Exists then
                let json = File.ReadAllText(settingsFile.FullName)
                JsonSerializer.Deserialize<Settings>(json)
                    |> Some
            else
                None
        with exn ->
            Trace.WriteLine($"Could not load settings: {exn.Message}")
            None
