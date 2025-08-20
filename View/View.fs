namespace Reliant.Photo

open Avalonia.FuncUI.Types

module View =

    /// Creates a view of the given model.
    let view model dispatch =
        match model.Mode with
            | Mode.Directory ->
                DirectoryView.view
                    model.DirectoryModel dispatch
                        :> IView
            | Mode.Image ->
                ImageView.view
                    model.ImageModel dispatch
