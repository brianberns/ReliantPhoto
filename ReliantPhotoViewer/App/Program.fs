namespace Reliant.Photo

open System

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Styling
open Avalonia.Themes.Fluent

open MsBox.Avalonia
open MsBox.Avalonia.Enums

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

    /// Handles an exception.
    let private handleException (exn : exn) =
        MessageBoxManager.GetMessageBoxStandard(
            "Error",
            exn.Message,
            ButtonEnum.Ok,
            Icon.Error)
                |> ignore

    [<EntryPoint>]
    let main args =

            // handle exceptions on background threads
        AppDomain.CurrentDomain
            .UnhandledException
            .Add(fun args ->
                args.ExceptionObject
                    :?> Exception
                    |> handleException)

        try
            AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .UseSkia()
                .StartWithClassicDesktopLifetime(args)
        with exn ->
            handleException exn
            1
