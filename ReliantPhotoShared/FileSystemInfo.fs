namespace Reliant.Photo

open System.IO

open Avalonia.Controls
open Avalonia.Interactivity
open Avalonia.Platform.Storage

module DirectoryInfo =

    /// Allows user to pick a directory.
    let onPick dispatchDir args =
        let topLevel =
            (args : RoutedEventArgs).Source
                :?> Control
                |> TopLevel.GetTopLevel
        async {
            let! folders =
                let options = FolderPickerOpenOptions()
                topLevel
                    .StorageProvider
                    .OpenFolderPickerAsync(options)
                    |> Async.AwaitTask
            option {
                let! folder = Seq.tryHead folders
                let! path =
                    folder.TryGetLocalPath()
                        |> Option.ofObj
                DirectoryInfo path
                    |> dispatchDir
            } |> ignore
        } |> Async.StartImmediate

module FileInfo =

    /// Allows user to pick a file.
    let onPick dispatchDir args =
        let topLevel =
            (args : RoutedEventArgs).Source
                :?> Control
                |> TopLevel.GetTopLevel
        async {
            let! files =
                let options = FilePickerOpenOptions()
                topLevel
                    .StorageProvider
                    .OpenFilePickerAsync(options)
                    |> Async.AwaitTask
            option {
                let! file = Seq.tryHead files
                let! path =
                    file.TryGetLocalPath()
                        |> Option.ofObj
                FileInfo path
                    |> dispatchDir
            } |> ignore
        } |> Async.StartImmediate
