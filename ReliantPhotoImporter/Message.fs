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

    let private onStartImport model =

            // create destination sub-directory
        let destDir =
            model.Destination.CreateSubdirectory(
                model.Name.Trim())

            // group files to import
        let groups =
            model.Source.RootDirectory.GetFiles(
                "*", SearchOption.AllDirectories)
                |> Array.where (tryIdentify >> Option.isSome)
                |> Array.groupBy (
                    _.Name
                        >> Path.GetFileNameWithoutExtension)
                |> Array.sortBy fst
                |> Array.map snd

        { model with
            ImportStatus =
                InProgress {|
                    Destination = destDir
                    FileGroups = groups
                    NumGroupsImported = 0
                |} },
        Cmd.none

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
                onStartImport model
