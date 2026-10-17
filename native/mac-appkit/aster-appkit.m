// SPDX-License-Identifier: GPL-3.0-or-later
// Aster Terminal native AppKit shutdown bridge, 2026.
#import <AppKit/AppKit.h>
#import <dispatch/dispatch.h>
#import <QuartzCore/CAMetalLayer.h>

static BOOL armed = NO;

static void useSrgb(CALayer *layer, CGColorSpaceRef space)
{
    if ([layer isKindOfClass:[CAMetalLayer class]])
        ((CAMetalLayer *)layer).colorspace = space;
    for (CALayer *child in layer.sublayers) useSrgb(child, space);
}

// Avalonia's untagged GPU surface otherwise uses the monitor's device RGB.
// Tag the backing layer, letting Core Animation adapt to the current display.
__attribute__((visibility("default"))) void aster_appkit_srgb_window(void *handle)
{
    if (![NSThread isMainThread] || handle == NULL) return;
    NSWindow *window = (__bridge NSWindow *)handle;
    window.colorSpace = [NSColorSpace sRGBColorSpace];
    CGColorSpaceRef space = CGColorSpaceCreateWithName(kCGColorSpaceSRGB);
    useSrgb(window.contentView.layer, space);
    CGColorSpaceRelease(space);
}

static void wakeExit(NSApplication *application, unsigned attempts)
{
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, 20 * NSEC_PER_MSEC), dispatch_get_main_queue(), ^{
        if (!armed) return;
        // stop: only sets a flag checked after dispatching a real NSEvent. A
        // stop posted inside an observer / nested close callback can be consumed
        // before the outer run loop resumes. Reissue it on a fresh main-queue
        // turn, followed by a process-local application event (never HID input).
        [application stop:nil];
        NSEvent *event = [NSEvent otherEventWithType:NSEventTypeApplicationDefined
                                          location:NSZeroPoint modifierFlags:0
                                         timestamp:0 windowNumber:0 context:nil
                                           subtype:0 data1:0 data2:0];
        [application postEvent:event atStart:NO];
        if (attempts > 1) wakeExit(application, attempts - 1);
    });
}

__attribute__((visibility("default"))) void aster_appkit_begin_exit(void)
{
    if (![NSThread isMainThread] || NSApp == nil || armed) return;
    armed = YES;
    wakeExit(NSApp, 3);
}

__attribute__((visibility("default"))) void aster_appkit_end_exit(void)
{
    armed = NO;
}
