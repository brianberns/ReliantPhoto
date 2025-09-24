namespace Reliant.Photo

open System
open System.IO
open System.Text.Json

/// Persistent user settings.
type Settings =
    {
        /// X-coordinate of window.
        Left : int

        /// Y-coordinate of window.
        Top : int

        /// Source drive.
        Source : string

        /// Destination directory.
        Destination : string
    }

module Settings =

    /// File in which settings are stored.
    let private settingsFile =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "ReliantPhotoImporter",
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
            printfn $"Could not save settings: {exn.Message}"

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
            printfn $"Could not load settings: {exn.Message}"
            None
