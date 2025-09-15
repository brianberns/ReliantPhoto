namespace Reliant.Photo

open Elmish

open Avalonia.Controls
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts

module Window =

    /// Gets the window icon.
    let getIcon () =
        Resource.get "ReliantPhotoImporter.png"
            |> WindowIcon

    /// Creates Elmish program.
    let private makeProgram dpiScale =

        let init = fun () -> ()
        let update = fun () () -> ()
        let view () dispatch = Avalonia.FuncUI.DSL.StackPanel.create []

        Program.mkSimple init update view

    /// Starts the Elmish MVU loop.
    let run (window : HostWindow) arg =
        makeProgram window.RenderScaling
            |> Program.withHost window
#if DEBUG
            |> Program.withConsoleTrace
#endif
            |> Program.runWithAvaloniaSyncDispatch arg

/// Main window.
type MainWindow(args : string[]) as this =
    inherit HostWindow(
        Title = "Reliant Photo Importer",
        Icon = Window.getIcon (),
        Width = 600.0,
        Height = 400.0,
        CanResize = false)
    do
        Window.run this ()

