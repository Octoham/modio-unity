﻿using ModIO.API;

using System;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;

using Debug = UnityEngine.Debug;
using WWWForm = UnityEngine.WWWForm;
using UnityWebRequest = UnityEngine.Networking.UnityWebRequest;
using UnityWebRequestAsyncOperation = UnityEngine.Networking.UnityWebRequestAsyncOperation;
using DownloadHandlerFile = UnityEngine.Networking.DownloadHandlerFile;


namespace ModIO
{
    /// <summary>An interface for sending requests to the mod.io servers.</summary>
    public static class APIClient
    {
        // ---------[ Nested Data-Types]---------
        /// <summary>Data required to collect and prepare callbacks.</summary>
        private struct RequestCallbackCollection
        {
            public List<Action<string>> successCallbacks;
            public List<Action<WebRequestError>> errorCallbacks;
        }

        // ---------[ CONSTANTS ]---------
        /// <summary>Denotes the version of the mod.io web API that this class is compatible with.</summary>
        public const string API_VERSION = "v1";

        /// <summary>URL for the test server</summary>
        public const string API_URL_TESTSERVER = "https://api.test.mod.io/";

        /// <summary>URL for the production server</summary>
        public const string API_URL_PRODUCTIONSERVER = "https://api.mod.io/";

        /// <summary>Version information to provide in the request header.</summary>
        public static readonly string USER_AGENT_HEADER = "modioUnityPlugin-" + ModIOVersion.Current.ToString("X.Y.Z");

        /// <summary>Collection of the HTTP request header keys used by mod.io.</summary>
        public static readonly string[] MODIO_REQUEST_HEADER_KEYS = new string[]
        {
            "authorization",
            "accept-language",
            "content-type",
            "x-unity-version",
            "user-agent",
        };

        // ---------[ SETTINGS ]---------
        /// <summary>Requested language for the API response messages.</summary>
        public static string languageCode = "en";

        // ---------[ DEBUG FUNCTIONALITY ]---------
        /// <summary>Asserts that the required authorization data for making API requests is set.</summary>
        public static bool AssertAuthorizationDetails(bool isUserTokenRequired)
        {
            if(PluginSettings.GAME_ID <= 0
               || String.IsNullOrEmpty(PluginSettings.GAME_API_KEY))
            {
                Debug.LogError("[mod.io] No API requests can be executed without a"
                               + " valid Game Id and Game API Key. These need to be"
                               + " saved into the Plugin Settings (mod.io > Edit Settings"
                               + " before any requests can be sent to the API.");
                return false;
            }

            if(isUserTokenRequired)
            {
                if(String.IsNullOrEmpty(LocalUser.OAuthToken))
                {
                    Debug.LogError("[mod.io] API request to modification or User-specific"
                                   + " endpoints cannot be made without first setting the"
                                   + " User Authorization Data instance with a valid token.");
                    return false;
                }
                else if(LocalUser.WasTokenRejected)
                {
                    Debug.LogWarning("[mod.io] An API request is being made with a UserAuthenticationData"
                                     + " token that has been flagged as previously rejected."
                                     + " A check to ensure"
                                     + " LocalUser.AuthenticationState == AuthenticationState.ValidToken"
                                     + " should be made prior to making user-authorization calls.");
                }
            }

            return true;
        }

        // ---------[ REQUEST HANDLING ]---------
        /// <summary>Active request mapping.</summary>
        private static Dictionary<string, UnityWebRequestAsyncOperation> _activeGetRequests
            = new Dictionary<string, UnityWebRequestAsyncOperation>();

        /// <summary>Active request-callback mapping.</summary>
        private static Dictionary<UnityWebRequestAsyncOperation, RequestCallbackCollection> _requestResponseMap
            = new Dictionary<UnityWebRequestAsyncOperation, RequestCallbackCollection>();

        /// <summary>Generates the object for a basic mod.io server request.</summary>
        public static UnityWebRequest GenerateQuery(string endpointURL,
                                                    string filterString,
                                                    APIPaginationParameters pagination)
        {
            APIClient.AssertAuthorizationDetails(false);

            string paginationString;
            if(pagination == null)
            {
                paginationString = string.Empty;
            }
            else
            {
                paginationString = ("&_limit=" + pagination.limit
                                    + "&_offset=" + pagination.offset);
            }

            string queryURL = (endpointURL
                               + "?" + filterString
                               + paginationString);

            UnityWebRequest webRequest = UnityWebRequest.Get(queryURL);

            if(LocalUser.AuthenticationState == AuthenticationState.ValidToken)
            {
                webRequest.SetRequestHeader("Authorization", "Bearer " + LocalUser.OAuthToken);
            }
            else
            {
                webRequest.url += "&api_key=" + PluginSettings.GAME_API_KEY;
            }

            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);
            webRequest.SetRequestHeader("user-agent", APIClient.USER_AGENT_HEADER);

