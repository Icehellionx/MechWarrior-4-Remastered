// Project-owned policy for the locally modified, game-local dinputto8 build.
// Button instances are zero-based: physical buttons 1..31 are instances 0..30.
#pragma once
#include <dinput.h>
#include <array>
#include <vector>

namespace mw4input
{
    constexpr DWORD MaxButtons = 31;

    inline bool IsHiddenButton(DWORD type)
    {
        return (DIDFT_GETTYPE(type) & DIDFT_BUTTON) != 0 &&
            (type & DIDFT_ANYINSTANCE) != DIDFT_ANYINSTANCE &&
            DIDFT_GETINSTANCE(type) >= MaxButtons;
    }

    struct FilteredFormat
    {
        std::vector<DIOBJECTDATAFORMAT> objects;
        std::vector<DWORD> hiddenOffsets;
    };

    struct ButtonObject
    {
        GUID guid;
        DWORD type;
    };

    inline FilteredFormat Filter(const DIDATAFORMAT& format, const std::vector<ButtonObject>& available)
    {
        FilteredFormat result;
        std::array<bool, MaxButtons> used{};
        // DirectInput reserves explicit instances before assigning wildcard
        // objects. Resolve wildcards ourselves from the permitted real objects:
        // merely counting wildcard entries can still expose physical button 32.
        for (DWORD index = 0; index < format.dwNumObjs; ++index)
        {
            const auto type = format.rgodf[index].dwType;
            if ((DIDFT_GETTYPE(type) & DIDFT_BUTTON) != 0 &&
                (type & DIDFT_ANYINSTANCE) != DIDFT_ANYINSTANCE && !IsHiddenButton(type))
                used[DIDFT_GETINSTANCE(type)] = true;
        }
        for (DWORD index = 0; index < format.dwNumObjs; ++index)
        {
            auto object = format.rgodf[index];
            const bool button = (DIDFT_GETTYPE(object.dwType) & DIDFT_BUTTON) != 0;
            const bool wildcard = (object.dwType & DIDFT_ANYINSTANCE) == DIDFT_ANYINSTANCE;
            bool hidden = button && IsHiddenButton(object.dwType);
            if (button && wildcard)
            {
                hidden = true;
                for (const auto& candidate : available)
                {
                    const auto instance = DIDFT_GETINSTANCE(candidate.type);
                    if (instance >= MaxButtons || used[instance] ||
                        (DIDFT_GETTYPE(object.dwType) & DIDFT_GETTYPE(candidate.type) & DIDFT_BUTTON) == 0 ||
                        (object.pguid && *object.pguid != candidate.guid)) continue;
                    object.dwType = (object.dwType & ~DIDFT_ANYINSTANCE) | DIDFT_MAKEINSTANCE(instance);
                    used[instance] = true;
                    hidden = false;
                    break;
                }
            }
            if (hidden)
            {
                if (object.dwOfs < format.dwDataSize) result.hiddenOffsets.push_back(object.dwOfs);
            }
            else result.objects.push_back(object);
        }
        return result;
    }

    inline void ClearHiddenState(void* state, DWORD size, const std::vector<DWORD>& offsets)
    {
        if (!state) return;
        auto bytes = static_cast<BYTE*>(state);
        for (auto offset : offsets) if (offset < size) bytes[offset] = 0;
    }
}
