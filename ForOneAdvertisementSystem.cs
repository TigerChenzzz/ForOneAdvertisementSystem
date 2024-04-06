using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.UI;
using Terraria.UI.Chat;
using static ForOneAdvertisementSystem.MiscHelper;
using ModPack = System.Collections.Generic.KeyValuePair<string, (Terraria.ModLoader.Mod Mod, System.Collections.Generic.Dictionary<string, object> ExtraValue)>;
using ModsDict = System.Collections.Generic.Dictionary<string, (Terraria.ModLoader.Mod Mod, System.Collections.Generic.Dictionary<string, object> ExtraValue)>;

namespace ForOneAdvertisementSystem;
/*
public class ForOneAdvertisementSystem : Mod {
    public static string GetLocalizedAdvertisement() => ForOneAdvertisementSystemT.GetLocalizedAdvertisement();
    public static bool Finished => ForOneAdvertisementSystemT.Finished;
    public override void Load() {
        ForOneAdvertisementSystemT.Load(this);
    }
}
*/

public class ForOneAdvertisementSystem {
    #region Load �Լ�����
    static bool FirstLoad;
    static object? theAttribute;
    static ModsDict? Mods {
        get {
            if(theAttribute == null) {
                return null;
            }
            return (ModsDict?)theAttribute.GetType().GetField("Mods")?.GetValue(theAttribute);
        }
        set {
            if(theAttribute == null) {
                return;
            }
            var modsField = theAttribute.GetType().GetField("Mods");
            if(modsField == null) {
                return;
            }
            modsField.SetValue(theAttribute, value);
        }
    }
    internal static class ExtraDataKeys {
        public const string DisplayName = "DisplayName";
        public const string LocalizedDisplayName = "LocalizedDisplayName";
        public const string DisplayNameGetter = "DisplayNameGetter";
    }
    static string GetName(ModPack modPack) {
        if(modPack.Value.ExtraValue.ContainsKey(ExtraDataKeys.DisplayNameGetter) && modPack.Value.ExtraValue[ExtraDataKeys.DisplayNameGetter] is Delegate dele && dele.TryCastDelegate<Func<string>>(out var getter) && getter != null) {
            return getter();
        }
        if(modPack.Value.ExtraValue.ContainsKey(ExtraDataKeys.LocalizedDisplayName) && modPack.Value.ExtraValue[ExtraDataKeys.LocalizedDisplayName] is LocalizedText localizedText) {
            return localizedText.Value;
        }
        if(modPack.Value.ExtraValue.ContainsKey(ExtraDataKeys.DisplayName) && modPack.Value.ExtraValue[ExtraDataKeys.DisplayName] is string displayName) {
            return displayName;
        }
        return modPack.Value.Mod.DisplayName;
    }
    static string GetModsName() {
        if(ModsCached == null || ModsCached.Count == 0) {
            return string.Empty;
        }
        if(ModsCached.Count <= 3) {
            return string.Join(", ", ModsCached.Select(GetName));
        }
        return string.Join(", ", ModsCached.RandomTake(3).Select(GetName)) + "...";
    }
    static bool _modsCachedGot;
    static ModsDict? _modsCached;
    internal static ModsDict? ModsCached {
        get {
            if(!_modsCachedGot) {
                _modsCachedGot = true;
                _modsCached = Mods;
            }
            return _modsCached;
        }
    }
    /// <summary>
    ///  ����������ƹ�ϵͳ
    /// </summary>
    /// <param name="mod">�ƹ�Դ</param>
    public static void Load(Mod mod) {
        LoadInner(mod);
    }
    /// <summary>
    ///  ����������ƹ�ϵͳ
    /// </summary>
    /// <param name="mod">�ƹ�Դ</param>
    /// <param name="displayName">�ƹ�Դ����ʾ����, Ĭ��Ϊ<see cref="Mod.DisplayName"/></param>
    public static void Load(Mod mod, string displayName) {
        LoadInner(mod, displayName);
    }
    /// <summary>
    ///  ����������ƹ�ϵͳ
    /// </summary>
    /// <param name="mod">�ƹ�Դ</param>
    /// <param name="localizedDisplayName">�ƹ�Դ����ʾ����, ���ػ��汾</param>
    public static void Load(Mod mod, LocalizedText? localizedDisplayName) {
        LoadInner(mod, localizedDisplayName: localizedDisplayName);
    }
    /// <summary>
    ///  ����������ƹ�ϵͳ
    /// </summary>
    /// <param name="mod">�ƹ�Դ</param>
    /// <param name="displayNameGetter">�ƹ�Դ����ʾ����, ί�а汾(ÿ�ν�������ʱ����һ��)</param>
    public static void Load(Mod mod, Func<string> displayNameGetter) {
        LoadInner(mod, displayNameGetter: displayNameGetter);
    }
    /// <summary>
    ///  ����������ƹ�ϵͳ
    /// </summary>
    /// <param name="mod">�ƹ�Դ</param>
    /// <param name="displayName">�ƹ�Դ����ʾ����, ��������(<see langword="null"/>), Ĭ��Ϊ<see cref="Mod.DisplayName"/></param>
    static void LoadInner(Mod mod, string? displayName = null, LocalizedText? localizedDisplayName = null, Func<string>? displayNameGetter = null) {
        AttributeCollection attributes = TypeDescriptor.GetAttributes(typeof(Main));
        foreach(var attr in attributes) {
            if(attr.GetType().Name == nameof(ForOneAdvertisementModsAttribute)) {
                theAttribute = attr;
                break;
            }
        }
        if(theAttribute == null) {
            FirstLoad = true;
            theAttribute = new ForOneAdvertisementModsAttribute();
            TypeDescriptor.AddAttributes(typeof(Main), (ForOneAdvertisementModsAttribute)theAttribute);
        }
        var mods = Mods;
        if(mods == null) {
            // ������ͻ, ֱ���˳�
            return;
        }
        // �����д�mod, ֱ���˳�
        if(mods.ContainsKey(mod.Name)) {
            return;
        }
        Dictionary<string, object> extraData = [];
        if(displayName != null && displayName != string.Empty) {
            extraData.Add(ExtraDataKeys.DisplayName, displayName);
        }
        if(localizedDisplayName != null) {
            extraData.Add(ExtraDataKeys.LocalizedDisplayName, localizedDisplayName);
        }
        if(displayNameGetter != null) {
            extraData.Add(ExtraDataKeys.DisplayNameGetter, displayNameGetter);
        }
        mods.Add(mod.Name, (mod, extraData));
        if(!FirstLoad) {
            return;
        }
        // ��������ִֻ��һ��
        Hook();
        savedContents = LoadData();
        LoadStart();
    }
    #endregion

