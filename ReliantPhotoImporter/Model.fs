namespace Reliant.Photo

open System
open System.IO

type Import =
    {
        /// Child destination directory.
        Destination : DirectoryInfo

        /// Image file groups.
        FileGroups : FileInfo[][]

        NumGroupsImported : int
    }

type ImportStatus =
    | NotStarted
    | Starting
    | InProgress of Import
    | Finished

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
    let init () =
        {
            Source = Array.last drives   // guess: last drive is most likely source
            Destination = myPicturesDir
            Name =
                DateTime.Today.ToString("dd-MMM-yyyy")
            ImportStatus = NotStarted
        }
