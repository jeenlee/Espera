﻿using Espera.Core.Management;
using Rareform.Extensions;
using Rareform.Reflection;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Espera.View.ViewModels
{
    internal sealed class PlaylistViewModel : ReactiveObject, IDataErrorInfo
    {
        private readonly Playlist playlist;
        private readonly Func<string, bool> renameRequest;
        private bool editName;
        private string saveName;
        private int songCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaylistViewModel"/> class.
        /// </summary>
        /// <param name="playlist">The playlist info.</param>
        /// <param name="renameRequest">A function that requests the rename of the playlist. Return true, if the rename is granted, otherwise false.</param>
        public PlaylistViewModel(Playlist playlist, Func<string, bool> renameRequest)
        {
            this.playlist = playlist;
            this.renameRequest = renameRequest;
            this.songCount = -1;
        }

        public bool EditName
        {
            get { return this.editName; }
            set
            {
                if (this.EditName != value)
                {
                    this.editName = value;

                    if (this.EditName)
                    {
                        this.saveName = this.Name;
                    }

                    else if (this.HasErrors())
                    {
                        this.Name = this.saveName;
                        this.saveName = null;
                    }

                    this.RaisePropertyChanged(x => x.EditName);
                }
            }
        }

        public string Error
        {
            get { return null; }
        }

        public Playlist Model
        {
            get { return this.playlist; }
        }

        public string Name
        {
            get { return this.playlist.IsTemporary ? "Now Playing" : this.playlist.Name; }
            set
            {
                if (this.Name != value)
                {
                    this.playlist.Name = value;
                    this.RaisePropertyChanged(x => x.Name);
                }
            }
        }

        public int SongCount
        {
            get
            {
                // We use this to get a value, even if the Songs property hasn't been called
                if (songCount == -1)
                {
                    return this.Songs.Count();
                }

                return songCount;
            }

            private set { this.RaiseAndSetIfChanged(x => x.SongCount, value); }
        }

        public IEnumerable<PlaylistEntryViewModel> Songs
        {
            get
            {
                var songs = this.playlist
                    .Select(entry => new PlaylistEntryViewModel(entry))
                    .ToList(); // We want a list, so that ReSharper doesn't complain about multiple enumerations

                this.SongCount = songs.Count;

                if (this.playlist.CurrentSongIndex.HasValue)
                {
                    PlaylistEntryViewModel entry = songs[this.playlist.CurrentSongIndex.Value];

                    if (!entry.IsCorrupted)
                    {
                        entry.IsPlaying = true;
                    }

                    // If there are more than 5 songs from the beginning of the playlist to the current played song,
                    // skip all, but 5 songs to the position of the currently played song
                    if (this.playlist.CurrentSongIndex > 5)
                    {
                        songs = songs.Skip(this.playlist.CurrentSongIndex.Value - 5).ToList();
                    }

                    foreach (var model in songs.TakeWhile(song => !song.IsPlaying))
                    {
                        model.IsInactive = true;
                    }
                }

                return songs;
            }
        }

        public string this[string columnName]
        {
            get
            {
                string error = null;

                if (columnName == Reflector.GetMemberName(() => this.Name))
                {
                    if (!this.renameRequest(this.Name))
                    {
                        error = "Name already exists.";
                    }

                    else if (String.IsNullOrWhiteSpace(this.Name))
                    {
                        error = "Name cannot be empty or whitespace.";
                    }
                }

                return error;
            }
        }
    }
}