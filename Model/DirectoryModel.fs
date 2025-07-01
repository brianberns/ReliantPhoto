namespace Reliant.Photo

open System
open System.IO

open Avalonia.Media.Imaging

/// Session identifier.
type SessionId = int64

/// Directory model.
type DirectoryModel =
    {
        /// Current directory.
        Directory : DirectoryInfo

        /// Unique ID of this session.
        SessionId : SessionId

        /// Loaded images.
        FileImages : FileImage[]

        /// Directory is in the process of loading?
        IsLoading : bool
    }

module DirectoryModel =

    /// Initializes model for the given directory.
    let init dir =
        {
            Directory = dir
            SessionId = DateTime.Now.Ticks
            FileImages = Array.empty
            IsLoading = false
        }
