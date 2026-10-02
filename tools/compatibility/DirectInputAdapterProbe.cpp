// Project-owned x86 probe for the real adapter/DirectInput boundary in an isolated VM.
#define DIRECTINPUT_VERSION 0x0700
#define DIDFT_OPTIONAL 0x80000000
#define INITGUID
#include <windows.h>
#include <dinput.h>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <array>
#include <vector>

static void Check(bool value, const char* message)
{
    if (!value) { std::fprintf(stderr, "FAIL: %s\n", message); std::exit(1); }
}
static void Ok(HRESULT result, const char* message) { Check(SUCCEEDED(result), message); }
struct Controller { GUID guid{}; bool found = false; };
static BOOL CALLBACK FindA(const DIDEVICEINSTANCEA* info, void* context)
{
    if (std::strstr(info->tszInstanceName, "vJoy"))
    { auto& found = *static_cast<Controller*>(context); found.guid = info->guidInstance; found.found = true; return DIENUM_STOP; }
    return DIENUM_CONTINUE;
}
static BOOL CALLBACK FindW(const DIDEVICEINSTANCEW* info, void* context)
{
    if (std::wcsstr(info->tszInstanceName, L"vJoy"))
    { auto& found = *static_cast<Controller*>(context); found.guid = info->guidInstance; found.found = true; return DIENUM_STOP; }
    return DIENUM_CONTINUE;
}
struct Objects { DWORD buttons = 0; DWORD axes = 0; DWORD hats = 0; DWORD maxButton = 0; };
template<class T> static BOOL Count(const T* info, void* context)
{
    auto& count = *static_cast<Objects*>(context);
    if (DIDFT_GETTYPE(info->dwType) & DIDFT_BUTTON)
    { ++count.buttons; count.maxButton = max(count.maxButton, DIDFT_GETINSTANCE(info->dwType)); }
    if (DIDFT_GETTYPE(info->dwType) & DIDFT_AXIS) ++count.axes;
    if (DIDFT_GETTYPE(info->dwType) & DIDFT_POV) ++count.hats;
    return DIENUM_CONTINUE;
}
static BOOL CALLBACK CountA(const DIDEVICEOBJECTINSTANCEA* info, void* context) { return Count(info, context); }
static BOOL CALLBACK CountW(const DIDEVICEOBJECTINSTANCEW* info, void* context) { return Count(info, context); }

