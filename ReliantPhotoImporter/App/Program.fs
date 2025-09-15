namespace Reliant.Photo

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Themes.Fluent

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add(FluentTheme())

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
            | :? IClassicDesktopStyleApplicationLifetime as lifetime ->
                lifetime.MainWindow <- MainWindow(lifetime.Args)
            | _ -> ()

module Program =

    [<EntryPoint>]
    let main args =
        try
            AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .UseSkia()
                .StartWithClassicDesktopLifetime(args)
        with exn ->
            printfn $"{exn}"
            1
