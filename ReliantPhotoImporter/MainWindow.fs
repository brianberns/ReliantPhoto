namespace Reliant.Photo

open System
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
    let saveSettings (window : Window) (model : Model) =
        Settings.save {
            Left = window.Position.X
            Top = window.Position.Y
            Source = string model.Source
            Destination = string model.Destination
        }

    /// Watches for app shutdown.
    let watchShutdown
        (window : Window) (status : ImportStatus) : Subscribe<_> =
        fun dispatch ->

                // watch for window close event
            let handler =
                EventHandler<WindowClosingEventArgs>(fun _ args ->
                    if status.IsImporting then
                        args.Cancel <- true
                        dispatch FinishImport   // to-do: wait for this to finish and then allow the window to close?
                    else
                        dispatch Shutdown)
            window.Closing.AddHandler(handler)

                // cleanup
            {
                new IDisposable with
                    member _.Dispose() =
                        window.Closing.RemoveHandler(handler)
            }

    /// Subscribes to app shutdown.
    let subscribeShutdown window model : Sub<_> =
        let status = model.ImportStatus
        [
            [ "Shutdown"; string status ],
                watchShutdown window status
        ]

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
            |> Program.withSubscription (
                subscribeShutdown window)

            |> Program.withTermination _.IsShutdown (
                saveSettings window)

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
                this.Position <-
                    PixelPoint(settings.Left, settings.Top))

            // ... source drive
        let sourceOpt =
            settingsOpt
                |> Option.bind (
                    _.Source >> DriveInfo.TryParse)

            // ... destination directory
        let destOpt =
            settingsOpt
                |> Option.bind (
                    _.Destination >> DirectoryInfo.TryParse)

        Window.run this (sourceOpt, destOpt)
