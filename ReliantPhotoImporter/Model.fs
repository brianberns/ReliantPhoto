namespace Reliant.Photo

open System
open System.IO

type Model =
    {
        Source : DirectoryInfo
        Destination : DirectoryInfo
    }

module Model =

    let private defaultDirectory =
        Environment.SpecialFolder.MyPictures
            |> Environment.GetFolderPath
            |> DirectoryInfo

    let init () =
        {
            Source = defaultDirectory
            Destination = defaultDirectory
        }
