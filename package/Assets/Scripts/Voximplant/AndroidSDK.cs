using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Assertions;

namespace Voximplant
{
    internal sealed class AndroidSDK : VoximplantSDK
    {
        private AndroidJavaClass jc;
        private AndroidJavaObject jo;

        public override void init(Action<bool> initCallback, bool preferH264)
        {
            Action finishInit = () => {
                initCallback(true);
            };

#if UNITY_ANDROID || UNITY_EDITOR
            Action initJava = () => {
                try {
                    jc = new AndroidJavaClass("com.voximplant.sdk.AVoImClient");
                }
                catch (UnityException e) {
                    Debug.unityLogger.Log("JC Error: " + e.Message);
                    initCallback(false);
                    return;
                }

                try {
                    jo = jc.CallStatic<AndroidJavaObject>("instance", preferH264);
                }
                catch (AndroidJavaException e) {
                    Debug.unityLogger.Log("JO Error: " + e.Message);
                    initCallback(false);
                    return;
                }
                jo.Call("setSDKObjectName", gameObject.name);

                finishInit();
            };


            if (!PermissionsRequester.RequirePermissionRequests()) {
                initJava(); // No permissions support at all
                return;
            }

            var requester = PermissionsRequester.Instance;

            string[] permissions = {
                "android.permission.RECORD_AUDIO",
                "android.permission.MODIFY_AUDIO_SETTINGS",
                "android.permission.INTERNET",
                "android.permission.CAMERA"
            };

            bool requestPermissions = false;
            foreach (var permission in permissions) {
                if (!requester.IsPermissionGranted(permission)) {
                    requestPermissions = true;
                    break;
                }
            }

            if (requestPermissions) {
                requester.RequestPermissions(permissions, statuses => {
                    foreach (var status in statuses) {
                        if (!status.Granted) {
                            initCallback(false);
                            return;
                        }
                    }

                    initJava();
                });
            } else {
                initJava();
            }
#endif
        }

        #region Java helpers

        private static AndroidJavaObject buildMap(Dictionary<string, string> dictionary)
        {
            if (dictionary == null) {
                return null;
            }

            var jmap = new AndroidJavaObject("java.util.HashMap");
            foreach (KeyValuePair<string, string> kvp in dictionary) {
                using (AndroidJavaObject k = new AndroidJavaObject("java.lang.String", kvp.Key)) {
                    using (AndroidJavaObject v = new AndroidJavaObject("java.lang.String", kvp.Value)) {
                        jmap.Call("put", k, v);
                    }
                }
            }
            return jmap;
        }

        #endregion

        public override void closeConnection()
        {
            jo.Call("closeConnection");
        }

        public override void connect()
        {
            jo.Call("connect");
        }


        public override void login(string username, string password)
        {
            jo.Call("login", JsonUtility.ToJson(new LoginClassParam(username, password)));
        }

        public override string createCall(string number, bool videoCall, string customData)
        {
            return jo.Call<string>("createCall", JsonUtility.ToJson(new CallClassParam(number, videoCall, customData)));
        }

        public override void startCall(string callId, Dictionary<string, string> headers = null)
        {
            jo.Call("startCall", callId, buildMap(headers));
        }

        public override void answer(string callId, Dictionary<string, string> headers = null)
        {
            jo.Call("answer", JsonUtility.ToJson(new AnswerClassParam(callId, headers)));
        }

        public override void declineCall(string callId, Dictionary<string, string> headers = null)
        {
            jo.Call("declineCall", JsonUtility.ToJson(new DeclineCallClassParam(callId, headers)));
        }

        public override void hangup(string callId, Dictionary<string, string> headers = null)
        {
            jo.Call("hangup", JsonUtility.ToJson(new HangupClassParam(callId, headers)));
        }

        public override void setMute(Boolean pState)
        {
            jo.Call("setMute", JsonUtility.ToJson(new BoolClassParam(pState)));
        }

        public override void sendVideo(Boolean pState)
        {
            jo.Call("sendVideo", JsonUtility.ToJson(new BoolClassParam(pState)));
        }

        public override void setCamera(Camera cameraPosition)
        {
            jo.Call("setCamera", cameraPosition == Camera.CAMERA_FACING_FRONT ? "1" : "0");
        }

        public override void disableTls()
        {
            jo.Call("disableTls");
        }

        public override void disconnectCall(string callId, Dictionary<string, string> headers = null)
        {
            jo.Call("disconnectCall", JsonUtility.ToJson(new DisconnectCallClassParam(callId, headers)));
        }

        public override void enableDebugLogging()
        {
            jo.Call("enableDebugLogging");
        }

        public override void loginUsingOneTimeKey(string login, string hash)
        {
            LoginOneTimeKeyClassParam param = new LoginOneTimeKeyClassParam(login, hash);
            jo.Call("loginUsingOneTimeKey", JsonUtility.ToJson(param));
        }

        public override void requestOneTimeKey(string pName)
        {
            jo.Call("requestOneTimeKey", JsonUtility.ToJson(new StringClassParam(pName)));
        }

        public override void sendDTMF(string callId, string digit)
        {
            jo.Call("sendDTMF", JsonUtility.ToJson(new DTFMClassParam(callId, digit)));
        }

