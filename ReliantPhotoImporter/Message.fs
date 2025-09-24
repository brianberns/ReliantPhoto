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

    /// Continue import.
    | ContinueImport of Import

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
        match model.ImportStatus with
            | NotStarted ->
                { model with
                    ImportStatus = Starting },
                Cmd.OfAsync.perform
                    startImport
                    model
                    ContinueImport
            | _ -> failwith "Invalid state"

    let private onImportStarted import model =
        match model.ImportStatus with
            | Starting ->
                model,
                Cmd.ofMsg (ContinueImport import)
            | _ -> failwith "Invalid state"

    let private continueImport import =
        async {
            let iGroup = import.NumGroupsImported
            assert(iGroup <= import.FileGroups.Length)
            if iGroup < import.FileGroups.Length then
                let groupName =
                    $"{import.Destination.Name} %03d{iGroup}"
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
        }

    let private onContinueImport import model =

        let ofSuccess import =
            if import.NumGroupsImported
                < import.FileGroups.Length then
                ContinueImport import
            else
                FinishImport

        let ofError (ex : exn) =
            printfn "%A" ex   // to-do: proper error handling
            FinishImport

        { model with
            ImportStatus = InProgress import },
        Cmd.OfAsync.either
            continueImport
            import
            ofSuccess
            ofError

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
            | ContinueImport import ->
                onContinueImport import model
            | FinishImport ->
                onFinishImport model