    #region ����
    static readonly List<Hook> hooks = [];
    static void Hook() {
        hooks.Add(new(typeof(ModContent).GetMethod("UnloadModContent", BindingFlags.Static | BindingFlags.NonPublic)!, On_ModContentUnloadModConetent));
        hooks.Add(new(typeof(PlayerLoader).GetMethod(nameof(PlayerLoader.OnEnterWorld), BindingFlags.Static | BindingFlags.Public)!, On_PlayerLoaderOnEnterWorld));
    }
    static void Unhook() {
        foreach(var hook in hooks) {
            hook.Undo();
            hook.Dispose();
        }
        hooks.Clear();
    }
    static void On_ModContentUnloadModConetent(Action orig) {
        orig();
        UnloadThis();
    }
    static void On_PlayerLoaderOnEnterWorld(Action<int> orig, int playerIndex) {
        orig(playerIndex);
        InGameNotificationsTracker.AddNotification(new AdvertisementNotification());
    }
    #endregion

    #region AdvertisementSystem

    #region ����
    static readonly string pingHostName = "tml-advertisement-space.notion.site";
    static readonly string mainUri = "https://tml-advertisement-space.notion.site/api/v3/loadCachedPageChunk";
    static readonly string subUri = "https://tml-advertisement-space.notion.site/api/v3/loadCachedPageChunks";
    static readonly string notionPageId = "06eafd8d-6f9e-4c4f-8674-d4d01557d95b";
    #endregion

    /// <summary>
    /// ��ȡ�ƹ�����
    /// ʹ��ǰ���ȼ��<see cref="Finished"/>Ϊ��ſ���ʹ��
    /// </summary>
    public static string GetLocalizedAdvertisement() {
        if(ModsCached == null || ModsCached.Count == 0) {
            return string.Empty;
        }
        string result = GetLocalizedAdvertisementInner();
        if(result == string.Empty) {
            return result;
        }
        return result.FormatWith(GetModsName());
    }
    static string GetLocalizedAdvertisementInner() {
        if(!Finished) {
            throw new("ʹ�� GetLocalizedAdvertisement ǰ���ȼ�� Finished !");
        }
        string key = Language.ActiveCulture.Name;
        string defaultKey = GameCulture.DefaultCulture.Name;
        string defaultDefaultKey = "en-US";
        var localizations = contents?.RandomTake(p => ModsCached!.ContainsKey(p.Key) ? 0f : p.Value.Weight).Value?.Localizations;
        if(localizations == null) {
            return string.Empty;
        }
        if(Succeeded && localizations != null) {
            if(localizations.ContainsKey(key)) {
                return localizations[key];
            }
            else if(localizations.ContainsKey(defaultKey)) {
                return localizations[defaultKey];
            }
            else if(localizations.ContainsKey(defaultDefaultKey)) {
                return localizations[defaultDefaultKey];
            }
        }
        var savedLocalizations = savedContents?.RandomTake(p => ModsCached!.ContainsKey(p.Key) ? 0f : p.Value.Weight).Value?.Localizations;
        if(savedLocalizations != null) {
            if(savedLocalizations.ContainsKey(key)) {
                return savedLocalizations[key];
            }
            else if(savedLocalizations.ContainsKey(defaultKey)) {
                return savedLocalizations[defaultKey];
            }
            else if(savedLocalizations.ContainsKey(defaultDefaultKey)) {
                return savedLocalizations[defaultDefaultKey];
            }
        }
        return string.Empty;
    }

