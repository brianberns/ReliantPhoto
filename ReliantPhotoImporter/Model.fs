namespace Reliant.Photo

open System
open System.IO

type ImportStatus =
    | NotStarted
    (*
    | Initializing
    | InProgress of
        {|
            Files : FileInfo[]
            NumFilesImported : int
        |}
    *)

type Model =
    {
        /// Source drive.
        Source : DirectoryInfo

        /// Destination directory.
        Destination : DirectoryInfo

        /// Import name.
        Name : string

        /// Import status.
        ImportStatus : ImportStatus
    }

module Model =

    let drives = DriveInfo.GetDrives()

    /// User's pictures folder.
    let private myPicturesDir =
        Environment.SpecialFolder.MyPictures
            |> Environment.GetFolderPath
            |> DirectoryInfo

    /// Initial model.
    let init () =
        {
            Source = (Array.last drives).RootDirectory
            Destination = myPicturesDir
            Name =
                DateTime.Today.ToString("dd-MMM-yyyy")
            ImportStatus = NotStarted
        }
