using UnityEngine;

namespace PlanetoidGen.Client.BusinessLogic.Managers
{
    /// <summary>
    /// A singleton <seealso cref="MonoBehaviour"/> with predefined
    /// Awake, OnDestroy, OnApplicationQuit.
    /// </summary>
    /// <typeparam name="T">Manager instance type.</typeparam>
    public abstract class SingletonManager<T> : MonoBehaviour
        where T : SingletonManager<T>
    {
        private static T _instance = null;

        private bool _alive = true;
        private bool _initialized = false;

        public static T Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }
                else
                {
                    //Find T
                    var managers = FindObjectsOfType<T>();
                    if (managers != null)
                    {
                        if (managers.Length == 1)
                        {
                            _instance = managers[0];
                            _instance.SelfInitialization();
                            return _instance;
                        }
                        else
                        {
                            if (managers.Length > 1)
                            {
                                Debug.LogError($"Have more that one {typeof(T).Name} in scene. " +
                                                "But this is Singleton! Check project.");
                                for (int i = 0; i < managers.Length; ++i)
                                {
                                    var manager = managers[i];
                                    Destroy(manager.gameObject);
                                }
                            }
                        }
                    }
                    // Create
                    var go = new GameObject(typeof(T).Name, typeof(T));
                    _instance = go.GetComponent<T>();
                    _instance.SelfInitialization();
                    return _instance;
                }
            }

            //Can be initialized externally
            set
            {
                _instance = value;
            }
        }

        /// <summary>
        /// Check flag if need work from OnDestroy or OnApplicationExit
        /// </summary>
        public static bool IsAlive
        {
            get
            {
                return _instance != null && _instance._alive;
            }
        }

        protected void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                SelfInitialization();
            }
            else if (_instance != this as T)
            {
                Debug.LogError($"Have more that one {typeof(T).Name} in scene. " +
                                "But this is Singleton! Check project.");
                DestroyImmediate(gameObject);
            }

            OnAwakeInvoked();
        }

        protected void OnDestroy() { Deinitialization(); _alive = false; }

        protected void OnApplicationQuit() { Deinitialization(); _alive = false; }

        private void SelfInitialization()
        {
            if (_initialized)
            {
                return;
            }

            DontDestroyOnLoad(gameObject);
            Initialization();

            _initialized = true;
        }

        protected virtual void OnAwakeInvoked() { }

        protected virtual void Initialization() { }

        protected virtual void Deinitialization() { }
    }
}