    class Content {
        public float Weight = 1f;
        public Dictionary<string, string> Localizations = [];
    }
    /// <summary>
    /// ʹ��ǰ���ȼ��<see cref="Succeeded"/>
    /// </summary>
    static Dictionary<string, Content>? contents;
    static Dictionary<string, Content>? savedContents;
    static Task<LoadResult>? loadTask;
    static readonly CancellationTokenSource cancellationTokenSource = new();
    internal static bool Succeeded => loadTask != null && loadTask.IsCompletedSuccessfully && loadTask.Result == LoadResult.Success;
    internal static bool Failed => loadTask != null && loadTask.IsCompleted && !Succeeded;
    internal static bool Finished => loadTask != null && loadTask.IsCompleted;
    /// <summary>
    /// ��ȫʹ��
    /// ���<see cref="loadTask"/>Ϊ����ʼ����
    /// </summary>
    internal static void LoadStart() {
        if(loadTask != null) {
            return;
        }
        loadTask = Task.Run(() => LoadAsync(notionPageId), cancellationTokenSource.Token);
        loadTask.ContinueWith(task => {
            if(task.IsCompletedSuccessfully && contents != null) {
                SaveData(contents);
            }
        }, cancellationTokenSource.Token);
    }
    static void UnloadThis() {
        cancellationTokenSource.Cancel();
        loadTask?.Dispose();
        loadTask = null;
    }
    #region ���ݴ洢
    /// <summary>
    /// ���ݱ����·��
    /// �����Ӳ��ɹ�ʱ���ñ��������
    /// </summary>
    internal static string DataPath {
        get {
            Directory.CreateDirectory(ConfigManager.ModConfigPath);
            return Path.Combine(ConfigManager.ModConfigPath, "ForOneAdvertisementContent.json");
        }
    }
    /// <summary>
    /// ��<paramref name="data"/>�����Ӧ·�����ļ���
    /// </summary>
    /// <param name="data"></param>
    static void SaveData(Dictionary<string, Content>? data) {
        File.WriteAllText(DataPath, JsonConvert.SerializeObject(data));
    }
    /// <summary>
    /// �����Ӧ·�����ļ�, ������س���������
    /// ���򷵻ؿ�
    /// ���س���Ҳ�᷵�ؿ�
    /// </summary>
    static Dictionary<string, Content>? LoadData() {
        string path = DataPath;
        if(!File.Exists(path)) {
            return null;
        }
        try {
            Dictionary<string, Content>? data = [];
            JsonConvert.PopulateObject(File.ReadAllText(path), data, ConfigManager.serializerSettings);
            return data;
        }
        catch(Exception e) when(e is JsonReaderException or JsonSerializationException) {
            // ModInstance.Logger.Warn("AdvertisementSystem: advertisement file failed to load");
            File.Delete(path);
        }
        return null;
    }
    #endregion

