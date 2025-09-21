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

    let private tryIdentify (file : FileInfo) =
        try
            Some (Image.Identify file.FullName)
        with :? UnknownImageFormatException ->
            None

    let private importImpl
        (sourceDir : DirectoryInfo)
        (destDir : DirectoryInfo)
        (name : string) =

            // group files to import
        let groups =
            sourceDir.EnumerateFiles(
                "*", SearchOption.AllDirectories)
                |> Seq.where (tryIdentify >> Option.isSome)
                |> Seq.groupBy (
                    _.Name
                        >> Path.GetFileNameWithoutExtension)
                |> Seq.sortBy fst
                |> Seq.map snd
                |> Seq.indexed

            // create destination sub-directory
        let destDir =
            destDir.CreateSubdirectory(name)

            // copy files to destination
        for (iGroup, files) in groups do
            let groupName = $"{name} %03d{iGroup + 1}"
            for file in files do
                let destFileName =
                    Path.Combine(
                        destDir.FullName,
                        $"{groupName}{file.Extension.ToLower()}")
                file.CopyTo(destFileName)
                    |> ignore

    /// Imports pictures using the given model.
    let import model =
        match model.SourceOpt with
            | Some source ->
                importImpl
                    source
                    model.Destination
                    (getNormalName model)
            | _ -> failwith "Invalid state"
