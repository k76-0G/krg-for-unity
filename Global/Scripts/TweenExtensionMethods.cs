#if NS_DG_TWEENING
using DG.Tweening;
#endif

namespace KRG
{
    public static class TweenExtensionMethods
    {
#if NS_DG_TWEENING
        public static T SetTimeThread<T>(this T t, ITimeThread th) where T : Tween
        {
            th.AddTween(t);
            return t;
        }
#endif
    }
}