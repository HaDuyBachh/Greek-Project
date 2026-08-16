using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class KidDeviceUsageController : MonoBehaviour
{
    public enum DeviceUsageType
    {
        PhoneOnly,
        TelevisionOnly,
        PhoneAndTelevision
    }

    public enum DeviceActivity
    {
        None,
        Phone,
        Television
    }

    [Header("Kid Type")]
    [SerializeField] private DeviceUsageType usageType = DeviceUsageType.PhoneAndTelevision;
    [SerializeField] private DeviceActivity firstDualChairActivity = DeviceActivity.Television;

    [Header("Scene References")]
    [SerializeField] private KidWaypointAnimationTester activityController;
    [SerializeField, Tooltip("Prebuilt child object tagged phone_handle.")]
    private GameObject phoneHandle;

    [Header("Device Animations")]
    [SerializeField] private string groundIdleAnimation = "SitGround";
    [SerializeField] private string groundPhoneAnimation = "SitGroundUsingPhone";
    [SerializeField] private string chairPhoneAnimation = "SitChairUsingPhone";
    [SerializeField] private string chairTelevisionAnimation = "SitChairIdle";

    [Header("Phone Handle")]
    [SerializeField] private bool showPhoneHandleOnlyDuringPhoneAnimation = true;

    private DeviceActivity currentActivity;
    private DeviceActivity nextDualChairActivity;

    public DeviceUsageType UsageType => usageType;
    public DeviceActivity CurrentActivity => currentActivity;
    public bool CanUsePhone => usageType != DeviceUsageType.TelevisionOnly;
    public bool CanUseTelevision => usageType != DeviceUsageType.PhoneOnly;
    public bool IsWatchingPhone => CanUsePhone && !activityController.IsTravelling &&
                                   IsAnimation(activityController.CurrentAnimationState,
                                       groundPhoneAnimation, chairPhoneAnimation);
    public bool IsWatchingTelevision => CanUseTelevision &&
                                        currentActivity == DeviceActivity.Television &&
                                        !activityController.IsTravelling &&
                                        activityController.CurrentChairSeat != null;
    public DeviceActivity NextChairActivity => ResolveNextChairActivity();

    private void Awake()
    {
        ValidatePrebuiltReferences();
        nextDualChairActivity = firstDualChairActivity == DeviceActivity.Phone
            ? DeviceActivity.Phone
            : DeviceActivity.Television;
        SetPhoneHandleVisible(false);
    }

    private void LateUpdate()
    {
        bool shouldShow = showPhoneHandleOnlyDuringPhoneAnimation
            ? IsWatchingPhone
            : currentActivity == DeviceActivity.Phone && CanUsePhone;
        SetPhoneHandleVisible(shouldShow);
    }

    private void OnDisable()
    {
        SetPhoneHandleVisible(false);
    }

    public void BeginGroundActivity()
    {
        currentActivity = CanUsePhone ? DeviceActivity.Phone : DeviceActivity.None;
    }

    public void BeginChairActivity()
    {
        currentActivity = ResolveNextChairActivity();
        if (usageType == DeviceUsageType.PhoneAndTelevision)
        {
            nextDualChairActivity = currentActivity == DeviceActivity.Phone
                ? DeviceActivity.Television
                : DeviceActivity.Phone;
        }
    }

    private DeviceActivity ResolveNextChairActivity()
    {
        switch (usageType)
        {
            case DeviceUsageType.PhoneOnly:
                return DeviceActivity.Phone;
            case DeviceUsageType.TelevisionOnly:
                return DeviceActivity.Television;
            default:
                return nextDualChairActivity;
        }
    }

    public void EndDeviceActivity()
    {
        currentActivity = DeviceActivity.None;
        SetPhoneHandleVisible(false);
    }

    public string ResolveNeutralGroundAnimation()
    {
        return currentActivity == DeviceActivity.Phone && CanUsePhone
            ? groundPhoneAnimation
            : groundIdleAnimation;
    }

    public string ResolveNeutralChairAnimation()
    {
        if (currentActivity == DeviceActivity.Phone && CanUsePhone)
        {
            return chairPhoneAnimation;
        }

        return chairTelevisionAnimation;
    }

    public bool ShouldOpenPhoneOnEnter()
    {
        return ResolveEnterActivity() == DeviceActivity.Phone;
    }

    public DeviceActivity ResolveEnterActivity()
    {
        if (IsWatchingPhone)
        {
            return DeviceActivity.Phone;
        }

        if (IsWatchingTelevision)
        {
            return DeviceActivity.Television;
        }

        if (usageType == DeviceUsageType.PhoneOnly)
        {
            return DeviceActivity.Phone;
        }

        return usageType == DeviceUsageType.TelevisionOnly
            ? DeviceActivity.Television
            : DeviceActivity.None;
    }

    private void SetPhoneHandleVisible(bool visible)
    {
        if (phoneHandle != null && phoneHandle.activeSelf != visible)
        {
            phoneHandle.SetActive(visible);
        }
    }

    private static bool IsAnimation(string currentAnimation, params string[] candidates)
    {
        if (string.IsNullOrWhiteSpace(currentAnimation) || candidates == null)
        {
            return false;
        }

        foreach (string candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) &&
                string.Equals(currentAnimation, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void ValidatePrebuiltReferences()
    {
        if (activityController == null || (CanUsePhone && phoneHandle == null))
        {
            Debug.LogError("Kid Device Usage requires its activity controller, plus a prebuilt phone_handle for Phone-capable Kids, assigned before Play.", this);
        }
    }
}
