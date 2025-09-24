namespace Reliant.Photo

open System.IO

open Elmish

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts

[<AutoOpen>]
module FileSystemExt =

    type DriveInfo with

        /// Tries to parse a drive.
        static member TryParse(name) =
            try Some (DriveInfo(name))
            with _ -> None

    type DirectoryInfo with

        /// Tries to parse a directory.
        static member TryParse(path) =
            try Some (DirectoryInfo(path))
            with _ -> None

module Window =

    /// Saves user settings.
    let saveSettings (window : Window) =
        Settings.save {
            Left = window.Position.X
            Top = window.Position.Y
            Source = ""
            Destination = ""
        }

    /// Starts the Elmish MVU loop.
    let run (window : HostWindow) arg =
        Program.mkProgram
            Message.init
            Message.update
            View.view

            |> Program.withHost window
#if DEBUG
            |> Program.withConsoleTrace
#endif
            |> Program.withErrorHandler (fun (msg, exn) ->
                printfn $"{msg}"
                printfn $"{exn.Message}"
                printfn $"{exn.StackTrace}")

            |> Program.runWithAvaloniaSyncDispatch arg

/// Main window.
type MainWindow(args : string[]) as this =
    inherit HostWindow(
        Title = "Reliant Photo Importer",
        CanResize = false)
    do
            // load settings at startup
        let settingsOpt = Settings.tryLoad()

            // ... window position
        settingsOpt
            |> Option.iter (fun settings ->
                this.Position <- PixelPoint(settings.Left, settings.Top))

            // ... source drive
        let sourceOpt =
            settingsOpt
                |> Option.bind (_.Source >> DriveInfo.TryParse)

            // ... destination directory
        let destOpt =
            settingsOpt
                |> Option.bind (_.Destination >> DirectoryInfo.TryParse)

            // save settings at exit
        this.Closing.Add(fun _ ->
            Window.saveSettings this)

        Window.run this (sourceOpt, destOpt)