    internal enum LoadResult {
        /// <summary>
        /// Ĭ��ֵ, ռλ��, ��ʱ�����ڷ���ֵ
        /// </summary>
        None,
        /// <summary>
        /// �ɹ�
        /// </summary>
        Success,
        /// <summary>
        /// �����������ȷ
        /// </summary>
        WrongParameters,
        /// <summary>
        /// ��Ӧ���ǳɹ���
        /// </summary>
        NotSuccessStatusCode,
        /// <summary>
        /// ��Ӧ�ĸ�ʽ����ȷ
        /// </summary>
        WrongFormat,
        /// <summary>
        /// json�����������
        /// </summary>
        ErrorInDeserialize,
        /// <summary>
        /// contentId�ڵڶ���������û�ҵ�
        /// </summary>
        ErrorContentId,
        /// <summary>
        /// ����֮��Ĵ���
        /// </summary>
        UnexpectedError,
    }
    private static async Task<LoadResult> LoadAsync(string notionId, int mainLimit = 30, int subLimit = 25, int lowLimit = 25) {
        if(mainLimit <= 0 || subLimit <= 0 || notionId == null || notionId.Length == 0) {
            return LoadResult.WrongParameters;
        }

        #region ��Pingһ�²���
        //HttpClient? httpClient = null;
        try {
            PingReply pingReply = new Ping().Send(pingHostName);
            if(pingReply.Status != IPStatus.Success) {
                return LoadResult.NotSuccessStatusCode;
            }
        }
        catch {
            return LoadResult.NotSuccessStatusCode;
        }
        #endregion

        contents = [];
        try {
            using HttpClient httpClient = new();
            #region ��һ��Post: ��ȡ��ҳ����(���������еı��ػ���ֵ)
            #region ��������
            using HttpContent mainRequest = new StringContent($$"""
            {
                "page": {
                    "id": "{{notionId}}"
                },
                "limit": {{mainLimit}},
                "cursor": {
                    "stack": []
                },
                "chunkNumber": 0,
                "verticalColumns": false
            }
            """);
            mainRequest.Headers.ContentType = new("application/json");
            #endregion
            #region ����Post����, �����Ӧ������
            using HttpResponseMessage mainResponse = await httpClient.PostAsync(mainUri, mainRequest);
            if(!mainResponse.IsSuccessStatusCode) {
                return LoadResult.NotSuccessStatusCode;
            }
            string mainResponseBody = await mainResponse.Content.ReadAsStringAsync();
            #endregion
            #region ����json
            if(JsonConvert.DeserializeObject(mainResponseBody) is not JObject mainJson) {
                return LoadResult.ErrorInDeserialize;
            }
            #endregion
            #region ������Ӧ����
            JToken? mainBlocks = mainJson["recordMap"]?["block"];
            if(mainBlocks == null) {
                return LoadResult.WrongFormat;
            }
            Dictionary<string, string[]> modContents = [];
            List<string> requests = [];
            foreach(JToken block in mainBlocks) {
                if(block is not JProperty blockProperty) {
                    continue;
                }
                #region ����ֲ�����
                string? modBlockId = null, spaceId = null;
                string? modName;
                #endregion
                JToken? value = blockProperty.Value["value"];
                #region �����κ�type����toggle�Ŀ�
                if(value?["type"]?.ToString() != "toggle") {
                    continue;
                }
                #endregion
                #region ��ȡ��toggle����ı�ֵ, ��Ϊģ����
                var modToken = value["properties"]?["title"]?[0]?[0];
                if(modToken is not JValue modNameValue) {
                    continue;//��ֱ�ӷ���, �п��������ȷʵ��������
                }
                modName = modNameValue.ToString();
                #region ��������ظ�ֵ�ļ�ֵ, ֱ������, ������
                if(modContents.ContainsKey(modName)) {
                    continue;
                }
                #endregion
                #endregion
                #region ��ȡ��toggle���blockId, spaceId��contents, ����contents�����ֵ��Ա����ʹ��
                modBlockId = blockProperty.Name;
                bool errorInContentFormat = false;
                string[]? blockContents = value["content"]?.Select(t => {
                    if(t is not JValue v) {
                        errorInContentFormat = true;
                        return "";
                    }
                    return v.ToString();
                }).ToArray();
                if (blockContents == null || errorInContentFormat) {
                    continue;
                }
                if(value["space_id"] is not JValue spaceIdValue) {
                    return LoadResult.WrongFormat;
                }
                spaceId = spaceIdValue.ToString();
                //û�����ݴ��������toggle�鵫toggle����û����, ����Ϊ��û����������, ֱ������
                if(blockContents.Length == 0) {
                    continue;
                }
                modContents[modName] = blockContents;
                #endregion
                #region ����һ�����������Ԫ�ط��� requests ��
                requests.Add($$"""
                    {
                        "page": {
                            "id": "{{modBlockId}}",
                            "spaceId": "{{spaceId}}"
                        },
                        "limit": {{subLimit}},
                        "chunkNumber": 0,
                        "cursor": {
                            "stack": []
                        },
                        "verticalColumns": false
                    }
                    """);
                #endregion
            }
            #endregion
            #endregion
            #region �ڶ���Post: ���ÿ��Mod�µı��ػ���
            #region ��������
            using HttpContent subRequest = new StringContent($$"""
                {
                    "requests": [
                        {{string.Join(',', requests)}}
                    ]
                }
                """);
            subRequest.Headers.ContentType = new("application/json");
            #endregion
            #region ����Post����, �����Ӧ������
            using HttpResponseMessage subResponse = await httpClient.PostAsync(subUri, subRequest);
            if(!subResponse.IsSuccessStatusCode) {
                return LoadResult.NotSuccessStatusCode;
            }
            string subResponseBody = await subResponse.Content.ReadAsStringAsync();
            #endregion
            #region ����json
            if(JsonConvert.DeserializeObject(subResponseBody) is not JObject subJson) {
                return LoadResult.ErrorInDeserialize;
            }
            #endregion
            #region ������Ӧ����
            requests.Clear();
            JToken? subBlocks = subJson["recordMap"]?["block"];
            if(subBlocks == null) {
                return LoadResult.WrongFormat;
            }
            Dictionary<string, Dictionary<string, string[]>> modLocalizationContents = [];
            foreach(string modName in modContents.Keys) {
                modLocalizationContents.Add(modName, []);
                Content content = new();
                string[] modBlockContents = modContents[modName];
                //��ʱ blockContents ��װ���� ĳЩ�������ݺͱ��ػ��� ��Ӧ�� blockId
                foreach(int i in modBlockContents.Length) {
                    string blockId = modBlockContents[i];
                    JToken? block = subBlocks[blockId];
                    if(block == null) {
                        return LoadResult.ErrorContentId;
                    }
                    /*
                    var contentToken = block["value"]?["properties"];
                    if(contentToken is not JArray contentArray) {
                        return LoadResult.WrongFormat;//ȷ����������Ҫ�ҵĿ鵫��û�ж�Ӧ��, ��϶��Ǹ�ʽ����
                    }
                    contents[i] = string.Join(null, contentArray.Select(t => {
                        return t is not JArray a ? null :
                                a.Count == 0 ? null :
                                a[0] is not JValue v ? null :
                                v.ToString();
                    }).Where(s => s != null));
                    */
                    var contentToken = block["value"]?["properties"]?["title"]?[0]?[0];
                    if(contentToken is not JValue contentValue) {
                        continue;
                    }
                    string str = contentValue.ToString();
                    if(str.StartsWith("weight:") && float.TryParse(str["weight:".Length..].Trim(), out var weight)) {
                        content.Weight = weight;
                        continue;
                    }
                    #region �����κ�type����toggle�Ŀ�
                    if(block["value"]?["type"]?.ToString() != "toggle") {
                        continue;
                    }
                    #endregion
                    if(modLocalizationContents[modName].ContainsKey(str)) {
                        continue;
                    }
                    bool errorInContentFormat = false;
                    string[]? blockContents = block["value"]?["content"]?.Select(t => {
                        if(t is not JValue v) {
                            errorInContentFormat = true;
                            return "";
                        }
                        return v.ToString();
                    }).ToArray();
                    if (blockContents == null || errorInContentFormat) {
                        continue;
                    }
                    if(block["value"]?["space_id"] is not JValue spaceIdValue) {
                        return LoadResult.WrongFormat;
                    }
                    //û�����ݴ��������toggle�鵫toggle����û����, ����Ϊ��û����������, ֱ������
                    if(blockContents.Length == 0) {
                        continue;
                    }
                    modLocalizationContents[modName].Add(str, blockContents);
                    var spaceId = spaceIdValue.ToString();
                    #region ����һ�����������Ԫ�ط��� requests ��
                    requests.Add($$"""
                        {
                            "page": {
                                "id": "{{blockId}}",
                                "spaceId": "{{spaceId}}"
                            },
                            "limit": {{lowLimit}},
                            "chunkNumber": 0,
                            "cursor": {
                                "stack": []
                            },
                            "verticalColumns": false
                        }
                        """);
                    #endregion
                }
                contents.Add(modName, content);
            }
            #endregion
            #endregion
            #region ������Post: ���ÿ�����ػ����µ�����
            #region ��������
            using HttpContent lowRequest = new StringContent($$"""
                {
                    "requests": [
                        {{string.Join(',', requests)}}
                    ]
                }
                """);
            lowRequest.Headers.ContentType = new("application/json");
            #endregion
            #region ����Post����, �����Ӧ������
            using HttpResponseMessage lowResponse = await httpClient.PostAsync(subUri, lowRequest);
            if(!lowResponse.IsSuccessStatusCode) {
                return LoadResult.NotSuccessStatusCode;
            }
            string lowResponseBody = await lowResponse.Content.ReadAsStringAsync();
            #endregion
            #region ����json
            if(JsonConvert.DeserializeObject(lowResponseBody) is not JObject lowJson) {
                return LoadResult.ErrorInDeserialize;
            }
            #endregion
            #region ������Ӧ����
            JToken? lowBlocks = lowJson["recordMap"]?["block"];
            if(lowBlocks == null) {
                return LoadResult.WrongFormat;
            }
            foreach(string modName in modLocalizationContents.Keys) {
                foreach(string localization in modLocalizationContents[modName].Keys) {
                    string[] localizationBlockContents = modLocalizationContents[modName][localization];
                    // ��ʱ localizationBlockContents ��װ�������ݶ�Ӧ�� blockId, ���ڽ�����ת��Ϊ��Ӧ��ʵ������
                    foreach(int blockIndex in localizationBlockContents.Length) {
                        string blockId = localizationBlockContents[blockIndex];
                        JToken? block = lowBlocks[blockId];
                        if(block == null) {
                            return LoadResult.ErrorContentId;
                        }
                        var contentToken = block["value"]?["properties"]?["title"];
                        if(contentToken is not JArray contentArray) {
                            return LoadResult.WrongFormat;//ȷ����������Ҫ�ҵĿ鵫��û�ж�Ӧ��, ��϶��Ǹ�ʽ����
                        }
                        localizationBlockContents[blockIndex] = string.Join(null, contentArray.Select(t =>
                            t is not JArray a || a.Count == 0 || a[0] is not JValue v ? null :
                            v.ToString()
                        ).Where(s => s != null));
                    }
                    contents[modName].Localizations.Add(localization, string.Join('\n', localizationBlockContents));

                }
                if(contents[modName].Localizations.Count == 0) {
                    contents.Remove(modName);
                }
            }
            #endregion
            #endregion
            return LoadResult.Success;
        }
        catch(Exception e) {
            if(e is SocketException or HttpRequestException) {
                // ModInstance.Logger.Warn("Advertisement LoadAsync: Web request exception: " + e.Message);
                return LoadResult.NotSuccessStatusCode;
            }
            // ModInstance.Logger.Warn("Advertisement LoadAsync: Unexpected exception: " + e.Message);
            throw;
            //return LoadResult.UnexpectedError;
        }
    }
    #endregion
}

