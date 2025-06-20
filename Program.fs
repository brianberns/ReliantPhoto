namespace Reliant.Photo

open System
open System.IO

open Elmish

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts
open Avalonia.Themes.Fluent

type MainWindow(args : _[]) as this =
    inherit HostWindow(
        Title = "Reliant Photo",
        Width = 400.0,
        Height = 400.0)
    do
        let path =
            if args.Length > 0 then
                FileInfo(args[0])
            else
                let dirInfo =
                    Environment.SpecialFolder.MyPictures
                        |> Environment.GetFolderPath
                        |> DirectoryInfo
                dirInfo.GetFiles()[0]

        Elmish.Program.mkProgram
            Location.init
            Location.update
            Location.view
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
