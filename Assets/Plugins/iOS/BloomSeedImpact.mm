#import <UIKit/UIKit.h>

extern "C" void BloomSeedImpact()
{
    dispatch_async(dispatch_get_main_queue(), ^{
#ifdef __IPHONE_17_5
        if (@available(iOS 17.5, *))
        {
            for (UIScene *scene in UIApplication.sharedApplication.connectedScenes)
            {
                if (scene.activationState != UISceneActivationStateForegroundActive ||
                    ![scene isKindOfClass:UIWindowScene.class])
                    continue;

                UIWindow *window = ((UIWindowScene *)scene).keyWindow;
                UIView *view = window.rootViewController.view;
                if (view != nil)
                {
                    [[UIImpactFeedbackGenerator feedbackGeneratorWithStyle:UIImpactFeedbackStyleMedium
                                                                   forView:view] impactOccurred];
                    return;
                }
            }
        }
#endif
        if (@available(iOS 10.0, *))
        {
            static UIImpactFeedbackGenerator *legacyImpact;
            if (legacyImpact == nil)
                legacyImpact = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
            [legacyImpact impactOccurred];
        }
    });
}
