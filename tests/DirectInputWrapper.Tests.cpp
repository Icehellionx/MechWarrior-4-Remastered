// Tests the actual patched upstream wrapper against a project-owned COM fake.
#include "dinputto8.h"
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <thread>

static void Check(bool value, const char* label)
{ if (!value) { std::fprintf(stderr, "FAIL: %s\n", label); std::exit(1); } }

struct Device final : IDirectInputDevice8W, IDirectInputDevice8A
{
    ULONG refs = 1;
    DWORD buttons = 128;
    DWORD type = DI8DEVTYPE_JOYSTICK;
    bool rejectFormat = false;
    bool rejectEnumeration = false;
    std::vector<DIOBJECTDATAFORMAT> format;
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** out) override
    {
        if (!out) return E_POINTER;
        *out = iid == IID_IDirectInputDevice8A ? static_cast<void*>(static_cast<IDirectInputDevice8A*>(this)) :
            static_cast<void*>(static_cast<IDirectInputDevice8W*>(this));
        AddRef(); return S_OK;
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return ++refs; }
    ULONG STDMETHODCALLTYPE Release() override { return --refs; }
    HRESULT STDMETHODCALLTYPE GetCapabilities(DIDEVCAPS* caps) override
    { caps->dwDevType = type; caps->dwButtons = buttons; caps->dwAxes = 4; caps->dwPOVs = 1; return S_OK; }
    template<class Info> HRESULT InfoFor(Info* info)
    { info->dwDevType = type; info->wUsagePage = 1; info->wUsage = 4; return S_OK; }
    HRESULT STDMETHODCALLTYPE GetDeviceInfo(DIDEVICEINSTANCEW* info) override { return InfoFor(info); }
    HRESULT STDMETHODCALLTYPE GetDeviceInfo(DIDEVICEINSTANCEA* info) override { return InfoFor(info); }
    template<class Info, class Callback> HRESULT Enumerate(Callback callback, void* context, DWORD flags)
    {
        if (rejectEnumeration) return DIERR_GENERIC;
        if (flags == DIDFT_ALL || (flags & DIDFT_AXIS))
        { Info object{}; object.dwSize = sizeof(object); object.dwType = DIDFT_ABSAXIS; callback(&object, context); }
        if (flags == DIDFT_ALL || (flags & DIDFT_BUTTON))
            for (DWORD index = 0; index < buttons; ++index)
            {
                Info object{}; object.dwSize = sizeof(object); object.guidType = GUID_Button; object.dwType = DIDFT_PSHBUTTON | DIDFT_MAKEINSTANCE(index);
                object.dwOfs = DIJOFS_BUTTON(index);
                if (callback(&object, context) == DIENUM_STOP) break;
            }
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE EnumObjects(LPDIENUMDEVICEOBJECTSCALLBACKW callback, void* context, DWORD flags) override
    { return Enumerate<DIDEVICEOBJECTINSTANCEW>(callback, context, flags); }
    HRESULT STDMETHODCALLTYPE EnumObjects(LPDIENUMDEVICEOBJECTSCALLBACKA callback, void* context, DWORD flags) override
    { return Enumerate<DIDEVICEOBJECTINSTANCEA>(callback, context, flags); }
    HRESULT STDMETHODCALLTYPE SetDataFormat(const DIDATAFORMAT* data) override
    {
        if (rejectFormat) return DIERR_INVALIDPARAM;
        format.assign(data->rgodf, data->rgodf + data->dwNumObjs); return S_OK;
    }
    HRESULT STDMETHODCALLTYPE GetDeviceState(DWORD size, void* state) override
    { std::memset(state, 0x80, size); return S_OK; }
    HRESULT STDMETHODCALLTYPE GetDeviceData(DWORD size, DIDEVICEOBJECTDATA* events, DWORD* count, DWORD) override
    {
        if (!count) return DIERR_INVALIDPARAM;
        const DWORD requested = *count; *count = 0;
        if (!events) return S_OK;
        for (const auto& object : format)
        {
            if (!(DIDFT_GETTYPE(object.dwType) & DIDFT_BUTTON) || *count >= requested) continue;
            DIDEVICEOBJECTDATA event{}; event.dwOfs = object.dwOfs; event.dwData = 0x80;
            std::memcpy(reinterpret_cast<BYTE*>(events) + *count * size, &event, size);
            ++*count;
        }
        return S_OK;
    }
    template<class Info> HRESULT Object(Info* info, DWORD id)
    { info->dwType = id; info->dwOfs = DIJOFS_BUTTON(DIDFT_GETINSTANCE(id)); return S_OK; }
    HRESULT STDMETHODCALLTYPE GetObjectInfo(DIDEVICEOBJECTINSTANCEW* info, DWORD id, DWORD) override { return Object(info, id); }
    HRESULT STDMETHODCALLTYPE GetObjectInfo(DIDEVICEOBJECTINSTANCEA* info, DWORD id, DWORD) override { return Object(info, id); }
#define STUB(name, ...) HRESULT STDMETHODCALLTYPE name(__VA_ARGS__) override { return E_NOTIMPL; }
    STUB(GetProperty, REFGUID, LPDIPROPHEADER)
    STUB(SetProperty, REFGUID, LPCDIPROPHEADER)
    STUB(Acquire)
    STUB(Unacquire)
    STUB(SetEventNotification, HANDLE)
    STUB(SetCooperativeLevel, HWND, DWORD)
    STUB(RunControlPanel, HWND, DWORD)
    STUB(Initialize, HINSTANCE, DWORD, REFGUID)
    STUB(CreateEffect, REFGUID, LPCDIEFFECT, LPDIRECTINPUTEFFECT*, LPUNKNOWN)
    STUB(EnumEffects, LPDIENUMEFFECTSCALLBACKW, LPVOID, DWORD)
    STUB(EnumEffects, LPDIENUMEFFECTSCALLBACKA, LPVOID, DWORD)
    STUB(GetEffectInfo, LPDIEFFECTINFOW, REFGUID)
    STUB(GetEffectInfo, LPDIEFFECTINFOA, REFGUID)
    STUB(GetForceFeedbackState, LPDWORD)
    STUB(SendForceFeedbackCommand, DWORD)
    STUB(EnumCreatedEffectObjects, LPDIENUMCREATEDEFFECTOBJECTSCALLBACK, LPVOID, DWORD)
    STUB(Escape, LPDIEFFESCAPE)
    STUB(Poll)
    STUB(SendDeviceData, DWORD, LPCDIDEVICEOBJECTDATA, LPDWORD, DWORD)
    STUB(EnumEffectsInFile, LPCWSTR, LPDIENUMEFFECTSINFILECALLBACK, LPVOID, DWORD)
    STUB(EnumEffectsInFile, LPCSTR, LPDIENUMEFFECTSINFILECALLBACK, LPVOID, DWORD)
    STUB(WriteEffectToFile, LPCWSTR, DWORD, LPDIFILEEFFECT, DWORD)
    STUB(WriteEffectToFile, LPCSTR, DWORD, LPDIFILEEFFECT, DWORD)
    STUB(BuildActionMap, LPDIACTIONFORMATW, LPCWSTR, DWORD)
    STUB(BuildActionMap, LPDIACTIONFORMATA, LPCSTR, DWORD)
    STUB(SetActionMap, LPDIACTIONFORMATW, LPCWSTR, DWORD)
    STUB(SetActionMap, LPDIACTIONFORMATA, LPCSTR, DWORD)
    STUB(GetImageInfo, LPDIDEVICEIMAGEINFOHEADERW)
    STUB(GetImageInfo, LPDIDEVICEIMAGEINFOHEADERA)
#undef STUB
};