#region Notification
internal class AdvertisementNotification : IInGameNotification {
    public bool ShouldBeRemoved => timeLeft <= 0;

    public const int KeepTimeInSecond = 10 * 2;
    public const int MaxTimeLeft = KeepTimeInSecond * 60;
    /// <summary>
    /// ����С�䵽�����ߴ����䵽��С��ʱ��
    /// </summary>
    public const int ScaleTime = 18;
    /// <summary>
    /// �ӽ������絽��ʼ��ʾ����С�ӳ�
    /// </summary>
    public const int StartDelay = 60;
    [Range(0f, 1f)] //!ע�ⲻ��Config�е��Ǹ�Range
    public const float DisappearScale = 0.4f;
    public int timeLeft = MaxTimeLeft;
    public int startDelayNow = StartDelay;
    public float borderX = 20f;
    public float borderY = 8f;

    readonly DynamicSpriteFont font = FontAssets.ItemStack.Value;    //����
    string? _text;
    TextSnippet[]? snippets;
    /// <summary>
    /// �������ŵ����ִ�С
    /// </summary>
    Vector2 textSize;
    bool _textSet;
    private void SetText() {
        if(_textSet) {
            return;
        }
        _textSet = true;
        var value = ForOneAdvertisementSystem.GetLocalizedAdvertisement();
        if(value == null || value == string.Empty) {
            return;
        }
        if(_text == value)
            return;
        _text = value;
        if(value == null) {
            snippets = null;
        }
        else if(value == "") {
            snippets = null;
            timeLeft = 0;
        }
        else {
            snippets = [.. ChatManager.ParseMessage(value, Color.White)];
        }
    }

    public float BaseScale => (timeLeft < ScaleTime) ? Lerp(0f, 1f, timeLeft / (float)ScaleTime, false, LerpType.CubicByK, 3f, -0.8f) :
        (timeLeft > MaxTimeLeft - ScaleTime) ? Lerp(0f, 1f, (MaxTimeLeft - timeLeft) / (float)ScaleTime) :
        1f;
    public const float EffectiveScale = 1.2f;
    public float Scale => BaseScale * EffectiveScale;

