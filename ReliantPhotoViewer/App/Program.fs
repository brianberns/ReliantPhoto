namespace Reliant.Photo

open System
open System.Text
open System.Threading
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

    /// Maximum display string length.
    let private maxStringLength = 2000

    /// Shortens the given string, if necessary.
    let private shorten (str : string) =
        let ellipsis = "…"
        if str.Length > maxStringLength then
            str.Substring(0, maxStringLength - ellipsis.Length)
                + ellipsis
        else str

    /// Creates an exception handler message box.
    let private createMessageBox (exn : exn) =

            // create dialog
        let msgBox =
            let sb = StringBuilder()
            sb.AppendLine(exn.Message) |> ignore
            sb.Append(shorten exn.StackTrace) |> ignore
            MessageBoxManager.GetMessageBoxStandard(
                "Error", sb.ToString())

            // ugh - we have to show the dialog asynchronously
        let owner =
            match Application.Current.ApplicationLifetime with
                | :? IClassicDesktopStyleApplicationLifetime as lifetime ->
                    lifetime.MainWindow
                | _ -> null
        msgBox.ShowWindowDialogAsync(owner)

    /// Handles an exception on the UI thread.
    let private handleUiException exn =
        assert(Dispatcher.UIThread.CheckAccess())
        let task = createMessageBox exn
        let frame = DispatcherFrame()            // nested event loop
        task.ContinueWith(fun (_ : Task<_>) ->
            frame.Continue <- false)             // exit the nested loop
            |> ignore
        Dispatcher.UIThread.PushFrame(frame)

    /// Handles an exception on a background thread.
    let private handleBackgroundException exn =
        assert(not (Dispatcher.UIThread.CheckAccess()))
        use waitHandle = new ManualResetEvent(false)
        Dispatcher.UIThread.InvokeAsync(fun () ->
            let task = createMessageBox exn
            task.ContinueWith(fun (_ : Task<_>) ->
                waitHandle.Set())        // unblock thread
                |> ignore)
            |> ignore
        waitHandle.WaitOne() |> ignore   // block thread to prevent .NET from aborting

    [<EntryPoint>]
    let main args =

            // handle exceptions on background threads
        AppDomain.CurrentDomain
            .UnhandledException
            .Add(fun args ->
                args.ExceptionObject
                    :?> Exception
                    |> handleBackgroundException)

        try
            AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .UseSkia()
                .StartWithClassicDesktopLifetime(args)
        with exn ->
            handleUiException exn
            1