using CreateEx = HRESULT (WINAPI*)(HINSTANCE, DWORD, REFIID, void**, IUnknown*);
template<class Input, class Device, class Info, class Find, class Enum>
static void Run(CreateEx create, REFIID inputIid, REFIID deviceIid, Find find, Enum enumerate,
    HWND window, DWORD physicalButtons, DWORD seconds)
{
    Input* input = nullptr;
    Ok(create(GetModuleHandleW(nullptr), 0x0700, inputIid, reinterpret_cast<void**>(&input), nullptr), "create DirectInput 7");
    Controller controller;
    Ok(input->EnumDevices(DIDEVTYPE_JOYSTICK, find, &controller, DIEDFL_ATTACHEDONLY), "enumerate controller");
    Check(controller.found, "vJoy must actually be visible to the test process");
    Device* device = nullptr;
    Ok(input->CreateDeviceEx(controller.guid, deviceIid, reinterpret_cast<void**>(&device), nullptr), "create joystick");
    DIDEVCAPS caps{ sizeof(caps) };
    Ok(device->GetCapabilities(&caps), "capabilities");
    const DWORD expected = min(physicalButtons, 31u);
    Check(caps.dwButtons == expected, "reported button cap");
    Objects objects;
    Ok(device->EnumObjects(enumerate, &objects, DIDFT_ALL), "objects before format");
    Check(objects.buttons == expected && objects.maxButton < 31, "object enumeration cap");
    Check(objects.axes == caps.dwAxes && objects.hats == caps.dwPOVs, "axes and hats unchanged");
    std::printf("PHASE: %s inventory caps=%lu axes=%lu hats=%lu\n",
        sizeof(Info) == sizeof(DIDEVICEOBJECTINSTANCEA) ? "ANSI" : "Unicode", caps.dwButtons, caps.dwAxes, caps.dwPOVs);
    Info hidden{ sizeof(hidden) };
    Check(FAILED(device->GetObjectInfo(&hidden, DIDFT_PSHBUTTON | DIDFT_MAKEINSTANCE(31), DIPH_BYID)), "button 32 object hidden");
    Ok(device->SetCooperativeLevel(window, DISCL_BACKGROUND | DISCL_NONEXCLUSIVE), "cooperative level");
    DIPROPDWORD buffer{};
    buffer.diph.dwSize = sizeof(buffer); buffer.diph.dwHeaderSize = sizeof(buffer.diph);
    buffer.diph.dwHow = DIPH_DEVICE; buffer.dwData = 128;
    Ok(device->SetProperty(DIPROP_BUFFERSIZE, &buffer.diph), "buffer size");
    bool button1 = false, button31 = false;
    for (const DIDATAFORMAT* format : { &c_dfDIJoystick, &c_dfDIJoystick2 })
    {
        Ok(device->SetDataFormat(format), "standard joystick format");
        Objects after;
        Ok(device->EnumObjects(enumerate, &after, DIDFT_BUTTON), "objects after format");
        Check(after.buttons == expected && after.maxButton < 31, "format cannot expose button 32");
        Ok(device->Acquire(), "acquire");
        std::printf("PHASE: polling format bytes=%lu for %lu seconds\n", format->dwDataSize, seconds);
        const auto start = GetTickCount64();
        bool firstPoll = true;
        do
        {
            device->Poll();
            if (firstPoll) std::printf("PHASE: poll returned\n");
            std::array<BYTE, sizeof(DIJOYSTATE2) + 16> state;
            state.fill(0xCC);
            Ok(device->GetDeviceState(format->dwDataSize, state.data()), "immediate state");
            if (firstPoll) std::printf("PHASE: immediate state returned\n");
            for (DWORD offset = format->dwDataSize; offset < state.size(); ++offset)
                Check(state[offset] == 0xCC, "state canary");
            auto joystick = reinterpret_cast<const DIJOYSTATE*>(state.data());
            button1 |= (joystick->rgbButtons[0] & 0x80) != 0;
            button31 |= (joystick->rgbButtons[30] & 0x80) != 0;
            for (DWORD index = 31; index < (format == &c_dfDIJoystick ? 32u : 128u); ++index)
                Check(state[DIJOFS_BUTTON(index)] == 0, "excess immediate buttons neutral");
            for (DWORD flags : std::array<DWORD, 2>{ DIGDD_PEEK, 0 })
            {
                DIDEVICEOBJECTDATA events[128]{}; DWORD count = 128;
                Ok(device->GetDeviceData(sizeof(DIDEVICEOBJECTDATA), events, &count, flags), "buffered data and peek");
                Check(count <= 128, "event count bounded");
                for (DWORD index = 0; index < count; ++index)
                    Check(events[index].dwOfs < DIJOFS_BUTTON(31) || events[index].dwOfs >= DIJOFS_BUTTON(128), "excess buffered buttons absent");
            }
            Sleep(20);
            if (firstPoll) std::printf("PHASE: buffered calls returned; tick elapsed=%llu ms\n", GetTickCount64() - start);
            firstPoll = false;
        } while (GetTickCount64() - start < seconds * 1000ull);
        Ok(device->Unacquire(), "unacquire");
        std::printf("PHASE: format polling completed\n");
    }
    // Explicit instances and caller-defined offsets must follow the same policy.
    DIOBJECTDATAFORMAT customObjects[] = {
        { &GUID_XAxis, 0, DIDFT_ABSAXIS | DIDFT_ANYINSTANCE, 0 },
        { &GUID_Button, 4, DIDFT_PSHBUTTON | DIDFT_MAKEINSTANCE(0) | DIDFT_OPTIONAL, 0 },
        { &GUID_Button, 5, DIDFT_PSHBUTTON | DIDFT_MAKEINSTANCE(30) | DIDFT_OPTIONAL, 0 },
        { &GUID_Button, 6, DIDFT_PSHBUTTON | DIDFT_MAKEINSTANCE(31) | DIDFT_OPTIONAL, 0 },
        { &GUID_Button, 7, DIDFT_PSHBUTTON | DIDFT_MAKEINSTANCE(63) | DIDFT_OPTIONAL, 0 },
    };
    DIDATAFORMAT custom{ sizeof(custom), sizeof(DIOBJECTDATAFORMAT), DIDF_ABSAXIS, 8, 5, customObjects };
    Ok(device->SetDataFormat(&custom), "custom format");
    Ok(device->Acquire(), "custom acquire");
    device->Poll(); BYTE state[10]; std::memset(state, 0xCC, sizeof(state));
    Ok(device->GetDeviceState(8, state), "custom state");
    Check(state[6] == 0 && state[7] == 0 && state[8] == 0xCC && state[9] == 0xCC, "custom offsets hidden and bounded");
    Ok(device->Unacquire(), "custom unacquire");
    // A mixed format must not let wildcard assignment consume button 32 after
    // an explicit button has reserved one of the first 31 instances.
    std::vector<DIOBJECTDATAFORMAT> mixedObjects;
    mixedObjects.push_back({ &GUID_Button, 0, DIDFT_PSHBUTTON | DIDFT_MAKEINSTANCE(0) | DIDFT_OPTIONAL, 0 });
    for (DWORD index = 1; index <= 32; ++index)
        mixedObjects.push_back({ &GUID_Button, index, DIDFT_PSHBUTTON | DIDFT_ANYINSTANCE | DIDFT_OPTIONAL, 0 });
    DIDATAFORMAT mixed{ sizeof(mixed), sizeof(DIOBJECTDATAFORMAT), DIDF_ABSAXIS, 36,
        static_cast<DWORD>(mixedObjects.size()), mixedObjects.data() };
    Ok(device->SetDataFormat(&mixed), "mixed explicit/wildcard format");
    Ok(device->Acquire(), "mixed acquire");
    std::printf("PHASE: mixed explicit/wildcard format polling\n");
    const auto mixedStart = GetTickCount64();
    do
    {
        device->Poll();
        BYTE mixedState[40]; std::memset(mixedState, 0xCC, sizeof(mixedState));
        Ok(device->GetDeviceState(36, mixedState), "mixed state");
        Check(mixedState[31] == 0 && mixedState[32] == 0, "mixed format excess buttons neutral");
        for (DWORD index = 36; index < sizeof(mixedState); ++index)
            Check(mixedState[index] == 0xCC, "mixed state canary");
        for (DWORD flags : std::array<DWORD, 2>{ DIGDD_PEEK, 0 })
        {
            DIDEVICEOBJECTDATA events[128]{}; DWORD count = 128;
            Ok(device->GetDeviceData(sizeof(events[0]), events, &count, flags), "mixed buffer and peek");
            Check(count <= 128, "mixed event count bounded");
            for (DWORD index = 0; index < count; ++index)
                Check(events[index].dwOfs < 31, "mixed excess buffered buttons absent");
        }
        Sleep(20);
    } while (GetTickCount64() - mixedStart < seconds * 1000ull);
    Ok(device->Unacquire(), "mixed unacquire");
    device->Release();
    for (const GUID* guid : { &GUID_SysKeyboard, &GUID_SysMouse })
    {
        Device* ordinary = nullptr;
        Ok(input->CreateDeviceEx(*guid, deviceIid, reinterpret_cast<void**>(&ordinary), nullptr), "keyboard/mouse create");
        DIDEVCAPS ordinaryCaps{ sizeof(ordinaryCaps) };
        Ok(ordinary->GetCapabilities(&ordinaryCaps), "keyboard/mouse capabilities");
        if (*guid == GUID_SysKeyboard) Check(ordinaryCaps.dwButtons >= 128, "keyboard is not capped");
        ordinary->Release();
    }
    input->Release();
    std::printf("PASS: caps=%lu axes=%lu hats=%lu button1=%d button31=%d\n", caps.dwButtons, caps.dwAxes, caps.dwPOVs, button1, button31);
}

