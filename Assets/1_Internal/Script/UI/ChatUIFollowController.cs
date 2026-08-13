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
    [SerializeField] private Camera roomCamera;

    [Header("Pool")]
    [SerializeField] private List<KidChatBinding> kids = new List<KidChatBinding>();
    [SerializeField] private List<ChatSlot> chats = new List<ChatSlot>();
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
        if (assignOneChatPerKidOnStart)
        {
            AssignDefaultChats();
        }
    }

    [ContextMenu("Initialize")]
    public void Initialize()
    {
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
                Debug.LogError($"{chat.chatRoot.name} requires a visible ChatUiAnchorFollower component.", chat.chatRoot);
                return;
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
                Debug.LogError($"{chat.chatRoot.name} requires a visible ChatUiAnchorFollower component.", chat.chatRoot);
                continue;
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
