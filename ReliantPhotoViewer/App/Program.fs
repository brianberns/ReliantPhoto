namespace Reliant.Photo

open System
open System.Text
open System.Threading.Tasks

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Styling
open Avalonia.Themes.Fluent
open Avalonia.Threading

open MsBox.Avalonia

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

            // create dialog
        let msgBox =
            let sb = StringBuilder()
            sb.AppendLine(exn.Message) |> ignore
            sb.Append(exn.StackTrace) |> ignore
            MessageBoxManager.GetMessageBoxStandard(
                "Error", sb.ToString())

            // ugh - we have to show the dialog asynchronously
        let task = msgBox.ShowAsync()

            // on UI thread?
        if Dispatcher.UIThread.CheckAccess() then
            let frame = DispatcherFrame()            // nested event loop
            task.ContinueWith(fun (_ : Task<_>) ->   // exit the nested loop
                frame.Continue <- false)
                |> ignore
            Dispatcher.UIThread.PushFrame(frame)
        else
            task.Wait()

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