        public override void sendInfo(string callId, string mimeType, string content, Dictionary<string, string> headers = null)
        {
            jo.Call("sendInfo", JsonUtility.ToJson(new InfoClassParam(callId, mimeType, content, headers)));
        }

        public override void sendMessage(string callId, string message, Dictionary<string, string> headers = null)
        {
            jo.Call("sendMessage", JsonUtility.ToJson(new SendMessageClassParam(callId, message, headers)));
        }

        public override void setCameraResolution(int width, int height)
        {
            jo.Call("setCameraResolution", JsonUtility.ToJson(new CameraResolutionClassParam(width, height)));
        }

        public override void setUseLoudspeaker(bool pUseLoudSpeaker)
        {
            jo.Call("setUseLoudspeaker", JsonUtility.ToJson(new BoolClassParam(pUseLoudSpeaker)));
        }

        #region Texture Rendering

        protected override void startVideoStreamRendering(string callId, VideoStream stream)
        {
            Assert.IsTrue(GraphicsDeviceIsSupported());

            jo.Call("beginSendingVideoForStream", callId, (int)stream);
        }

        #endregion

        #region Video Source

        protected override void beginCallVideoStream(string pCallId, uint width, uint height)
        {
            jo.Call("registerCallVideoStream", pCallId, (int)width, (int)height);
        }

        protected override void callVideoStreamTextureUpdated(string pCallId, IntPtr pTexturePtr, int width, int height)
        {
            jo.Call("callVideoStreamTextureUpdated", pCallId, pTexturePtr.ToInt32());
        }

        protected override void endCallVideoStream(string pCallId)
        {
            jo.Call("unregisterCallVideoStream", pCallId);
        }

        #endregion

        #region Native Callbacks

        protected void faonLoginSuccessful(string p)
        {
            AddLog("faonLoginSuccessful: " + p);
            JSONNode node = Utils.GetParamList(p);
            OnLoginSuccessful(node[0].Value);
        }

        protected void faonLoginFailed(string p)
        {
            AddLog("faonLoginFailed: " + p);
            JSONNode node = Utils.GetParamList(p);

            OnLoginFailed(LoginFailureReasonFromString(node[0].Value));
        }

        protected void faonOneTimeKeyGenerated(string p)
        {
            AddLog("faonOneTimeKeyGenerated: " + p);
            OnOneTimeKeyGenerated(p);
        }

        protected void faonConnectionSuccessful()
        {
            AddLog("faonConnectionSuccessful");
            OnConnectionSuccessful();
        }

        protected void faonConnectionClosed()
        {
            AddLog("faonConnectionClosed");
            CleanupAllVideoStreams();
            OnConnectionClosed();
        }

        protected void faonConnectionFailedWithError(string p)
        {
            AddLog("faonConnectionFailedWithError" + p);
            OnConnectionFailedWithError(p);
        }

        protected void faonCallConnected(string p)
        {
            AddLog("faonCallConnected: " + p);
            JSONNode node = Utils.GetParamList(p);
            OnCallConnected(node[0].Value, node[1].AsDictionary);
        }

        protected void faonCallDisconnected(string p)
        {
            AddLog("faonCallDisconnected: " + p);
            CleanupAllVideoStreams();
            JSONNode node = Utils.GetParamList(p);
            OnCallDisconnected(node[0].Value, node[1].AsDictionary);
        }

        protected void faonCallRinging(string p)
        {
            AddLog("faonCallRinging: " + p);
            JSONNode node = Utils.GetParamList(p);
            OnCallRinging(node[0].Value, node[1].AsDictionary);
        }

        protected void faonCallFailed(string p)
        {
            AddLog("faonCallFailed: " + p);
            CleanupAllVideoStreams();
            JSONNode node = Utils.GetParamList(p);
            OnCallFailed(node[0].Value, node[1].AsInt, node[2].Value, node[3].AsDictionary);
        }

        protected void faonCallAudioStarted(string p)
        {
            AddLog("faonCallAudioStarted" + p);
            OnCallAudioStarted(p);
        }

        protected void faonIncomingCall(string p)
        {
            AddLog("faonIncomingCall: " + p);
            JSONNode node = Utils.GetParamList(p);
            OnIncomingCall(node[0].Value, node[1].Value, node[2].Value, node[3].AsBool, node[4].AsDictionary);
        }

        protected void faonSIPInfoReceivedInCall(string p)
        {
            AddLog("faonSIPInfoReceivedInCall");
            JSONNode node = Utils.GetParamList(p);
            OnSIPInfoReceivedInCall(node[0].Value, node[1].Value, node[2].Value, node[3].AsDictionary);
        }

        protected void faonMessageReceivedInCall(string p)
        {
            AddLog("faonMessageReceivedInCall");
            JSONNode node = Utils.GetParamList(p);
            OnMessageReceivedInCall(node[0].Value, node[1].Value, null);
        }

        protected void faonNetStatsReceived(string p)
        {
            AddLog("faonNetStatsReceived");
            JSONNode node = Utils.GetParamList(p);
            OnNetStatsReceived(node[0], node[1].AsInt);
        }

        protected void faonOnStartCall(string callId)
        {
            AddLog("faonOnStartCall: " + callId);
            OnStartCall(callId);
        }

        #endregion
    }
}