            return webRequest;
        }

        /// <summary>Generates the object for a mod.io GET request.</summary>
        public static UnityWebRequest GenerateGetRequest(string endpointURL,
                                                         string filterString,
                                                         APIPaginationParameters pagination)
        {
            APIClient.AssertAuthorizationDetails(true);

            string paginationString;
            if(pagination == null)
            {
                paginationString = string.Empty;
            }
            else
            {
                paginationString = ("&_limit=" + pagination.limit
                                    + "&_offset=" + pagination.offset);
            }

            string constructedURL = (endpointURL
                                     + "?" + filterString
                                     + paginationString);

            UnityWebRequest webRequest = UnityWebRequest.Get(constructedURL);
            webRequest.SetRequestHeader("Authorization", "Bearer " + LocalUser.OAuthToken);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);
            webRequest.SetRequestHeader("user-agent", APIClient.USER_AGENT_HEADER);

            return webRequest;
        }

        /// <summary>Generates the object for a mod.io PUT request.</summary>
        public static UnityWebRequest GeneratePutRequest(string endpointURL,
                                                         StringValueParameter[] valueFields)
        {
            APIClient.AssertAuthorizationDetails(true);

            WWWForm form = new WWWForm();
            if(valueFields != null)
            {
                foreach(StringValueParameter valueField in valueFields)
                {
                    form.AddField(valueField.key, valueField.value);
                }
            }

            UnityWebRequest webRequest = UnityWebRequest.Post(endpointURL, form);
            webRequest.method = UnityWebRequest.kHttpVerbPUT;
            webRequest.SetRequestHeader("Authorization", "Bearer " + LocalUser.OAuthToken);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);
            webRequest.SetRequestHeader("user-agent", APIClient.USER_AGENT_HEADER);

            return webRequest;
        }

        /// <summary>Generates the object for a mod.io POST request.</summary>
        public static UnityWebRequest GeneratePostRequest(string endpointURL,
                                                          StringValueParameter[] valueFields,
                                                          BinaryDataParameter[] dataFields)
        {
            APIClient.AssertAuthorizationDetails(true);

            WWWForm form = new WWWForm();
            if(valueFields != null)
            {
                foreach(StringValueParameter valueField in valueFields)
                {
                    form.AddField(valueField.key, valueField.value);
                }
            }
            if(dataFields != null)
            {
                foreach(BinaryDataParameter dataField in dataFields)
                {
                    form.AddBinaryData(dataField.key, dataField.contents, dataField.fileName, dataField.mimeType);
                }
            }


            UnityWebRequest webRequest = UnityWebRequest.Post(endpointURL, form);
            webRequest.SetRequestHeader("Authorization", "Bearer " + LocalUser.OAuthToken);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);
            webRequest.SetRequestHeader("user-agent", APIClient.USER_AGENT_HEADER);

            return webRequest;
        }

        /// <summary>Generates the object for a mod.io DELETE request.</summary>
        public static UnityWebRequest GenerateDeleteRequest(string endpointURL,
                                                            StringValueParameter[] valueFields)
        {
            APIClient.AssertAuthorizationDetails(true);

            WWWForm form = new WWWForm();
            if(valueFields != null)
            {
                foreach(StringValueParameter valueField in valueFields)
                {
                    form.AddField(valueField.key, valueField.value);
                }
            }

            UnityWebRequest webRequest = UnityWebRequest.Post(endpointURL, form);
            webRequest.method = UnityWebRequest.kHttpVerbDELETE;
            webRequest.SetRequestHeader("Authorization", "Bearer " + LocalUser.OAuthToken);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);
            webRequest.SetRequestHeader("user-agent", APIClient.USER_AGENT_HEADER);

            return webRequest;
        }

        /// <summary>A wrapper for sending a UnityWebRequest and attaching callbacks.</summary>
        public static UnityWebRequestAsyncOperation SendRequest(UnityWebRequest webRequest,
                                                                Action<string> successCallback,
                                                                Action<WebRequestError> errorCallback)
        {
            Debug.Assert(webRequest != null);
            Debug.Assert(!string.IsNullOrEmpty(webRequest.url));

            UnityWebRequestAsyncOperation requestOperation = null;
            RequestCallbackCollection callbackCollection;

            // - prevent parallel get requests -
            if(webRequest.method == UnityWebRequest.kHttpVerbGET)
            {
                // create new request
                if(!APIClient._activeGetRequests.TryGetValue(webRequest.url, out requestOperation))
                {
                    requestOperation = webRequest.SendWebRequest();
                    requestOperation.completed += (operation) =>
                    {
                        APIClient._activeGetRequests.Remove(webRequest.url);
                    };

                    APIClient._activeGetRequests.Add(webRequest.url, requestOperation);
                }

                // fetch callback collection
                if(!APIClient._requestResponseMap.TryGetValue(requestOperation, out callbackCollection))
                {
                    callbackCollection = new RequestCallbackCollection()
                    {
                        successCallbacks = new List<Action<string>>(),
                        errorCallbacks = new List<Action<WebRequestError>>(),
                    };

                    APIClient._requestResponseMap.Add(requestOperation, callbackCollection);

                    requestOperation.completed += APIClient.ProcessResponse;
                }

                // append callbacks
                if(successCallback != null)
                {
                    callbackCollection.successCallbacks.Add(successCallback);
                }
                if(errorCallback != null)
                {
                    callbackCollection.errorCallbacks.Add(errorCallback);
                }
            }
            else
            {
                // - start new request -
                if(requestOperation == null
                   || !APIClient._requestResponseMap.TryGetValue(requestOperation, out callbackCollection))
                {
                    requestOperation = webRequest.SendWebRequest();

                    // map callbacks
                    callbackCollection = new RequestCallbackCollection()
                    {
                        successCallbacks = new List<Action<string>>(),
                        errorCallbacks = new List<Action<WebRequestError>>(),
                    };

                    APIClient._requestResponseMap.Add(requestOperation, callbackCollection);
                    requestOperation.completed += APIClient.ProcessResponse;
                }

                // append callbacks
                if(successCallback != null)
                {
                    callbackCollection.successCallbacks.Add(successCallback);
                }
                if(errorCallback != null)
                {
                    callbackCollection.errorCallbacks.Add(errorCallback);
                }
            }

            #if DEBUG
                DebugUtilities.DebugWebRequest(requestOperation, LocalUser.instance);
            #endif

            return requestOperation;
        }

        /// <summary>A wrapper for sending a web request to mod.io and parsing the result.</summary>
        public static UnityWebRequestAsyncOperation SendRequest<T>(UnityWebRequest webRequest,
                                                                   Action<T> successCallback,
                                                                   Action<WebRequestError> errorCallback)
        {
            Action<string> processResponse = (responseBody) =>
            {
                // TODO(@jackson): Don't call success on exception
                if(successCallback != null)
                {
                    T response = default(T);

                    try
                    {
                        response = JsonConvert.DeserializeObject<T>(responseBody);
                    }
                    catch(Exception e)
                    {
                        // TODO(@jackson): Error!
                        Debug.LogWarning("[mod.io] Failed to convert response into " + typeof(T).ToString() + " representation\n\n"
                                         + Utility.GenerateExceptionDebugString(e));
                    }

                    successCallback(response);
                }
            };

            return APIClient.SendRequest(webRequest, processResponse, errorCallback);
        }

        /// <summary>A wrapper for sending a web request without handling the response.</summary>
        public static UnityWebRequestAsyncOperation SendRequest(UnityWebRequest webRequest,
                                                                Action successCallback,
                                                                Action<WebRequestError> errorCallback)
        {
            return APIClient.SendRequest(webRequest,
                                         (b) => { if(successCallback != null) { successCallback.Invoke(); } },
                                         errorCallback);
        }

        /// <summary>A wrapper for processing the response for a given web request.</summary>
        private static void ProcessResponse(UnityEngine.AsyncOperation operation)
        {
            // early out
            if(operation == null)
            {
                Debug.LogWarning("[mod.io] Attempted to process response a null operation.");
                return;
            }

            // check webRequest
            UnityWebRequestAsyncOperation webRequestOperation = operation as UnityWebRequestAsyncOperation;
            if(webRequestOperation == null
               || webRequestOperation.webRequest == null)
            {
                Debug.LogWarning("[mod.io] Unable to retrieve UnityWebRequest from operation.");
                return;
            }

            // check callbackCollection
            RequestCallbackCollection callbackCollection;
            if(!APIClient._requestResponseMap.TryGetValue(webRequestOperation, out callbackCollection))
            {
                Debug.LogWarning("[mod.io] Unable to callbackCollection for the operation: " + webRequestOperation.webRequest.url);
                return;
            }

            // - process callbacks -
            string responseBody = null;
            WebRequestError error = null;
            APIClient.ProcessRequestResponse(webRequestOperation.webRequest, out responseBody, out error);

            if(error != null)
            {
                foreach(var errorCallback in callbackCollection.errorCallbacks)
                {
                    if(errorCallback != null)
                    {
                        errorCallback.Invoke(error);
                    }
                }
            }
            else
            {
                foreach(var successCallback in callbackCollection.successCallbacks)
                {
                    if(successCallback != null)
                    {
                        successCallback.Invoke(responseBody);
                    }
                }
            }

            APIClient._requestResponseMap.Remove(webRequestOperation);
        }

        /// <summary>Processes the response for the given request.</summary>
        private static void ProcessRequestResponse(UnityWebRequest webRequest,
                                                   out string success, out WebRequestError error)
        {
            success = null;
            error = null;

            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                error = WebRequestError.GenerateFromWebRequest(webRequest);
            }
            else
            {
                success = string.Empty;

                if(webRequest.downloadHandler != null
                   && !(webRequest.downloadHandler is DownloadHandlerFile))
                {
                    try
                    {
                        success = webRequest.downloadHandler.text;
                    }
                    catch
                    {
                        success = string.Empty;
                    }
                }
            }
        }


        // ---------[ AUTHENTICATION ]---------
        /// <summary>Wrapper object for [[ModIO.APIClient.GetOAuthToken]] requests.</summary>
        [System.Serializable]
        #pragma warning disable 0649
        private struct AccessTokenObject { public string access_token; }
        #pragma warning restore 0649

        /// <summary>Generates the web request for a mod.io Authentication request.</summary>
        public static UnityWebRequest GenerateAuthenticationRequest(string endpointURL,
                                                                    string authenticationKey,
                                                                    string authenticationValue)
        {
            KeyValuePair<string, string> authData
                = new KeyValuePair<string, string>(authenticationKey, authenticationValue);

            return APIClient.GenerateAuthenticationRequest(endpointURL, authData);
        }

        /// <summary>Generates the web request for a mod.io Authentication request.</summary>
        public static UnityWebRequest GenerateAuthenticationRequest(string endpointURL,
                                                                    params KeyValuePair<string, string>[] authData)
        {
            APIClient.AssertAuthorizationDetails(false);
            Debug.Assert(authData.Length > 0, "[mod.io] Authentication data was empty.");

            WWWForm form = new WWWForm();
            form.AddField("api_key", PluginSettings.GAME_API_KEY);

            foreach(var kvp in authData)
            {
                form.AddField(kvp.Key, kvp.Value);
            }

            UnityWebRequest webRequest = UnityWebRequest.Post(endpointURL, form);
            webRequest.SetRequestHeader("Accept-Language", APIClient.languageCode);
            webRequest.SetRequestHeader("user-agent", APIClient.USER_AGENT_HEADER);

            return webRequest;
        }

        /// <summary>Requests a login code be sent to an email address.</summary>
        public static void SendSecurityCode(string emailAddress,
                                            Action<APIMessage> successCallback,
                                            Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/oauth/emailrequest";

            UnityWebRequest webRequest = APIClient.GenerateAuthenticationRequest(endpointURL,
                                                                                 "email",
                                                                                 emailAddress);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Requests a user OAuthToken in exchange for a security code.</summary>
        public static void GetOAuthToken(string securityCode,
                                         Action<string> successCallback,
                                         Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/oauth/emailexchange";

            UnityWebRequest webRequest = APIClient.GenerateAuthenticationRequest(endpointURL,
                                                                                 "security_code",
                                                                                 securityCode);
            Action<AccessTokenObject> onSuccessWrapper = (result) =>
            {
                successCallback(result.access_token);
            };

            APIClient.SendRequest(webRequest, onSuccessWrapper, errorCallback);
        }

        /// <summary>Request an OAuthToken using a Steam User authentication ticket.</summary>
        public static void RequestSteamAuthentication(byte[] pTicket, uint pcbTicket,
                                                      Action<string> successCallback,
                                                      Action<WebRequestError> errorCallback)
        {
            if(pTicket == null
               || pTicket.Length == 0
               || pTicket.Length > 1024)
            {
                Debug.LogWarning("[mod.io] Steam Ticket is invalid. Ensure that the"
                                 + " pTicket is not null, and is less than 1024 bytes.");

                if(errorCallback != null)
                {
                    errorCallback(WebRequestError.GenerateLocal("Steam Ticket is invalid. Ensure"
                        + " that the pTicket is not null, and is less than 1024 bytes."));
                }
            }
            else
            {
                // create vars
                string encodedTicket = Utility.EncodeBufferAsString(pTicket, pcbTicket);

                if(string.IsNullOrEmpty(encodedTicket))
                {
                    if(errorCallback != null)
                    {
                        string message = ("Failed to convert Steam ticket"
                                          + " and so authentication cannot"
                                          + " be attempted.");
                        errorCallback(WebRequestError.GenerateLocal(message));
                    }
                }
                else
                {
                    APIClient.RequestSteamAuthentication(encodedTicket,
                                                         successCallback,
                                                         errorCallback);
                }
            }
        }

        /// <summary>Request an OAuthToken using an encoded Steam User authentication ticket.</summary>
        public static void RequestSteamAuthentication(string base64EncodedTicket,
                                                      Action<string> successCallback,
                                                      Action<WebRequestError> errorCallback)
        {
            if(string.IsNullOrEmpty(base64EncodedTicket))
            {
                Debug.LogWarning("[mod.io] Encoded Steam Ticket is invalid."
                    + " Ensure that the base64EncodedTicket is not null or empty.");

                if(errorCallback != null)
                {
                    errorCallback(WebRequestError.GenerateLocal("Encoded Steam Ticket is invalid."
                        + " Ensure that the base64EncodedTicket is not null or empty."));
                }

                return;
            }

            // create vars
            string endpointURL = PluginSettings.API_URL + @"/external/steamauth";

            UnityWebRequest webRequest = APIClient.GenerateAuthenticationRequest(endpointURL,
                                                                                 "appdata",
                                                                                 base64EncodedTicket);

            // send request
            Action<AccessTokenObject> onSuccessWrapper = (result) =>
            {
                successCallback(result.access_token);
            };

            APIClient.SendRequest(webRequest, onSuccessWrapper, errorCallback);
        }

        /// <summary>Request an OAuthToken using a GOG user authentication ticket.</summary>
        public static void RequestGOGAuthentication(byte[] data, uint dataSize,
                                                    Action<string> successCallback,
                                                    Action<WebRequestError> errorCallback)
        {
            if(data == null
               || data.Length == 0
               || data.Length > 1024)
            {
                Debug.LogWarning("[mod.io] GOG Ticket is invalid. Ensure that the"
                                 + " data is not null, and is less than 1024 bytes.");

                if(errorCallback != null)
                {
                    errorCallback(WebRequestError.GenerateLocal("GOG Ticket is invalid. Ensure"
                        + " that the data is not null, and is less than 1024 bytes."));
                }
            }
            else
            {
                // create vars
                string encodedTicket = Utility.EncodeBufferAsString(data, dataSize);

                if(string.IsNullOrEmpty(encodedTicket))
                {
                    if(errorCallback != null)
                    {
                        string message = ("Failed to convert GOG ticket"
                                          + " and so authentication cannot"
                                          + " be attempted.");
                        errorCallback(WebRequestError.GenerateLocal(message));
                    }
                }
                else
                {
                    APIClient.RequestGOGAuthentication(encodedTicket,
                                                       successCallback,
                                                       errorCallback);
                }
            }
        }

        /// <summary>Request an OAuthToken using a GOG Galaxy App ticket.</summary>
        public static void RequestGOGAuthentication(string base64EncodedTicket,
                                                    Action<string> successCallback,
                                                    Action<WebRequestError> errorCallback)
        {
            if(string.IsNullOrEmpty(base64EncodedTicket))
            {
                Debug.LogWarning("[mod.io] Encoded GOG Galaxy Ticket is invalid."
                    + " Ensure that the base64EncodedTicket is not null or empty.");

                if(errorCallback != null)
                {
                    errorCallback(WebRequestError.GenerateLocal("Encoded GOG Galaxy Ticket is invalid."
                        + " Ensure that the base64EncodedTicket is not null or empty."));
                }

                return;
            }

            // create vars
            string endpointURL = PluginSettings.API_URL + @"/external/galaxyauth";

            UnityWebRequest webRequest = APIClient.GenerateAuthenticationRequest(endpointURL,
                                                                                 "appdata",
                                                                                 base64EncodedTicket);

            // send request
            Action<AccessTokenObject> onSuccessWrapper = (result) =>
            {
                successCallback(result.access_token);
            };

            APIClient.SendRequest(webRequest, onSuccessWrapper, errorCallback);
        }

        /// <summary>Request an OAuthToken using an itch.io JWT token.</summary>
        public static void RequestItchIOAuthentication(string jwtToken,
                                                       Action<string> successCallback,
                                                       Action<WebRequestError> errorCallback)
        {
            if(string.IsNullOrEmpty(jwtToken))
            {
                Debug.LogWarning("[mod.io] itch.io JWT Token is invalid."
                    + " Ensure that the jwtToken is not null or empty.");

                if(errorCallback != null)
                {
                    errorCallback(WebRequestError.GenerateLocal("itch.io JWT Token is invalid."
                        + " Ensure that the jwtToken is not null or empty."));
                }

                return;
            }

            // create vars
            string endpointURL = PluginSettings.API_URL + @"/external/itchioauth";

            UnityWebRequest webRequest = APIClient.GenerateAuthenticationRequest(endpointURL,
                                                                                 "itchio_token",
                                                                                 jwtToken);

            // send request
            Action<AccessTokenObject> onSuccessWrapper = (result) =>
            {
                successCallback(result.access_token);
            };

            APIClient.SendRequest(webRequest, onSuccessWrapper, errorCallback);
        }

        /// <summary>Request an OAuthToken using an Oculus Rift data.</summary>
        public static void RequestOculusRiftAuthentication(string oculusUserNonce,
                                                           int oculusUserId,
                                                           string oculusUserAccessToken,
                                                           Action<string> successCallback,
                                                           Action<WebRequestError> errorCallback)
        {
            if(string.IsNullOrEmpty(oculusUserNonce))
            {
                Debug.LogWarning("[mod.io] Oculus Rift user nonce is invalid."
                    + " Ensure that the oculusUserNonce is not null or empty.");

                if(errorCallback != null)
                {
                    errorCallback(WebRequestError.GenerateLocal("Oculus Rift user nonce is invalid."
                        + " Ensure that the oculusUserNonce is not null or empty."));
                }

                return;
            }

            if(string.IsNullOrEmpty(oculusUserAccessToken))
            {
                Debug.LogWarning("[mod.io] Oculus Rift user access token is invalid."
                    + " Ensure that the oculusUserAccessToken is not null or empty.");

                if(errorCallback != null)
                {
                    errorCallback(WebRequestError.GenerateLocal("Oculus Rift user access token is invalid."
                        + " Ensure that the oculusUserAccessToken is not null or empty."));
                }

                return;
            }

            // create vars
            string endpointURL = PluginSettings.API_URL + @"/external/oculusauth";
            KeyValuePair<string, string>[] authData = new KeyValuePair<string, string>[3]
            {
                new KeyValuePair<string, string>("nonce",       oculusUserNonce),
                new KeyValuePair<string, string>("user_id",     oculusUserId.ToString()),
                new KeyValuePair<string, string>("access_token",oculusUserAccessToken),
            };

            UnityWebRequest webRequest = APIClient.GenerateAuthenticationRequest(endpointURL, authData);

            // send request
            Action<AccessTokenObject> onSuccessWrapper = (result) =>
            {
                successCallback(result.access_token);
            };

            APIClient.SendRequest(webRequest, onSuccessWrapper, errorCallback);
        }

        /// <summary>Requests an OAuthToken using an Xbox signed token.</summary>
        public static void RequestXboxLiveAuthentication(string xboxLiveUserToken,
                                                         Action<string> successCallback,
                                                         Action<WebRequestError> errorCallback)
        {
            if(string.IsNullOrEmpty(xboxLiveUserToken))
            {
                Debug.LogWarning("[mod.io] Xbox Live token is invalid."
                    + " Ensure that the xboxLiveUserToken is not null or empty.");

                if(errorCallback != null)
                {
                    errorCallback(WebRequestError.GenerateLocal("Xbox Live token is invalid."
                        + " Ensure that the xboxLiveUserToken is not null or empty."));
                }
            }

            // create vars
            string endpointURL = PluginSettings.API_URL + @"/external/xboxauth";

            UnityWebRequest webRequest = APIClient.GenerateAuthenticationRequest(endpointURL,
                                                                                 "xbox_token",
                                                                                 xboxLiveUserToken);

            // send request
            Action<AccessTokenObject> onSuccessWrapper = (result) =>
            {
                successCallback(result.access_token);
            };

            APIClient.SendRequest(webRequest, onSuccessWrapper, errorCallback);
        }

        // ---------[ GAME ENDPOINTS ]---------
        /// <summary>Fetches all the game profiles from the mod.io servers.</summary>
        public static void GetAllGames(RequestFilter filter, APIPaginationParameters pagination,
                                       Action<RequestPage<GameProfile>> successCallback,
                                       Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 filter.GenerateFilterString(),
                                                                 pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the game's/app's profile from the mod.io servers.</summary>
        public static void GetGame(Action<GameProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 "",
                                                                 null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Updates the game's profile on the mod.io servers.</summary>
        public static void EditGame(EditGameParameters parameters,
                                    Action<GameProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                      parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MOD ENDPOINTS ]---------
        /// <summary>Fetches all mod profiles from the mod.io servers.</summary>
        public static void GetAllMods(RequestFilter filter, APIPaginationParameters pagination,
                                      Action<RequestPage<ModProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches a mod profile from the mod.io servers.</summary>
        public static void GetMod(int modId,
                                  Action<ModProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 "",
                                                                 null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits a new mod profile to the mod.io servers.</summary>
        public static void AddMod(AddModParameters parameters,
                                  Action<ModProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                       parameters.stringValues.ToArray(),
                                                                       parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits changes to an existing mod profile.</summary>
        public static void EditMod(int modId,
                                   EditModParameters parameters,
                                   Action<ModProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                      parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Deletes a mod profile from the mod.io servers.</summary>
        public static void DeleteMod(int modId,
                                     Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MODFILE ENDPOINTS ]---------
        /// <summary>Fetches all modfiles for a given mod from the mod.io servers.</summary>
        public static void GetAllModfiles(int modId,
                                          RequestFilter filter, APIPaginationParameters pagination,
                                          Action<RequestPage<Modfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/files";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetch the a modfile from the mod.io servers.</summary>
        public static void GetModfile(int modId, int modfileId,
                                      Action<Modfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/files/" + modfileId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              "",
                                                              null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits a new modfile and binary to the mod.io servers.</summary>
        public static void AddModfile(int modId,
                                      AddModfileParameters parameters,
                                      Action<Modfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/files";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits changes to an existing modfile.</summary>
        public static void EditModfile(int modId, int modfileId,
                                       EditModfileParameters parameters,
                                       Action<Modfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/files/" + modfileId;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                      parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ MEDIA ENDPOINTS ]---------
        /// <summary>Submit new game media to the mod.io servers.</summary>
        public static void AddGameMedia(AddGameMediaParameters parameters,
                                        Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/media";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits new mod media to the mod.io servers.</summary>
        public static void AddModMedia(int modId,
                                       AddModMediaParameters parameters,
                                       Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/media";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                       parameters.stringValues.ToArray(),
                                                                       parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Deletes mod media from a mod on the mod.io servers.</summary>
        public static void DeleteModMedia(int modId,
                                          DeleteModMediaParameters parameters,
                                          Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/media";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ SUBSCRIBE ENDPOINTS ]---------
        /// <summary>Subscribes the authenticated user to a mod.</summary>
        public static void SubscribeToMod(int modId,
                                          Action<ModProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/subscribe";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    null,
                                                                    null);


            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Unsubscribes the authenticated user from a mod.</summary>
        public static void UnsubscribeFromMod(int modId,
                                              Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/subscribe";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ EVENT ENDPOINTS ]---------
        /// <summary>Fetches the update events for a given mod.</summary>
        public static void GetModEvents(int modId,
                                        RequestFilter filter, APIPaginationParameters pagination,
                                        Action<RequestPage<ModEvent>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/events";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 filter.GenerateFilterString(),
                                                                 pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches all the mod update events for the game profile</summary>
        public static void GetAllModEvents(RequestFilter filter, APIPaginationParameters pagination,
                                           Action<RequestPage<ModEvent>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/events";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ STATS ENDPOINTS ]---------
        /// <summary>Fetches the statistics for all mods.</summary>
        public static void GetAllModStats(RequestFilter filter, APIPaginationParameters pagination,
                                          Action<RequestPage<ModStatistics>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/stats";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 filter.GenerateFilterString(),
                                                                 pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the statics for a mod.</summary>
        public static void GetModStats(int modId,
                                       Action<ModStatistics> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/stats";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL, "", null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        // ---------[ TAG ENDPOINTS ]---------
        /// <summary>Fetches the tag categories specified by the game profile.</summary>
        public static void GetGameTagOptions(Action<RequestPage<ModTagCategory>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/tags";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 "",
                                                                 null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits new mod tag categories to the mod.io servers.</summary>
        public static void AddGameTagOption(AddGameTagOptionParameters parameters,
                                            Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/tags";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Removes mod tag options from the mod.io servers.</summary>
        public static void DeleteGameTagOption(DeleteGameTagOptionParameters parameters,
                                               Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/tags";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the tags applied to the given mod.</summary>
        public static void GetModTags(int modId,
                                      RequestFilter filter, APIPaginationParameters pagination,
                                      Action<RequestPage<ModTag>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/tags";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits new mod tags to the mod.io servers.</summary>
        public static void AddModTags(int modId, AddModTagsParameters parameters,
                                      Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/tags";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Removes tags from the given mod.</param>
        public static void DeleteModTags(int modId,
                                         DeleteModTagsParameters parameters,
                                         Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/tags";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ RATING ENDPOINTS ]---------
        /// <summary>Submits a user's rating for a mod.</summary>
        public static void AddModRating(int modId, AddModRatingParameters parameters,
                                        Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/ratings";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ METADATA ENDPOINTS ]---------
        /// <summary>Fetches all the KVP metadata for a mod.</summary>
        public static void GetAllModKVPMetadata(int modId,
                                                APIPaginationParameters pagination,
                                                Action<RequestPage<MetadataKVP>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/metadatakvp";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              "",
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submit KVP Metadata to a mod.</summary>
        public static void AddModKVPMetadata(int modId, AddModKVPMetadataParameters parameters,
                                             Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/metadatakvp";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Deletes KVP metadata from a mod.</summary>
        public static void DeleteModKVPMetadata(int modId, DeleteModKVPMetadataParameters parameters,
                                                Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/metadatakvp";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ DEPENDENCIES ENDPOINTS ]---------
        /// <summary>Fetches all the dependencies for a mod.</summary>
        public static void GetAllModDependencies(int modId,
                                                 RequestFilter filter, APIPaginationParameters pagination,
                                                 Action<RequestPage<ModDependency>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/dependencies";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits new dependencides for a mod.</summary>
        public static void AddModDependencies(int modId, AddModDependenciesParameters parameters,
                                              Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/dependencies";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Removes dependencides from a mod.</summary>
        public static void DeleteModDependencies(int modId, DeleteModDependenciesParameters parameters,
                                                 Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/dependencies";

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ TEAM ENDPOINTS ]---------
        /// <summary>Fetches the team members for a mod.</summary>
        public static void GetAllModTeamMembers(int modId,
                                                RequestFilter filter, APIPaginationParameters pagination,
                                                Action<RequestPage<ModTeamMember>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/team";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 filter.GenerateFilterString(),
                                                                 pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits a new team member to a mod.</summary>
        public static void AddModTeamMember(int modId, AddModTeamMemberParameters parameters,
                                            Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/team";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                       parameters.stringValues.ToArray(),
                                                                       parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits changes to a mod team member.</summary>
        public static void UpdateModTeamMember(int modId, int teamMemberId,
                                               UpdateModTeamMemberParameters parameters,
                                               Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/team/" + teamMemberId;

            UnityWebRequest webRequest = APIClient.GeneratePutRequest(endpointURL,
                                                                   parameters.stringValues.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits a delete request for a mod team member.</summary>
        public static void DeleteModTeamMember(int modId, int teamMemberId,
                                               Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/team/" + teamMemberId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ COMMENT ENDPOINTS ]---------
        /// <summary>Fetches all the comments for a mod.</summary>
        public static void GetAllModComments(int modId,
                                             RequestFilter filter, APIPaginationParameters pagination,
                                             Action<RequestPage<ModComment>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/comments";

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                              filter.GenerateFilterString(),
                                                              pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches a mod comment by id.</summary>
        public static void GetModComment(int modId, int commentId,
                                         Action<ModComment> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/comments/" + commentId;

            UnityWebRequest webRequest = APIClient.GenerateQuery(endpointURL,
                                                                 "",
                                                                 null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Submits a delete request for a mod comment.</summary>
        public static void DeleteModComment(int modId, int commentId,
                                            Action successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/games/" + PluginSettings.GAME_ID + @"/mods/" + modId + @"/comments/" + commentId;

            UnityWebRequest webRequest = APIClient.GenerateDeleteRequest(endpointURL,
                                                                         null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ USER ENDPOINTS ]---------
        /// <summary>Fetches the owner for a mod resource.</summary>
        public static void GetResourceOwner(APIResourceType resourceType, int resourceID,
                                            Action<UserProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/general/owner";
            StringValueParameter[] valueFields = new StringValueParameter[]
            {
                StringValueParameter.Create("resource_type", resourceType.ToString().ToLower()),
                StringValueParameter.Create("resource_id", resourceID),
            };

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    valueFields,
                                                                    null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        // ---------[ REPORT ENDPOINTS ]---------
        /// <summary>Submits a report against a mod/resource on mod.io.</summary>
        public static void SubmitReport(SubmitReportParameters parameters,
                                        Action<APIMessage> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/report";

            UnityWebRequest webRequest = APIClient.GeneratePostRequest(endpointURL,
                                                                    parameters.stringValues.ToArray(),
                                                                    parameters.binaryData.ToArray());

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }


        // ---------[ ME ENDPOINTS ]---------
        /// <summary>Fetches the user profile for the authenticated user.</summary>
        public static void GetAuthenticatedUser(Action<UserProfile> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/me";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL, "", null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the subscriptions for the authenticated user.</summary>
        public static void GetUserSubscriptions(RequestFilter filter, APIPaginationParameters pagination,
                                                Action<RequestPage<ModProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/me/subscribed";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetch the update events for the authenticated user.</summary>
        public static void GetUserEvents(RequestFilter filter, APIPaginationParameters pagination,
                                         Action<RequestPage<UserEvent>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/me/events";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the games that the authenticated user is a team member of.</summary>
        public static void GetUserGames(Action<RequestPage<GameProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/me/games";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL, "", null);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the mods that the authenticated user is a team member of.</summary>
        public static void GetUserMods(RequestFilter filter, APIPaginationParameters pagination,
                                       Action<RequestPage<ModProfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/me/mods";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches the modfiles that the authenticated user uploaded.</summary>
        public static void GetUserModfiles(RequestFilter filter, APIPaginationParameters pagination,
                                           Action<RequestPage<Modfile>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/me/files";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }

        /// <summary>Fetches _all_ the ratings submitted by the authenticated user.</summary>
        public static void GetUserRatings(RequestFilter filter, APIPaginationParameters pagination,
                                          Action<RequestPage<ModRating>> successCallback, Action<WebRequestError> errorCallback)
        {
            string endpointURL = PluginSettings.API_URL + @"/me/ratings";

            UnityWebRequest webRequest = APIClient.GenerateGetRequest(endpointURL,
                                                                      filter.GenerateFilterString(),
                                                                      pagination);

            APIClient.SendRequest(webRequest, successCallback, errorCallback);
        }
    }
}
