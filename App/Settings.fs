namespace Reliant.Photo

open System
open System.Diagnostics
open System.IO
open System.Text.Json

/// Persistent user settings.
type Settings =
    {
        /// X-coordinate of window.
        Left : int

        /// Y-coordinate of window.
        Top : int

        /// Width of window.
        Width : double

        /// Height of window.
        Height : double

        /// Window is maximized?
        Maximized : bool
    }

module Settings =

    /// File in which settings are stored.
    let private settingsFile =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "ReliantPhoto",
            "Settings.json")
            |> FileInfo

    /// Serialization options.
    let private options =
        JsonSerializerOptions(WriteIndented = true)

    /// Saves the given settings.
    let save (settings : Settings) =
        try
            let json = JsonSerializer.Serialize(settings, options)
            settingsFile.Directory.Create()
            File.WriteAllText(settingsFile.FullName, json)
        with exn ->
            Trace.WriteLine($"Could not save settings: {exn.Message}")

    /// Tries to load settings from a file.
    let tryLoad () =
        try
            if settingsFile.Exists then
                settingsFile.FullName
                    |> File.ReadAllText
                    |> JsonSerializer.Deserialize<Settings>
                    |> Some
            else None
        with exn ->
            Trace.WriteLine($"Could not load settings: {exn.Message}")
            None
