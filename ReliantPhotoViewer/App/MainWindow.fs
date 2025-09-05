namespace Reliant.Photo

open System
open System.IO

open Elmish

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts
open Avalonia.Platform

module Window =

    /// Current directory.
    let mutable directory =
        Environment.SpecialFolder.MyPictures
            |> Environment.GetFolderPath
            |> DirectoryInfo

    /// Gets the window icon.
    let getIcon () =
        Resource.get "ReliantPhoto.png"
            |> WindowIcon

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

    /// Applies effects that can't currently be expressed in
    /// the FuncUI DSL.
    let private applyEffects (window : Window) model =

            // current directory and window title
        let dir, title =
            match model with
                | DirectoryMode (dirModel, _) ->
                    dirModel.Directory,
                    dirModel.Directory.FullName
                | ImageMode (_, Situated_ situated) ->
                    situated.File.Directory,
                    situated.File.Name
                | _ -> directory, window.Title
        directory <- dir
        window.Title <- title

            // full screen?
        let fullScreen =
            match model with
                | ImageMode (_, Loaded loaded) ->
                    loaded.FullScreen
                | _ -> false
        window.WindowState <-
            if fullScreen then WindowState.FullScreen
            else WindowState.Normal

    /// Subscribes to effects.
    let subscribe (window : Window) model =

            // non-DSL effects
        applyEffects window model

            // Elmish subscription
        match Model.tryGetDirectoryModel model with
            | Some dirModel ->
                dirModel
                    |> DirectoryMessage.subscribe
                    |> Sub.map "directory" MkDirectoryMessage
            | None -> []

    /// Gets initial directory and file.
    let getInitialArg (args : _[]) =
        if args.Length = 0 then
            Choice1Of2 directory
        else
            Choice2Of2 (FileInfo args[0])

    /// Starts the Elmish MVU loop.
    let run window arg =
        let dpiScale = TopLevel.GetTopLevel(window).RenderScaling
        Program.mkProgram
            Message.init
            (Message.update dpiScale)
            View.view
            |> Program.withSubscription (
                subscribe window)
            |> Program.withHost window
#if DEBUG
            |> Program.withConsoleTrace
#endif
            |> Program.runWithAvaloniaSyncDispatch arg

/// Main window.
type MainWindow(args : string[]) as this =
    inherit HostWindow(
        Title = "Reliant Photo Viewer",
        Icon = Window.getIcon (),
        MinWidth = 600.0,
        MinHeight = 400.0)
    do
            // load settings at startup
        Window.loadSettings this

            // save settings at exit
        this.Closing.Add(fun _ ->
            Window.saveSettings this)

            // run the app
        Window.getInitialArg args
            |> Window.run this