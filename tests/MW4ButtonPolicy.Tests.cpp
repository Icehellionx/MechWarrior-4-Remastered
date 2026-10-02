#define DIRECTINPUT_VERSION 0x0800
#include "../tools/compatibility/MW4ButtonPolicy.h"
#include <array>
#include <cstdio>
#include <cstdlib>

static void Check(bool value, const char* label)
{
    if (!value) { std::fprintf(stderr, "FAIL: %s\n", label); std::exit(1); }
}

int main()
{
    std::vector<mw4input::ButtonObject> available;
    for (DWORD index = 0; index < 128; ++index)
        available.push_back({ {}, static_cast<DWORD>(DIDFT_PSHBUTTON | DIDFT_MAKEINSTANCE(index)) });
    for (DWORD count : { 0u, 8u, 31u, 32u, 64u, 128u })
    {
        for (bool wildcard : { false, true })
        {
            std::vector<DIOBJECTDATAFORMAT> objects;
            objects.push_back({ nullptr, 0, DIDFT_ABSAXIS | DIDFT_MAKEINSTANCE(32), 0 });
            objects.push_back({ nullptr, 4, DIDFT_POV | DIDFT_MAKEINSTANCE(0), 0 });
            for (DWORD index = 0; index < count; ++index)
                objects.push_back({ nullptr, 8 + index, DIDFT_PSHBUTTON |
                    static_cast<DWORD>(wildcard ? DIDFT_ANYINSTANCE : DIDFT_MAKEINSTANCE(index)), 0 });
            DIDATAFORMAT format{ sizeof(DIDATAFORMAT), sizeof(DIOBJECTDATAFORMAT), DIDF_ABSAXIS,
                136, static_cast<DWORD>(objects.size()), objects.data() };
            const auto filtered = mw4input::Filter(format, available);
            const DWORD kept = count < 31 ? count : 31;
            Check(filtered.objects.size() == kept + 2, "cap and enumeration agree at 8/31/32/64/128");
            Check(filtered.objects[0].dwOfs == 0 && filtered.objects[1].dwOfs == 4, "axis and POV retained");
            Check(filtered.hiddenOffsets.size() == count - kept, "all excess button offsets recorded");
            std::array<BYTE, 138> state;
            state.fill(0x80);
            mw4input::ClearHiddenState(state.data(), 136, filtered.hiddenOffsets);
            Check(state[136] == 0x80 && state[137] == 0x80, "state buffer canaries preserved");
            for (DWORD index = 0; index < count; ++index)
                Check(state[8 + index] == (index < 31 ? 0x80 : 0), "first 31 pressed buttons preserved; excess neutral");
            Check(objects.size() == count + 2, "caller format unmodified");
        }
    }
    for (bool explicitFirst : { false, true })
    {
        std::vector<DIOBJECTDATAFORMAT> objects;
        DIOBJECTDATAFORMAT explicitObject{ nullptr, 0, DIDFT_PSHBUTTON | DIDFT_MAKEINSTANCE(0), 0 };
        if (explicitFirst) objects.push_back(explicitObject);
        for (DWORD index = 1; index <= 32; ++index)
            objects.push_back({ nullptr, index, DIDFT_PSHBUTTON | DIDFT_ANYINSTANCE | 0x80000000u, 0 });
        if (!explicitFirst) objects.push_back(explicitObject);
        DIDATAFORMAT format{ sizeof(format), sizeof(DIOBJECTDATAFORMAT), DIDF_ABSAXIS,
            36, static_cast<DWORD>(objects.size()), objects.data() };
        const auto filtered = mw4input::Filter(format, available);
        Check(filtered.objects.size() == 31, "mixed explicit/wildcard cap independent of order");
        for (const auto& object : filtered.objects)
            Check(DIDFT_GETINSTANCE(object.dwType) < 31, "wildcards resolved to permitted real instances");
        Check(filtered.hiddenOffsets == std::vector<DWORD>({31, 32}), "mixed overflow offsets recorded");
    }
    // Sparse devices and different button types must use actual matching objects,
    // never an assumed contiguous instance range or a hidden matching object.
    const GUID buttonGuid{ 1, 2, 3, {} };
    const GUID otherGuid{ 2, 2, 3, {} };
    std::vector<mw4input::ButtonObject> sparse{
        { otherGuid, DIDFT_PSHBUTTON | DIDFT_MAKEINSTANCE(0) },
        { buttonGuid, DIDFT_TGLBUTTON | DIDFT_MAKEINSTANCE(2) },
        { buttonGuid, DIDFT_PSHBUTTON | DIDFT_MAKEINSTANCE(5) },
        { buttonGuid, DIDFT_PSHBUTTON | DIDFT_MAKEINSTANCE(31) },
    };
    DIOBJECTDATAFORMAT sparseObjects[] = {
        { &buttonGuid, 0, DIDFT_PSHBUTTON | DIDFT_ANYINSTANCE | 0x80000000u, 0 },
        { &buttonGuid, 1, DIDFT_PSHBUTTON | DIDFT_ANYINSTANCE | 0x80000000u, 0 },
    };
    DIDATAFORMAT sparseFormat{ sizeof(sparseFormat), sizeof(DIOBJECTDATAFORMAT), DIDF_ABSAXIS, 4, 2, sparseObjects };
    const auto filteredSparse = mw4input::Filter(sparseFormat, sparse);
    Check(filteredSparse.objects.size() == 1 && DIDFT_GETINSTANCE(filteredSparse.objects[0].dwType) == 5,
        "wildcards respect sparse instances, GUID and button type");
    Check((filteredSparse.objects[0].dwType & 0x80000000u) != 0, "optional flag preserved");
    Check(filteredSparse.hiddenOffsets == std::vector<DWORD>({1}), "hidden-only matching object cannot leak");
    Check(!mw4input::IsHiddenButton(DIDFT_ABSAXIS | DIDFT_MAKEINSTANCE(40)), "axis instance above 31 unaffected");
    Check(!mw4input::IsHiddenButton(DIDFT_PSHBUTTON | DIDFT_MAKEINSTANCE(30)), "button 31 retained");
    Check(mw4input::IsHiddenButton(DIDFT_PSHBUTTON | DIDFT_MAKEINSTANCE(31)), "button 32 hidden");
    mw4input::ClearHiddenState(nullptr, 0, { 0, 8192 });
    std::puts("MW4 button policy tests passed.");
}
