﻿// #define DO_NOT_LOAD_CACHE

using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

using ModIO.API;

namespace ModIO
{
    /// <summary>An interface for storing/loading data retrieved for the mod.io servers on disk.</summary>
    public static class CacheClient
    {
        // ---------[ MEMBERS ]---------
        /// <summary>Directory for the cache.</summary>
        private static string _cacheDirectory = null;

        // ---------[ INITIALIZATION ]---------
        static CacheClient()
        {
            string dir;
            #pragma warning disable 0162
            #if DEBUG
            if(GlobalSettings.USE_TEST_SERVER)
            {
                dir = Application.persistentDataPath + "/modio_testServer/";
            }
            else
            #endif
            {
                dir = Application.persistentDataPath + "/modio/";
            }
            #pragma warning restore 0162

            TrySetCacheDirectory(dir);
        }

        /// <summary>Attempts to set the cache directory.</summary>
        public static bool TrySetCacheDirectory(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to set cache directory."
                                      + "\nDirectory: " + directory + "\n\n");

                Debug.LogError(warningInfo
                               + Utility.GenerateExceptionDebugString(e));

                return false;
            }

            CacheClient._cacheDirectory = directory;
            return true;
        }

        // ---------[ GET DIRECTORIES ]---------
        /// <summary>Retrieves the directory the CacheClient uses.</summary>
        public static string GetCacheDirectory()
        {
            return CacheClient._cacheDirectory;
        }

        /// <summary>Generates the path for a mod cache directory.</summary>
        public static string GenerateModDirectoryPath(int modId)
        {
            return(CacheClient._cacheDirectory
                   + "mods/"
                   + modId.ToString() + "/");
        }

        /// <summary>[Obsolete] Generates the path for a cached mod build directory.</summary>
        [Obsolete("Use CacheClient.GenerateModBinariesDirectoryPath() instead.")]
        public static string GenerateModBuildsDirectoryPath(int modId)
        { return CacheClient.GenerateModBinariesDirectoryPath(modId); }

        /// <summary>Generates the path for a cached mod build directory.</summary>
        public static string GenerateModBinariesDirectoryPath(int modId)
        {
            return(CacheClient.GenerateModDirectoryPath(modId)
                   + "binaries/");
        }


        // ---------[ BASIC FILE I/O ]---------
        /// <summary>Reads an entire file and parses the JSON Object it contains.</summary>
        public static T ReadJsonObjectFile<T>(string filePath)
        {
            if(File.Exists(filePath))
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read json object from file."
                                          + "\nFile: " + filePath + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));
                }
            }
            return default(T);
        }

        /// <summary>Writes an object to a file in the JSON Object format.</summary>
        public static bool WriteJsonObjectFile<T>(string filePath,
                                                  T jsonObject)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, JsonConvert.SerializeObject(jsonObject));
                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to write json object to file."
                                      + "\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return false;
        }

        /// <summary>Loads an entire binary file as a byte array.</summary>
        public static byte[] LoadBinaryFile(string filePath)
        {
            byte[] fileData = null;

            if(File.Exists(filePath))
            {
                try
                {
                    fileData = File.ReadAllBytes(filePath);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read binary file."
                                          + "\nFile: " + filePath + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));

                    fileData = null;
                }
            }

            return fileData;
        }

        /// <summary>Writes an entire binary file.</summary>
        public static bool WriteBinaryFile(string filePath,
                                           byte[] data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllBytes(filePath, data);
                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to write binary file."
                                      + "\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return false;
        }

        /// <summary>Loads the image data from a file into a new Texture.</summary>
        public static Texture2D ReadImageFile(string filePath)
        {
            Texture2D texture = null;

            if(File.Exists(filePath))
            {
                byte[] imageData = CacheClient.LoadBinaryFile(filePath);

                if(imageData != null)
                {
                    texture = new Texture2D(0,0);
                    texture.LoadImage(imageData);
                }
            }

            return texture;
        }

        /// <summary>Writes a texture to a PNG file.</summary>
        public static bool WritePNGFile(string filePath,
                                        Texture2D texture)
        {
            Debug.Assert(Path.GetExtension(filePath).Equals(".png"),
                         "[mod.io] Images can only be saved in PNG format."
                         + "\n" + filePath
                         + " is an invalid file path.");

            return CacheClient.WriteBinaryFile(filePath, texture.EncodeToPNG());
        }

        /// <summary>Deletes a file.</summary>
        public static bool DeleteFile(string filePath)
        {
            try
            {
                if(File.Exists(filePath)) { File.Delete(filePath); }
                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to delete file."
                                      + "\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return false;
        }

        /// <summary>Deletes a directory.</summary>
        public static bool DeleteDirectory(string directoryPath)
        {
            try
            {
                if(Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }

                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to delete directory."
                                      + "\nDirectory: " + directoryPath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            return false;
        }


        // ---------[ AUTHENTICATED USER ]---------
        /// <summary>Wrapper object for managing the authenticated user's information.</summary>
        [Serializable]
        private class AuthenticatedUser
        {
            public string oAuthToken;
            public int userId;
            public List<int> modIds;
        }

        /// <summary>File path for the authenticated user data.</summary>
        public static string userFilePath
        { get { return CacheClient._cacheDirectory + "user.data"; } }

        /// <summary>Stores the authenticated user token in the cache.</summary>
        public static bool SaveAuthenticatedUserToken(string oAuthToken)
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au == null)
            {
                au = new AuthenticatedUser();
            }

            au.oAuthToken = oAuthToken;

            return CacheClient.WriteJsonObjectFile(userFilePath, au);
        }

        /// <summary>Retrieves the authenticated user token from the cache.</summary>
        public static string LoadAuthenticatedUserToken()
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au != null)
            {
                return au.oAuthToken;
            }
            return null;
        }

        /// <summary>Clears the authenticated user token from the cache.</summary>
        public static bool ClearAuthenticatedUserToken()
        {
            return CacheClient.SaveAuthenticatedUserToken(string.Empty);
        }

        /// <summary>Stores the authenticated user's profile in the cache.</summary>
        public static bool SaveAuthenticatedUserProfile(UserProfile userProfile)
        {
            CacheClient.SaveUserProfile(userProfile);

            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au == null)
            {
                au = new AuthenticatedUser();
            }

            au.userId = userProfile.id;

            return CacheClient.WriteJsonObjectFile(userFilePath, au);
        }

        /// <summary>Retrieves the authenticated user's profile from the cache.</summary>
        public static UserProfile LoadAuthenticatedUserProfile()
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au != null
               && au.userId > 0)
            {
                return LoadUserProfile(au.userId);
            }
            return null;
        }

        /// <summary>Clears the authenticated user's profile from the cache.</summary>
        public static bool ClearAuthenticatedUserProfile()
        {
            return CacheClient.SaveUserProfile(null);
        }

        /// <summary>Stores the authenticated user's mod subscriptions in the cache.</summary>
        [Obsolete("Use ModManager.SetSubscribedModIds() instead.")]
        public static bool SaveAuthenticatedUserSubscriptions(List<int> subscribedModIds)
        {
            ModManager.SetSubscribedModIds(subscribedModIds);
            return true;
        }

        /// <summary>[Obsolete] Retrieves the authenticated user's mod subscriptions from the cache.</summary>
        [Obsolete("Use ModManager.GetSubscribedModIds() instead.")]
        public static List<int> LoadAuthenticatedUserSubscriptions()
        {
            return new List<int>(ModManager.GetSubscribedModIds());
        }

        /// <summary>[Obsolete] Clears the authenticated user's mod subscriptions from the cache.</summary>
        [Obsolete("Use ModManager.SetSubscribedModIds(null) instead.")]
        public static bool ClearAuthenticatedUserSubscriptions()
        {
            ModManager.SetSubscribedModIds(null);
            return true;
        }

        /// <summary>Stores the authenticated user's mods in the cache.</summary>
        public static bool SaveAuthenticatedUserMods(List<int> modIds)
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au == null)
            {
                au = new AuthenticatedUser();
            }

            au.modIds = modIds;

            return CacheClient.WriteJsonObjectFile(userFilePath, au);
        }

        /// <summary>Retrieves the authenticated user's mods from the cache.</summary>
        public static List<int> LoadAuthenticatedUserMods()
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au != null)
            {
                return au.modIds;
            }

            return null;
        }

        /// <summary>Clears the authenticated user's mods from the cache.</summary>
        public static bool ClearAuthenticatedUserMods()
        {
            return CacheClient.SaveAuthenticatedUserMods(null);
        }


        /// <summary>Deletes the authenticated user's data from the cache.</summary>
        public static bool DeleteAuthenticatedUser()
        {
            return CacheClient.DeleteFile(CacheClient.userFilePath);
        }


        // ---------[ GAME PROFILE ]---------
        /// <summary>File path for the game profile data.</summary>
        public static string gameProfileFilePath
        { get { return CacheClient._cacheDirectory + "game_profile.data"; } }

        /// <summary>Stores the game's profile in the cache.</summary>
        public static bool SaveGameProfile(GameProfile profile)
        {
            return CacheClient.WriteJsonObjectFile(gameProfileFilePath, profile);
        }

        /// <summary>Retrieves the game's profile from the cache.</summary>
        public static GameProfile LoadGameProfile()
        {
            return CacheClient.ReadJsonObjectFile<GameProfile>(gameProfileFilePath);
        }


        // ---------[ MOD PROFILES ]---------
        /// <summary>Generates the file path for a mod's profile data.</summary>
        public static string GenerateModProfileFilePath(int modId)
        {
            return (CacheClient.GenerateModDirectoryPath(modId)
                    + "profile.data");
        }

        /// <summary>Stores a mod's profile in the cache.</summary>
        public static bool SaveModProfile(ModProfile profile)
        {
            Debug.Assert(profile.id > 0,
                         "[mod.io] Cannot cache a mod without a mod id");

            return CacheClient.WriteJsonObjectFile(GenerateModProfileFilePath(profile.id),
                                                   profile);
        }

        /// <summary>Retrieves a mod's profile from the cache.</summary>
        public static ModProfile LoadModProfile(int modId)
        {
            string profileFilePath = GenerateModProfileFilePath(modId);
            ModProfile profile = CacheClient.ReadJsonObjectFile<ModProfile>(profileFilePath);
            return(profile);
        }

        /// <summary>Stores a collection of mod profiles in the cache.</summary>
        public static bool SaveModProfiles(IEnumerable<ModProfile> modProfiles)
        {
            bool isSuccessful = true;
            foreach(ModProfile profile in modProfiles)
            {
                isSuccessful = CacheClient.SaveModProfile(profile) && isSuccessful;
            }
            return isSuccessful;
        }

        /// <summary>[Obsolete] Iterates through all of the mod profiles in the cache.</summary>
        [Obsolete("Use CacheClient.IterateAllModProfiles() instead.")]
        public static IEnumerable<ModProfile> AllModProfiles()
        { return CacheClient.IterateAllModProfiles(); }

        /// <summary>Iterates through all of the mod profiles in the cache.</summary>
        public static IEnumerable<ModProfile> IterateAllModProfiles()
        {
            return IterateAllModProfilesFromOffset(0);
        }

        /// <summary>Iterates through all of the mod profiles from the given offset.</summary>
        public static IEnumerable<ModProfile> IterateAllModProfilesFromOffset(int offset)
        {
            string profileDirectory = CacheClient._cacheDirectory + "mods/";

            if(Directory.Exists(profileDirectory))
            {
                string[] modDirectories;
                try
                {
                    modDirectories = Directory.GetDirectories(profileDirectory);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read mod profile directory."
                                          + "\nDirectory: " + profileDirectory + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));

                    modDirectories = new string[0];
                }

                int offsetDirCount = modDirectories.Length - offset;
                if(offsetDirCount > 0)
                {
                    string[] offsetModDirectories = new string[offsetDirCount];
                    Array.Copy(modDirectories, offset,
                               offsetModDirectories, 0,
                               offsetDirCount);

                    foreach(string modDirectory in offsetModDirectories)
                    {
                        ModProfile profile = CacheClient.ReadJsonObjectFile<ModProfile>(modDirectory + "/profile.data");

                        if(profile != null)
                        {
                            yield return profile;
                        }
                    }
                }
            }
        }

        /// <summary>Determines how many ModProfiles are currently stored in the cache.</summary>
        public static int CountModProfiles()
        {
            string profileDirectory = CacheClient._cacheDirectory + "mods/";

            if(Directory.Exists(profileDirectory))
            {
                string[] modDirectories;
                try
                {
                    modDirectories = Directory.GetDirectories(profileDirectory);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read mod profile directory."
                                          + "\nDirectory: " + profileDirectory + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));

                    modDirectories = new string[0];
                }

                return modDirectories.Length;
            }

            return 0;
        }

        /// <summary>Deletes all of a mod's data from the cache.</summary>
        public static bool DeleteMod(int modId)
        {
            string modDir = CacheClient.GenerateModDirectoryPath(modId);

            // TODO(@jackson): Cleanup
            string installLocation = (CacheClient.GetCacheDirectory()
                                      + "_installedMods/"
                                      + modId.ToString() + "/");
            if(Directory.Exists(installLocation))
            {
                CacheClient.DeleteDirectory(installLocation);
            }

            return CacheClient.DeleteDirectory(modDir);
        }

        // ---------[ MOD STATISTICS ]---------
        public static string GenerateModStatisticsFilePath(int modId)
        {
            return(CacheClient.GenerateModDirectoryPath(modId)
                   + "stats.data");
        }

        public static ModStatistics LoadModStatistics(int modId)
        {
            string statsFilePath = GenerateModStatisticsFilePath(modId);
            ModStatistics stats = CacheClient.ReadJsonObjectFile<ModStatistics>(statsFilePath);
            return(stats);
        }

        public static bool SaveModStatistics(ModStatistics stats)
        {
            Debug.Assert(stats.modId > 0,
                         "[mod.io] Cannot cache a mod without a mod id");

            string statsFilePath = GenerateModStatisticsFilePath(stats.modId);
            return CacheClient.WriteJsonObjectFile(statsFilePath, stats);
        }

        // ---------[ MODFILES ]---------
        /// <summary>Generates the file path for a modfile.</summary>
        public static string GenerateModfileFilePath(int modId, int modfileId)
        {
            return(CacheClient.GenerateModBinariesDirectoryPath(modId)
                   + modfileId + ".data");
        }

        /// <summary>Generates the file path for a mod binary.</summary>
        public static string GenerateModBinaryZipFilePath(int modId, int modfileId)
        {
            return(CacheClient.GenerateModBinariesDirectoryPath(modId)
                   + modfileId + ".zip");
        }

        /// <summary>Stores a modfile in the cache.</summary>
        public static bool SaveModfile(Modfile modfile)
        {
            Debug.Assert(modfile.modId > 0,
                         "[mod.io] Cannot cache a modfile without a mod id");
            Debug.Assert(modfile.id > 0,
                         "[mod.io] Cannot cache a modfile without a modfile id");

            return CacheClient.WriteJsonObjectFile(GenerateModfileFilePath(modfile.modId, modfile.id),
                                                   modfile);
        }

        /// <summary>Retrieves a modfile from the cache.</summary>
        public static Modfile LoadModfile(int modId, int modfileId)
        {
            string modfileFilePath = GenerateModfileFilePath(modId, modfileId);
            var modfile = CacheClient.ReadJsonObjectFile<Modfile>(modfileFilePath);
            return modfile;
        }

        /// <summary>Stores a mod binary's ZipFile data in the cache.</summary>
        public static bool SaveModBinaryZip(int modId, int modfileId,
                                            byte[] modBinary)
        {
            Debug.Assert(modId > 0,
                         "[mod.io] Cannot cache a mod binary without a mod id");
            Debug.Assert(modfileId > 0,
                         "[mod.io] Cannot cache a mod binary without a modfile id");

            string filePath = GenerateModBinaryZipFilePath(modId, modfileId);
            return CacheClient.WriteBinaryFile(filePath, modBinary);
        }

        /// <summary>Retrieves a mod binary's ZipFile data from the cache.</summary>
        public static byte[] LoadModBinaryZip(int modId, int modfileId)
        {
            string filePath = GenerateModBinaryZipFilePath(modId, modfileId);
            byte[] zipData = CacheClient.LoadBinaryFile(filePath);
            return zipData;
        }

        /// <summary>Deletes a modfile and binary from the cache.</summary>
        public static bool DeleteModfileAndBinaryZip(int modId, int modfileId)
        {
            bool isSuccessful = CacheClient.DeleteFile(CacheClient.GenerateModfileFilePath(modId, modfileId));
            isSuccessful = CacheClient.DeleteFile(CacheClient.GenerateModBinaryZipFilePath(modId, modfileId)) && isSuccessful;
            return isSuccessful;
        }

        /// <summary>Deletes all modfiles and binaries from the cache.</summary>
        public static bool DeleteAllModfileAndBinaryData(int modId)
        {
            return CacheClient.DeleteDirectory(CacheClient.GenerateModBinariesDirectoryPath(modId));
        }

        // ---------[ MOD MEDIA ]---------
        /// <summary>Generates the directory path for a mod logo collection.</summary>
        public static string GenerateModLogoCollectionDirectoryPath(int modId)
        {
            return(CacheClient.GenerateModDirectoryPath(modId)
                   + "logo/");
        }

        /// <summary>Generates the file path for a mod logo.</summary>
        public static string GenerateModLogoFilePath(int modId, LogoSize size)
        {
            return (GenerateModLogoCollectionDirectoryPath(modId)
                    + size.ToString() + ".png");
        }

        /// <summary>Generates the file path for a mod logo's cached version information.</summary>
        public static string GenerateModLogoVersionInfoFilePath(int modId)
        {
            return(CacheClient.GenerateModLogoCollectionDirectoryPath(modId)
                   + "versionInfo.data");
        }

        /// <summary>[Obsolete] Generates the directory path for the cached mod media.</summary>
        [Obsolete("Use CacheClient.GenerateModMediaDirectoryPath() instead.")]
        public static string GenerateModGalleryImageDirectoryPath(int modId)
        {
            return(GenerateModMediaDirectoryPath(modId));
        }

        /// <summary>Generates the directory path for the cached mod media.</summary>
        public static string GenerateModMediaDirectoryPath(int modId)
        {
            return(GenerateModDirectoryPath(modId)
                   + "mod_media/");
        }

        /// <summary>Generates the file path for a mod galley image.</summary>
        public static string GenerateModGalleryImageFilePath(int modId,
                                                             string imageFileName,
                                                             ModGalleryImageSize size)
        {
            return(GenerateModMediaDirectoryPath(modId)
                   + "images_" + size.ToString() + "/"
                   + Path.GetFileNameWithoutExtension(imageFileName)
                   + ".png");
        }

        /// <summary>Generates the file path for a YouTube thumbnail.</summary>
        public static string GenerateModYouTubeThumbnailFilePath(int modId,
                                                                 string youTubeId)
        {
            return(GenerateModMediaDirectoryPath(modId)
                   + "youTube/"
                   + youTubeId + ".png");
        }

        /// <summary>Retrieves the file paths for the mod logos in the cache.</summary>
        public static Dictionary<LogoSize, string> LoadModLogoFilePaths(int modId)
        {
            return CacheClient.ReadJsonObjectFile<Dictionary<LogoSize, string>>(CacheClient.GenerateModLogoVersionInfoFilePath(modId));
        }

        /// <summary>Stores a mod logo in the cache.</summary>
        public static bool SaveModLogo(int modId, string fileName,
                                       LogoSize size, Texture2D logoTexture)
        {
            Debug.Assert(modId > 0,
                         "[mod.io] Cannot cache a mod logo without a mod id");
            Debug.Assert(!String.IsNullOrEmpty(fileName),
                         "[mod.io] Cannot cache a mod logo without file name as it"
                         + " is used for versioning purposes");

            string logoFilePath = CacheClient.GenerateModLogoFilePath(modId, size);
            CacheClient.WritePNGFile(logoFilePath, logoTexture);

            // - Version Info -
            var versionInfo = CacheClient.LoadModLogoFilePaths(modId);
            if(versionInfo == null)
            {
                versionInfo = new Dictionary<LogoSize, string>();
            }

            versionInfo[size] = fileName;
            return CacheClient.WriteJsonObjectFile(GenerateModLogoVersionInfoFilePath(modId),
                                                   versionInfo);
        }

        /// <summary>Retrieves a mod logo from the cache.</summary>
        public static Texture2D LoadModLogo(int modId, LogoSize size)
        {
            string logoFilePath = CacheClient.GenerateModLogoFilePath(modId, size);
            Texture2D logoTexture = CacheClient.ReadImageFile(logoFilePath);
            return(logoTexture);
        }

        /// <summary>Stores a mod gallery image in the cache.</summary>
        public static bool SaveModGalleryImage(int modId,
                                               string imageFileName,
                                               ModGalleryImageSize size,
                                               Texture2D imageTexture)
        {
            Debug.Assert(modId > 0,
                         "[mod.io] Cannot cache a mod image without a mod id");

            string imageFilePath = CacheClient.GenerateModGalleryImageFilePath(modId,
                                                                               imageFileName,
                                                                               size);
            return CacheClient.WritePNGFile(imageFilePath, imageTexture);
        }

        /// <summary>Retrieves a mod gallery image from the cache.</summary>
        public static Texture2D LoadModGalleryImage(int modId,
                                                    string imageFileName,
                                                    ModGalleryImageSize size)
        {
            string imageFilePath = CacheClient.GenerateModGalleryImageFilePath(modId,
                                                                               imageFileName,
                                                                               size);
            Texture2D imageTexture = CacheClient.ReadImageFile(imageFilePath);

            return(imageTexture);
        }

        /// <summary>Stores a YouTube thumbnail in the cache.</summary>
        public static bool SaveModYouTubeThumbnail(int modId,
                                                   string youTubeId,
                                                   Texture2D thumbnail)
        {
            Debug.Assert(modId > 0,
                         "[mod.io] Cannot cache a mod image without a mod id");

            string thumbnailFilePath = CacheClient.GenerateModYouTubeThumbnailFilePath(modId,
                                                                                       youTubeId);
            return CacheClient.WritePNGFile(thumbnailFilePath, thumbnail);
        }

        /// <summary>Retrieves a YouTube thumbnail from the cache.</summary>
        public static Texture2D LoadModYouTubeThumbnail(int modId,
                                                        string youTubeId)
        {
            string thumbnailFilePath = CacheClient.GenerateModYouTubeThumbnailFilePath(modId,
                                                                                       youTubeId);

            Texture2D thumbnailTexture = CacheClient.ReadImageFile(thumbnailFilePath);

            return(thumbnailTexture);
        }

        // ---------[ MOD TEAM ]---------
        /// <summary>Generates the file path for a mod team's data.</summary>
        public static string GenerateModTeamFilePath(int modId)
        {
            return(CacheClient.GenerateModDirectoryPath(modId)
                   + "team.data");
        }

        /// <summary>Stores a mod team's data in the cache.</summary>
        public static bool SaveModTeam(int modId,
                                       List<ModTeamMember> modTeam)
        {
            Debug.Assert(modId > 0,
                         "[mod.io] Cannot cache a mod team without a mod id");

            string filePath = CacheClient.GenerateModTeamFilePath(modId);
            return CacheClient.WriteJsonObjectFile(filePath, modTeam);
        }

        /// <summary>Retrieves a mod team's data from the cache.</summary>
        public static List<ModTeamMember> LoadModTeam(int modId)
        {
            string filePath = CacheClient.GenerateModTeamFilePath(modId);
            var modTeam = CacheClient.ReadJsonObjectFile<List<ModTeamMember>>(filePath);
            return modTeam;
        }

        /// <summary>Deletes a mod team's data from the cache.</summary>
        public static bool DeleteModTeam(int modId)
        {
            return CacheClient.DeleteFile(CacheClient.GenerateModTeamFilePath(modId));
        }

        // ---------[ USERS ]---------
        /// <summary>Generates the file path for a user's profile.</summary>
        public static string GenerateUserProfileFilePath(int userId)
        {
            return(CacheClient.GetCacheDirectory()
                   + "users/" + userId + "/profile.data");
        }

        /// <summary>Generates the file path for a user's profile.</summary>
        public static string GenerateUserAvatarDirectoryPath(int userId)
        {
            return(CacheClient.GetCacheDirectory()
                   + "users/" + userId + "_avatar/");
        }

        /// <summary>Generates the file path for a user's profile.</summary>
        public static string GenerateUserAvatarFilePath(int userId, UserAvatarSize size)
        {
            return(CacheClient.GenerateUserAvatarDirectoryPath(userId)
                   + size.ToString() + ".png");
        }

        /// <summary>Stores a user's profile in the cache.</summary>
        public static bool SaveUserProfile(UserProfile userProfile)
        {
            Debug.Assert(userProfile.id > 0,
                         "[mod.io] Cannot cache a user profile without a user id");

            string filePath = CacheClient.GenerateUserProfileFilePath(userProfile.id);
            return CacheClient.WriteJsonObjectFile(filePath, userProfile);
        }

        /// <summary>Retrieves a user's profile from the cache.</summary>
        public static UserProfile LoadUserProfile(int userId)
        {
            string filePath = CacheClient.GenerateUserProfileFilePath(userId);
            var userProfile = CacheClient.ReadJsonObjectFile<UserProfile>(filePath);
            return(userProfile);
        }

        /// <summary>Deletes a user's profile from the cache.</summary>
        public static bool DeleteUserProfile(int userId)
        {
            return CacheClient.DeleteFile(CacheClient.GenerateUserProfileFilePath(userId));
        }

        /// <summary>Iterates through all the user profiles in the cache.</summary>
        public static IEnumerable<UserProfile> IterateAllUserProfiles()
        {
            string profileDirectory = CacheClient.GetCacheDirectory() + "users/";

            if(Directory.Exists(profileDirectory))
            {
                string[] userFiles;
                try
                {
                    userFiles = Directory.GetFiles(profileDirectory);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read user profile directory."
                                          + "\nDirectory: " + profileDirectory + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));

                    userFiles = new string[0];
                }

                foreach(string profileFilePath in userFiles)
                {
                    var profile = CacheClient.ReadJsonObjectFile<UserProfile>(profileFilePath);
                    if(profile != null)
                    {
                        yield return profile;
                    }
                }
            }
        }

        /// <summary>Stores a user's avatar in the cache.</summary>
        public static bool SaveUserAvatar(int userId, UserAvatarSize size,
                                          Texture2D avatarTexture)
        {
            Debug.Assert(userId > 0,
                         "[mod.io] Cannot cache a user avatar without a user id");

            string avatarFilePath = CacheClient.GenerateUserAvatarFilePath(userId, size);
            return CacheClient.WritePNGFile(avatarFilePath, avatarTexture);
        }

        /// <summary>Retrieves a user's avatar from the cache.</summary>
        public static Texture2D LoadUserAvatar(int userId, UserAvatarSize size)
        {
            string avatarFilePath = CacheClient.GenerateUserAvatarFilePath(userId, size);
            Texture2D avatarTexture = CacheClient.ReadImageFile(avatarFilePath);
            return(avatarTexture);
        }

        /// <summary>Delete's a user's avatars from the cache.</summary>
        public static bool DeleteUserAvatar(int userId)
        {
            return CacheClient.DeleteDirectory(CacheClient.GenerateUserAvatarDirectoryPath(userId));
        }
    }
}
