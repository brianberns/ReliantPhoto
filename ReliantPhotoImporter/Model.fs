namespace Reliant.Photo

open System
open System.IO

type Model =
    {
        SourceOpt : Option<DirectoryInfo>
        Destination : DirectoryInfo
    }

module Model =

    let private myPicturesDir =
        Environment.SpecialFolder.MyPictures
            |> Environment.GetFolderPath
            |> DirectoryInfo

    let init () =
        {
            SourceOpt = None
            Destination = myPicturesDir
        }