template<class Info> static BOOL Count(const Info* info, void* context)
{
    if (DIDFT_GETTYPE(info->dwType) & DIDFT_BUTTON)
    { Check(DIDFT_GETINSTANCE(info->dwType) < 31, "excess enumeration hidden"); ++*static_cast<DWORD*>(context); }
    return DIENUM_CONTINUE;
}
static BOOL CALLBACK CountA(const DIDEVICEOBJECTINSTANCEA* info, void* context) { return Count(info, context); }
static BOOL CALLBACK CountW(const DIDEVICEOBJECTINSTANCEW* info, void* context) { return Count(info, context); }

int main()
{
    // The reproducible-build helper refactor must retain upstream effect behavior.
    DIEFFECT effect{}; LPCDIEFFECT missing = nullptr;
    Check(!FixLegacyEffect(GUID_Spring, missing, effect), "null effect unchanged");
    DWORD axes[] = { DIJOFS_Y, DIJOFS_X }; LONG direction[] = { 0, 0 };
    DIEFFECT original{}; original.dwSize = sizeof(original); original.cAxes = 1;
    original.rgdwAxes = axes; original.rglDirection = direction;
    LPCDIEFFECT source = &original;
    Check(FixLegacyEffect(GUID_ConstantForce, source, effect), "legacy Y axis remapped");
    Check(source == &effect && effect.cAxes == 1 && *effect.rgdwAxes == DIJOFS_X &&
        original.rgdwAxes == axes && axes[0] == DIJOFS_Y, "effect copied without changing caller storage");
    for (const GUID* guid : { &GUID_Spring, &GUID_Damper, &GUID_Friction, &GUID_Inertia, &GUID_ConstantForce })
    {
        original.cAxes = 2; original.dwFlags = DIEFF_POLAR | DIEFF_SPHERICAL;
        original.cbTypeSpecificParams = 2 * sizeof(DICONDITION); source = &original;
        Check(FixLegacyEffect(*guid, source, effect), "two axis effect normalized");
        Check(effect.cAxes == 1 && effect.dwFlags == DIEFF_CARTESIAN && original.cAxes == 2,
            "legacy flags and caller storage preserved");
        Check(effect.cbTypeSpecificParams == (*guid == GUID_ConstantForce ? 2 : 1) * sizeof(DICONDITION),
            "condition payload size contract preserved");
    }
    for (DWORD count : { 8u, 31u, 32u, 64u, 128u })
    {
        Device fake; fake.buttons = count;
        auto wrapper = new m_IDirectInputDeviceX(static_cast<IDirectInputDevice8W*>(&fake));
        wrapper->SetVersion(0x0700);
        DIDEVCAPS caps{ sizeof(caps) };
        Check(SUCCEEDED(wrapper->GetCapabilities(&caps)) && caps.dwButtons == min(count, 31u), "DX7 capability cap");
        DWORD ansi = 0, unicode = 0;
        Check(SUCCEEDED(wrapper->EnumObjects(CountA, &ansi, DIDFT_BUTTON)), "ANSI enumeration");
        Check(SUCCEEDED(wrapper->EnumObjects(CountW, &unicode, DIDFT_BUTTON)), "Unicode enumeration");
        Check(ansi == caps.dwButtons && unicode == caps.dwButtons, "enumeration and caps agree");
        DIDEVICEOBJECTINSTANCEW hidden{ sizeof(hidden) };
        Check(wrapper->GetObjectInfo(&hidden, DIDFT_PSHBUTTON | DIDFT_MAKEINSTANCE(31), DIPH_BYID) == DIERR_OBJECTNOTFOUND, "hidden object query");
        for (const auto format : { &c_dfDIJoystick, &c_dfDIJoystick2 })
        {
            Check(SUCCEEDED(wrapper->SetDataFormat(format)), "format accepted");
            DWORD deliveredButtons = 0;
            for (const auto& object : fake.format) if (DIDFT_GETTYPE(object.dwType) & DIDFT_BUTTON) ++deliveredButtons;
            Check(deliveredButtons == min(count, 31u), "proxy receives only permitted existing button objects");
            std::vector<BYTE> state(format->dwDataSize + 8, 0xCC);
            Check(SUCCEEDED(wrapper->GetDeviceState(format->dwDataSize, state.data())), "polling state");
            Check(state[DIJOFS_BUTTON(30)] == (count >= 31 ? 0x80 : 0) && state[DIJOFS_BUTTON(31)] == 0, "existing button 31 retained and 32 neutral");
            Check(state[format->dwDataSize] == 0xCC, "state write contained");
            for (DWORD flags : std::vector<DWORD>{ 0, DIGDD_PEEK })
            {
                DIDEVICEOBJECTDATA events[128]{}; DWORD available = 128;
                Check(SUCCEEDED(wrapper->GetDeviceData(sizeof(events[0]), events, &available, flags)), "buffer and peek");
                Check(available == min(count, 31u), "no excess buffered events");
                const DWORD legacyStride = 16; // DIDEVICEOBJECTDATA_DX3 on x86
                BYTE legacy[legacyStride + 8]; std::memset(legacy, 0xCC, sizeof(legacy));
                DWORD one = 1;
                Check(SUCCEEDED(wrapper->GetDeviceData(legacyStride, reinterpret_cast<DIDEVICEOBJECTDATA*>(legacy), &one, flags)), "legacy buffered record");
                Check(one == 1 && legacy[legacyStride] == 0xCC && legacy[legacyStride + 3] == 0xCC, "legacy peek cannot overwrite caller buffer");
            }
            fake.rejectFormat = true;
            Check(FAILED(wrapper->SetDataFormat(&c_dfDIJoystick)), "format failure propagated");
            fake.rejectFormat = false;
            Check(SUCCEEDED(wrapper->GetDeviceState(format->dwDataSize, state.data())) && state[DIJOFS_BUTTON(31)] == 0, "failed format preserves mask");
        }
        std::vector<DIOBJECTDATAFORMAT> mixedObjects;
        mixedObjects.push_back({ &GUID_Button, 0, DIDFT_PSHBUTTON | DIDFT_MAKEINSTANCE(0), 0 });
        for (DWORD index = 1; index <= 32; ++index)
            mixedObjects.push_back({ &GUID_Button, index, DIDFT_PSHBUTTON | DIDFT_ANYINSTANCE | 0x80000000u, 0 });
        DIDATAFORMAT mixed{ sizeof(mixed), sizeof(DIOBJECTDATAFORMAT), DIDF_ABSAXIS, 36,
            static_cast<DWORD>(mixedObjects.size()), mixedObjects.data() };
        Check(SUCCEEDED(wrapper->SetDataFormat(&mixed)), "actual wrapper mixed format accepted");
        Check(fake.format.size() == min(count, 31u), "mixed format contains only allowed real objects");
        for (const auto& object : fake.format)
            Check(DIDFT_GETINSTANCE(object.dwType) < 31, "mixed wildcards cannot reach hidden instance");
        BYTE mixedState[40]; std::memset(mixedState, 0xCC, sizeof(mixedState));
        Check(SUCCEEDED(wrapper->GetDeviceState(36, mixedState)) && mixedState[31] == 0 && mixedState[32] == 0 && mixedState[36] == 0xCC,
            "mixed format state masked and bounded");
        fake.rejectEnumeration = true;
        Check(FAILED(wrapper->SetDataFormat(&c_dfDIJoystick2)), "inventory failure propagated without changing format");
        fake.rejectEnumeration = false;
        Check(SUCCEEDED(wrapper->GetDeviceState(36, mixedState)) && mixedState[31] == 0,
            "inventory failure preserves hidden state mask");
        Check(SUCCEEDED(wrapper->SetDataFormat(&c_dfDIJoystick2)), "restore standard format before concurrent test");
        std::thread polling([&] {
            for (int iteration = 0; iteration < 500; ++iteration)
            {
                BYTE state[sizeof(DIJOYSTATE2)]{};
                Check(SUCCEEDED(wrapper->GetDeviceState(sizeof(state), state)), "concurrent polling");
                DIDEVICEOBJECTDATA events[128]{}; DWORD available = 128;
                Check(SUCCEEDED(wrapper->GetDeviceData(sizeof(events[0]), events, &available, DIGDD_PEEK)), "concurrent buffered peek");
                Check(available <= 31, "concurrent peek retains cap");
            }
        });
        for (int iteration = 0; iteration < 500; ++iteration)
            Check(SUCCEEDED(wrapper->SetDataFormat(iteration % 2 ? &c_dfDIJoystick : &c_dfDIJoystick2)), "concurrent format changes");
        polling.join();
        wrapper->Release(); Check(fake.refs == 0, "proxy lifetime balanced");
    }
    for (DWORD type : { static_cast<DWORD>(DI8DEVTYPE_KEYBOARD), static_cast<DWORD>(DI8DEVTYPE_MOUSE) })
    {
        Device fake; fake.type = type;
        auto wrapper = new m_IDirectInputDeviceX(static_cast<IDirectInputDevice8W*>(&fake)); wrapper->SetVersion(0x0700);
        DIDEVCAPS caps{ sizeof(caps) }; Check(SUCCEEDED(wrapper->GetCapabilities(&caps)) && caps.dwButtons == 128, "keyboard and mouse not capped");
        wrapper->Release(); Check(fake.refs == 0, "ordinary device lifetime balanced");
    }
    std::puts("Actual DirectInput wrapper tests passed.");
}
