namespace Reliant.Photo

open Elmish
open System.IO
open SixLabors.ImageSharp

/// Application message.
type Message =

    /// Set source drive.
    | SetSource of DriveInfo

    /// Set parent destination directory.
    | SetDestination of DirectoryInfo

    /// Set import name.
    | SetName of string

    /// Start the import.
    | StartImport

    /// Continue import.
    | ContinueImport of Import

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

    /// Starts an import.
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
                    |> Array.groupBy (
                        _.Name
                            >> Path.GetFileNameWithoutExtension)
                    |> Array.where (
                        snd >> Array.exists isImage)
                    |> Array.sortBy fst
                    |> Array.map snd

            return {
                Destination = destDir
                FileGroups = groups
                NumGroupsImported = 0
            }
        }

    /// Handles an error.
    let private handleError (ex : exn) =
        HandleError ex.Message

    /// Starts an import.
    let private onStartImport model =
        assert(not model.ImportStatus.IsImporting)
        { model with
            ImportStatus = Starting },
        Cmd.OfAsync.either
            startImport
            model
            ContinueImport
            handleError

    /// Continues an import.
    let private continueImport import =
        async {
            let iGroup = import.NumGroupsImported
            assert(iGroup <= import.FileGroups.Length)

                // groups remain to be imported?
            if iGroup < import.FileGroups.Length then
                let groupName =
                    $"{import.Destination.Name} %03d{iGroup}"

                    // import each file in the group
                for sourceFile in import.FileGroups[iGroup] do
                    let destFile =
                        Path.Combine(
                            import.Destination.FullName,
                            $"{groupName}{sourceFile.Extension.ToLower()}")
                            |> FileInfo
                    sourceFile.CopyTo(destFile.FullName)
                        |> ignore

                return {
                    import with
                        NumGroupsImported = iGroup + 1 }

            else return import
        }

    /// Continues an import.
    let private onContinueImport import model =

        let handleSuccess import =
            if import.NumGroupsImported
                < import.FileGroups.Length then
                ContinueImport import
            else
                FinishImport

        { model with
            ImportStatus = InProgress import },
        Cmd.OfAsync.either
            continueImport
            import
            handleSuccess
            handleError

    /// Finishes an import.
    let onFinishImport model =
        assert(model.ImportStatus.IsImporting)
        { model with
            ImportStatus = Finished },
        Cmd.none

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
            | StartImport ->
                onStartImport model
            | ContinueImport import ->
                onContinueImport import model
            | FinishImport ->
                onFinishImport model
            | HandleError error ->
                onHandleError error model
            | Shutdown ->
                model, Cmd.none
