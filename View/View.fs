namespace Reliant.Photo

open Avalonia.FuncUI.Types

module View =

    /// Creates a view of the given model.
    let view model dispatch =
        match model.ImageModelOpt with
            | None ->
                DirectoryView.view
                    model.DirectoryModel dispatch
                    :> IView
            | Some imgModel ->
                ImageView.view imgModel dispatch