    float Opacity => DisappearScale == 1f ? (Scale == 1f ? 1 : 0) :
        Scale < DisappearScale ? 0f :
        (Scale - DisappearScale) / (1f - DisappearScale);

    public void Update() {
        if(startDelayNow > 0) {
            startDelayNow -= 1;
            return;
        }
        if(!ForOneAdvertisementSystem.Finished) {
            return;
        }
        timeLeft -= 1;
        timeLeft = Math.Max(timeLeft, 0);
    }

    public void DrawInGame(SpriteBatch spriteBatch, Vector2 bottomAnchorPosition) {
        if(!ForOneAdvertisementSystem.Finished || ShouldBeRemoved) {
            return;
        }
        float opacity = Opacity;
        float maxWidth = Main.screenWidth * 0.7f;
        if(opacity <= 0f) {
            return;
        }
        Vector2 scale = new(Scale); //����
        SetText();  //����
        if(snippets == null) {
            return;
        }
        textSize = ChatManager.GetStringSize(font, snippets, scale, maxWidth);
        //���ķ���
        Rectangle panelRect = NewRectangle(bottomAnchorPosition, textSize + new Vector2(borderX, borderY) * 2 * scale, new Vector2(.5f, 1f));
        //�������Ƿ��ڰ����, ���������һЩ����
        bool hovering = panelRect.Contains(Main.MouseScreen.ToPoint());
        OnMouseOver(ref hovering);
        //�������, �������ڰ��������ɫ��һ��
        Utils.DrawInvBG(spriteBatch, panelRect, new Color(64, 109, 164) * (hovering ? 0.75f : 0.5f) * opacity);//UI����ɫ
        Color color = Color.LightCyan;
        color.A = Main.mouseTextColor;
        color *= opacity;
        if(snippets!.Length > 0 && snippets[0].Color != color) {
            foreach(var snippet in snippets) {
                snippet.Color = color;
            }
        }
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, snippets,
            position: panelRect.TopLeft() + new Vector2(borderX, borderY + 4) * scale,
            rotation: 0f,
            color: Color.Black * opacity,// new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor / 5, Main.mouseTextColor) * opacity,
            shadowColor: Color.Black * opacity,
            origin: Vector2.Zero,
            baseScale: scale, out _,
            maxWidth: maxWidth);
    }

    private void OnMouseOver(ref bool hovering) {
        if(!hovering) {
            return;
        }
        // This method is called when the user hovers over the notification.

        // Skip if we're ignoring mouse input.
        if(PlayerInput.IgnoreMouseInterface || Main.LocalPlayer.mouseInterface) {
            hovering = false;
            return;
        }

        // We are now interacting with a UI.
        Main.LocalPlayer.mouseInterface = true;

        if(!Main.mouseLeft || !Main.mouseLeftRelease) {
            return;
        }

        Main.mouseLeftRelease = false;

        // In our example, we just accelerate the exiting process on click.
        // If you want it to close immediately, you can just set timeLeft to 0.
        // This allows the notification time to shrink and fade away, as expected.
        timeLeft = Math.Min(timeLeft, ScaleTime);
    }

    public void PushAnchor(ref Vector2 positionAnchorBottom) {
        // Anchoring is used for determining how much space a popup takes up, essentially.
        // This is because notifications visually stack. In our case, we want to let other notifications
        // go in front of ours once we start fading off, so we scale the offset based on opacity.
        positionAnchorBottom.Y -= textSize.Y + borderY * 2 * Scale;
    }
}
#endregion

[AttributeUsage(AttributeTargets.Class)]
internal class ForOneAdvertisementModsAttribute : Attribute {
    public ModsDict Mods = [];
}

