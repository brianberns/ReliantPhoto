namespace Reliant.Photo

open System
open System.IO

open Elmish

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts
open Avalonia.LogicalTree
open Avalonia.Threading

module Window =

    /// Gets the window icon.
    let getIcon () =
        Resource.get "ReliantPhoto.png"
            |> WindowIcon

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
            Maximized =
                match window.WindowState with
                    | WindowState.Maximized
                    | WindowState.FullScreen -> true
                    | _ -> false
            Directory = directory.FullName
        }

    /// Default window title.
    let defaultTitle = "Reliant Photo Viewer"

    /// Sets the current directory.
    let private setCurrentDirectory model =
        directory <-
            match model with
                | DirectoryMode (dirModel, _) ->
                    dirModel.Directory
                | ImageMode (_, Situated_ situated) ->
                    situated.File.Directory
                | _ -> directory

    /// Sets the window title
    let private setWindowTitle (window : Window) model =
        window.Title <-
            match model with
                | DirectoryMode (dirModel, _) ->
                    DirectoryInfo.normalizedPath
                        dirModel.Directory
                | ImageMode (_, Situated_ situated) ->
                    situated.File.Name
                | ImageMode (_, Empty _) ->
                    defaultTitle
                | _ -> window.Title

    /// Tries to find a child element by type.
    let rec private tryFindChild<'t when 't :> Control>
        (parent : ILogical) =
        parent.LogicalChildren
            |> Seq.tryPick (function
                | :? 't as child -> Some child
                | child -> tryFindChild child)

    /// Saved window state.
    let mutable private savedWindowStateOpt = None

    /// Sets the window state.
    let private setWindowState (window : Window) model =
        match model, savedWindowStateOpt with

            | ImageMode (_, Sized_ sized), None
                when sized.FullScreen ->

                    // switch to full screen
                savedWindowStateOpt <- Some window.WindowState
                window.WindowState <- WindowState.FullScreen

            | ImageMode (_, Sized_ sized), Some state
                when not sized.FullScreen ->

                    // restore previous state
                savedWindowStateOpt <- None
                window.WindowState <- state

            | _ -> ()

            // hack: grab focus so key bindings work
        if isNull (window.FocusManager.GetFocusedElement()) then
            tryFindChild<Border> window
                |> Option.iter (fun control ->
                    control.Focus() |> ignore)

    /// Watches for DPI scale changes.
    let watchDpiScale (window : Window) : Subscribe<_> =
        fun dispatch ->

                // watch for changes
            window.ScalingChanged.Add(fun _ ->
                window.RenderScaling
                    |> DpiChanged
                    |> MkImageMessage
                    |> dispatch)

                // cleanup
            {
                new IDisposable with
                    member _.Dispose() = ()
            }

    /// Subscribes to DPI scale changes.
    let subscribeDpiScale window : Sub<_> =
        [
            [ "DpiScale" ], watchDpiScale window
        ]

    /// Subscribes to effects.
    let subscribe window model =

            // non-DSL effects
        setCurrentDirectory model
        setWindowTitle window model
        setWindowState window model

            // DPI scale subscription
        let dpiSub = subscribeDpiScale window

            // Elmish subscription
        let dirSub =
            match Model.tryGetDirectoryModel model with
                | Some dirModel ->
                    dirModel
                        |> DirectoryMessage.subscribe
                        |> Sub.map "directory" MkDirectoryMessage
                | None -> []

        Sub.batch [ dpiSub; dirSub ]

    /// Gets initial directory and file.
    let getInitialArg (args : _[]) =
        if args.Length = 0 then
            Choice1Of2 directory
        else
            Choice2Of2 (FileInfo args[0])

    /// Creates Elmish program.
    let private makeProgram dpiScale =

        let init = Message.init dpiScale
        let update = Message.update
        let view = View.view

#if DEBUG
        /// Short model string.
        let getModelString = function
            | DirectoryMode (dirModel, _) ->
                dirModel.Directory.FullName
            | ImageMode (_, imgModel) ->
                $"{Microsoft.FSharp.Reflection
                    .FSharpValue.GetUnionFields(
                        imgModel,
                        typeof<ImageModel>) |> fst}"

        /// Traced init function.
        let init arg =
            let model, cmd = init arg
            printfn $"Initial state: {getModelString model}"
            model, cmd

        /// Message number.
        let mutable msgNum = 0

        /// Traced update function.
        let update msg model =
            msgNum <- msgNum + 1
            printfn ""
            printfn $"Message #{msgNum}: {msg}"
            let model, cmd = update msg model
            printfn $"Updated state: {getModelString model}"
            model, cmd
#endif

        Program.mkProgram init update view

    /// Starts the Elmish MVU loop.
    let run (window : HostWindow) arg =
        makeProgram window.RenderScaling
            |> Program.withSubscription (
                subscribe window)
            |> Program.withHost window
            |> Program.runWithAvaloniaSyncDispatch arg

/// Main window.
type MainWindow(args : string[]) as this =
    inherit HostWindow(
        Title = Window.defaultTitle,
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