int main(int argc, char** argv)
{
    std::setvbuf(stdout, nullptr, _IONBF, 0);
    Check(argc == 4, "usage: probe <adapter.dll> <physical button count> <poll seconds per format>");
    HMODULE adapter = LoadLibraryA(argv[1]); Check(adapter != nullptr, "load adapter");
    auto create = reinterpret_cast<CreateEx>(GetProcAddress(adapter, "DirectInputCreateEx")); Check(create != nullptr, "create export");
    HWND window = CreateWindowExW(0, L"STATIC", L"MW4 adapter probe", WS_OVERLAPPED, 0, 0, 1, 1, nullptr, nullptr, GetModuleHandleW(nullptr), nullptr);
    Check(window != nullptr, "private probe window");
    Run<IDirectInput7A, IDirectInputDevice7A, DIDEVICEOBJECTINSTANCEA>(create, IID_IDirectInput7A, IID_IDirectInputDevice7A, FindA, CountA, window, std::strtoul(argv[2], nullptr, 10), std::strtoul(argv[3], nullptr, 10));
    Run<IDirectInput7W, IDirectInputDevice7W, DIDEVICEOBJECTINSTANCEW>(create, IID_IDirectInput7W, IID_IDirectInputDevice7W, FindW, CountW, window, std::strtoul(argv[2], nullptr, 10), std::strtoul(argv[3], nullptr, 10));
    DestroyWindow(window); FreeLibrary(adapter);
}
