namespace Reliant.Photo

open System
open System.IO

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts

module Window =

    let loadSettings (window : Window) =
        Settings.tryLoad ()
            |> Option.iter (fun settings ->
            if settings.Maximized then
                window.WindowState <- WindowState.Maximized
                // other settings are garbage when window is maximized: https://github.com/AvaloniaUI/Avalonia/issues/5285
            else
                window.Position <- PixelPoint(settings.Left, settings.Top)
                window.Width <- settings.Width
                window.Height <- settings.Height)

    let saveSettings (window : Window) =
        Settings.save {
            Left = window.Position.X
            Top = window.Position.Y
            Width = window.Width
            Height = window.Height
            Maximized = window.WindowState = WindowState.Maximized
        }

    let getInitialArg (args : _[]) =
        if args.Length = 0 then
            Environment.SpecialFolder.MyPictures
                |> Environment.GetFolderPath
                |> DirectoryInfo
                :> FileSystemInfo
        else
            args[0]
                |> FileInfo
                :> _

    let setTitle (window : Window) model =
        window.Title <- model.File.Name

    let run window arg =
        Elmish.Program.mkProgram
            Message.init
            (Message.update (setTitle window))
            View.view
            |> Program.withHost window
            |> Program.runWithAvaloniaSyncDispatch arg

type MainWindow(args : _[]) as this =
    inherit HostWindow(Title = "Reliant Photo")
    do
        Window.loadSettings this
        this.Closing.Add(fun _ ->
            Window.saveSettings this)
        Window.getInitialArg args
            |> Window.run this
