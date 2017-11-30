﻿using Playnite.Database;
using Playnite.Models;
using Playnite.Providers.BattleNet;
using Playnite.Providers.GOG;
using Playnite.Providers.Origin;
using Playnite.Providers.Steam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Playnite.MetaProviders
{
    public interface IMetadataProvider
    {
        bool GetSupportsIdSearch();
        List<MetadataSearchResult> SearchGames(string gameName);
        GameMetadata GetGameData(string gameId);
    }

    public enum MetadataSource
    {
        Store,
        IGDB,
        IGDBOverStore,
        StoreOverIGDB
    }

    public class MetadataFieldSettings : ObservableObject
    {
        private bool import = true;
        public bool Import
        {
            get => import;
            set
            {
                import = value;
                OnPropertyChanged("Import");
            }
        }

        private MetadataSource source = MetadataSource.StoreOverIGDB;
        public MetadataSource Source
        {
            get => source;
            set
            {
                source = value;
                OnPropertyChanged("Source");
            }
        }
    }

    public class MetadataDownloaderSettings : ObservableObject
    {
        private MetadataFieldSettings genre = new MetadataFieldSettings();
        public MetadataFieldSettings Genre
        {
            get => genre;
            set
            {
                genre = value;
                OnPropertyChanged("Genre");
            }
        }

        private MetadataFieldSettings releaseDate = new MetadataFieldSettings();
        public MetadataFieldSettings ReleaseDate
        {
            get => releaseDate;
            set
            {
                releaseDate = value;
                OnPropertyChanged("ReleaseDate");
            }
        }

        private MetadataFieldSettings developer = new MetadataFieldSettings();
        public MetadataFieldSettings Developer
        {
            get => developer;
            set
            {
                developer = value;
                OnPropertyChanged("Developer");
            }
        }

        private MetadataFieldSettings publisher = new MetadataFieldSettings();
        public MetadataFieldSettings Publisher
        {
            get => publisher;
            set
            {
                publisher = value;
                OnPropertyChanged("Publisher");
            }
        }

        private MetadataFieldSettings tag = new MetadataFieldSettings();
        public MetadataFieldSettings Tag
        {
            get => tag;
            set
            {
                tag = value;
                OnPropertyChanged("Tag");
            }
        }

        private MetadataFieldSettings description = new MetadataFieldSettings();
        public MetadataFieldSettings Description
        {
            get => description;
            set
            {
                description = value;
                OnPropertyChanged("Description");
            }
        }

        private MetadataFieldSettings coverImage = new MetadataFieldSettings() { Source = MetadataSource.IGDBOverStore };
        public MetadataFieldSettings CoverImage
        {
            get => coverImage;
            set
            {
                coverImage = value;
                OnPropertyChanged("CoverImage");
            }
        }

        private MetadataFieldSettings backgroundImage = new MetadataFieldSettings() { Source = MetadataSource.Store };
        public MetadataFieldSettings BackgroundImage
        {
            get => backgroundImage;
            set
            {
                backgroundImage = value;
                OnPropertyChanged("BackgroundImage");
            }
        }

        private MetadataFieldSettings icon = new MetadataFieldSettings() { Source = MetadataSource.Store };
        public MetadataFieldSettings Icon
        {
            get => icon;
            set
            {
                icon = value;
                OnPropertyChanged("Icon");
            }
        }

        private MetadataFieldSettings links = new MetadataFieldSettings();
        public MetadataFieldSettings Links
        {
            get => links;
            set
            {
                links = value;
                OnPropertyChanged("Links");
            }
        }
    }

    public class MetadataSearchResult
    {
        public string Id
        {
            get; set;
        }

        public string Name
        {
            get; set;
        }

        public DateTime? ReleaseDate
        {
            get; set;
        }

        public MetadataSearchResult()
        {
        }

        public MetadataSearchResult(string id, string name, DateTime? releaseDate)
        {
            Id = id;
            Name = name;
            ReleaseDate = releaseDate;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class MetadataDownloader
    {
        IMetadataProvider steamProvider;
        IMetadataProvider originProvider;
        IMetadataProvider gogProvider;
        IMetadataProvider battleNetProvider;
        IMetadataProvider igdbProvider;

        public MetadataDownloader()
        {
            steamProvider = new SteamMetadataProvider();
            originProvider = new OriginMetadataProvider();
            gogProvider = new GogMetadataProvider();
            battleNetProvider = new BattleNetMetadataProvider();
            igdbProvider = new IGDBMetadataProvider();
        }

        public MetadataDownloader(
            IMetadataProvider steamProvider,
            IMetadataProvider originProvider,
            IMetadataProvider gogProvider,
            IMetadataProvider battleNetProvider,
            IMetadataProvider igdbProvider)
        {
            this.steamProvider = steamProvider;
            this.originProvider = originProvider;
            this.gogProvider = gogProvider;
            this.battleNetProvider = battleNetProvider;
            this.igdbProvider = igdbProvider;
        }

        private IMetadataProvider GetMetaProviderByProvider(Provider provider)
        {
            switch (provider)
            {
                case Provider.GOG:
                    return gogProvider;
                case Provider.Origin:
                    return originProvider;
                case Provider.Steam:
                    return steamProvider;
                case Provider.BattleNet:
                    return battleNetProvider;
                case Provider.Uplay:
                case Provider.Custom:
                default:
                    return null;
            }
        }

        private string ReplaceNumsForRomans(Match m)
        {
            return Roman.To(int.Parse(m.Value));
        }

        private GameMetadata ProcessDownload(IGame game, ref GameMetadata data)
        {
            if (data == null)
            {
                data = DownloadGameData(game.Name, game.ProviderId, GetMetaProviderByProvider(game.Provider));
            }

            return data;
        }

        private GameMetadata ProcessField(
            IGame game,
            MetadataFieldSettings field,
            ref GameMetadata storeData,
            ref GameMetadata igdbData,
            Func<GameMetadata, object> propertySelector)
        {
            if (field.Import)
            {
                if (field.Source == MetadataSource.Store && game.Provider != Provider.Custom && game.Provider != Provider.Uplay)
                {
                    storeData = ProcessDownload(game, ref storeData);
                    return storeData;
                }
                else if (field.Source == MetadataSource.IGDB)
                {
                    if (igdbData == null)
                    {
                        igdbData = DownloadGameData(game.Name, "", igdbProvider);
                    }

                    return igdbData;
                }
                else if (field.Source == MetadataSource.IGDBOverStore)
                {
                    if (igdbData == null)
                    {
                        igdbData = DownloadGameData(game.Name, "", igdbProvider);
                    }

                    if (igdbData.GameData == null && game.Provider != Provider.Custom && game.Provider != Provider.Uplay)
                    {
                        if (storeData == null)
                        {
                            storeData = ProcessDownload(game, ref storeData);
                        }

                        return storeData;
                    }
                    else
                    {
                        var igdbValue = propertySelector(igdbData);
                        if (igdbValue != null)
                        {
                            return igdbData;
                        }
                        else if (game.Provider != Provider.Custom && game.Provider != Provider.Uplay)
                        {
                            if (storeData == null)
                            {
                                storeData = ProcessDownload(game, ref storeData);
                            }

                            if (storeData.GameData != null)
                            {
                                return storeData;
                            }
                            else
                            {
                                return igdbData;
                            }
                        }
                    }
                }
                else if (field.Source == MetadataSource.StoreOverIGDB)
                {
                    if (game.Provider != Provider.Custom && game.Provider != Provider.Uplay)
                    {
                        if (storeData == null)
                        {
                            storeData = ProcessDownload(game, ref storeData);
                        }

                        if (storeData.GameData == null)
                        {
                            if (igdbData == null)
                            {
                                igdbData = DownloadGameData(game.Name, "", igdbProvider);
                            }

                            return igdbData;
                        }
                        else
                        {
                            var storeValue = propertySelector(storeData);
                            if (storeValue != null)
                            {
                                return storeData;
                            }
                            else
                            {
                                if (igdbData == null)
                                {
                                    igdbData = DownloadGameData(game.Name, "", igdbProvider);
                                }

                                return igdbData;
                            }
                        }
                    }
                    else
                    {
                        if (igdbData == null)
                        {
                            igdbData = DownloadGameData(game.Name, "", igdbProvider);
                        }

                        return igdbData;
                    }
                }
            }

            return null;
        }

        public void DownloadMetadata(List<IGame> games, GameDatabase database, MetadataDownloaderSettings settings)
        {
            foreach (Game game in games)
            {
                GameMetadata storeData = null;
                GameMetadata igdbData = null;
                GameMetadata gameData;

                // Genre
                gameData = ProcessField(game, settings.Genre, ref storeData, ref igdbData, (a) => a.GameData?.Genres);
                game.Genres = gameData?.GameData?.Genres ?? game.Genres;

                // Release Date
                gameData = ProcessField(game, settings.ReleaseDate, ref storeData, ref igdbData, (a) => a.GameData?.ReleaseDate);
                game.ReleaseDate = gameData?.GameData?.ReleaseDate ?? game.ReleaseDate;

                // Developer
                gameData = ProcessField(game, settings.Developer, ref storeData, ref igdbData, (a) => a.GameData?.Developers);
                game.Developers = gameData?.GameData?.Developers ?? game.Developers;

                // Publisher
                gameData = ProcessField(game, settings.Publisher, ref storeData, ref igdbData, (a) => a.GameData?.Publishers);
                game.Publishers = gameData?.GameData?.Publishers ?? game.Publishers;

                // Tags / Features
                gameData = ProcessField(game, settings.Tag, ref storeData, ref igdbData, (a) => a.GameData?.Tags);
                game.Tags = gameData?.GameData?.Tags ?? game.Tags;

                // Description
                gameData = ProcessField(game, settings.Description, ref storeData, ref igdbData, (a) => a.GameData?.Description);
                game.Description = gameData?.GameData?.Description ?? game.Description;

                // Links
                gameData = ProcessField(game, settings.Links, ref storeData, ref igdbData, (a) => a.GameData?.Links);
                game.Links = gameData?.GameData?.Links ?? game.Links;

                // BackgroundImage
                gameData = ProcessField(game, settings.BackgroundImage, ref storeData, ref igdbData, (a) => a.BackgroundImage);
                game.BackgroundImage = gameData?.BackgroundImage ?? game.BackgroundImage;

                // Cover
                gameData = ProcessField(game, settings.CoverImage, ref storeData, ref igdbData, (a) => a.Image);
                if (gameData?.Image != null)
                {
                    if (!string.IsNullOrEmpty(game.Image))
                    {
                        database.DeleteImageSafe(game.Image, game);
                    }

                    var imageId = database.AddFileNoDuplicate(gameData.Image);
                    game.Image = imageId;
                }

                // Icon
                gameData = ProcessField(game, settings.Icon, ref storeData, ref igdbData, (a) => a.Icon);
                if (gameData?.Icon != null)
                {
                    if (!string.IsNullOrEmpty(game.Icon))
                    {
                        database.DeleteImageSafe(game.Icon, game);
                    }

                    var iconId = database.AddFileNoDuplicate(gameData.Icon);
                    game.Icon = iconId;
                }

                database.UpdateGameInDatabase(game);
            }
        }

        public virtual GameMetadata DownloadGameData(string gameName, string id, IMetadataProvider provider)
        {
            if (provider.GetSupportsIdSearch())
            {
                return provider.GetGameData(id);
            }
            else
            {
                var name = StringExtensions.NormalizeGameName(gameName);
                var results = provider.SearchGames(name.Trim());

                Func<string, GameMetadata> matchFun = matchName =>
                {
                    var res = results.FirstOrDefault(a => string.Equals(matchName, a.Name, StringComparison.InvariantCultureIgnoreCase));
                    if (res != null)
                    {
                        return provider.GetGameData(res.Id);
                    }
                    else
                    {
                        return null;
                    }
                };

                GameMetadata data = null;
                string testName = string.Empty;

                // Direct comparison
                data = matchFun(name);
                if (data != null)
                {
                    return data;
                }

                // Try replacing roman numerals: 3 => III
                testName = Regex.Replace(name, @"\d+", ReplaceNumsForRomans);
                data = matchFun(testName);
                if (data != null)
                {
                    return data;
                }

                // Try adding The
                testName = "The " + name;
                data = matchFun(testName);
                if (data != null)
                {
                    return data;
                }

                // Try chaning & / and
                testName = Regex.Replace(name, @"\s+and\s+", " & ", RegexOptions.IgnoreCase);
                data = matchFun(testName);
                if (data != null)
                {
                    return data;
                }

                // Try without subtitle
                var testResult = results.FirstOrDefault(a =>
                {
                    if (!string.IsNullOrEmpty(a.Name) && a.Name.Contains(":"))
                    {
                        return string.Equals(name, a.Name.Split(':')[0], StringComparison.InvariantCultureIgnoreCase);
                    }
                    
                    return false;
                });

                if (testResult != null)
                {
                    return provider.GetGameData(testResult.Id);
                }
                                
                return data ?? new GameMetadata();
            }
        }
    }
}