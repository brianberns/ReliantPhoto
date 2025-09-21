namespace Reliant.Photo

open System
open System.IO

type Model =
    {
        /// Source directory, if any chosen.
        SourceOpt : Option<DirectoryInfo>

        /// Destination directory.
        Destination : DirectoryInfo

        /// Import name, if any.
        Name : string
    }

module Model =

    /// User's pictures folder.
    let private myPicturesDir =
        Environment.SpecialFolder.MyPictures
            |> Environment.GetFolderPath
            |> DirectoryInfo

    /// Initial model.
    let init () =
        {
            SourceOpt = None
            Destination = myPicturesDir
            Name =
                DateTime.Today.ToString("dd-MMM-yyyy")
        }

    /// Given model is ready to import?
    let isComplete model =

        let validSource =
            match model.SourceOpt with
                | Some source when source.Exists ->
                    true
                | _ -> false

        validSource
            && model.Destination.Exists

    /// Imports pictures using the given model.
    let import model =
        match model.SourceOpt with
            | Some source ->
                ()
            | _ -> failwith "Invalid state"
