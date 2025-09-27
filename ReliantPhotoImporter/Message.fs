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
        let model =
            { model with
                ImportStatus = GatheringFiles }
        let cmd =
            Cmd.OfAsync.either
                gatherFiles
                model
                FilesGathered
                handleError
        model, cmd

    /// Files have been gathered for import.
    let private onFilesGathered import model =
        match model.ImportStatus with
            | GatheringFiles ->
                { model with
                    ImportStatus = InProgress import },
                Cmd.ofMsg ImportGroup
            | _ -> model, Cmd.none   // e.g. import canceled

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
                let nGroupsImported =
                    import.NumGroupsImported + 1

                    // import group asynchronously
                let cmd =
                    let nextMessage =
                        if nGroupsImported < import.FileGroups.Length then
                            ImportGroup
                        else
                            FinishImport
                    Cmd.OfAsync.either
                        importGroup
                        import
                        (fun () -> nextMessage)
                        handleError

                    // update model
                let model =
                    { model with
                        ImportStatus =
                            InProgress
                                { import with
                                    NumGroupsImported = nGroupsImported } }

                model, cmd

            | _ -> model, Cmd.none   // e.g. import canceled

    /// Finishes an import.
    let private onFinishImport model =
        let nGroups =
            match model.ImportStatus with
                | InProgress import -> import.NumGroupsImported
                | _ -> 0
        { model with
            ImportStatus = Finished nGroups },
        Cmd.none

    /// Handles an error.
    let private onHandleError error model =
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
