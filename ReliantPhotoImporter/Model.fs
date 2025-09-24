namespace Reliant.Photo

open System
open System.IO

/// An import in progress.
type Import =
    {
        /// Child destination directory.
        Destination : DirectoryInfo

        /// Image file groups.
        FileGroups : FileInfo[][]

        /// Number of groups imported so far.
        NumGroupsImported : int
    }

/// Import status.
type ImportStatus =

    /// Not started yet.
    | NotStarted

    /// Import started but not yet processing files.
    | Starting

    /// Import in progress.
    | InProgress of Import

    /// Import finished. 
    | Finished

/// Application model.
type Model =
    {
        /// Source drive.
        Source : DriveInfo

        /// Parent destination directory.
        Destination : DirectoryInfo

        /// Import name.
        Name : string

        /// Import status.
        ImportStatus : ImportStatus
    }

module Model =

    /// Available drives.
    let drives =
        DriveInfo.GetDrives()
            |> Array.where _.IsReady

    /// User's pictures folder.
    let private myPicturesDir =
        Environment.SpecialFolder.MyPictures
            |> Environment.GetFolderPath
            |> DirectoryInfo

    /// Initial model.
    let init (sourceOpt, destOpt) =

            // validate arguments
        let source =
            sourceOpt
                |> Option.bind (fun source ->
                    Array.tryFind
                        (DriveInfo.same source)
                        drives)
                |> Option.defaultValue (
                    Array.last drives)   // guess: first drive is most likely source
        let dest =
            match destOpt with
                | Some (dest : DirectoryInfo)
                    when dest.Exists -> dest
                | _ -> myPicturesDir

        {
            Source = source
            Destination = dest
            Name =
                DateTime.Today.ToString("dd-MMM-yyyy")
            ImportStatus = NotStarted
        }
