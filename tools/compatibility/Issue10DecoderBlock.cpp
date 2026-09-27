// Experimental registration-free COM rejection for issue #10.
// This DLL must only be named in an exact-game application manifest. It does
// not register itself or modify the Windows decoder registration.
#include <windows.h>
#include <unknwn.h>

namespace {
const CLSID kAffectedDecoder = {0x5261169d, 0x9b6c, 0x435f,
    {0xb1, 0xd5, 0xf7, 0x9b, 0xaf, 0x70, 0x0c, 0x71}};

}

extern "C" HRESULT __stdcall Issue10GetClassObject(
    REFCLSID clsid, REFIID, void** result)
{
    if (result == nullptr) return E_POINTER;
    *result = nullptr;
    if (!IsEqualCLSID(clsid, kAffectedDecoder)) return CLASS_E_CLASSNOTAVAILABLE;
    return CLASS_E_CLASSNOTAVAILABLE;
}

extern "C" HRESULT __stdcall Issue10CanUnloadNow()
{
    return S_OK;
}
