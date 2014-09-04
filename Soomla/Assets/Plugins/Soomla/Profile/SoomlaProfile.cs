/// Copyright (C) 2012-2014 Soomla Inc.
///
/// Licensed under the Apache License, Version 2.0 (the "License");
/// you may not use this file except in compliance with the License.
/// You may obtain a copy of the License at
///
///      http://www.apache.org/licenses/LICENSE-2.0
///
/// Unless required by applicable law or agreed to in writing, software
/// distributed under the License is distributed on an "AS IS" BASIS,
/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and
/// limitations under the License.using System;

using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Soomla.Profile
{
	/// <summary>
	/// This is the main class controlling the whole SOOMLA Profile module.
	/// Use this class to perform various social and authentication operations on users.
	/// The Profile module will work with the social and authentication plugins you provide and
	/// define in AndroidManifest.xml or your iOS project's plist.
	/// </summary>
	public class SoomlaProfile
	{
		static SoomlaProfile _instance = null;
		static SoomlaProfile instance {
			get {
				if(_instance == null) {
					#if UNITY_ANDROID && !UNITY_EDITOR
					_instance = new SoomlaProfileAndroid();
					#elif UNITY_IOS && !UNITY_EDITOR
					_instance = new SoomlaProfileIOS();
					#else
					_instance = new SoomlaProfile();
					#endif
				}
				return _instance;
			}
		}


		static Dictionary<Provider, SocialProvider> providers = new Dictionary<Provider, SocialProvider>();

		/// <summary>
		/// Initializes the SOOMLA Profile Module.
		/// 
		/// NOTE: must be called before any of its methods can be used
		/// </summary>
		public static void Initialize() {
			instance._initialize();
			providers.Add(Provider.FACEBOOK, new FBSocialProvider());
			ProfileEvents.OnSoomlaProfileInitialized();
		}

		/// <summary>
		/// Checks if the user is logged into the supplied provider
		/// </summary>
		/// <returns><c>true</c> if is logged in the specified provider; otherwise, <c>false</c>.</returns>
		/// <param name="provider">The provider on which to check if the user is logged in.</param>
		public static bool IsLoggedIn(Provider provider) {
			return providers.ContainsKey(provider) && providers[provider].IsLoggedIn();
		}

		/// <summary>
		/// Updates the user's status on the provided provider. Upon a successful
		/// update, the user will receive the supplied reward.
		///
		/// This operation requires a successful login.
		/// </summary>
		/// <param name="provider">The <c>Provider</c> the given status should be posted to.</param>
		/// <param name="status">The actual status text.</param>
		/// <param name="reward">A <c>Reward</c> to give to the user after a successful posting.</param>
		public static void UpdateStatus(Provider provider, string status, string payload="", Reward reward = null) {
		
			ProfileEvents.OnSocialActionStarted(provider, SocialActionType.UPDATE_STATUS, payload);
			providers[provider].UpdateStatus(status,
	        /* success */	() => {
						ProfileEvents.OnSocialActionFinished(provider, SocialActionType.UPDATE_STATUS, payload); 
						if (reward != null) {
							reward.Give();
						}
					},
			/* fail */		(string error) => {  ProfileEvents.OnSocialActionFailed (provider, SocialActionType.UPDATE_STATUS, error, payload); }
				);

		}

		/// <summary>
		/// Will post a full story to the user's social page.
		/// A story contains: title, description, image and more.
		///
		/// This operation requires a successful login.
		/// </summary>
		/// <param name="provider">The <c>Provider</c> the given story should be posted to.</param>
		/// <param name="message">A message that will be shown along with the story.</param>
		/// <param name="name">The name (title) of the story.</param>
		/// <param name="caption">A caption.</param>
		/// <param name="description">The actual story text.</param>
		/// <param name="link">A link to a web page.</param>
		/// <param name="pictureUrl">A link to an image on the web.</param>
		/// <param name="reward">A <c>Reward</c> to give to the user after a successful posting.</param>
		public static void UpdateStory(Provider provider, string message, string name,
		                               string caption, string link,
		                               string pictureUrl, string payload="", Reward reward = null) {

			ProfileEvents.OnSocialActionStarted(provider, SocialActionType.UPDATE_STORY, payload);
			providers[provider].UpdateStory(message, name, caption, link, pictureUrl,
	        /* success */	() => { 
									ProfileEvents.OnSocialActionFinished(provider, SocialActionType.UPDATE_STORY, payload); 
									if (reward != null) {
										reward.Give();
									}
								},
			/* fail */		(string error) => {  ProfileEvents.OnSocialActionFailed (provider, SocialActionType.UPDATE_STORY, error, payload); },
			/* cancel */	() => {  ProfileEvents.OnSocialActionCancelled(provider, SocialActionType.UPDATE_STORY, payload); }
				);

		}

//		public static void UploadImage(Provider provider, string message, string filename,
//		                               byte[] imageBytes, int quality, Reward reward) {
//			instance._uploadImage(provider, message, filename, imageBytes, quality, reward);
//		}
//
		public static void UploadImage(Provider provider, Texture2D tex2D, string fileName, string message, string payload="",
		                               Reward reward = null) {

			ProfileEvents.OnSocialActionStarted(provider, SocialActionType.UPLOAD_IMAGE, payload);
			providers[provider].UploadImage(tex2D, fileName, message,
			/* success */	() => { 
									ProfileEvents.OnSocialActionFinished(provider, SocialActionType.UPLOAD_IMAGE, payload); 
									if (reward != null) {
										reward.Give();
									}
								},
			/* fail */		(string error) => {  ProfileEvents.OnSocialActionFailed (provider, SocialActionType.UPLOAD_IMAGE, error, payload); },
			/* cancel */	() => {  ProfileEvents.OnSocialActionCancelled(provider, SocialActionType.UPLOAD_IMAGE, payload); }
				);

		}

		public static void UploadCurrentScreenShot(MonoBehaviour mb, Provider provider, string title, string message, string payload="", Reward reward = null) {
			mb.StartCoroutine(TakeScreenshot(provider, title, message, payload, reward));
		}

		/// <summary>
		/// Will fetch UserProfiles of contacts of the current user.
		///
		/// This operation requires a successful login.
		/// </summary>
		/// <param name="provider">The <c>Provider</c> we should try to fetch contacts from.</param>
		/// <param name="reward">A <c>Reward</c> to give to the user after a successful fetching.</param>
		public static void GetContacts(Provider provider, string payload="") {

			ProfileEvents.OnGetContactsStarted(provider, payload);
			providers[provider].GetContacts(
			/* success */	(List<UserProfile> profiles) => { 
										ProfileEvents.OnGetContactsFinished(provider, profiles, payload);
									},
			/* fail */		(string message) => {  ProfileEvents.OnGetContactsFailed(provider, message, payload); }
			);
		}

		// TODO: this is irrelevant for now. Will be updated soon.
//		public static void AddAppRequest(Provider provider, string message, string[] to, string extraData, string dialogTitle) {
//			providers[provider].AppRequest(message, to, extraData, dialogTitle,
//			    /* success */	(string requestId, List<string> recipients) => {
//									string requestsStr = KeyValueStorage.GetValue("soomla.profile.apprequests");
//									List<string> requests = new List<string>();
//									if (!string.IsNullOrEmpty(requestsStr)) {
//										requests = requestsStr.Split(',').ToList();
//									}
//									requests.Add(requestId);
//									KeyValueStorage.SetValue("soomla.profile.apprequests", string.Join(",", requests.ToArray()));
//									KeyValueStorage.SetValue(requestId, string.Join(",", recipients.ToArray()));
//									ProfileEvents.OnAddAppRequestFinished(provider, requestId);
//								},
//				/* fail */		(string errMsg) => {
//									ProfileEvents.OnAddAppRequestFailed(provider, errMsg);
//								});
//		}


		/// <summary>
		///  Will fetch posts from user feed
		///
		/// </summary>
		/// <param name="provider">Provider.</param>
		/// <param name="reward">Reward.</param>
//		public static void GetFeed(Provider provider, Reward reward) {
//
//			// TODO: implement with FB SDK
//
//		}

		/// <summary>
		/// Will log you out from the given provider.
		/// </summary>
		/// <param name="provider">The provider to log out from.</param>
		public static void Logout(Provider provider) {

			ProfileEvents.OnLogoutStarted(provider);
			providers[provider].Logout(
				/* success */	() => { ProfileEvents.OnLogoutFinished(provider); },
				/* fail */		(string message) => {  ProfileEvents.OnLogoutFailed (provider, message); }
			);

		}

		/// <summary>
		/// Will log you in to the given provider.
		/// </summary>
		/// <param name="provider">The provider to log in to.</param>
		/// <param name="reward">Give your users a reward for logging in.</param>
		public static void Login(Provider provider, string payload="", Reward reward = null) {
			ProfileEvents.OnLoginStarted(provider, payload);
			providers[provider].Login(
			/* success */	(UserProfile userProfile) => { 
									StoreUserProfile(userProfile);
									ProfileEvents.OnLoginFinished(userProfile, payload); 
									if (reward != null) {
										reward.Give();
									}
								},
			/* fail */		(string message) => {  ProfileEvents.OnLoginFailed (provider, message, payload); },
			/* cancel */	() => {  ProfileEvents.OnLoginCancelled(provider, payload); }
			);
		}

		/// <summary>
		/// Like the specified pageName in the given provider.
		/// </summary>
		/// <param name="provider">The provider the page exists in.</param>
		/// <param name="pageName">The name of the page as written in facebook in the URL. 
		/// For a url http://www.facebook.com/MyPage you need to provide pageName="MyPage".</param>
		/// <param name="reward">Give your users a reward for his like.</param>
		public static void Like(Provider provider, string pageName, Reward reward=null) {
			providers[provider].Like(pageName);

			if (reward != null) {
				reward.Give();
			}
		}


		/// <summary>
		/// This will fetch the UserProfile that is saved for the given provider.
		/// UserProfiles are automatically saved in the local storage for a provider after a successful login.
		/// </summary>
		/// <returns>The stored user profile.</returns>
		/// <param name="provider">The provider you need to fetch UserProfile for.</param>
		public static UserProfile GetStoredUserProfile(Provider provider) {
			return instance._getStoredUserProfile(provider);
		}

		/// <summary>
		/// Stores the user profile.
		/// The UserProfile should contain the provider internally
		/// </summary>
		/// <param name="userProfile">User profile.</param>
		/// <param name="notify">If set to <c>true</c> notify.</param>
		public static void StoreUserProfile (UserProfile userProfile, bool notify = false) {
			instance._storeUserProfile (userProfile, notify);
		}

		/// <summary>
		/// Opens the app rating page.
		/// </summary>
		public static void OpenAppRatingPage() {
			instance._openAppRatingPage ();
		}


		protected virtual void _initialize() { }

		protected virtual void _openAppRatingPage() { }




		protected virtual UserProfile _getStoredUserProfile(Provider provider) {
#if UNITY_EDITOR
			string key = keyUserProfile(provider);
			string value = PlayerPrefs.GetString (key);
			if (!string.IsNullOrEmpty(value)) {
				return new UserProfile (new JSONObject (value));
			}
#endif
			return null;
		}

		protected virtual void _storeUserProfile(UserProfile userProfile, bool notify) {
#if UNITY_EDITOR
			string key = keyUserProfile(userProfile.Provider);
			string val = userProfile.toJSONObject().ToString();
			SoomlaUtils.LogDebug(TAG, "key/val:" + key + "/" + val);
			PlayerPrefs.SetString(key, val);

			if (notify) {
				ProfileEvents.OnUserProfileUpdated(userProfile);
			}
#endif
		}

		private static IEnumerator TakeScreenshot(Provider provider, string title, string message, string payload, Reward reward)
		{
			yield return new WaitForEndOfFrame();
			
			var width = Screen.width;
			var height = Screen.height;
			var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
			// Read screen contents into the texture
			tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			tex.Apply();
			
			UploadImage(provider, tex, title, message, payload, reward);
		}

		/** keys when running in editor **/
#if UNITY_EDITOR
		private const string DB_KEY_PREFIX = "soomla.profile.";

		private static string keyUserProfile(Provider provider) {
			return DB_KEY_PREFIX + "userprofile." + provider.ToString();
		}
#endif

		/// <summary> Class Members </summary>

		protected const string TAG = "SOOMLA SoomlaProfile";
	}
}
