namespace Reliant.Photo

open Elmish

open Avalonia.Controls
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts

module Window =

    /// Gets the window icon.
    let getIcon () =
        "ReliantPhotoImporter.png"
            |> Resource.get Asset.path
            |> WindowIcon

    /// Creates Elmish program.
    let private makeProgram =
        Program.mkProgram
            Message.init
            Message.update
            View.view
            |> Program.withErrorHandler (fun (msg, exn) ->
                printfn $"{msg}"
                printfn $"{exn.Message}"
                printfn $"{exn.StackTrace}")

    /// Starts the Elmish MVU loop.
    let run (window : HostWindow) arg =
        makeProgram
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
        CanResize = false)
    do
        Window.run this ()

