namespace Reliant.Photo

open Elmish

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts

module Window =

    /// Loads user settings, if possible.
    let loadSettings (window : Window) =
        Settings.tryLoad ()
            |> Option.iter (fun settings ->
                window.Position <- PixelPoint(settings.Left, settings.Top))

    /// Saves user settings.
    let saveSettings (window : Window) =
        Settings.save {
            Left = window.Position.X
            Top = window.Position.Y
            Source = ""
            Destination = ""
        }

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
        CanResize = false)
    do
            // load settings at startup
        Window.loadSettings this

            // save settings at exit
        this.Closing.Add(fun _ ->
            Window.saveSettings this)

        Window.run this ()
