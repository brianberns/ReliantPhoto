namespace Reliant.Photo

open System
open System.IO
open System.Runtime.InteropServices

open Avalonia.Controls
open Avalonia.Interactivity
open Avalonia.Platform.Storage

module FileSystemInfo =

    /// Path string comparison.
    let comparison =
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            || RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
                StringComparison.OrdinalIgnoreCase
        else StringComparison.Ordinal

module DriveInfo =

    /// Drive equality.
    let same (driveA : DriveInfo) (driveB : DriveInfo) =
        String.Equals(
            driveA.Name,
            driveB.Name,
            FileSystemInfo.comparison)

module DirectoryInfo =

    /// Normalizes the given directory's path.
    let normalizedPath (dir : DirectoryInfo) =
        dir.FullName
            |> Path.TrimEndingDirectorySeparator

    /// Directory equality.
    let same dirA dirB =
        String.Equals(
            normalizedPath dirA,
            normalizedPath dirB,
            FileSystemInfo.comparison)

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

    /// File equality.
    let same (fileA : FileInfo) (fileB : FileInfo) =
        String.Equals(
            fileA.FullName,
            fileB.FullName,
            FileSystemInfo.comparison)

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
