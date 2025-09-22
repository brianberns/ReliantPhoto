namespace Reliant.Photo

open Elmish
open System.IO
open SixLabors.ImageSharp

type Message =
    | SetSource of DriveInfo
    | SetDestination of DirectoryInfo
    | SetName of string
    | StartImport
    // | ImportImages of FileInfo[]
    // | ImportImage of (FileInfo (*source*) * FileInfo (*destination*))

module Message =

    let init () =
        Model.init (),
        Cmd.none

    let private tryIdentify (file : FileInfo) =
        try
            Some (Image.Identify file.FullName)
        with :? UnknownImageFormatException ->
            None

    (*
    let private startImport
        (sourceDir : DirectoryInfo)
        (destDir : DirectoryInfo)
        (name : string) =

            // create destination sub-directory
        let destDir =
            destDir.CreateSubdirectory(name)

            // group files to import
        let groups =
            sourceDir.GetFiles(
                "*", SearchOption.AllDirectories)
                |> Array.where (tryIdentify >> Option.isSome)
                |> Array.groupBy (
                    _.Name
                        >> Path.GetFileNameWithoutExtension)
                |> Array.sortBy fst
                |> Array.map snd

        { model with
            ImportStatus =
                InProgress {
                    FileGroups = groups
                    NumGroupsImported = 0
                } }
        *)

        (*
            // copy files to destination
        seq {
            for (iGroup, sourceFiles) in groups do
                let groupName = $"{name} %03d{iGroup + 1}"
                for sourceFile in sourceFiles do
                    let destFile =
                        Path.Combine(
                            destDir.FullName,
                            $"{groupName}{sourceFile.Extension.ToLower()}")
                            |> FileInfo
                    ImportImage (sourceFile, destFile)
        }

    let private startImportAsync sourceDir destDir name =
        async {
            return startImport sourceDir destDir name
        }

    /// Starts importing images using the given model.
    let private onStartImport model =
        match model.SourceOpt, model.ImportStatus with
            | Some source, NotStarted ->
                let model =
                    { model with
                        ImportStatus = Initializing }
                let cmd =
                    Cmd.OfAsync.perform
                        (fun () ->
                            startImportAsync
                                source
                                model.Destination
                                model.Name)
                        ()
                        ImportImages
                model, cmd
            | _ -> failwith "Invalid state"
        *)

    let update message model =
        match message with
            | SetSource drive ->
                { model with Source = drive },
                Cmd.none
            | SetDestination dir ->
                { model with Destination = dir },
                Cmd.none
            | SetName name ->
                { model with Name = name },
                Cmd.none
            | StartImport ->
                model,
                Cmd.none

