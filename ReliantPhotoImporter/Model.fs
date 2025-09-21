namespace Reliant.Photo

open System
open System.IO

open SixLabors.ImageSharp

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

    /// Gets the normalized name of the given model.
    let getNormalName model =
        let name = model.Name.Trim()
        if String.IsNullOrWhiteSpace name then
            "Image"
        else name

    /// Given model is ready to import?
    let isComplete model =

        let validSource =
            match model.SourceOpt with
                | Some source when source.Exists ->
                    true
                | _ -> false

        validSource
            && model.Destination.Exists

    let private importImpl
        (source : DirectoryInfo)
        (dest : DirectoryInfo)
        (name : string) =
        for file in source.EnumerateFiles() do
            let imageInfo = Image.Identify file.FullName
            ()

    /// Imports pictures using the given model.
    let import model =
        match model.SourceOpt with
            | Some source ->
                importImpl
                    source
                    model.Destination
                    (getNormalName model)
            | _ -> failwith "Invalid state"
