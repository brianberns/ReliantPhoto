namespace Reliant.Photo

open Elmish
open System.IO
open SixLabors.ImageSharp

type Message =

    /// Set source drive.
    | SetSource of DriveInfo

    /// Set parent destination directory.
    | SetDestination of DirectoryInfo

    /// Set import name.
    | SetName of string

    /// Start the import.
    | StartImport

    /// Import started.
    | ImportStarted of Import

    /// Continue import.
    | ContinueImport

    /// Finish import
    | FinishImport

module Message =

    let init () =
        Model.init (),
        Cmd.none

    let private tryIdentify (file : FileInfo) =
        try
            Some (Image.Identify file.FullName)
        with :? UnknownImageFormatException ->
            None

    let private startImport model =
        async {
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

            return {
                Destination = destDir
                FileGroups = groups
                NumGroupsImported = 0
            }
        }

    let private onStartImport model =
        { model with
            ImportStatus = Starting },
        Cmd.OfAsync.perform
            startImport
            model
            ImportStarted

    let private onImportStarted import model =
        match model.ImportStatus with
            | Starting ->
                { model with
                    ImportStatus = InProgress import },
                Cmd.ofMsg ContinueImport
            | _ -> failwith "Invalid state"

    let private onContinueImport model =
        match model.ImportStatus with
            | InProgress import ->
                if import.NumGroupsImported >= import.FileGroups.Length then
                    model, Cmd.ofMsg FinishImport
                else
                    model, Cmd.ofMsg ContinueImport
            | _ -> failwith "Invalid state"

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

    let onFinishImport model =
        match model.ImportStatus with
            | InProgress _ ->
                { model with
                    ImportStatus = Finished },
                Cmd.none
            | _ -> failwith "Invalid state"

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
            | ImportStarted import ->
                onImportStarted import model
            | ContinueImport ->
                onContinueImport model
            | FinishImport ->
                onFinishImport model
