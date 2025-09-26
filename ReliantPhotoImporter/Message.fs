namespace Reliant.Photo

open System
open System.IO

open Elmish

open SixLabors.ImageSharp

/// Application message.
type Message =

    /// Set source drive.
    | SetSource of DriveInfo

    /// Set parent destination directory.
    | SetDestination of DirectoryInfo

    /// Set import name.
    | SetName of string

    /// Start gathering files to import.
    | GatherFiles

    /// Files have been gathered for import.
    | FilesGathered of Import

    /// Import the next group of files.
    | ImportGroup

    /// Finish import.
    | FinishImport

    /// Handle error.
    | HandleError of string

    /// Application shutdown.
    | Shutdown

module Message =

    /// Creates initial model and command.
    let init arg =
        Model.init arg,
        Cmd.none

    /// Determines if the given file is an image.
    let private isImage (file : FileInfo) =
        try
            Image.Identify file.FullName
                |> ignore
            true
        with :? UnknownImageFormatException ->
            false

    /// Gathers files to import.
    let private gatherFiles model =
        async {
                // create destination sub-directory
            let importName = model.Name.Trim()
            let destDir =
                model.Destination
                    .CreateSubdirectory(importName)

                // determine next available offset
            let offset =
                destDir.GetFiles($"{importName}*")
                    |> Seq.map (fun file ->
                        let fileName =
                            Path.GetFileNameWithoutExtension(file.Name)
                        fileName[importName.Length ..].Trim())
                    |> Seq.choose (
                        Int32.TryParse >> Dictionary.toOption)
                    |> Seq.tryMax
                    |> Option.defaultValue 0

                // group files to import
            let groups =
                model.Source.RootDirectory.GetFiles(
                    "*",
                    EnumerationOptions(RecurseSubdirectories = true))   // ignore hidden and system files
                    |> Array.groupBy (
                        _.Name
                            >> Path.GetFileNameWithoutExtension)
                    |> Array.where (
                        snd >> Array.exists isImage)
                    |> Array.sortBy fst
                    |> Array.map snd

            return {
                Destination = destDir
                Offset = offset
                FileGroups = groups
                NumGroupsImported = 0
            }
        }

    /// Handles an error.
    let private handleError (ex : exn) =
        HandleError ex.Message

    /// Gathers files to import.
    let private onGatherFiles model =
        assert(not model.ImportStatus.IsImporting)
        { model with
            ImportStatus = GatheringFiles },
        Cmd.OfAsync.either
            gatherFiles
            model
            FilesGathered
            handleError

    /// Files have been gathered for import.
    let private onFilesGathered import model =
        { model with
            ImportStatus = InProgress import },
        Cmd.ofMsg ImportGroup

    /// Imports the next file group.
    let private importGroup import =
        async {
                // get group to import
            let iGroup = import.NumGroupsImported
            assert(iGroup < import.FileGroups.Length)
            let groupName =
                let suffix = iGroup + import.Offset + 1
                $"{import.Destination.Name} %03d{suffix}"
            let fileGroup = import.FileGroups[iGroup]

                // import each file in the group
            for sourceFile in fileGroup do
                let destFile =
                    Path.Combine(
                        import.Destination.FullName,
                        $"{groupName}{sourceFile.Extension.ToLower()}")
                        |> FileInfo
                sourceFile.CopyTo(destFile.FullName)
                    |> ignore
        }

    /// Imports the next file group.
    let private onImportGroup model =
        match model.ImportStatus with
            | InProgress import ->

                    // update progress
                let import' =
                    { import with
                        NumGroupsImported =
                            import.NumGroupsImported + 1 }
                let model =
                    { model with
                        ImportStatus = InProgress import' }

                    // determine next step
                let nextMessage =
                    if import'.NumGroupsImported < import'.FileGroups.Length then
                        ImportGroup
                    else
                        FinishImport

                    // import group asynchronously
                let cmd =
                    Cmd.OfAsync.either
                        importGroup
                        import
                        (fun () -> nextMessage)
                        handleError

                model, cmd

            | _ -> failwith "Invalid state"

    /// Finishes an import.
    let onFinishImport model =
        match model.ImportStatus with
            | InProgress import ->
                { model with
                    ImportStatus =
                        Finished import.FileGroups.Length },
                Cmd.none
            | _ -> failwith "Invalid state"

    /// Handles an error.
    let onHandleError error model =
        { model with
            ImportStatus = Error error },
        Cmd.none

    /// Updates the model based on the given message.
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
            | GatherFiles ->
                onGatherFiles model
            | FilesGathered import ->
                onFilesGathered import model
            | ImportGroup ->
                onImportGroup model
            | FinishImport ->
                onFinishImport model
            | HandleError error ->
                onHandleError error model
            | Shutdown ->
                model, Cmd.none
