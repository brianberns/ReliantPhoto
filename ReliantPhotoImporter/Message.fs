namespace Reliant.Photo

open System
open System.IO

type Message =
    | SetSource of DirectoryInfo
    | SetDestination of DirectoryInfo
    | SetName of string
    | StartImport

module Message =

    let update message model =
        match message with
            | SetSource dir ->
                { model with SourceOpt = Some dir }
            | SetDestination dir ->
                { model with Destination = dir }
            | SetName name ->
                { model with Name = name }
            | StartImport ->
                Model.import model
                model
