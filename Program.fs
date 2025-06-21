namespace Reliant.Photo

open System
open System.IO

open Elmish

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts
open Avalonia.Themes.Fluent

type MainWindow(args : _[]) as this =
    inherit HostWindow(Title = "Reliant Photo")

    let onClosing _ =
        Settings.save {
            Left = this.Position.X
            Top = this.Position.Y
            Width = this.Width
            Height = this.Height
            Maximized = this.WindowState = WindowState.Maximized
        }

    do
        Settings.tryLoad ()
            |> Option.iter (fun settings ->
            if settings.Maximized then
                this.WindowState <- WindowState.Maximized
                // other settings are garbage when window is maximized: https://github.com/AvaloniaUI/Avalonia/issues/5285
            else
                this.Position <- PixelPoint(settings.Left, settings.Top)
                this.Width <- settings.Width
                this.Height <- settings.Height)

        let path =
            if args.Length > 0 then
                FileInfo(args[0])
            else
                let dirInfo =
                    Environment.SpecialFolder.MyPictures
                        |> Environment.GetFolderPath
                        |> DirectoryInfo
                dirInfo.GetFiles()[0]

        this.Closing.Add(onClosing)

        Elmish.Program.mkProgram
            Image.init
            Image.update
            Image.view
            |> Program.withHost this
            |> Program.runWith path

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add(FluentTheme())
        this.RequestedThemeVariant <- Styling.ThemeVariant.Dark

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
            | :? IClassicDesktopStyleApplicationLifetime as lifetime ->
                lifetime.MainWindow <- MainWindow(lifetime.Args)
            | _ -> ()

module Program =

    [<EntryPoint>]
    let main args =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)
