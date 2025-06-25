namespace Reliant.Photo

open System.IO

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media

module Cursor =

    /// Wait cursor.
    let wait = new Cursor(StandardCursorType.Wait)

module DirectoryView =

    /// Creates an image control with hover effect.
    let private createImage
        (file : FileInfo) (source : IImage) dispatch =
        Component.create (
            file.FullName,
            fun ctx ->
                let isHovered = ctx.useState false
                Border.create [
                    Border.background (
                        if isHovered.Current then "DarkGray"
                        else "Transparent")
                    Border.child (
                        Image.create [
                            Image.source source
                            Image.height source.Size.Height   // why is this necessary?
                            Image.stretch Stretch.Uniform
                            Image.margin 8.0
                            Image.onTapped (fun _ ->
                                dispatch (SwitchToImage file))
                        ])
                    Border.onPointerEntered (fun _ -> isHovered.Set true)
                    Border.onPointerExited (fun _ -> isHovered.Set false)
                ]
        )

    /// Creates a view of the given model.
    let view (model : DirectoryModel) dispatch =
        DockPanel.create [

            if model.IsLoading then
                DockPanel.cursor Cursor.wait
                DockPanel.background "Transparent"   // needed to force the cursor change for some reason

            let images =
                [
                    for file, result in model.ImageLoadPairs do
                        match result with
                            | Ok source ->
                                createImage file source dispatch
                                    :> IView
                            | _ -> ()
                ]

            DockPanel.children [
                ScrollViewer.create [
                    ScrollViewer.content (
                        WrapPanel.create [
                            WrapPanel.orientation Orientation.Horizontal
                            WrapPanel.margin 8.0
                            WrapPanel.children images
                        ]
                    )
                ]
            ]
        ]
