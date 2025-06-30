namespace Reliant.Photo

open Elmish

open System
open System.IO

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts

module Window =

    let mutable directory =
        Environment.SpecialFolder.MyPictures
            |> Environment.GetFolderPath
            |> DirectoryInfo

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
                directory <- DirectoryInfo(settings.Directory))

    let saveSettings (window : Window) =
        Settings.save {
            Left = window.Position.X
            Top = window.Position.Y
            Width = window.Width
            Height = window.Height
            Maximized = window.WindowState = WindowState.Maximized
            Directory = directory.FullName
        }

    let subscribe (window : Window) = function
        | MkDirectoryModel dirModel ->

            directory <- dirModel.Directory
            if dirModel.Directory.FullName <> window.Title then
                window.Title <- dirModel.Directory.FullName

            dirModel
                |> DirectoryMessage.subscribe
                |> Sub.map "directory" MkDirectoryMessage

        | MkImageModel imgModel ->

            if imgModel.File.Name <> window.Title then
                window.Title <- imgModel.File.Name

            Sub.none

    let run window =
        Program.mkProgram Message.init Message.update View.view
            |> Program.withSubscription (
                subscribe window)
            |> Program.withHost window
            |> Program.withConsoleTrace
            |> Program.runWithAvaloniaSyncDispatch
                directory

type MainWindow(_args : string[]) as this =
    inherit HostWindow(
        Title = "Reliant Photo Viewer",
        Icon = WindowIcon("icon.png"))
    do
        Window.loadSettings this
        this.Closing.Add(fun _ ->
            Window.saveSettings this)
        Window.run this