internal static class MiscHelper {
    public static IEnumerator<int> GetEnumerator(this int self) {
        for(int i = 0; i < self; ++i) {
            yield return i;
        }
    }
    public static IEnumerable<T> RandomTake<T>(this IEnumerable<T> self, int count) {
        T[] array = [..self];
        int length = array.Length;
        foreach(var i in Math.Min(length, count)) {
            int rand = i + Main.rand.Next(length - i);
            yield return array[rand];
            array[rand] = array[i];
        }
    }
    public static T RandomTake<T>(this IEnumerable<T> self) {
        T[] array = [..self];
        int length = array.Length;
        return array[Main.rand.Next(length)];
    }
    public static T? RandomTake<T>(this IEnumerable<T> self, Func<T, float> getWeight) {
        T[] array = [..self];
        float[] weights = [..self.Select(getWeight)];
        float total = weights.Sum();
        float rand = Main.rand.NextFloat(total);
        foreach(int i in array.Length) {
            rand -= weights[i];
            if(rand < 0) {
                return array[i];
            }
        }
        return default;
    }
    #region NewRectangle
    public static Rectangle NewRectangle(Vector2 position, Vector2 size, Vector2 anchor = default)
        => NewRectangle(position.X, position.Y, size.X, size.Y, anchor.X, anchor.Y);
    public static Rectangle NewRectangle(int x, int y, int width, int height, float anchorX, float anchorY)
        => new((int)(x - anchorX * width), (int)(y - anchorY * height), width, height);
    public static Rectangle NewRectangle(float x, float y, float width, float height, float anchorX, float anchorY)
        => new((int)(x - anchorX * width), (int)(y - anchorY * height), (int)width, (int)height);
    #endregion
    #region Lerp
    public enum LerpType {
        Linear,
        Quadratic,
        Cubic,
        CubicByK,
        Sin,
        Stay,
    }
    public static System.Numerics.Matrix4x4 NewMatrix(Vector4 v1, Vector4 v2, Vector4 v3, Vector4 v4) {
        return new(v1.X, v1.Y, v1.Z, v1.W,
                    v2.X, v2.Y, v2.Z, v2.W,
                    v3.X, v3.Y, v3.Z, v3.W,
                    v4.X, v4.Y, v4.Z, v4.W);
    }
    public static float NewLerpValue(float val, bool clamped, LerpType type, params float[] pars) {
        #region �߽���
        if(clamped) {
            if(val <= 0) {
                return 0;
            }
            if(val >= 1) {
                return 1;
            }
        }
        if(val == 0) {
            return 0;
        }
        if(val == 1) {
            return 1;
        }
        #endregion
        switch(type) {
        case LerpType.Linear:
            return val;
        case LerpType.Quadratic:
            //pars[0]:���κ����ļ���
            if(pars.Length <= 0) {
                throw new TargetParameterCountException("pars not enough");
            }
            if(pars[0] == 0.5f) {
                return 0;
            }
            return val * (val - 2 * pars[0]) / (1 - 2 * pars[0]);
        case LerpType.Cubic:
            //pars[0], pars[1]:���κ�������������
            if(pars.Length <= 1) {
                throw new TargetParameterCountException("pars not enough");
            }
            return ((val - 3 * (pars[0] + pars[1]) / 2) * val + 3 * pars[0] * pars[1]) * val /
                (1 - 3 * (pars[0] + pars[1]) / 2 + 3 * pars[0] * pars[1]);
        case LerpType.CubicByK:
            //pars[0], pars[1]:������б��
            //par[2], par[3](������):���Ⱥ͸߶�
            if(pars.Length < 2) {
                throw new TargetParameterCountException("pars not enough");
            }
            float par2 = pars.Length < 3 ? 1 : pars[2], par3 = pars.Length < 4 ? 1 : pars[3];
            if(par2 == 0) {
                return 0;
            }
            Vector4 va = new(0, par2 * par2 * par2, 0, 3 * par2 * par2);
            Vector4 vb = new(0, par2 * par2, 0, 2 * par2);
            Vector4 vc = new(0, par2, 1, 1);
            Vector4 vd = new(1, 1, 0, 0);
            Vector4 v0 = new(0, par3, pars[0], pars[1]);
            var d0 = NewMatrix(va, vb, vc, vd);
            var da = NewMatrix(v0, vb, vc, vd);
            var db = NewMatrix(va, v0, vc, vd);
            var dc = NewMatrix(va, vb, v0, vd);
            var dd = NewMatrix(va, vb, vc, v0);
            if(d0.GetDeterminant() == 0) {
                return 0;
            }
            if(par3 == 0) {
                return (((da.GetDeterminant() * val + db.GetDeterminant()) * val + dc.GetDeterminant()) * val + dd.GetDeterminant()) / d0.GetDeterminant();
            }
            return (((da.GetDeterminant() * val + db.GetDeterminant()) * val + dc.GetDeterminant()) * val + dd.GetDeterminant()) / d0.GetDeterminant() / par3;
        case LerpType.Sin:
            //pars[0], pars[1] : ����λ���ķ�֮һ������
            if(pars.Length < 2) {
                throw new TargetParameterCountException("pars not enough");
            }
            float x1 = (float)(Math.PI / 2 * pars[0]), x2 = (float)(Math.PI / 2 * pars[1]), x = Lerp(x1, x2, val);
            float y1 = (float)Math.Sin(x1), y2 = (float)Math.Sin(x2), y = (float)Math.Sin(x);
            if((pars[0] - pars[1]) % 4 == 0 || (pars[0] + pars[1]) % 4 == 2) {
                return y - y1;
            }
            return (y - y1) / (y2 - y1);
        case LerpType.Stay:
            return val > 1 ? 1 : 0;
        }
        return val;
    }
    public static Vector2 NewVector2(double x, double y) => new((float)x, (float)y);
    public static Vector3 NewVector3(double x, double y, double z) => new((float)x, (float)y, (float)z);
    public static Vector4 NewVector4(double x, double y, double z, double w) => new((float)x, (float)y, (float)z, (float)w);
    public static double NewLerpValue(double val, bool clamped, LerpType type, params double[] pars) {

        #region �߽���
        if(clamped) {
            if(val <= 0) {
                return 0;
            }
            if(val >= 1) {
                return 1;
            }
        }
        if(val == 0) {
            return 0;
        }
        if(val == 1) {
            return 1;
        }
        #endregion
        switch(type) {
        case LerpType.Linear:
            return val;
        case LerpType.Quadratic:
            //pars[0]:���κ����ļ���
            if(pars.Length <= 0) {
                throw new TargetParameterCountException("pars not enough");
            }
            if(pars[0] == 0.5f) {
                return 0;
            }
            return val * (val - 2 * pars[0]) / (1 - 2 * pars[0]);
        case LerpType.Cubic:
            //pars[0], pars[1]:���κ�������������
            if(pars.Length <= 1) {
                throw new TargetParameterCountException("pars not enough");
            }
            return ((val - 3 * (pars[0] + pars[1]) / 2) * val + 3 * pars[0] * pars[1]) * val /
                (1 - 3 * (pars[0] + pars[1]) / 2 + 3 * pars[0] * pars[1]);
        case LerpType.CubicByK:
            //pars[0], pars[1]:������б��
            //par[2], par[3](������):���Ⱥ͸߶�
            if(pars.Length < 2) {
                throw new TargetParameterCountException("pars not enough");
            }
            double par2 = pars.Length < 3 ? 1 : pars[2], par3 = pars.Length < 4 ? 1 : pars[3];
            if(par2 == 0) {
                return 0;
            }
            Vector4 va = NewVector4(0, par2 * par2 * par2, 0, 3 * par2 * par2);
            Vector4 vb = NewVector4(0, par2 * par2, 0, 2 * par2);
            Vector4 vc = NewVector4(0, par2, 1, 1);
            Vector4 vd = NewVector4(1, 1, 0, 0);
            Vector4 v0 = NewVector4(0, par3, pars[0], pars[1]);
            var d0 = NewMatrix(va, vb, vc, vd);
            var da = NewMatrix(v0, vb, vc, vd);
            var db = NewMatrix(va, v0, vc, vd);
            var dc = NewMatrix(va, vb, v0, vd);
            var dd = NewMatrix(va, vb, vc, v0);
            if(d0.GetDeterminant() == 0) {
                return 0;
            }
            if(par3 == 0) {
                return (((da.GetDeterminant() * val + db.GetDeterminant()) * val + dc.GetDeterminant()) * val + dd.GetDeterminant()) / d0.GetDeterminant();
            }
            return (((da.GetDeterminant() * val + db.GetDeterminant()) * val + dc.GetDeterminant()) * val + dd.GetDeterminant()) / d0.GetDeterminant() / par3;
        case LerpType.Sin:
            //pars[0], pars[1] : ����λ���ķ�֮һ������
            if(pars.Length < 2) {
                throw new TargetParameterCountException("pars not enough");
            }
            double x1 = (Math.PI / 2 * pars[0]), x2 = (Math.PI / 2 * pars[1]), x = Lerp(x1, x2, val);
            double y1 = Math.Sin(x1), y2 = Math.Sin(x2), y = Math.Sin(x);
            if((pars[0] - pars[1]) % 4 == 0 || (pars[0] + pars[1]) % 4 == 2) {
                return y - y1;
            }
            return (y - y1) / (y2 - y1);
        case LerpType.Stay:
            return val > 1 ? 1 : 0;
        }
        return val;
    }
    public static float Lerp(float left, float right, float val, bool clamped = false, LerpType type = LerpType.Linear, params float[] pars) {
        val = NewLerpValue(val, clamped, type, pars);
        return left * (1 - val) + right * val;
    }
    public static int Lerp(int left, int right, float val, bool clamped = false, LerpType type = LerpType.Linear, params float[] pars) {
        val = NewLerpValue(val, clamped, type, pars);
        return (int)(left * (1 - val) + right * val);
    }
    public static Vector2 Lerp(Vector2 left, Vector2 right, float val, bool clamped = false, LerpType type = LerpType.Linear, params float[] pars) {
        val = NewLerpValue(val, clamped, type, pars);
        return left * (1 - val) + right * val;
    }
    public static Vector3 Lerp(Vector3 left, Vector3 right, float val, bool clamped = false, LerpType type = LerpType.Linear, params float[] pars) {
        val = NewLerpValue(val, clamped, type, pars);
        return left * (1 - val) + right * val;
    }
    public static Vector4 Lerp(Vector4 left, Vector4 right, float val, bool clamped = false, LerpType type = LerpType.Linear, params float[] pars) {
        val = NewLerpValue(val, clamped, type, pars);
        return left * (1 - val) + right * val;
    }
    public static double Lerp(double left, double right, double val, bool clamped = false, LerpType type = LerpType.Linear, params double[] pars) {
        val = NewLerpValue(val, clamped, type, pars);
        return left * (1 - val) + right * val;
    }
    public static float Lerp(float left, float right, double val, bool clamped = false, LerpType type = LerpType.Linear, params double[] pars) {
        val = NewLerpValue(val, clamped, type, pars);
        return (float)(left * (1 - val) + right * val);
    }
    public static int Lerp(int left, int right, double val, bool clamped = false, LerpType type = LerpType.Linear, params double[] pars) {
        val = NewLerpValue(val, clamped, type, pars);
        return (int)(left * (1 - val) + right * val);
    }
    public static Vector2 Lerp(Vector2 left, Vector2 right, double val, bool clamped = false, LerpType type = LerpType.Linear, params double[] pars) {
        val = NewLerpValue(val, clamped, type, pars);
        return NewVector2(Lerp(left.X, right.X, val), Lerp(left.Y, right.Y, val));
    }
    public static Vector3 Lerp(Vector3 left, Vector3 right, double val, bool clamped = false, LerpType type = LerpType.Linear, params double[] pars) {
        val = NewLerpValue(val, clamped, type, pars);
        return NewVector3(Lerp(left.X, right.X, val), Lerp(left.Y, right.Y, val), Lerp(left.Z, right.Z, val));
    }
    public static Vector4 Lerp(Vector4 left, Vector4 right, double val, bool clamped = false, LerpType type = LerpType.Linear, params double[] pars) {
        val = NewLerpValue(val, clamped, type, pars);
        return NewVector4(Lerp(left.X, right.X, val), Lerp(left.Y, right.Y, val), Lerp(left.Z, right.Z, val), Lerp(left.W, right.W, val));
    }
    #endregion
}