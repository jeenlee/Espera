﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Espera.Core;
using ReactiveUI;
using Splat;

namespace Espera.View.ViewModels
{
    public sealed class ArtistViewModel : ReactiveObject, IComparable<ArtistViewModel>, IEquatable<ArtistViewModel>, IDisposable
    {
        private readonly ReactiveList<LocalSong> songs; 
        private readonly ObservableAsPropertyHelper<BitmapSource> cover;
        private readonly int orderHint;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="artistName"></param>
        /// <param name="songs"></param>
        /// <param name="orderHint">
        /// A hint that tells this instance which position it has in the artist list. This helps for
        /// priorizing the album cover loading. The higher the number, the earlier it is in the list
        /// (Think of a reversed sorted list).
        /// </param>
        public ArtistViewModel(string artistName, IEnumerable<LocalSong> songs, int orderHint = 1)
        {
            this.songs = new ReactiveList<LocalSong>();

            this.orderHint = orderHint;

            this.cover = this.songs.ItemsAdded.Select(x => x.WhenAnyValue(y => y.ArtworkKey))
                .Merge()
                .Where(x => x != null)
                .Distinct() // Ignore duplicate artworks
                .Select(LoadArtworkAsync)
                .Concat()
                .FirstOrDefaultAsync(pic => pic != null)
                .ToProperty(this, x => x.Cover);
            var connect = this.Cover; // Connect the property to the source observable immediately

            this.UpdateSongs(songs);

            this.Name = artistName;
            this.IsAllArtists = false;
        }

        public ArtistViewModel(string allArtistsName)
        {
            this.Name = allArtistsName;
            this.IsAllArtists = true;
        }

        public BitmapSource Cover
        {
            get { return this.cover == null ? null : this.cover.Value; }
        }

        public bool IsAllArtists { get; private set; }

        public string Name { get; private set; }

        public int CompareTo(ArtistViewModel other)
        {
            if (this.IsAllArtists && other.IsAllArtists)
            {
                return 0;
            }

            if (this.IsAllArtists)
            {
                return -1;
            }

            if (other.IsAllArtists)
            {
                return 1;
            }

            return String.Compare(SortHelpers.RemoveArtistPrefixes(this.Name), SortHelpers.RemoveArtistPrefixes(other.Name), StringComparison.InvariantCultureIgnoreCase);
        }

        public void Dispose()
        {
            this.cover.Dispose();
        }

        public bool Equals(ArtistViewModel other)
        {
            return this.Name == other.Name;
        }

        public void UpdateSongs(IEnumerable<LocalSong> songs)
        {
            var songsToAdd = songs.Where(x => !this.songs.Contains(x)).ToList();

            // Can't use AddRange here, ReactiveList resets the list on big changes and we don't get the add notification
            foreach (LocalSong song in songsToAdd)
            {
                this.songs.Add(song);
            }
        }

        private async Task<BitmapSource> LoadArtworkAsync(string key)
        {
            try
            {
                IBitmap img = await ArtworkCache.Instance.Retrieve(key, 50, orderHint);

                return img.ToNative();
            }

            catch (KeyNotFoundException ex)
            {
                this.Log().WarnException(string.Format("Could not find key {0} of album cover. This reeks like a threading problem", key), ex);

                return null;
            }

            catch (ArtworkCacheException ex)
            {
                this.Log().ErrorException(string.Format("Unable to load artwork with key {0} from cache", key), ex);

                return null;
            }

            catch (Exception ex)
            {
                this.Log().InfoException(string.Format("Akavache threw an error on artist cover loading for key {0}", key), ex);

                return null;
            }
        }
    }
}