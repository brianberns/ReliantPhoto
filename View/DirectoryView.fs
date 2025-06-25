namespace Reliant.Photo

open System
open System.IO

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Threading

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
                // Persist the ScaleTransform instance
                let scaleTransform = ctx.useState (ScaleTransform(1.0, 1.0))
                // Animate scale on hover using DispatcherTimer
                let animateScale target =
                    let scale = scaleTransform.Current
                    let startX = scale.ScaleX
                    let startY = scale.ScaleY
                    let endX = if target then 1.08 else 1.0
                    let endY = if target then 1.08 else 1.0
                    let duration = 0.18
                    let steps = 18
                    let mutable step = 0
                    let timer =
                        DispatcherTimer(
                            Interval =
                                TimeSpan.FromMilliseconds(
                                    duration * 1000.0 / float steps))
                    timer.Tick.Add(fun _ ->
                        let t = float step / float steps
                        scale.ScaleX <- startX + (endX - startX) * t
                        scale.ScaleY <- startY + (endY - startY) * t
                        step <- step + 1
                        if step > steps then timer.Stop())
                    timer.Start()
                Border.create [
                    Border.child (
                        Image.create [
                            Image.source source
                            Image.height source.Size.Height   // why is this necessary?
                            Image.stretch Stretch.Uniform
                            Image.margin 8.0
                            Image.onTapped (fun _ ->
                                dispatch (SwitchToImage file))
                            Image.renderTransformOrigin RelativePoint.Center
                            Image.renderTransform scaleTransform.Current
                        ]
                    )
                    Border.onPointerEntered (fun _ ->
                        isHovered.Set true
                        animateScale true)
                    Border.onPointerExited (fun _ ->
                        isHovered.Set false
                        animateScale false)
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
                            WrapPanel.orientation
                                Orientation.Horizontal
                            WrapPanel.margin 8.0
                            WrapPanel.children images
                        ]
                    )
                ]
            ]
        ]
