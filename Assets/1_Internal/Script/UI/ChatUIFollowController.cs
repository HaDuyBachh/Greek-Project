using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ChatUIFollowController : MonoBehaviour
{
    [Serializable]
    public class KidChatBinding
    {
        public string kidId;
        public Transform kidRoot;
        public Transform uiAnchor;
        public List<string> allowedChatIds = new List<string>();

        public bool HasAnchor => uiAnchor != null;
    }

    [Serializable]
    public class ChatSlot
    {
        public string chatId;
        public RectTransform chatRoot;
        public RectTransform talkRoot;
        public RectTransform emoteRoot;
        public RectTransform videoRoot;
        public RectTransform videoContentRoot;
        [SerializeField] private string activeUserKidId;

        public string ActiveUserKidId => activeUserKidId;
        public bool IsInUse => !string.IsNullOrWhiteSpace(activeUserKidId);

        public void SetUser(string kidId)
        {
            activeUserKidId = kidId;
        }

        public void ClearUser()
        {
            activeUserKidId = string.Empty;
        }
    }

    [Header("Projection")]
    [SerializeField] private string cameraName = "Main_room";
    [SerializeField] private Camera roomCamera;

    [Header("Pool")]
    [SerializeField] private List<KidChatBinding> kids = new List<KidChatBinding>();
    [SerializeField] private List<ChatSlot> chats = new List<ChatSlot>();
    [SerializeField] private bool autoDiscoverSceneLinks = true;
    [SerializeField] private bool assignOneChatPerKidOnStart = true;
    [SerializeField] private bool hideUnusedChats = true;

    public IReadOnlyList<KidChatBinding> Kids => kids;
    public IReadOnlyList<ChatSlot> Chats => chats;
    public Camera ProjectionCamera => roomCamera;

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        Initialize();

        if (assignOneChatPerKidOnStart)
        {
            AssignDefaultChats();
        }
    }

    [ContextMenu("Initialize")]
    public void Initialize()
    {
        if (roomCamera == null)
        {
            roomCamera = ChatUiAnchorUtility.FindCameraByName(cameraName);
        }

        if (autoDiscoverSceneLinks)
        {
            DiscoverChats();
            DiscoverKidsFromChats();
        }

        ResolveKidAnchors();
        PrepareChatSlots();
    }

    public bool RequestChat(string kidId, out ChatSlot assignedSlot)
    {
        assignedSlot = GetAssignedChat(kidId);
        if (assignedSlot != null)
        {
            return true;
        }

        KidChatBinding kid = GetKid(kidId);
        if (kid == null || !kid.HasAnchor)
        {
            return false;
        }

        foreach (ChatSlot chat in chats)
        {
            if (chat == null || chat.chatRoot == null || chat.IsInUse || !CanKidUseChat(kid, chat))
            {
                continue;
            }

            Assign(chat, kid);
            assignedSlot = chat;
            return true;
        }

        return false;
    }

    public bool RequestRandomChat(string kidId, out ChatSlot assignedSlot)
    {
        return RequestRandomChat(kidId, string.Empty, out assignedSlot);
    }

    public bool RequestRandomChat(string kidId, string excludedChatId, out ChatSlot assignedSlot)
    {
        assignedSlot = GetAssignedChat(kidId);
        if (assignedSlot != null)
        {
            return true;
        }

        KidChatBinding kid = GetKid(kidId);
        if (kid == null || !kid.HasAnchor)
        {
            return false;
        }

        List<ChatSlot> availableChats = new List<ChatSlot>();
        foreach (ChatSlot chat in chats)
        {
            if (chat != null && chat.chatRoot != null && !chat.IsInUse && CanKidUseChat(kid, chat))
            {
                availableChats.Add(chat);
            }
        }

        if (availableChats.Count == 0)
        {
            return false;
        }

        if (availableChats.Count > 1 && !string.IsNullOrWhiteSpace(excludedChatId))
        {
            availableChats.RemoveAll(chat =>
                string.Equals(chat.chatId, excludedChatId, StringComparison.OrdinalIgnoreCase));
        }

        assignedSlot = availableChats[UnityEngine.Random.Range(0, availableChats.Count)];
        Assign(assignedSlot, kid);
        return true;
    }

    public bool AssignChatToKid(string kidId, string chatId)
    {
        KidChatBinding kid = GetKid(kidId);
        ChatSlot chat = GetChat(chatId);

        if (kid == null || chat == null || !kid.HasAnchor || !CanKidUseChat(kid, chat))
        {
            return false;
        }

        if (chat.IsInUse && !string.Equals(chat.ActiveUserKidId, kidId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        ReleaseChatUsedByKid(kidId);
        Assign(chat, kid);
        return true;
    }

    public void ReleaseChatUsedByKid(string kidId)
    {
        foreach (ChatSlot chat in chats)
        {
            if (chat != null && string.Equals(chat.ActiveUserKidId, kidId, StringComparison.OrdinalIgnoreCase))
            {
                Release(chat);
            }
        }
    }

    public void ReleaseChat(string chatId)
    {
        ChatSlot chat = GetChat(chatId);
        if (chat != null)
        {
            Release(chat);
        }
    }

    public void SetProjectionCamera(Camera projectionCamera)
    {
        if (projectionCamera == null || roomCamera == projectionCamera)
        {
            return;
        }

        roomCamera = projectionCamera;

        foreach (ChatSlot chat in chats)
        {
            if (chat == null || chat.chatRoot == null)
            {
                continue;
            }

            ChatUiAnchorFollower follower = chat.chatRoot.GetComponent<ChatUiAnchorFollower>();
            if (follower != null)
            {
                follower.WorldCamera = roomCamera;
            }
        }
    }

    public bool IsChatInUse(string chatId)
    {
        ChatSlot chat = GetChat(chatId);
        return chat != null && chat.IsInUse;
    }

    public string GetChatUser(string chatId)
    {
        ChatSlot chat = GetChat(chatId);
        return chat != null ? chat.ActiveUserKidId : string.Empty;
    }

    [ContextMenu("Assign Default Chats")]
    public void AssignDefaultChats()
    {
        foreach (KidChatBinding kid in kids)
        {
            if (kid == null || string.IsNullOrWhiteSpace(kid.kidId))
            {
                continue;
            }

            RequestChat(kid.kidId, out _);
        }
    }

    [ContextMenu("Release All Chats")]
    public void ReleaseAllChats()
    {
        foreach (ChatSlot chat in chats)
        {
            if (chat != null)
            {
                Release(chat);
            }
        }
    }

    private void Assign(ChatSlot chat, KidChatBinding kid)
    {
        chat.SetUser(kid.kidId);

        if (chat.chatRoot != null)
        {
            chat.chatRoot.gameObject.SetActive(true);

            ChatUiAnchorFollower follower = chat.chatRoot.GetComponent<ChatUiAnchorFollower>();
            if (follower == null)
            {
                follower = chat.chatRoot.gameObject.AddComponent<ChatUiAnchorFollower>();
            }

            follower.WorldCamera = roomCamera;
            follower.WorldAnchor = kid.uiAnchor;
            follower.CanvasRoot = ChatUiAnchorUtility.FindCanvasRoot(chat.chatRoot);
        }
    }

    private void Release(ChatSlot chat)
    {
        chat.ClearUser();

        if (chat.chatRoot == null)
        {
            return;
        }

        ChatUiAnchorFollower follower = chat.chatRoot.GetComponent<ChatUiAnchorFollower>();
        if (follower != null)
        {
            follower.WorldAnchor = null;
        }

        if (hideUnusedChats)
        {
            chat.chatRoot.gameObject.SetActive(false);
        }
    }

    private KidChatBinding GetKid(string kidId)
    {
        foreach (KidChatBinding kid in kids)
        {
            if (kid != null && string.Equals(kid.kidId, kidId, StringComparison.OrdinalIgnoreCase))
            {
                return kid;
            }
        }

        return null;
    }

    private ChatSlot GetChat(string chatId)
    {
        foreach (ChatSlot chat in chats)
        {
            if (chat != null && string.Equals(chat.chatId, chatId, StringComparison.OrdinalIgnoreCase))
            {
                return chat;
            }
        }

        return null;
    }

    private ChatSlot GetAssignedChat(string kidId)
    {
        foreach (ChatSlot chat in chats)
        {
            if (chat != null && string.Equals(chat.ActiveUserKidId, kidId, StringComparison.OrdinalIgnoreCase))
            {
                return chat;
            }
        }

        return null;
    }

    private bool CanKidUseChat(KidChatBinding kid, ChatSlot chat)
    {
        if (kid.allowedChatIds == null || kid.allowedChatIds.Count == 0)
        {
            return true;
        }

        foreach (string allowedChatId in kid.allowedChatIds)
        {
            if (string.Equals(allowedChatId, chat.chatId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void DiscoverChats()
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject sceneObject in objects)
        {
            if (sceneObject == null || !sceneObject.scene.IsValid() || !sceneObject.scene.isLoaded)
            {
                continue;
            }

            RectTransform rectTransform = sceneObject.transform as RectTransform;
            if (rectTransform == null || !sceneObject.name.StartsWith("Chat_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (GetChat(sceneObject.name) != null)
            {
                continue;
            }

            chats.Add(new ChatSlot
            {
                chatId = sceneObject.name,
                chatRoot = rectTransform
            });
        }
    }

    private void DiscoverKidsFromChats()
    {
        foreach (ChatSlot chat in chats)
        {
            if (chat == null || string.IsNullOrWhiteSpace(chat.chatId) || !chat.chatId.StartsWith("Chat_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string kidId = chat.chatId.Substring("Chat_".Length);
            KidChatBinding kid = GetKid(kidId);
            if (kid == null)
            {
                kid = new KidChatBinding
                {
                    kidId = kidId
                };

                kids.Add(kid);
            }

            if (!kid.allowedChatIds.Contains(chat.chatId))
            {
                kid.allowedChatIds.Add(chat.chatId);
            }
        }
    }

    private void ResolveKidAnchors()
    {
        foreach (KidChatBinding kid in kids)
        {
            if (kid == null || string.IsNullOrWhiteSpace(kid.kidId))
            {
                continue;
            }

            if (kid.kidRoot == null)
            {
                GameObject kidObject = ChatUiAnchorUtility.FindLoadedSceneObject(kid.kidId);
                if (kidObject != null)
                {
                    kid.kidRoot = kidObject.transform;
                }
            }

            if (kid.uiAnchor == null && kid.kidRoot != null)
            {
                kid.uiAnchor = ChatUiAnchorUtility.FindAnchorForChild(kid.kidId);
            }
        }
    }

    private void PrepareChatSlots()
    {
        foreach (ChatSlot chat in chats)
        {
            if (chat == null || chat.chatRoot == null)
            {
                continue;
            }

            ChatUiAnchorFollower follower = chat.chatRoot.GetComponent<ChatUiAnchorFollower>();
            if (follower == null)
            {
                follower = chat.chatRoot.gameObject.AddComponent<ChatUiAnchorFollower>();
            }

            follower.WorldCamera = roomCamera;
            follower.CanvasRoot = ChatUiAnchorUtility.FindCanvasRoot(chat.chatRoot);
            ResolveChatInsertRoots(chat);

            if (hideUnusedChats && !chat.IsInUse)
            {
                chat.chatRoot.gameObject.SetActive(false);
            }
        }
    }

    private void ResolveChatInsertRoots(ChatSlot chat)
    {
        if (chat == null || chat.chatRoot == null)
        {
            return;
        }

        if (chat.talkRoot == null)
        {
            chat.talkRoot = FindDirectChildRect(chat.chatRoot, "Talk");
        }

        if (chat.emoteRoot == null)
        {
            chat.emoteRoot = FindDirectChildRect(chat.chatRoot, "Emote");
        }

        if (chat.videoRoot == null)
        {
            chat.videoRoot = FindDirectChildRect(chat.chatRoot, "Video");
        }

        if (chat.videoContentRoot == null && chat.videoRoot != null)
        {
            chat.videoContentRoot = FindDirectChildRect(chat.videoRoot, "Vid");
        }
    }

    private RectTransform FindDirectChildRect(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
            {
                return child as RectTransform;
            }
        }

        return null;
    }
}
