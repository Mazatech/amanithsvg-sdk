/****************************************************************************
** Copyright (C) 2013-2018 Mazatech S.r.l. All rights reserved.
**
** This file is part of AmanithSVG software, an SVG rendering engine.
**
** W3C (World Wide Web Consortium) and SVG are trademarks of the W3C.
** OpenGL is a registered trademark and OpenGL ES is a trademark of
** Silicon Graphics, Inc.
**
** This file is provided AS IS with NO WARRANTY OF ANY KIND, INCLUDING THE
** WARRANTY OF DESIGN, MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE.
**
** For any information, please contact info@mazatech.com
**
****************************************************************************/
#import "UnityAppController.h"

#if defined(__CYGWIN32__)
	#define UNITY_INTERFACE_API __stdcall
	#define UNITY_INTERFACE_EXPORT __declspec(dllexport)
#elif defined(WIN32) || defined(_WIN32) || defined(__WIN32__) || defined(_WIN64) || defined(WINAPI_FAMILY)
	#define UNITY_INTERFACE_API __stdcall
	#define UNITY_INTERFACE_EXPORT __declspec(dllexport)
#elif defined(__MACH__) || defined(__ANDROID__) || defined(__linux__) || defined(__QNX__)
	#define UNITY_INTERFACE_API
	#define UNITY_INTERFACE_EXPORT
#else
	#define UNITY_INTERFACE_API
	#define UNITY_INTERFACE_EXPORT
#endif

#if defined(__cplusplus)
extern "C" {
#endif

void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(struct IUnityInterfaces* interfaces);
void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload(void);

#ifdef __cplusplus 
} /* extern "C" */
#endif


@interface MyAppController : UnityAppController {
}
- (void)shouldAttachRenderDelegate;
@end

@implementation MyAppController
- (void)shouldAttachRenderDelegate;
{
	UnityRegisterRenderingPluginV5(&UnityPluginLoad, &UnityPluginUnload);
}
@end
IMPL_APP_CONTROLLER_SUBCLASS(MyAppController);
