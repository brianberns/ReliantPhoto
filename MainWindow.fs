namespace Reliant.Photo

open System
open System.IO

open Elmish

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

    let getInitialPath (args : _[]) =
        if args.Length > 0 then
            FileInfo(args[0])
        else
            let dirInfo =
                Environment.SpecialFolder.MyPictures
                    |> Environment.GetFolderPath
                    |> DirectoryInfo
            dirInfo.GetFiles()[0]

    let run window path =
        Elmish.Program.mkProgram
            Image.init
            Image.update
            Image.view
            |> Program.withHost window
            |> Program.runWith path

type MainWindow(args : _[]) as this =
    inherit HostWindow(Title = "Reliant Photo")
    do
        Window.loadSettings this
        this.Closing.Add(fun _ ->
            Window.saveSettings this)
        Window.getInitialPath args
            |> Window.run this
