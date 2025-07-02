namespace Reliant.Photo

open Elmish

open System
open System.IO

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts

module Window =

    /// Current directory.
    let mutable directory =
        Environment.SpecialFolder.MyPictures
            |> Environment.GetFolderPath
            |> DirectoryInfo

    /// Loads user settings, if possible.
    let loadSettings (window : Window) =
        Settings.tryLoad ()
            |> Option.iter (fun settings ->

                if settings.Maximized then
                    window.WindowState <- WindowState.Maximized
                    // other settings are garbage when window is maximized: https://github.com/AvaloniaUI/Avalonia/issues/5285
                else
                    window.Position <- PixelPoint(settings.Left, settings.Top)
                    window.Width <- settings.Width
                    window.Height <- settings.Height

                let dir = DirectoryInfo(settings.Directory)
                if dir.Exists then directory <- dir)

    /// Saves user settings.
    let saveSettings (window : Window) =
        Settings.save {
            Left = window.Position.X
            Top = window.Position.Y
            Width = window.Width
            Height = window.Height
            Maximized = window.WindowState = WindowState.Maximized
            Directory = directory.FullName
        }

    /// Subscribes to effects.
    let subscribe (window : Window) model =
        [
                // directory subscriptions
                // ... side effects
            let dirModel = model.DirectoryModel
            directory <- dirModel.Directory
            if dirModel.Directory.FullName <> window.Title then
                window.Title <- dirModel.Directory.FullName

                // ... Elmish subscription
            yield! dirModel
                |> DirectoryMessage.subscribe
                |> Sub.map "directory" MkDirectoryMessage

                // image subscriptions
            match model.ImageModelOpt with
                | Some imgModel ->

                        // side effects
                    if imgModel.File.Name <> window.Title then
                        window.Title <- imgModel.File.Name

                        // Elmish subscription
                    yield! Sub.none

                | None -> ()
        ]

    /// Gets initial file or directory.
    let getInitialArg (args : _[]) =
        if args.Length = 0 then
            directory :> FileSystemInfo
        else
            args[0]
                |> FileInfo
                :> _

    /// Starts the Elmish MVU loop.
    let run window arg =
        Program.mkProgram Message.init Message.update View.view
            |> Program.withSubscription (
                subscribe window)
            |> Program.withHost window
            |> Program.withConsoleTrace
            |> Program.runWithAvaloniaSyncDispatch arg

/// Main window.
type MainWindow(args : string[]) as this =
    inherit HostWindow(
        Title = "Reliant Photo Viewer",
        Icon =
            WindowIcon(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "icon.png")))
    do
            // load settings at startup
        Window.loadSettings this

            // save settings at exit
        this.Closing.Add(fun _ ->
            Window.saveSettings this)

            // run the app
        Window.getInitialArg args
            |> Window.run this
