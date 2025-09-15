namespace Reliant.Photo

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Styling
open Avalonia.Themes.Fluent

type App() =
    inherit Application(
        RequestedThemeVariant = ThemeVariant.Dark)

    override this.Initialize() =
        this.Styles.Add(FluentTheme())

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
            | :? IClassicDesktopStyleApplicationLifetime as lifetime ->
                lifetime.MainWindow <- MainWindow(lifetime.Args)
            | _ -> ()

module Program =

    /// Maximum display string length.
    let private maxStringLength = 2000

    /// Shortens the given string, if necessary.
    let private shorten (str : string) =
        let ellipsis = "…"
        if str.Length > maxStringLength then
            str.Substring(0, maxStringLength - ellipsis.Length)
                + ellipsis
        else str

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
