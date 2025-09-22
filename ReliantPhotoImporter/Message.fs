namespace Reliant.Photo

open Elmish
open System.IO
open SixLabors.ImageSharp

type Message =
    | SetSource of DirectoryInfo
    | SetDestination of DirectoryInfo
    | SetName of string
    | StartImport
    | ImportImages of FileInfo[]

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
    let private onStartImport model =
        match model.SourceOpt with
            | Some source ->
                importImpl
                    source
                    model.Destination
                    (Model.getNormalName model)
            | _ -> failwith "Invalid state"
    *)

    let private getImageFiles (dir : DirectoryInfo) =
        async {
            return
                dir.EnumerateFiles(
                    "*", SearchOption.AllDirectories)
                    |> Seq.where (tryIdentify >> Option.isSome)
                    |> Seq.toArray
        }

    let private onStartImport model =
        match model.SourceOpt, model.IsImporting with
            | Some source, false ->
                let model =
                    { model with IsImporting = true }
                let cmd =
                    Cmd.OfAsync.perform
                        getImageFiles
                        source
                        ImportImages
                model, cmd
            | _ -> failwith "Invalid state"

    let update message model =
        match message with
            | SetSource dir ->
                { model with SourceOpt = Some dir },
                Cmd.none
            | SetDestination dir ->
                { model with Destination = dir },
                Cmd.none
            | SetName name ->
                { model with Name = name },
                Cmd.none
            | StartImport ->
                onStartImport model
            | ImportImages files ->
                model,
                Cmd.none
