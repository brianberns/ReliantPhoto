namespace Reliant.Photo

open System.IO
open System.Runtime.InteropServices

open Avalonia
open Avalonia.Media.Imaging
open Avalonia.Platform

open SixLabors.ImageSharp
open SixLabors.ImageSharp.Formats
open SixLabors.ImageSharp.Metadata.Profiles.Exif
open SixLabors.ImageSharp.PixelFormats
open SixLabors.ImageSharp.Processing

module private ImageSharp =

    /// Gets the orientation of the given image.
    let private getOrientation (imageInfo : ImageInfo) =
        option {
            let! profile =
                Option.ofObj imageInfo.Metadata.ExifProfile
            let flag, exifValue =
                profile.TryGetValue(ExifTag.Orientation)
            if flag then
                return exifValue.Value
        } |> Option.defaultValue ExifOrientationMode.TopLeft

    /// Gets the target size for the given target height,
    /// accounting for rotation, if necessary.
    let private getTargetSize targetHeight imageInfo =
        match getOrientation imageInfo with
            | ExifOrientationMode.LeftTop
            | ExifOrientationMode.RightTop
            | ExifOrientationMode.RightBottom
            | ExifOrientationMode.LeftBottom ->
                Size(targetHeight, 0)   // rotate
            | _ ->
                Size(0, targetHeight)

    /// Loads an image from the given file.
    let private loadImageImpl options (file : FileInfo) =

            // load image
        use image =
            use image = Image.Load(options, file.FullName)   // load image
            image.Mutate(_.AutoOrient() >> ignore)           // rotate, if necessary
            image.CloneAs<Bgra32>()                          // convert to Avalonia format

            // create destination bitmap
        let width = image.Width
        let height = image.Height
        let wb =
            let size = PixelSize(width, height)
            let dpi = Vector(96.0, 96.0)
            let format = PixelFormat.Bgra8888
            let alphaFormat = AlphaFormat.Unpremul
            new WriteableBitmap(size, dpi, format, alphaFormat)

            // copy pixel data to bitmap
        do
            use fbLock = wb.Lock()
            image.ProcessPixelRows(fun accessor ->
                for y = 0 to height - 1 do
                    let row = accessor.GetRowSpan(y)
                    let destAddress =
                        fbLock.Address + nativeint (y * fbLock.RowBytes)
                    let byteRow = MemoryMarshal.AsBytes(row)
                    Marshal.Copy(
                        byteRow.ToArray(), 0,
                        destAddress, byteRow.Length))

        wb :> Bitmap

    module private DecoderOptions =

        /// Default decoder options.
        let empty = DecoderOptions()

    /// Loads an image from the given file.
    let loadImage file =
        loadImageImpl DecoderOptions.empty file

    /// Loads a thumbnail image from the given file.
    let loadThumbnail height (file : FileInfo) =
        let imageInfo = Image.Identify file.FullName
        let options =
            DecoderOptions(
                TargetSize =   // decode to the given height
                    getTargetSize height imageInfo)
        loadImageImpl options file
