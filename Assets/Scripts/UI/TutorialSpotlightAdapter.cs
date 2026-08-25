using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CandyShop
{
    // Optional integration with the free "Tutorial Spotlight" asset (Den4ik, asset 363804).
    // The package downloads via Package Manager -> My Assets, so it may or may not be present.
    // We resolve TutorialSpotlightManager by reflection at runtime:
    //   - package imported -> spotlight overlay with a click-through hole on the target
    //   - package missing  -> Available == false, tutorial cards simply show without the overlay
    public static class TutorialSpotlightAdapter
    {
        private static bool _resolved;
        private static Type _managerType;
        private static MethodInfo _setTarget;
        private static MethodInfo _show;
        private static MethodInfo _hide;
        private static bool _warned;

        public static bool Available
        {
            get { Resolve(); return _show != null && _hide != null; }
        }

        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = null;
                try { t = assembly.GetType("TutorialSpotlightManager"); } catch { }
                if (t == null)
                {
                    try
                    {
                        var types = assembly.GetTypes();
                        foreach (var candidate in types)
                            if (candidate.Name == "TutorialSpotlightManager") { t = candidate; break; }
                    }
                    catch { }
                }
                if (t == null) continue;

                _managerType = t;
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (m.Name != "ShowSpotlight" && m.Name != "HideSpotlight" && m.Name != "SetSpotlightTarget")
                        continue;
                    if (m.Name == "ShowSpotlight" && _show == null) _show = m;
                    else if (m.Name == "HideSpotlight" && _hide == null) _hide = m;
                    else if (m.Name == "SetSpotlightTarget" && _setTarget == null) _setTarget = m;
                }
                break;
            }

            if (_managerType == null && !_warned)
            {
                _warned = true;
                Debug.Log("TutorialSpotlightManager not found — tutorial runs without spotlight overlay.");
            }
        }

        private static object ManagerInstance()
        {
            Resolve();
            if (_managerType == null) return null;

            if (typeof(MonoBehaviour).IsAssignableFrom(_managerType))
            {
                var found = UnityEngine.Object.FindObjectOfType(_managerType);
                if (found != null) return found;
                var host = new GameObject("TutorialSpotlightManager");
                return host.AddComponent(_managerType);
            }
            return null; // static methods will be invoked directly
        }

        // Show the spotlight hole around a UI rect. Returns false when the package is absent.
        public static bool Show(RectTransform target)
        {
            if (!Available || target == null) return false;
            try
            {
                var instance = ManagerInstance();
                if (_setTarget != null)
                    _setTarget.Invoke(instance, BuildArgs(_setTarget, target));
                _show.Invoke(instance, BuildArgs(_show, target));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("TutorialSpotlight invoke failed: " + ex.Message);
                return false;
            }
        }

        public static void Hide()
        {
            if (!Available) return;
            try
            {
                _hide.Invoke(ManagerInstance(), new object[0]);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("TutorialSpotlight hide failed: " + ex.Message);
            }
        }

        // Match method signature shape: 0 params, or 1st param accepting the rect.
        private static object[] BuildArgs(MethodInfo method, RectTransform target)
        {
            var ps = method.GetParameters();
            if (ps.Length == 0) return new object[0];
            if (ps.Length == 1)
            {
                if (ps[0].ParameterType == typeof(RectTransform)) return new object[] { target };
                if (ps[0].ParameterType == typeof(GameObject)) return new object[] { target.gameObject };
                if (typeof(Component).IsAssignableFrom(ps[0].ParameterType)) return new object[] { target };
                if (ps[0].ParameterType == typeof(Vector4)) return new object[] { WorldRect(target) };
                if (ps[0].ParameterType == typeof(Rect)) return new object[] { WorldRect(target) };
            }
            // Unknown signature: pass nulls and let the package defaults apply.
            var args = new object[ps.Length];
            if (args.Length > 0) args[0] = target;
            return args;
        }

        private static Rect WorldRect(RectTransform rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
        }
    }
}
